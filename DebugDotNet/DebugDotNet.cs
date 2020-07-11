using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;


namespace DebugDotNet
{

    /* The intial structures posted on pinvoke.et for DEBUG_EVENT are a launch pad into this.
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
    /// Approach at  a lower level. This lies closer to our internale <see cref="NativeMethods"/> class
    /// </summary>
    public  sealed class Win32DebugApi
        {
            private static bool DebugPrivFlag = false;

            /// <summary>
            /// Set or clear the SE_DEBUG_PRIV privilege.  Doing this without pinvoke.net seems to require admin
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
                        // set the priv
                    }
                    else
                    {
                        // remove the priv
                        Process.LeaveDebugMode();
                    }
                    DebugPrivFlag = value;
                }
            }


            /// <summary>
            ///  the INVALID_HANDLE value for CreateProcess
            /// </summary>
            public static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

            /// <summary>
            /// If you call WaitForDebugEventEx() with this as the timeout it waits until an event occures
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
        /// <param name="dwProcessId">process id of event receiced</param>
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
        /// Debug target process with specified it. HIGH CHANCE of Evivated Admin privilage needed
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



/// <summary>
/// Native Mathods class
/// </summary>
   internal static class NativeMethods
    {
    [DllImport("kernel32.dll", SetLastError =true)]
    public static extern bool DebugSetProcessKillOnExit(bool KillOnExit);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UInt32 GetFinalPathNameByHandleW(IntPtr hFile,
                                                       IntPtr lpszFileName,
                                                       UInt32 cchFilePath,
                                                       UInt32 dwFlags);

        public static string GetFinalPathNameByHandle(IntPtr hFile, FinalFilePathFlags Flags)
        {
            string ret;
            int Result;
            IntPtr UnmanagedBlock = (IntPtr)0;
            IntPtr BlockSize = UnmanagedBlock;
            try
            {
                while (true)
                {
                    if (BlockSize != (IntPtr)0)
                    {
                        UnmanagedBlock = Marshal.AllocHGlobal(sizeof(char) * (int)BlockSize);
                    }
                    Result = (int)GetFinalPathNameByHandleW(hFile, UnmanagedBlock, (UInt32)BlockSize.ToInt32(), (uint)Flags);
                    if (Result == 0) // something outside of
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        if (Result > BlockSize.ToInt32())
                        {
                            // we are not actually got all bytes yet.
                            if (UnmanagedBlock != (IntPtr)0)
                            {
                                Marshal.FreeHGlobal(UnmanagedBlock);
                                UnmanagedBlock = (IntPtr)0;
                            }
                            BlockSize = (IntPtr)Result + 1;
                        }
                        else
                        {
                            // we have a block contained the rest.
                            break;
                        }
                    }
                }


                ret = Marshal.PtrToStringUni(UnmanagedBlock, Result);
                return ret;
            }
            finally
            {
                if (UnmanagedBlock != (IntPtr)0)
                {
                    Marshal.FreeHGlobal(UnmanagedBlock);
                }
            }

        }

 

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        IntPtr lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead);

    /// <summary>
    /// Direct Import of ContinueDebugEvent() from the Win32 Api
    /// </summary>
    /// <param name="dwProcessId">Process Id of the event to continue</param>
    /// <param name="dwThreadId">Thread id of the event to continue</param>
    /// <param name="dwContinueStatus">how the debugger (thats you) responded to this</param>
    /// <returns></returns>

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, ContinueStatus dwContinueStatus);

    /// <summary>
    /// Direct Import of the DebugActiveProcess from the Win32 Api
    /// </summary>
    /// <param name="dwProcessId">Debug the process with the specified id</param>
    /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcess(int dwProcessId);

    /// <summary>
    /// Import of DebugActiveProcessStop
    /// </summary>
    /// <param name="dwProcessId">Quite debugging this process's id</param>
    /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcessStop(int dwProcessId);


    /// <summary>
    /// diret input of WaitForDebugEventX
    /// </summary>
    /// <param name="lpDebugEvent">specify a debug event to hold data (use an instance of DEBUG_EVENT class as a ref)</param>
    /// <param name="dwMilliseconds">specifiy timeout</param>
    /// <returns>returns true if an event occured within the time trame</returns>
    [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEventEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WaitForDebugEventEx(ref DebugEvent lpDebugEvent, uint dwMilliseconds);

}




