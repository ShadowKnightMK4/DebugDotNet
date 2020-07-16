using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DebugDotNet;
using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;

namespace DebugDotNet.Win32.Debugger
{
    /// <summary>
    /// Exposed events to subscribe to various Win32 event posts (DEBUG_EVENT, ect...)
    /// If you Subscribe to the General one wou'll be getting the same event 2x. Plan accordingly.
    /// </summary>
    public class EventDebugger: IDisposable
    {
        DebugEventWorkerThread Handler;
        DebugEventCapture CaptureData;
        DebugEvent ReadyToDispatch = new DebugEvent();

        /// <summary>
        /// Tell Windows what ppens to the program being debugged when the debugged ends. See MSDN DebugSetProcessKillOnExit()
        /// </summary>
        public bool EndDebugProcessOnQuit
        {
            get
            {
                return Handler.KillDebugedProcessOnExit;
            }
            set
            {
                Handler.KillDebugedProcessOnExit = value;
            }
        }

        /// <summary>
        /// If true we also debug spawned child processes <see cref="DebugEventWorkerThread.TrackChildProcess"/>
        /// </summary>
        public bool DebugSpawnedProceses
        {
            get
            {
                return Handler.TrackChildProcess;
            }
            set
            {
                Handler.TrackChildProcess = value;
            }
        }




        /// <summary>
        /// If set to true this class does *NOT* wait for your code to respond. It just tracks the event and savesit for you to look at.
        /// </summary>
        public bool MonitorOnly
        {
            set
            {
                Handler.SingleThreadMode = !value;
            }
            get
            {
                return Handler.SingleThreadMode;
            }
        }

  


        /// <summary>
        /// destructor
        /// </summary>
        ~EventDebugger()
        {
            Dispose(false);
        }

        /// <summary>
        /// free my resources
        /// </summary>
        /// <param name="Managed"></param>
        protected virtual void Dispose(bool Managed)
        {
            if (Managed)
            {

            }
            if (Handler != null)
            {
                if (Handler.IsDisposed == false)
                    Handler.Dispose();
            }
        }
        /// <summary>
        /// free resources 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Make a new instance with the target process and tell how to debug in.
        /// </summary>
        /// <param name="Start"></param>
        /// <param name="CreationSettings"></param>
        public EventDebugger(Process Start, DebuggerCreationSetting CreationSettings= DebuggerCreationSetting.RunProgramThenAttach)
        {
            Handler = new DebugEventWorkerThread(Start, CreationSettings);
            CaptureData = new DebugEventCapture();
            Handler.SingleThreadMode = true;
        }

        /// <summary>
        /// Begin debugger
        /// </summary>
        public void Start()
        {
            Handler.Events = CaptureData;
            Handler.Start(p => { DispatchEvents(); });
        }

        /// <summary>
        /// Check for any events and pull from the query, dispatch to the event handlers
        /// </summary>
        public void DispatchEvents()
        {
            if (CaptureData.EventCount() > 0)
            {

                ContinueStatus Response = ContinueStatus.DebugContinue;

                ReadyToDispatch = CaptureData.PullEvent();

                GeneralDebugEvent?.Invoke(ref ReadyToDispatch, ref  Response);
                switch (ReadyToDispatch.dwDebugEventCode)
                {
                    case DebugEventType.CreateProcessDebugEvent:
                        CreateProcessEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.CreateThreadDebugEvent:
                        CreateThreadEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.ExceptionDebugEvent:
                        ExceptionDebugEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.ExitProcessDebugEvent:
                        ExitProcessEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.ExitThreadDebugEvent:
                        ExitThreadEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.LoadDllDebugEvent:
                        LoadDllDebugEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.OutputDebugStringEvent:
                        OutputDebugStringEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.RipEvent:
                        RipEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                    case DebugEventType.UnloadDllDebugEvent:
                        UnloadDllDebugEvent?.Invoke(ref ReadyToDispatch, ref Response);
                        break;
                }

                Win32DebugApi.ContinueDebugEvent(ReadyToDispatch.dwProcessId, ReadyToDispatch.dwThreadId, Response);
            }
        }
        #region delegate prototypes
        /// <summary>
        /// Triggers when any event is received
        /// </summary>
        /// <param name="EventData">ref to a DebugEvent that already has the data</param>
        /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void AnyEventCallBack(ref DebugEvent EventData, ref ContinueStatus Response);

        /// <summary>
        /// Triggers when an exception happens in the debugged process
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void ExceptionEventCallback(ref DebugEvent EventData, ref ContinueStatus Response);
        /// <summary>
        /// Triggers when a debugged process creates a thread.  The DebugEvent has a <see cref="CreateThreadDebugInfo"/>
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void CreateThreadEventCallback(ref DebugEvent EventData, ref ContinueStatus Response);
        /// <summary>
        /// Triggers when the process is created
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void CreateProcessEventCallback(ref DebugEvent EventData, ref ContinueStatus Response);
        /// <summary>
        /// Triggers when a thread ends in the debugged process
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void ExitThreadEventCallback(ref DebugEvent EventData, ref ContinueStatus Response);
        /// <summary>
        /// Triggers when the process loads a dll into its address space
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void LoadDllDebugEventCallback(ref DebugEvent EventData, ref ContinueStatus Response);
        /// <summary>
        /// Triggers when the process has a dll unloaded from its address space
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void UnloadDllDebugEventCallback(ref DebugEvent EventData, ref ContinueStatus Response);
        /// <summary>
        /// Triggers when the process dies outside of system debugger control debugger 
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void RipInfoEventCallback(ref DebugEvent EventData, ref ContinueStatus Response);

        /// <summary>
        /// Triggers when the process emitted a debug string
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void OutputDebugStringCallBack(ref DebugEvent EventData, ref ContinueStatus Response);

        /// <summary>
        /// Triggers when a process exits
        /// </summary>
        /// <param name="EventData">Reference to the event this routine received.</param>
        /// <param name="Response">ref to a Reponse to the event that gets send back to Windows</param>
        public delegate void ExitProcessEventCallBack(ref DebugEvent EventData, ref ContinueStatus Response);

        #endregion


        /// <summary>
        /// Subscribe to get debug string messages
        /// </summary>
        public event OutputDebugStringCallBack OutputDebugStringEvent;
        /// <summary>
        /// Subscribe to receives callback when any Debug Event is recieved.
        /// </summary>
        public event AnyEventCallBack GeneralDebugEvent;
        /// <summary>
        /// Suscribe to receive exception events
        /// </summary>
        public event ExceptionEventCallback ExceptionDebugEvent;
        /// <summary>
        /// Subscribe to receive Thread Creation Events
        /// </summary>
        public event CreateThreadEventCallback CreateThreadEvent;
        /// <summary>
        /// Subscribe to receive Process Creation Events
        /// </summary>
        public event CreateProcessEventCallback CreateProcessEvent;
        /// <summary>
        /// Subscribe to receive Exit Thread Events
        /// </summary>
        public event ExitThreadEventCallback ExitThreadEvent;
        /// <summary>
        /// Subscribe to recieve Dll Loading Events
        /// </summary>
        public event LoadDllDebugEventCallback LoadDllDebugEvent;
        /// <summary>
        /// Subscribe to receive Dll Unloading Events
        /// </summary>
        public event UnloadDllDebugEventCallback UnloadDllDebugEvent;
        /// <summary>
        /// Subscribe to receive Rip Events
        /// </summary>
        public event RipInfoEventCallback RipEvent;

        /// <summary>
        /// Subscribe to get when a debugged process exits
        /// </summary>
        public event ExitProcessEventCallBack ExitProcessEvent;

    }
}
