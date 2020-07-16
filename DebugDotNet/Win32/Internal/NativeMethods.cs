using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace DebugDotNet.Win32.Internal
{
    /// <summary>
    /// Native Mathods class
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessW(
    [MarshalAs(UnmanagedType.LPWStr)]
            string lpApplicationName,

    [MarshalAs(UnmanagedType.LPWStr)]
            string lpArgs,

    IntPtr lpProcessAttributes,
    IntPtr lpThreadAttributes,

    bool bInheritHandles,

    uint dwCreationFlags,


    IntPtr lpEnvironment,

    [MarshalAs(UnmanagedType.LPWStr)]
            string lpCurrentDirectory,
[In] ref STARTUPINFO lpStartupInfo,
out PROCESS_INFORMATION lpProcessInformation);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr hProcesss, ref IntPtr pExitCode);
        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
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

}
