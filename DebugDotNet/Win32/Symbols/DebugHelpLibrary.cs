using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDotNet.Win32.Symbols
{


    /// <summary>
    /// This is the layout for getting DebugHelp version info or telling the library what version you expect.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DebugHelpVersion : IEquatable<DebugHelpVersion>
    {
        /// <summary>
        /// The Major Version either implemented by DebugHelp or the one your software expects
        /// </summary>
        public uint MajorVersion { get; set; }
        /// <summary>
        /// The Minor Version either implemented by DebugHelp or the one your software expects
        /// </summary>
        public uint MinorVersion { get; set; }
        /// <summary>
        /// Revision Version  either implemented by DebugHelp or the one your software expects
        /// </summary>
        public uint Revision { get; set; }
        /// <summary>
        /// Reserved for use by OS, DebugDotNet just copies the value unchanged to there
        /// </summary>
        public uint Reserved { get; set; }

        /// <summary>
        /// Get a string that contained <see cref="MajorVersion"/>, <see cref="MinorVersion"/> and <see cref="Revision"/>
        /// </summary>
        /// <returns>Return a string that contained <see cref="MajorVersion"/>, <see cref="MinorVersion"/> and <see cref="Revision"/></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{ Major {0}, Minor {1}, Revision {2} }}", MajorVersion, MinorVersion, Revision);
        }

        /// <summary>
        /// Get a string that's shorter than the <see cref="ToString"/>
        /// </summary>
        /// <returns>returns a shorter string</returns>
        /// <example>
        ///     It could return 4.5.0 if that's the Major, minor and revision version installed 
        /// </example>
        public string ToStringShort()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", MajorVersion, MinorVersion, Revision);
        }
        /// <summary>
        /// Compare if an object and a <see cref="DebugHelpVersion"/> object are equal
        /// </summary>
        /// <param name="obj">check this one</param>
        /// <returns>returns true if equal</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj is DebugHelpVersion)
                {
                    return Equals((DebugHelpVersion)obj);
                }
                return false;
            }
        }

        /// <summary>
        /// get a hash code of the members of this structure
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return MajorVersion.GetHashCode() + MinorVersion.GetHashCode() + Reserved.GetHashCode() + Revision.GetHashCode();
        }

        /// <summary>
        /// compare left and right if they are equal
        /// </summary>
        /// <param name="left">left side of compare</param>
        /// <param name="right">right side of compare</param>
        /// <returns></returns>
        public static bool operator ==(DebugHelpVersion left, DebugHelpVersion right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// compare left and right to see if they are different
        /// </summary>
        /// <param name="left">left side of compare</param>
        /// <param name="right">right side of compare</param>
        /// <returns></returns>
        public static bool operator !=(DebugHelpVersion left, DebugHelpVersion right)
        {
            return !(left == right);
        }

        /// <summary>
        /// compare any two <see cref="DebugHelpVersion"/> structs
        /// </summary>
        /// <param name="other">against</param>
        /// <returns>true if equal</returns>
        public bool Equals(DebugHelpVersion other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (other.MajorVersion != MajorVersion)
                {
                    return false;
                }
                if (other.MinorVersion != MinorVersion)
                {
                    return false;
                }
                if (other.Reserved != Reserved)
                {
                    return false;
                }
                if (other.Revision != Revision)
                {
                    return false;
                }
                return true;
            }
        }
    }
    /// <summary>
    /// Dbghlp.dll symbol wrappign
    /// </summary>
    public sealed class DebugHelpLibrary: IDisposable
    {
        /// <summary>
        /// Used to sync access  calls
        /// </summary>
        static object SyncAcess;
        /// <summary>
        /// used as a handle to the process we ask the debug library about
        /// </summary>
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
        /// <param name="Target">The process to use as handle.</param>
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
        /// Initialize the library with an arbitrary handle, database search path and specificy if the library loads symbols for the caller
        /// </summary>
        /// <param name="NativeHandle">if debugging a process and this is a process handle, Use Handle to process being debugged</param>
        /// <param name="SearchPath">Specifc a search path. Use ; to seperate individual folders or simple pass null to revert to default</param>
        /// <param name="InvadeCaller">if set, this tells the library to load symbols for (your) program -- the caller</param>
        public DebugHelpLibrary(IntPtr NativeHandle, string SearchPath, bool InvadeCaller)
        {
            SyncAcess = 0;
            CommonIni(NativeHandle, SearchPath, InvadeCaller);
        }
        /// <summary>
        /// Options to Pass to the <see cref="SymSetOptions"/> public routine. This is lifted from <see href="https://docs.microsoft.com/en-us/windows/win32/api/dbghelp/nf-dbghelp-symsetoptions"/>
        /// </summary>
        [Flags]
        public enum SymbolOptions :uint
        {
            SYMOPT_ALLOW_ABSOLUTE_SYMBOLS = 0x00000800,
            SYMOPT_ALLOW_ZERO_ADDRESS = 0x01000000,
            SYMOPT_AUTO_PUBLICS = 0x00010000,
            SYMOPT_CASE_INSENSITIVE = 0x00000001,
            SYMOPT_DEBUG = 0x80000000,
            SYMOPT_DEFERRED_LOADS = 0x00000004,
            SYMOPT_DISABLE_SYMSRV_AUTODETECT = 0x02000000,
            SYMOPT_EXACT_SYMBOLS = 0x00000400,
            SYMOPT_FAIL_CRITICAL_ERRORS = 0x00000200,
            SYMOPT_FAVOR_COMPRESSED = 0x00800000,
            SYMOPT_FLAT_DIRECTORY = 0x00400000,
            SYMOPT_IGNORE_CVREC = 0x00000080,
            SYMOPT_IGNORE_IMAGEDIR = 0x00200000,
            SYMOPT_IGNORE_NT_SYMPATH = 0x00001000,
            SYMOPT_INCLUDE_32BIT_MODULES = 0x00002000,
            SYMOPT_LOAD_ANYTHING = 0x00000040,
            SYMOPT_LOAD_LINES = 0x00000010,
            SYMOPT_NO_CPP = 0x00000008,
            SYMOPT_NO_IMAGE_SEARCH = 0x00020000,
            SYMOPT_NO_PROMPTS = 0x00080000,
            SYMOPT_NO_PUBLICS = 0x00008000,
            SYMOPT_NO_UNQUALIFIED_LOADS = 0x00000100,
            SYMOPT_OVERWRITE = 0x00100000,
            /// <summary>
            /// <list type="table">
            /// Do not use private symbols. The version of DbgHelp that shipped with earlier Windows release supported only public symbols; this option provides compatibility with this limitation.
            ///  <listheader>version specifies</listheader>
            /// <item>
            ///     <term>DbgHelp 5.1</term>
            ///     <description>Is Unsupported Value</description>
            /// </item>
            /// </list>
            /// </summary>
            SYMOPT_PUBLICS_ONLY = 0x00004000,
            /// <summary>
            /// DbgHelp will not load any symbol server other than SymSrv. SymSrv will not use the downstream store specified in _NT_SYMBOL_PATH. After this flag has been set, it cannot be cleared.
            /// <list type="table">
            /// <listheader>version specifies</listheader>
            /// <item>
            ///     <term>DbgHelp 6.0 and 6.1</term>
            ///     <description>The Flag can be cleared.</description>
            /// </item>
            /// <item>
            ///     <term>DbgHelp 5.1</term>
            ///     <description>The Flag is not supported</description>
            /// </item>
            /// </list>
            /// </summary>
            SYMOPT_SECURE = 0x00040000,
            /// <summary>
            /// All symbols are presented in undecorated form. This option has no effect on global or local symbols because they are stored undecorated. This option applies only to public symbols.
            /// </summary>
            SYMOPT_UNDNAME = 0x00000002



        }

        /// <summary>
        /// call the Debug Help native ImagehlpApiVersion routine and get a struct to version data. Should the return an error your get null returned instead of the <see cref="DebugHelpVersion"/> struct
        /// </summary>
        /// <returns>returns a <see cref="DebugHelpVersion"/> on OK and null on error</returns>
        public static DebugHelpVersion? GetDebugVersion()
        {
            lock (SyncAcess)
            {
                DebugHelpVersion ret = new DebugHelpVersion();

                IntPtr Ptr = ImagehlpApiVersionImport();
                if (Ptr != IntPtr.Zero)
                {
                    ret = (DebugHelpVersion)Marshal.PtrToStructure(Ptr, typeof(DebugHelpVersion));
                    return ret;
                }
                return null;
            }
        }

        /// <summary>
        /// Tell Debug Help what version you are planning for. Calls the Native version of ImagehlpApiVersionEx.
        /// </summary>
        /// <returns>returns the version structure that ImagehlpApiVersionEx would return or null if an error happened </returns>
        public static DebugHelpVersion? SetExpectedDebugVersion(DebugHelpVersion Version)
        {
            DebugHelpVersion ret = new DebugHelpVersion();
            lock (SyncAcess)
            {
                IntPtr Ptr = ImagehlpApiVersionImportEx(Version);
                if (Ptr != IntPtr.Zero)
                {
                    Marshal.PtrToStructure(Ptr, ret);
                    return ret;
                }
                return null;
            }
        }


        /// <summary>
        /// Set the Symbol Options that Dbghlp.dll will use
        /// </summary>
        /// <param name="Options">Options Flags</param>
        /// <returns>returns previous settings</returns>
        public static SymbolOptions SymSetOptions(SymbolOptions Options)
        {
            lock (SyncAcess)
            {
                return (SymbolOptions)SymSetOptionsImport((uint)Options);
            }
        }

        /// <summary>
        /// set the search path to use for this instance of DebugHelp lib
        /// </summary>
        /// <param name="SearchPath">use </param>
        /// <returns></returns>
        public bool SymSetSearchPath(string SearchPath)
        {
            return SymSetSearchPathImport(ProcessHandle, SearchPath);
        }

        [DllImport("Dbghelp.dll", EntryPoint = "ImagehlpApiVersionEx")]
        static extern IntPtr ImagehlpApiVersionImportEx([MarshalAs(UnmanagedType.Struct)] DebugHelpVersion Expected);

        [DllImport("Dbghelp.dll", EntryPoint = "ImagehlpApiVersion")]
        static extern IntPtr ImagehlpApiVersionImport();
        /*
        BOOL IMAGEAPI SymSetSearchPath(
  HANDLE hProcess,
  PCSTR SearchPath
);*/
        [DllImport("Dbghelp.dll", EntryPoint = "SymSetSearchPathW", SetLastError =true)]
        static extern bool SymSetSearchPathImport(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string SearchPath);

        /*
        DWORD IMAGEAPI SymSetOptions(
  DWORD SymOptions
);*/
        [DllImport("Dbghelp.dll", EntryPoint ="SymSetOptions", SetLastError =true)]
        static extern UInt32 SymSetOptionsImport(UInt32 SysOptions);
        /*
         * BOOL IMAGEAPI SymInitialize(
  HANDLE hProcess,
  PCSTR  UserSearchPath,
  BOOL   fInvadeProcess
);
         */
        [DllImport("Dbghelp.dll", CharSet = CharSet.Ansi,SetLastError =true)]
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        // The routine takes Ansi strings.
        static extern bool SymInitialize(IntPtr Handle, [MarshalAs(UnmanagedType.LPStr)] string SearchPath, bool InvadeCaller);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments

        [DllImport("Dbghelp.dll", SetLastError =true)]
        static extern bool SymCleanup(IntPtr Handle);
    }
}
