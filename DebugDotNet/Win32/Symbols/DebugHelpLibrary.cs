using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDotNet.Win32.Symbols
{
    /// <summary>
    /// Dbghlp.dll symbol wrappign
    /// </summary>
    public sealed class DebugHelpLibrary: IDisposable
    {
        /// <summary>
        /// Used to sync access pebetwen calls
        /// </summary>
        static object SyncAcess;
        IntPtr ProcessHandle;

        /// <summary>
        /// returns if object disposed
        /// </summary>
        public bool IsDisposed { get; private set; }


        /// <summary>
        /// calls dispose()
        /// </summary>
        ~DebugHelpLibrary()
        {
            Dispose();
        }
        /// <summary>
        /// Perform cleanup on the process
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            if (SymCleanup(ProcessHandle) == false)
            {
                
            }
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }


        /// <summary>
        /// privite setup routine that the public constructors call
        /// </summary>
        /// <param name="NativeProcessHandle">Handle to ID the caller, typically a Win32 HANDLE value</param>
        /// <param name="SearchPath">Specifc a search path. Use ; to seperate individual folders or simple pass null to revert to default</param>
        /// <param name="InvadeSelf">if set, this tells the library to load symbols for (your) program -- the caller</param>
        private void CommonIni(IntPtr NativeProcessHandle, string SearchPath, bool InvadeSelf)
        {
            lock (SyncAcess)
            {
                if (SymInitialize(NativeProcessHandle, SearchPath, InvadeSelf) == false)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err != 0)
                    {
                        throw new Win32Exception(err);
                    }
                    else
                    {
                        // symbols already loaded. Don't throw an exception here
                    }
                }
            }
            ProcessHandle = NativeProcessHandle;
        }

        /// <summary>
        /// Make an instance of the debug symbol library pointing to this running process
        /// </summary>
        /// <param name="Target">The process to to use as as handle.</param>
        /// throws <exception cref="ArgumentNullException"> if Target is null</exception>
        /// Throws <exception cref="InvalidOperationException"> if Target is not running or already exited</exception>
        public DebugHelpLibrary(Process Target)
        {
            SyncAcess = 0;
            if (Target == null)
            {
                throw new ArgumentNullException(nameof(Target));
            }
            if (Target.HasExited)
            {
                throw new InvalidOperationException(StringMessages.DebugHelpLibraryProcessExited + "  " + Target.ProcessName);
            }
            CommonIni(Target.Handle, null, false);
        }
        /// <summary>
        /// Calls SymInitalize() with handle to current process and default search path. Loads Symbols of callee
        /// </summary>
        public DebugHelpLibrary()
        {
            SyncAcess = 0;
            CommonIni(Process.GetCurrentProcess().Handle, null, true);
        }

        /// <summary>
        /// Initalize the library with an arbitrary handle, database search path and specificy if the library loads symbols for the caller
        /// </summary>
        /// <param name="NativeHandle">if debugging a process and this is a process handle, Use Handle to process being debugged</param>
        /// <param name="SearchPath">Specifc a search path. Use ; to seperate individual folders or simple pass null to revert to default</param>
        /// <param name="InvadeCaller">if set, this tells the library to load symbols for (your) program -- the caller</param>
        public DebugHelpLibrary(IntPtr NativeHandle, string SearchPath, bool InvadeCaller)
        {
            SyncAcess = 0;
            CommonIni(NativeHandle, SearchPath, InvadeCaller);
        }

        /*
         * BOOL IMAGEAPI SymInitialize(
  HANDLE hProcess,
  PCSTR  UserSearchPath,
  BOOL   fInvadeProcess
);
         */
        [DllImport("Dbghelp.dll", CharSet = CharSet.Ansi, SetLastError =true)]
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        // The routine takes Ansi strings.
        static extern bool SymInitialize(IntPtr Handle, [MarshalAs(UnmanagedType.LPStr)] string SearchPath, bool InvadeCaller);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments

        [DllImport("Dbghelp.dll", SetLastError =true)]
        static extern bool SymCleanup(IntPtr Handle);
    }
}
