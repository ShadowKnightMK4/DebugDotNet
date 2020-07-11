using System;
using System.Collections.Generic;
using System.Text;
using DebugEventDotNet.Root;
using System.Threading;
using System.Diagnostics;
using DebugEventDotNet;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;


using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;

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
        private object OpenDoorLock = false;
        /// <summary>
        /// add a new event
        /// </summary>
        /// <param name="EventData">the event to add</param>
        public void PushEvent(DebugEvent EventData)
        {
            lock (OpenDoorLock)
            {
                OpenDoorLock = true;
                Event.Enqueue(EventData);
                OpenDoorLock = false;
            }
        }

        /// <summary>
        /// return the next event without removing it
        /// </summary>
        /// <returns>the next debug event in the list</returns>
        public DebugEvent PeekEvent()
        {
            lock (OpenDoorLock)
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
            lock (OpenDoorLock)
            {
                OpenDoorLock = true;
                ret = Event.Dequeue();
                OpenDoorLock = false;
            }
            return ret;
        }

        /// <summary>
        /// lock this and return event count
        /// </summary>
        /// <returns></returns>
        public int EventCount()
        {
            lock (OpenDoorLock)
            {
                return Event.Count;
            }
        }
/// <summary>
///     Event the DEBUG_EVENT quere
/// </summary>
        public void ClearEvents()
        {
            lock (OpenDoorLock)
            {
                Event.Clear();
            }
        }


    }


    

    /// <summary>
    /// Implements Debug Event Watching on a seperate thread.
    /// </summary>
    public class DebugEventWorkerThread: IDisposable
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


                if (!KillDebugProcessExit)
                {
                    NativeMethods.DebugActiveProcessStop(DebugHandle.Id);
                }
                else
                {
                    NativeMethods.DebugActiveProcessStop(DebugHandle.Id);
                    DebugHandle.Kill();
                }
                
                


                InternalThread?.Dispose();
                DebugHandle?.Dispose();
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
                    throw new InvalidOperationException("Can't set this after Start() has been called");
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
        /// the internal Task if SingleThreadMode is calse
        /// </summary>
        readonly Task InternalThread;
        /// <summary>
        /// the internal handle the Task and this class uses
        /// </summary>
        readonly Process DebugHandle;
        /// <summary>
        /// a shared var; this is locked on assignment via stop.
        /// </summary>
        bool QuitThread = false;
        #endregion

        #region Debug Event capture
        /// <summary>
        /// the debug event catcher that we push events to as we see them
        /// </summary>
        public DebugEventCapture Events { get; set; } = new DebugEventCapture();
        #endregion


        /// <summary>
        /// the creation setting passed at start
        /// </summary>
        /// 
        private readonly DebuggerCreationSettings Setting;


        /// <summary>
        /// Associate this class instance with the target based on <see cref="Setting"></see>
        /// </summary>
        void AttachDebugtarget()
        {
            switch (Setting)
            {
                case DebuggerCreationSettings.AttachRunningProgram:
                    {
                        if (Win32DebugApi.DebugActiveProcess(DebugHandle.Id) == false)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        break;
                    }
                case DebuggerCreationSettings.RunProgramThenAttach:
                    {
                        DebugHandle.Start();
                        if (Win32DebugApi.DebugActiveProcess(DebugHandle.Id) == false)
                        {
                            // there was an erroy attaching. Kill spawned process and throw error
                            DebugHandle.Kill();
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        break;
                    }
                case DebuggerCreationSettings.CreateWithDebug:
                    {
                        using (DebugProcess tmp = new DebugProcess())
                        {
                            tmp.StartInfo.FileName = DebugHandle.StartInfo.FileName;
                            tmp.StartInfo.Arguments = DebugHandle.StartInfo.Arguments;
                            tmp.DebugSetting = DebugProcess.CreateFlags.DEBUG_PROCESS;
                            tmp.Start();
                        }
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException(Enum.GetName(typeof(DebuggerCreationSettings), Setting) + " is not supported");
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
        /// <param name="Idle">This is called in the message pump</param>
        void DebugEventMessagePump(bool AutoContinue, Action<DebugEvent> Idle)
        {
            AttachDebugtarget();


            DebugEvent Event = new DebugEvent();
            while ( (QuitThread == false) && (InternalThread.IsCompleted == false) && (InternalThread.IsFaulted == false))
            {
                
                if (Win32DebugApi.WaitForDebugEvent(ref Event,  Win32DebugApi.Infinite) == true)
                {
                    Events.PushEvent(Event);
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
        public DebugEventWorkerThread(Process TargetProcess, DebuggerCreationSettings Setting)
        {
            
            if (TargetProcess == null)
            {
                throw new ArgumentNullException(nameof(TargetProcess));
            }
            InternalThread = new Task(InternalThreadCallback);
            this.Setting = Setting;
            // I want my own copy
            DebugHandle = new Process
            {
                StartInfo = TargetProcess.StartInfo
            };


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
                throw new InvalidOperationException("Already running!");
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
            lock (this)
            {
                if (InternalThread.Status == TaskStatus.Running)
                {
                    QuitThread = true;
                }
            }
        }
    }


}
