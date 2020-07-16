using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.ConstrainedExecution;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using DebugDotNet.Win32.Internal;

namespace DebugDotNet.Win32.Tools
{
    // This also works with CharSet.Ansi as long as the calling function uses the same character set.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO
    {
        public Int32 cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public Int32 dwX;
        public Int32 dwY;
        public Int32 dwXSize;
        public Int32 dwYSize;
        public Int32 dwXCountChars;
        public Int32 dwYCountChars;
        public Int32 dwFillAttribute;
        public Int32 dwFlags;
        public Int16 wShowWindow;
        public Int16 cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    /// <summary>
    /// c# of SecurityAttributes
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes
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
    }
    /// <summary>
    /// Intended to launch a new process with the debug flag set. Most of the information exposed
    /// via the underlying class won't work. This is the bare minimum of enough to launch and debug the target
    /// </summary>
    public class DebugProcess : Process
    {
        /// <summary>
        /// Flags to describe how this process will be debugged
        /// </summary>
        [Flags]
        public enum CreateFlags : int
        {
            /// <summary>
            /// Launch with no Debug flag
            /// </summary>
            DoNotDebug = 0x00000000,
            /// <summary>
            /// Launch with debugging this process plus any processors it spawns
            /// </summary>
            DebugProcessAndChild = 0x00000001,
            /// <summary>
            /// Debug *just* this process
            /// </summary>
            DebugOnlyThisProcess = 0x00000002,
            /// <summary>
            /// If the target is a console app, this forces a new console rather than using the one (you) have
            /// </summary>
            ForceNewConsole = 0x00000010
        }

        [Flags]
        private enum StartupInfo_settings : uint
        {
            STARTF_USESHOWWINDOW = 0x00000001
        }

        [Flags]
        private enum ShowWindow_settings : uint
        {
            /// <summary>
            /// Minimizes a window, even if the thread that owns the window is not responding.This flag should only be used when minimizing windows from a different thread. 
            /// </summary>
            SW_FORCEMINIMIZE = 11,


            /// <summary>
            /// //Hides the window and activates another window.
            /// </summary>
            SW_HIDE = 0,

            /// <summary>
            /// //Maximizes the specified window. 
            /// </summary>
            SW_MAXIMIZE = 3,

            /// <summary>
            /// Minimizes the specified window and activates the next top-level window in the Z order.
            /// </summary>
            SW_MINIMIZE = 6,

            /// <summary>
            /// Activates and displays the window.If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window. 
            /// </summary>
            SW_RESTORE = 9,

            /// <summary>
            ///Activates the window and displays it in its current size and position.
            /// </summary>
            SW_SHOW = 5,

            /// <summary>
            /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
            /// </summary>
            SW_SHOWDEFAULT = 10,

            /// <summary>
            ///  Activates the window and displays it as a maximized window.
            /// </summary>
            SW_SHOWMAXIMIZED =  SW_MAXIMIZE,

            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            SW_SHOWMINIMIZED = 2,

            /// <summary>
            /// Displays the window as a minimized window.This value is similar to SW_SHOWMINIMIZED, except the window is not activated. 
            /// </summary>
            SW_SHOWMINNOACTIVE = 7,


            /// <summary>
            /// Displays the window in its current size and position.This value is similar to SW_SHOW, except that the window is not activated. 
            /// </summary>
            SW_SHOWNA = 8,


            /// <summary>
            /// Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated. 
            /// </summary>
            SW_SHOWNOACTIVATE = 4,


            /// <summary>
            /// Normal Window
            /// </summary>
            SW_SHOWNORMAL = 1
        }



        /// <summary>
        /// PROCESS_INFORMATION struct for CreateProcessor
        /// </summary>
        private PROCESS_INFORMATION pInfo;


        /// <summary>
        /// Get the id of the process after it's been made
        /// </summary>
        public new int Id => pInfo.dwProcessId;

        /// <summary>
        /// Specify how to debug this process
        /// </summary>
        public CreateFlags DebugSetting { get; set; } = CreateFlags.DoNotDebug;

        /// <summary>
        /// used to check if the process is still alive before returning cerrtain values
        /// </summary>
        const int StillActive = 259;


        /// <summary>
        /// Safly get rid of handles stored in pInfo
        /// </summary>
        /// <param name="Man"></param>
        protected void Diposing(bool Man)
        {
            
           NativeMethods.CloseHandle(pInfo.hProcess);
            NativeMethods.CloseHandle(pInfo.hThread);

            base.Dispose(Man);
        }

        /// <summary>
        /// setup default startinfo
        /// </summary>
        /// <param name="start"></param>
        private void FetchStartupInfo(out STARTUPINFO start)
        {
            start = new STARTUPINFO();
            start.cb = Marshal.SizeOf(start);


            if (this.StartInfo.CreateNoWindow == true)
            {
                start.dwFlags = (start.dwFlags) | (int)StartupInfo_settings.STARTF_USESHOWWINDOW;
                start.wShowWindow = (short)ShowWindow_settings.SW_HIDE;
            }
            else
            {
                start.dwFlags = (start.dwFlags) | (int)StartupInfo_settings.STARTF_USESHOWWINDOW;
                start.wShowWindow = (short)ShowWindow_settings.SW_SHOWNORMAL;
            }

            if ((string.IsNullOrEmpty(StartInfo.UserName) == false))
            {
                throw new NotImplementedException(StringMessages.DebugProcessNoUserNameAllowed);

            }




        }
        /// <summary>
        /// Get the c# process class for this process
        /// </summary>
        /// <returns></returns>
        public Process GetProcess()
        {
            IntPtr err = IntPtr.Zero;
            int e;
            if (NativeMethods.GetExitCodeProcess(pInfo.hProcess, ref err) == true)
            {
                e = err.ToInt32();
                if (e == StillActive)  // STILL_ACTIVE
                    return GetProcessById(pInfo.dwProcessId);
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
            try
            {
                if (StartInfo.UseShellExecute == true)
                {
                    base.Start();
                }
                else
                {
                    FetchStartupInfo(out STARTUPINFO lpStartupInfo);
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

                    if (NativeMethods.CreateProcessW(StartInfo.FileName, arguments, IntPtr.Zero, IntPtr.Zero, true, (uint)DebugSetting, IntPtr.Zero, currentdir, ref lpStartupInfo, out pInfo))
                    {
                    
                    }
                    else
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }


                }
            }
            finally
            {
                if (pInfo.hProcess != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.hProcess);
                }
                if (pInfo.hThread != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.hThread);
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
            PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();
            STARTUPINFO lpStartupInfo = new STARTUPINFO();
            SecurityAttributes Default = new SecurityAttributes();
            Process Ret;
            string args;
            Default.bInheritHandle = 0;
            Default.lpSecurityDescriptor = IntPtr.Zero;
            Default.nLength = 0;



            
            lpStartupInfo.cb = Marshal.SizeOf(lpStartupInfo);
            
 

            
            IntPtr noargs = Marshal.AllocHGlobal(sizeof(char));
            UnmangedToolKit.Memset((byte*)noargs.ToPointer(), 0, sizeof(char));
            
            args = this.StartInfo.Arguments;
            if (string.IsNullOrEmpty(args))
            {
                args = null;
            }
            try
            {
                if (NativeMethods.CreateProcessW(app, null, IntPtr.Zero, IntPtr.Zero, true, (uint)DebugSetting, IntPtr.Zero,  null, ref lpStartupInfo, out pInfo))
                {
                    Ret = Process.GetProcessById(pInfo.dwProcessId);
                    return Ret;
                }
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (pInfo.hProcess != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.hProcess);
                }
                if (pInfo.hThread != ((IntPtr)(-1)))
                {
                    NativeMethods.CloseHandle(pInfo.hThread);
                }
                Marshal.FreeHGlobal(noargs);
            }
        }



      }
}
