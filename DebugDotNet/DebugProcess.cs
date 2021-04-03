using DebugDotNet.Win32.Internal;
using DebugDotNet.Win32.Enums;
using static DebugDotNet.NativeHelpers.Detours.DetoursWrappers;
using DebugDotNet.Win32.Structs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;

namespace DebugDotNet.Win32.Tools
{




    /// <summary>
    /// c# of SecurityAttributes
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityAttributes : IEquatable<SecurityAttributes>
    {
        /// <summary>
        /// length of the struct in bytes
        /// </summary>
        public int nLength;
        /// <summary>
        /// specifies the SecurityDescriptor;
        /// </summary>
        public IntPtr lpSecurityDescriptor;

        /// <summary>
        /// is this inherited
        /// </summary>
        public int bInheritHandle;


        /// <summary>
        /// Compare any two <see cref="SecurityAttributes"/>
        /// </summary>
        /// <param name="obj">compare against</param>
        /// <returns>true if equal and false if not.</returns>
        public bool Equals(SecurityAttributes obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj.bInheritHandle != bInheritHandle)
                {
                    return false;
                }
                if (obj.lpSecurityDescriptor != lpSecurityDescriptor)
                {
                    return false;
                }

                if (obj.nLength != nLength)
                {
                    return false;
                }
                return true;
            }
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj is SecurityAttributes)
                {
                    return Equals((SecurityAttributes)obj);
                }
                return false;
            }
        }

        /// <summary>
        /// return hashcode of each memeber
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.bInheritHandle.GetHashCode() + this.lpSecurityDescriptor.GetHashCode() + this.nLength.GetHashCode();
        }

        /// <summary>
        /// Compare if any two <see cref="SecurityAttributes"/> structs are equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>returns true if equal and false if not</returns>
        public static bool operator ==(SecurityAttributes left, SecurityAttributes right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare if any two <see cref="SecurityAttributes"/> structs are NOT equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>returns true if different and false if not</returns>
        public static bool operator !=(SecurityAttributes left, SecurityAttributes right)
        {
            return !(left == right);
        }


    }
    /// <summary>
    /// Intended to launch a new process with the debug flag set. Most of the information exposed
    /// via the underlying class won't work. This is the bare minimum of enough to launch and debug the target
    /// </summary>
    public class DebugProcess : Process
    {
        /// <summary>
        /// Force the process being launched to use this dlls also via detours. Not supported if current setting is not <see cref="DebuggerCreationSetting.CreateWithDebug"/>
        /// Requires the DLL to be setup for this as detailed at <see href="https://github.com/microsoft/Detours/wiki/OverviewHelpers"/>
        /// </summary>
        public List<string> ForceLoadDlls
        { 
            get
            {
                if (intForceLoadDlls == null)
                {
                    intForceLoadDlls = new List<string>();
                }
                return intForceLoadDlls;
            }
        }


        private List<string> intForceLoadDlls;
        /// <summary>
        /// Flags to describe how this process will be debugged
        /// </summary>
 

        [Flags]
        private enum StartupInfo_settings : uint
        {
            STARTF_USESHOWWINDOW = 0x00000001
        }

 


        /// <summary>
        /// PROCESS_INFORMATION struct for CreateProcessor
        /// </summary>
        private ProcessInformation pInfo;


        /// <summary>
        /// Get the id of the process after it's been made
        /// </summary>
        public new int Id => pInfo.ProcessId;

        /// <summary>
        /// Specify how to debug this process
        /// </summary>
        public CreateFlags DebugSetting { get; set; } = CreateFlags.DoNotDebug;

        /// <summary>
        /// used to check if the process is still alive before returning cerrtain values
        /// </summary>
        const int StillActive = 259;


        /// <summary>
        /// Safely get rid of handles stored in pInfo if we go out of scope.
        /// </summary>
        /// <param name="Man">true if managed needs to be disposed also.</param>
        protected void Diposing(bool Man)
        {
            
           NativeMethods.CloseHandle(pInfo.ProcessHandleRaw);
            NativeMethods.CloseHandle(pInfo.ThreadHandleRaw);

            base.Dispose(Man);
        }

        /// <summary>
        /// setup default startinfo
        /// </summary>
        /// <param name="start">The variable to dump the settings too</param>
        private void FetchStartupInfo(out StartupInfo start)
        {
            start = new StartupInfo();
            start.cb = Marshal.SizeOf(start);


            if (this.StartInfo.CreateNoWindow == true)
            {
                start.dwFlags = (start.dwFlags) | StartInfoFlags.StartInfoUseShowWindow;
                start.wShowWindow = ShowWindowSettings.Hide;
            }
            else
            {
                start.dwFlags = (start.dwFlags) | StartInfoFlags.StartInfoUseShowWindow; 
                start.wShowWindow = ShowWindowSettings.ShowNormal;
            }

            if ((string.IsNullOrEmpty(StartInfo.UserName) == false))
            {
                throw new NotImplementedException(StringMessages.DebugProcessNoUserNameAllowed);

            }




        }
        /// <summary>
        /// Get the c# process class for this process
        /// </summary>
        /// <returns>Returns a Usable <see cref="Process"/> class for you if the process has not already quit.</returns>
        /// <exception cref="InvalidOperationException"> Is thrown if the process already exited</exception>
        /// <exception cref="Win32Exception"> Is thrown if there's an error in checking if the process already quit.</exception>
        public Process GetProcess()
        {
            IntPtr err = IntPtr.Zero;
            int e;
            if (NativeMethods.GetExitCodeProcess(pInfo.ProcessHandleRaw, ref err) == true)
            {
                e = err.ToInt32();
                if (e == StillActive)  // STILL_ACTIVE
                    return GetProcessById(pInfo.ProcessId);
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new InvalidOperationException("Process already quit");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());

        }


        /// <summary>
        /// Refresh for ShellExecute Process
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public new void Refresh()
        {
            if (StartInfo.UseShellExecute == true)
            {
                base.Refresh();
            }
            else
            {
                throw new NotImplementedException("Refresh() not supported for DebugProcess()");
            }
        }

        /// <summary>
        /// Start the process. If shellexcute is true then same as Process.Start()
        /// </summary>
        public new void Start()
        {
#pragma warning disable CS0219 // The variable '_Default' is assigned but its value is never used
            SecurityAttributes _Default = new SecurityAttributes();
#pragma warning restore CS0219 // The variable '_Default' is assigned but its value is never used

            string arguments;
            string currentdir;
            IntPtr Env = IntPtr.Zero;
            try
            {
                
                if (StartInfo.UseShellExecute == true)
                {
                    base.Start();
                }
                else
                {
                    FetchStartupInfo(out StartupInfo lpStartupInfo);
                    if (string.IsNullOrEmpty(StartInfo.Arguments) == false)
                    {
                        arguments = StartInfo.Arguments;
                    }
                    else
                    {
                        arguments = null;
                    }

                    if ( (string.IsNullOrEmpty(StartInfo.WorkingDirectory) == false))
                    {
                        currentdir = StartInfo.WorkingDirectory;
                    }
                    else
                    {
                        currentdir = null;
                    }

                    if (intForceLoadDlls.Count == 0)
                    {
                        Env = Marshal.StringToHGlobalUni(UnmangedToolKit.EnviromentToStringReady(this.StartInfo));
                        if (NativeMethods.CreateProcessW(StartInfo.FileName, arguments, IntPtr.Zero, IntPtr.Zero, true, (uint)DebugSetting, Env, currentdir, ref lpStartupInfo, out pInfo))
                        {

                        }
                        else
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                    else
                    {
                        if ( DetourCreateProcessWithDllEx(StartInfo.FileName, arguments, new SecurityAttributes(), new SecurityAttributes(), true, DebugSetting, string.Empty, currentdir, ref lpStartupInfo, out pInfo, ForceLoadDlls))
                        {

                        }
                        else
                        {
                            int err = Marshal.GetLastWin32Error();
                            if (err != 0)
                            {
                                throw new Win32Exception(err);
                            }
                        }

                    }




                }
            }
            finally
            {
                Marshal.FreeHGlobal(Env);
                if (pInfo.ProcessHandleRaw != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.ProcessHandleRaw);
                }
                if (pInfo.ThreadHandleRaw != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.ThreadHandleRaw);
                }
            }
        }
        /// <summary>
        /// create the passed process and debug it. 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public unsafe Process Debug(string app)
        {
            ProcessInformation pInfo = new ProcessInformation();
            StartupInfo lpStartupInfo = new StartupInfo();
            SecurityAttributes Default = new SecurityAttributes();
            Process Ret;
            string args;
            Default.bInheritHandle = 0;
            Default.lpSecurityDescriptor = IntPtr.Zero;
            Default.nLength = 0;



            
            lpStartupInfo.cb = Marshal.SizeOf(lpStartupInfo);
            
 

            
            IntPtr noargs = Marshal.AllocHGlobal(sizeof(char));
            UnmangedToolKit.Memset((byte*)noargs.ToPointer(), 0, sizeof(char));
            
            args = StartInfo.Arguments;
            if (string.IsNullOrEmpty(args))
            {
                args = null;
            }
            try
            {
                if (NativeMethods.CreateProcessW(app, null, IntPtr.Zero, IntPtr.Zero, true, (uint)DebugSetting, IntPtr.Zero,  null, ref lpStartupInfo, out pInfo))
                {
                    Ret = Process.GetProcessById(pInfo.ProcessId);
                    return Ret;
                }
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (pInfo.ProcessHandleRaw != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.ProcessHandleRaw);
                }
                if (pInfo.ThreadHandleRaw != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.ThreadHandleRaw);
                }
                Marshal.FreeHGlobal(noargs);
            }
        }



      }
}
