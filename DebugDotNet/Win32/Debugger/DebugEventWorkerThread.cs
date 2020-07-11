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
        /// add a new event
        /// </summary>
        /// <param name="EventData">the event to add</param>
        public void PushEvent(DebugEvent EventData)
        {
            lock (this)
            {
                Event.Enqueue(EventData);
            }
        }

        /// <summary>
        /// return the next event without removing it
        /// </summary>
        /// <returns>the next debug event in the list</returns>
        public DebugEvent PeekEvent()
        {
            lock (this)
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
            lock (this)
            {
                ret = Event.Dequeue();
            }
            return ret;
        }

        /// <summary>
        /// lock this and return event count
        /// </summary>
        /// <returns></returns>
        public int EventCount()
        {
            lock (this)
            {
                return Event.Count;
            }
        }
/// <summary>
///     Event the DEBUG_EVENT quere
/// </summary>
        public void ClearEvents()
        {
            lock (this)
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
                InternalThread.Dispose();
                DebugHandle.Dispose();
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
        /// Choose how to relate DebugEventWorkerThread to the debugged process.
                /// </summary>
        public enum CreationSetting
        {
            /// <summary>
            /// Attach to running existing program
            /// </summary>
            AttachRunningProgram = 1,
            /// <summary>
            /// spawn program then attach (same as launching program first) then using AttachRunningProgram
            /// </summary>
            RunProgramThenAttach = 2,
            /// <summary>
            /// Create the process explicity with the worker thread with the debug flag passed
            /// </summary>
            CreateWithDebug = 3
        };

        /// <summary>
        /// the internal Task 
        /// </summary>
        readonly Task InternalThread;
        /// <summary>
        /// the internal handle the Task uses
        /// </summary>
        readonly Process DebugHandle;
        /// <summary>
        /// a shared var; this is locked on assignment via stop.
        /// </summary>
        bool QuitThread = false;

        /// <summary>
        /// the debug event catcher that we push events to as we see them
        /// </summary>
        public  DebugEventCapture Events { get; set; } = new DebugEventCapture();
        /// <summary>
        /// the creation setting passed at start
        /// </summary>
        private readonly CreationSetting Setting;
        
        /// <summary>
        /// the internal thread that watches for debug events and posts them to Events.
        /// </summary>
        void InternalThreadCallback()
        {
            switch (Setting)
            {
                case CreationSetting.AttachRunningProgram:
                    {
                        if (Win32DebugApi.DebugActiveProcess(DebugHandle.Id) == false)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        break;
                    }
                case CreationSetting.RunProgramThenAttach:
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
                case CreationSetting.CreateWithDebug:
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
                        throw new InvalidOperationException(Enum.GetName(typeof(CreationSetting), Setting) + " is not supported");
                    }
            }


            DebugEvent Event = new DebugEvent();
            while ( (QuitThread == false) && (InternalThread.IsCompleted == false) && (InternalThread.IsFaulted == false))
            {
                
                if (Win32DebugApi.WaitForDebugEvent(ref Event,  Win32DebugApi.Infinite) == true)
                {
                    Events.PushEvent(Event);
                    if (Win32DebugApi.ContinueDebugEvent(Event.dwProcessId, Event.dwThreadId, ContinueStatus.DBG_CONTINUE) == false)
                    {
                        throw new Win32Exception("(ContinueDebugEvent) " + Marshal.GetLastWin32Error());
                    }
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
        public DebugEventWorkerThread(Process TargetProcess, CreationSetting Setting)
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
        /// Spawn the thread to watch for events.
        /// </summary>
        public void Start()
        {
            InternalThread.Start();
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
