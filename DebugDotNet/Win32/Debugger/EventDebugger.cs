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
    /// EventDebugger Tracks Loaded Dlls in a dictionary of these
    /// </summary>
    public struct EventDebuggerLoadedDll : IEquatable<EventDebuggerLoadedDll>
    {
        /// <summary>
        /// Name of the Dll, can be Null if the debugger could not get the file the dll is from
        /// </summary>
        public string Name;
        /// <summary>
        /// Base Memory of the Dll in the debugged process's memory
        /// </summary>
        public IntPtr BaseLocation;

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (!(obj is EventDebuggerLoadedDll DllOther))
                {
                    return false;
                }
                else return Equals(DllOther);
            }
        }

        /// <summary>
        /// get a hash of each element
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.BaseLocation.GetHashCode() + this.Name.GetHashCode();
        }

        /// <summary>
        /// Compare left and right if equal
        /// </summary>
        /// <param name="left">left side</param>
        /// <param name="right">right side</param>
        /// <returns>true if equal</returns>
        public static bool operator ==(EventDebuggerLoadedDll left, EventDebuggerLoadedDll right)
        {
            return left.Equals(right);
        }
        /// <summary>
        /// Compare left and right if NOT equal
        /// </summary>
        /// <param name="left">left side</param>
        /// <param name="right">right side</param>
        /// <returns>true if different</returns>
        public static bool operator !=(EventDebuggerLoadedDll left, EventDebuggerLoadedDll right)
        {
            return !(left == right);
        }

        /// <summary>
        /// is other equal to this?
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(EventDebuggerLoadedDll other)
        {
           if (other == null)
            {
                return false;
            }
           else
            {
                if (other.BaseLocation != BaseLocation)
                {
                    return false;
                }
                if (other.Name != Name)
                {
                    return false;
                }
                return true;
            }
        }
    }


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
        /// Retrieve a list of Dlls that have be seen via the debugger.
        /// </summary>
        public Dictionary<IntPtr, EventDebuggerLoadedDll> TrackedDlls
        {
            get
            {
                if (LoadedModules == null)
                {
                    LoadedModules = new Dictionary<IntPtr, EventDebuggerLoadedDll>();
                }
                return LoadedModules;
            }
        }
        Dictionary<IntPtr, EventDebuggerLoadedDll> LoadedModules = new Dictionary<IntPtr, EventDebuggerLoadedDll>();
        #region debugger control flags and settings

        /// <summary>
        /// if set to true we keep a running list of loaded libraries (DLLs)
        /// </summary>
        public bool TrackingModules
        { 
            get
            {
                return TrackingModulesInt;
            }
            set
            {
                TrackingModulesInt = value;
            }
        }

        /// <summary>
        /// If set to true we keep a running list of created threads 
        /// </summary>
        public bool TrackingThreads
        { 
            get
            {
                return TrackingThreadsInt;
            }
            set
            {
                TrackingThreadsInt = value;
            }
        }


        /// <summary>
        /// If set we track Dll Load and Unloads, and accociate them to a name
        /// </summary>
        bool TrackingModulesInt;
        /// <summary>
        /// if Set we track Thread Creation and Exit
        /// </summary>
        bool TrackingThreadsInt;

        /// <summary>
        /// Tell Windows what Hpppens to the program being debugged when the debugged ends. See MSDN DebugSetProcessKillOnExit()
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
        #endregion



        #region disposal and destructors
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

        #endregion

        #region Forced Dll Loaded
        /// <summary>
        /// <see cref="DebuggerCreationSetting.CreateWithDebug"/> only.  Clear any previously added dlls to be forced to load
        /// </summary>
        public void ClearForceLoadDll()
        {
            Handler.ForceLoadDll.Clear();
        }
        /// <summary>
        /// <see cref="DebuggerCreationSetting.CreateWithDebug"/> only. This adds dlls that we'll force the spawned process to load with <see cref="NativeHelpers.Detours.DetoursWrappers.DetourCreateProcessWithDllEx(string, string, Tools.SecurityAttributes, Tools.SecurityAttributes, bool, CreateFlags, string, string, ref StartupInfo, out ProcessInformation, List{string})"/>
        /// </summary>
        /// <param name="DllList"></param>
        public void AddForceLoadDllRange( IEnumerable<string> DllList)
        {
            Handler.ForceLoadDll.AddRange(DllList);
        }
        #endregion

        #region constructors
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
        #endregion


        #region Start Debugging
        /// <summary>
        /// Begin debugger
        /// </summary>
        public void Start()
        {
            Handler.Events = CaptureData;
            Handler.Start(p => { DispatchEvents(); });
        }
        #endregion

        #region Event Handling
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
                        if (TrackingModules)
                        {
                            var NewModule = new EventDebuggerLoadedDll();
                            var LoadedDll = ReadyToDispatch.LoadDllInfo;
                            NewModule.BaseLocation = LoadedDll.BaseDllAddress;
                            NewModule.Name = Win32.Tools.UnmangedToolKit.TrimPathProcessingConst(LoadedDll.ImageName);
                            LoadedModules.Add(LoadedDll.BaseDllAddress, NewModule);
                        }
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
                        if (TrackingModules)
                        {
                            var EventData = ReadyToDispatch.UnloadDllInfo;
                            if (LoadedModules.ContainsKey(EventData.BaseDllAddress))
                            {
                                LoadedModules.Remove(EventData.BaseDllAddress);
                            }
                        }
                        break;
                }

                Win32DebugApi.ContinueDebugEvent(ReadyToDispatch.dwProcessId, ReadyToDispatch.dwThreadId, Response);
            }
        }
        #region delegate prototypes
        /// <summary>
        /// Triggers when any event is received.
        /// </summary>
        /// <remarks> Should you subscribe to this and a specific event, you'll see two instances of that event.</remarks>
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
        #endregion
    }
}
