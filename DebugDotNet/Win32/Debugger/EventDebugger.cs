using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DebugDotNet;
using DebugDotNet.Win32.Structs;

namespace DebugDotNet.Win32.Debugger
{
    /// <summary>
    /// Exposed events to subscribe to various Win32 event posts (DEBUG_EVENT, ect...)
    /// </summary>
    public class EventDebugger
    {
        DebugEventWorkerThread Handler;
        

        public EventDebugger(Process Start)
        {

        }
        /// <summary>
        /// Triggers when an event is received
        /// </summary>
        /// <param name="EventData"></param>
        public delegate void DebugEventGeneralProtype(ref DebugEvent EventData);

        /// <summary>
        /// Subscribe to receives callback when any Debug Event is recieved
        /// </summary>
        public event DebugEventGeneralProtype GeneralDebugEvent;
    }
}
