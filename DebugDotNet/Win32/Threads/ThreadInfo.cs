using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DebugDotNet.Win32.Internal;

namespace DebugDotNet.Win32.Threads
{

    /// <summary>
    /// Enumerate Threads 
    /// </summary>
    public static class DebugDotNetThreads
    {
        /// <summary>
        /// Makes a snapshot of the process's threads and returns a list of thme
        /// </summary>
        /// <param name="Target"></param>
        /// <returns></returns>
        public  static List<uint> GetProcessThreads(this Process Target)
        {
            List<uint> ret = new List<uint>();
            if (Target == null)
            {
                throw new ArgumentNullException(nameof(Target));
            }
            else
            {
                if (Target.HasExited == true)
                {
                    throw new InvalidOperationException("Process exited");
                }

                THREADENTRY32 CallbackBuff = new THREADENTRY32(0);
                bool result = true;
                IntPtr Handle = Win32DebugApi.InvalidHandleValue;
                try
                {
                  Handle= NativeMethods.CreateToolhelp32Snapshot(ToolHelp32Settings.TH32CS_SNAPTHREAD, Target.Id);
                    if (Handle != Win32DebugApi.InvalidHandleValue)
                    {
                        if (NativeMethods.Thread32First(Handle, out CallbackBuff))
                        {
                            ret.Add(CallbackBuff.th32ThreadId);
                        }
                        while (true)
                        {
                            result = NativeMethods.Thread32Next(Handle, out CallbackBuff);
                            if (result)
                            {
                                ret.Add(CallbackBuff.th32ThreadId);
                            }
                            else
                            {
                                break;
                            }
                        }

                    }
                }
                finally
                {
                    if (Handle != Win32DebugApi.InvalidHandleValue)
                    {
                        NativeMethods.CloseHandle(Handle);
                    }
                }
                return null;

            }
        }
    }

}

namespace DebugDotNet.Win32.Internal
{

    /// <summary>
    /// Possible Settings for CreateToolhelp32Snapshot
    /// </summary>
    internal enum ToolHelp32Settings: uint
    {
        /// <summary>
        /// Is the handle inheritable
        /// </summary>
        TH32CS_INHERIT = 0x80000000,
        /// <summary>
        /// Include  heaps in the list
        /// </summary>

        TH32CS_SNAPHEAPLIST = 0x00000001,
        /// <summary>
        /// Include modules in the list
        /// </summary>
        TH32CS_SNAPMODULE = 0x00000008,
        /// <summary>
        /// Include 32-bit modules  in the list
        /// </summary>
        TH32CS_SNAPMODULE32 = 0x00000010,
        /// <summary>
        /// Include Processes in the list
        /// </summary>
        TH32CS_SNAPPROCESS = 0x00000002,
        /// <summary>
        /// Include threads in the list
        /// </summary>
        TH32CS_SNAPTHREAD = 0x00000004,
        /// <summary>
        /// TH32CS_SNAPHEAPLIST + TH32CS_SNAPMODULE + TH32CS_SNAPPROCESS + TH32CS_SNAPTHREAD
        /// </summary>
        TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST + TH32CS_SNAPMODULE + TH32CS_SNAPPROCESS + TH32CS_SNAPTHREAD


    };
    internal  static partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError =true)]
        public static extern IntPtr CreateToolhelp32Snapshot(ToolHelp32Settings Flags, int ProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]

        public static extern bool Thread32First(IntPtr Handle, [MarshalAs(UnmanagedType.Struct)]  out THREADENTRY32 Thread32Struct);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Thread32Next(IntPtr Handle, 
            [MarshalAs(UnmanagedType.Struct)]
            out THREADENTRY32 Thread32Struct);


    }

}

