using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Internal;
using System.Resources;
using System.Runtime.CompilerServices;

namespace DebugDotNet
{

    /* The initial structures posted on pinvoke.et for DEBUG_EVENT are a launch pad into this.
     * 
     *  naming scheme for the structs
        SOMETHING_INTERNAL      <-  this struct is directly marshaled from unmanaged memory with no processing
        SOMETHING               <-  this struct is derived from SOMETHING_INTERNAL under the DEBUG_EVENT struct code


        notice!
            These are all handled the same way and there is currently no special processing. We marshal the data into the struct when asked too and free any relevent pointer
        CREATE_THREAD_DEBUG_INFO
        EXIT_THREAD_DEBUG_INFO
        EXIT_PROCESS_DEBUG_INFO
        UNLOAD_DLL_DEBUG_INFO

     */

    /// <summary>
    /// Approach at  a lower level. This lies closer to our internal <see cref="NativeMethods"/> class
    /// </summary>
    public  sealed class Win32DebugApi
        {
            private static bool DebugPrivFlag = false;

            /// <summary>
            /// Set or clear the SE_DEBUG_PRIV privilege.  Doing this without pinvoke.net seems to require Admin privilege
            /// </summary>
            public static bool DebugPriv
            {
                get
                {
                    return DebugPrivFlag;
                }
                set
                {
                    if (value == true)
                    {
                        Process.EnterDebugMode();
                       // set the privilege
                    }
                    else
                    {
                    // remove the privilege
                    Process.LeaveDebugMode();
                    }
                    DebugPrivFlag = value;
                }
            }


            /// <summary>
            ///  the INVALID_HANDLE C/C++ value for CreateProcess and other Windows API Kin
            /// </summary>
            public static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

            /// <summary>
            /// If you call WaitForDebugEventEx() with this as the timeout it waits until an event occurs
            /// </summary>
            public static readonly uint Infinite = 0xFFFFFFFF;


 
        
            /// <summary>
            /// Wait until a debug event happens.
            /// </summary>
            /// <param name="DebugEvent">The struct to pass the results too</param>
            /// <param name="Milliseconds">How long to wait (use INFINITE) to wait until something happens</param>
            /// <see cref="NativeMethods.WaitForDebugEventEx(ref DebugEvent, uint)"/>
            /// <returns>returns true if an event happened</returns>
            public static bool WaitForDebugEvent(ref DebugEvent DebugEvent, uint Milliseconds)
            {
                bool result = NativeMethods.WaitForDebugEventEx(ref DebugEvent, Milliseconds);
                return result;
            }

        /// <summary>
        /// Tell Windows what to do with the debug event after your code is done handling it
        /// </summary>
        /// <param name="dwProcessId">process id of event received</param>
        /// <param name="dwThreadId">thread id of event received</param>
        /// <param name="continueStatus">tell windows how your debugger responded</param>
        /// <returns>the results of calling the NativeMethods.ContinueDebugEvent() </returns>
        public static bool ContinueDebugEvent(int dwProcessId, int dwThreadId, ContinueStatus continueStatus)
        {
            var auto = NativeMethods.ContinueDebugEvent(dwProcessId, dwThreadId, continueStatus);
            return auto;

        }

        /// <summary>
        /// Quit receiving events from a process that had DebugActiveProcess() called against it
        /// </summary>
        /// <param name="dwProcessId">the id of the process to quit receiving debug events from</param>
        /// <returns>returns if the call worked or not.</returns>
        public static bool DebugActiveProcessStop(int dwProcessId)
        {
            return NativeMethods.DebugActiveProcessStop(dwProcessId);
        }
        /// <summary>
        /// Debug target process with specified it. HIGH CHANCE of Elevated Admin privilege needed
        /// </summary>
        /// <param name="dwProcessId">the process id to debug</param>
        /// <returns>if the call worked or not</returns>
            public static bool DebugActiveProcess(int dwProcessId)
            {
                var Auto = NativeMethods.DebugActiveProcess(dwProcessId);
                return Auto;
            }

        /// <summary>
        /// Try Debugging the passes already running process with the contained ID
        /// </summary>
        /// <param name="Target"></param>
        /// <returns>true if it worked</returns>
        public static bool DebugActiveProcess(Process Target)
            {
            if (Target == null)
            {
                throw new ArgumentNullException(nameof(Target));
            }
                return DebugActiveProcess(Target.Id);
            }

        }


    }







