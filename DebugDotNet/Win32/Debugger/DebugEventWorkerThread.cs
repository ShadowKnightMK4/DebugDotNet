using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;


using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Tools;
using DebugDotNet.Win32.Internal;

namespace DebugDotNet.Win32.Debugger
{

    /// <summary>
    /// syncronized access to a DEBUG_EVENT query.
    /// </summary>
    public class DebugEventCapture
    {
        private readonly Queue<DebugEvent> Event= new Queue<DebugEvent>();
        
        /// <summary>
        /// When this is locked and true we are currently accessing the private query herer
        /// </summary>
        private object SyncAcessObject = false;
        /// <summary>
        /// add a new event
        /// </summary>
        /// <param name="EventData">the event to add</param>
        public void PushEvent(DebugEvent EventData)
        {
            lock (SyncAcessObject)
            {
                SyncAcessObject = true;
                Event.Enqueue(EventData);
                SyncAcessObject = false;
            }
        }

        /// <summary>
        /// return the next event without removing it
        /// </summary>
        /// <returns>the next debug event in the list</returns>
        public DebugEvent PeekEvent()
        {
            lock (SyncAcessObject)
            {
                return Event.Peek();
            }
        }

        /// <summary>
        /// removes the least recently pushed event from the que
        /// </summary>
        /// <returns></returns>
        public DebugEvent PullEvent()
        {
            DebugEvent ret;
            lock (SyncAcessObject)
            {
                SyncAcessObject = true;
                ret = Event.Dequeue();
                SyncAcessObject = false;
            }
            return ret;
        }

        /// <summary>
        /// lock this and return event count
        /// </summary>
        /// <returns></returns>
        public int EventCount()
        {
            lock (SyncAcessObject)
            {
                return Event.Count;
            }
        }
/// <summary>
///     Event the DEBUG_EVENT quere
/// </summary>
        public void ClearEvents()
        {
            lock (SyncAcessObject)
            {
                Event.Clear();
            }
        }


    }




    /// <summary>
    /// Implement a WaitForDebugEvent loop, and Continue Debug Event Loop
    /// </summary>
    public class DebugEventWorkerThread : IDisposable
    {
        #region IDisposble and isDisposed
        /// <summary>
        /// Returns if object was disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Cleanup the unmanged part
        /// </summary>
        ~DebugEventWorkerThread()
        {
            Dispose(false);
        }
        /// <summary>
        /// Dispose() unmanged resources with the ooption to toss the manged ones also
        /// </summary>
        /// <param name="Managed">True for managed also.</param>
        protected virtual void Dispose(bool Managed)
        {
            if (!IsDisposed)
            {
                if (Managed)
                {

                }


                DebugHandle.ForEach(
                    p =>
                    {
                        if (KillDebugProcessExit)
                        {
                            if (p.HasExited == false)
                            {
                                p.Kill();
                            }
                        }
                        else
                        {
                            NativeMethods.DebugActiveProcessStop(p.Id);
                        }
                    }
                    );





                if (InternalThread != null)
                {
                    if ( (InternalThread.IsCompleted == true) || (InternalThread.IsFaulted == true) || (InternalThread.IsCanceled) == true)
                    {
                        InternalThread?.Dispose();
                    }
                    else
                    {
                        
                    }
                }
                DebugHandle?.ForEach(p => { p?.Dispose(); });
                IsDisposed = true;
            }
        }

        /// <summary>
        /// dispose of this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


        /// <summary>
        /// If true and (your) debugger quits, this kills the process being debugged. Default is true. See the Msdn Win32 Routine DebugSetProcessKillOnExit
        /// </summary>
        public bool KillDebugedProcessOnExit
        {
            get
            {
                return KillDebugProcessExit;
            }
            set
            {
                NativeMethods.DebugSetProcessKillOnExit(value);
                KillDebugProcessExit = value;
            }
        }

        /// <summary>
        /// Private variable for <see cref="KillDebugedProcessOnExit"/>. This is also used for Thread sync if needed
        /// </summary>
        private bool KillDebugProcessExit;



        /// <summary>
        /// If false we launch a seperate task, create the target (if needed) and monitor for events on that teask
        /// </summary>
        public bool SingleThreadMode
        {
            get
            {
                return SingleThread_int;
            }
            set
            {
                if (AlreadyRunning)
                {
                    throw new InvalidOperationException(StringMessages.DebugEventWorkerThreadSetSingleThreadModeAfteStart);
                }
                SingleThread_int = value;
            }
        }

        #region Debuging State
        private bool SingleThread_int;

        /// <summary>
        /// true if start already called
        /// </summary>
        private bool AlreadyRunning;
        /// <summary>
        /// the internal Task if SingleThreadMode is false
        /// </summary>
        readonly Task InternalThread;
        /// <summary>
        /// the internal handle the Task and this class uses
        /// </summary>
        readonly List<Process> DebugHandle;
        /// <summary>
        /// a shared var; this is locked on assignment via stop.
        /// </summary>
        bool QuitThread = false;
        private object SyncAccess;
        #endregion

        #region Debug Event capture
        /// <summary>
        /// the debug event catcher that we push events to as we see them
        /// </summary>
        public DebugEventCapture Events { get; set; } = new DebugEventCapture();
        #endregion


        /// <summary>
        /// For <see cref="DebuggerCreationSetting.CreateWithDebug"/> only. This specifies a list of dlls to force the target to load at start. Requires the Detour Helper dll
        /// </summary>
        public List<string> ForceLoadDll { get;  } = new List<string>();

        /// <summary>
        /// the creation setting passed at start <see cref="DebugProcess"/> for more information
        /// </summary>
        /// 
        private readonly DebuggerCreationSetting Setting;

        /// <summary>
        /// if the process we debug is a console app, this allocates a new use for the app's use
        /// </summary>
        public bool ForceNewConsole { get; set; } = true;

        /// <summary>
        /// If true we add processes spawned to our internal list and remove them from our internal list
        /// </summary>
        public bool TrackChildProcess { get; set; }

        /// <summary>
        /// Associate this class instance with the target based on <see cref="Setting"></see>
        /// </summary>
        void AttachDebugtarget()
        {
            if (DebugHandle.Count == 0)
            {
                throw new InvalidOperationException(StringMessages.DebugEventWorkerThreadEmptyInternalList);
            }
            if ( (Setting != DebuggerCreationSetting.CreateWithDebug ) && 
                 (ForceLoadDll.Count != 0))
            {
                throw new InvalidOperationException("ForceLoadDll should contain no elements unless Setting= DebuggerCreationSetting.CreateWithDebug");
            }
            switch (Setting)
            {
                case DebuggerCreationSetting.AttachRunningProgram:
                    {
                        if (Win32DebugApi.DebugActiveProcess(DebugHandle[0].Id) == false)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        break;
                    }
                case DebuggerCreationSetting.RunProgramThenAttach:
                    {
                        DebugHandle[0].Start();
                        if (Win32DebugApi.DebugActiveProcess(DebugHandle[0].Id) == false)
                        {
                            // there was an erroy attaching. Kill spawned process and throw error
                            DebugHandle[0].Kill();
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        break;
                    }
                case DebuggerCreationSetting.CreateWithDebug:
                    {
                        using (DebugProcess tmp = new DebugProcess())
                        {
                            tmp.StartInfo.FileName = DebugHandle[0].StartInfo.FileName;
                            tmp.StartInfo.Arguments = DebugHandle[0].StartInfo.Arguments;

                            ForceLoadDll.ForEach(p => { tmp.ForceLoadDlls.Add(p); });
                            if (TrackChildProcess)
                            {
                                tmp.DebugSetting = CreateFlags.DebugProcessAndChild;
                            }
                            else
                            {
                                tmp.DebugSetting = CreateFlags.DebugOnlyThisProcess;
                            }
                            if (ForceNewConsole)
                            {
                                tmp.DebugSetting |= CreateFlags.ForceNewConsole;
                            }
                            tmp.StartInfo.UseShellExecute = false;
                            tmp.Start();
                            DebugHandle[0] = Process.GetProcessById(tmp.Id);
                        }
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException(Enum.GetName(typeof(DebuggerCreationSetting), Setting) + " is not supported");
                    }
            }
        }

        /// <summary>
        /// The internal (seperate) thread just pumps messages and puts them in the query. There is no other processing
        /// 
        /// </summary>
        void InternalThreadCallback()
        {
            DebugEventMessagePump(true, p => { return; });
        }
        /// <summary>
        /// The message pump
        /// </summary>
        /// <param name="AutoContinue">If true we just sign off with DBG_REPLY later and keep collecting messages</param>
        /// <param name="Idle">This is called in the message pump between <see cref="Win32DebugApi.WaitForDebugEvent(ref DebugEvent, uint)"/> and <see cref="Win32DebugApi.ContinueDebugEvent(int, int, ContinueStatus)"/></param>
        void DebugEventMessagePump(bool AutoContinue, Action<DebugEvent> Idle)
        {
            AttachDebugtarget();


            DebugEvent Event = new DebugEvent();
            while ( (QuitThread == false) && (InternalThread.IsCompleted == false) && (InternalThread.IsFaulted == false))
            {
                
                if (Win32DebugApi.WaitForDebugEvent(ref Event,  Win32DebugApi.Infinite) == true)
                {
                    Events.PushEvent(Event);
                    if (TrackChildProcess)
                    {
                        if (Event.dwDebugEventCode == DebugEventType.CreateProcessDebugEvent)
                        {
                            if (DebugHandle[0].Id != Event.dwProcessId)
                            {
                                DebugHandle.Add(Process.GetProcessById(Event.dwProcessId));
                            }
                        }
                        else
                        {
                            if (Event.dwDebugEventCode == DebugEventType.ExitProcessDebugEvent)
                            {
                                DebugHandle.RemoveAll(p =>
                               {
                                   return (p.Id == Event.dwProcessId);
                               });
                            }
                        }

                        
                    }

                    if (DebugHandle.Count == 0)
                    {
                        // nothing to debug. End loop
                        QuitThread = true;
                    }
                    if (AutoContinue)
                    {
                        ContinueStatus Response;
                        if (Event.dwDebugEventCode == DebugEventType.ExceptionDebugEvent)
                        {
                            Response = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                        }
                        else
                        {
                            Response = ContinueStatus.DBG_CONTINUE;
                        }
                        if (Win32DebugApi.ContinueDebugEvent(Event.dwProcessId, Event.dwThreadId, Response) == false)
                        {
                            throw new Win32Exception("(ContinueDebugEvent) " + Marshal.GetLastWin32Error());
                        }
                    }
                    Idle?.Invoke(Event);
                }
                else
                {
                    if (Marshal.GetLastWin32Error() != 0) 
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
            }
            return;

        }


        /// <summary>
        /// Constructed that sets up the worker thread and associates The Specific Process with this instanced of DebugEventWorkerThread
        /// </summary>
        /// <param name="TargetProcess">Process to watch events for</param>
        /// <param name="Setting">How to relate the process to this class instance</param>
        public DebugEventWorkerThread(Process TargetProcess, DebuggerCreationSetting Setting)
        {
            
            if (TargetProcess == null)
            {
                throw new ArgumentNullException(nameof(TargetProcess));
            }
            InternalThread = new Task(InternalThreadCallback);
            this.Setting = Setting;
            // I want my own copy
            DebugHandle = new List<Process>();
            DebugHandle.Add( new Process
            {
                StartInfo = TargetProcess.StartInfo
            });


        }



        
        /// <summary>
        /// Start debugging
        /// </summary>
        /// <param name="Idle">Single Thread Only. This is called during the message pump to allow the caller to do things.</param>
        public void Start(Action<DebugEvent> Idle)
        {
            if (AlreadyRunning == false)
            {
                if (SingleThread_int == false)
                {
                    InternalThread.Start();
                }
                else
                {
                    DebugEventMessagePump(false, Idle);
                }
                AlreadyRunning = true;
            }
            else
            {
                throw new InvalidOperationException(StringMessages.DebugEventWorkerThreadAlreadyRunning);
            }
            
        }

        /// <summary>
        /// was there an error in the worker thread?
        /// </summary>
        public bool IsFaulted
            {
                get
                {
                    return InternalThread.IsFaulted;
                }
            }

        /// <summary>
        /// was the worker thread finished? (The worker thread never quits so this should be false always)
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return InternalThread.IsCompleted;
            }
        }


        
        /// <summary>
        /// set a private varible to tell the the underyling workher thread to quit.
        /// </summary>
        public void Stop()
        {
            lock (SyncAccess)
            {
                if (InternalThread.Status == TaskStatus.Running)
                {
                    QuitThread = true;
                }
            }
        }
    }


}
