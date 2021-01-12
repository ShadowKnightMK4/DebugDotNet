using System;
using System.Collections.Generic;
using System.Text;
using DebugDotNet.Win32.Internal;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DebugDotNet.Win32.Tools;
using DebugDotNet.Win32.Enums;
using System.Globalization;
using System.Reflection;
using DebugDotNet.Win32.Structs;

namespace DebugDotNet.NativeHelpers.Detours
{
    /// <summary>
    /// Detours Library Wrappers
    /// </summary>
    public static class DetoursWrappers
    {
      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="TargetApplication">CreateProcess's lpApplicationName</param>
        /// <param name="Arguments">CreateProcess's lpArguments</param>
        /// <param name="ProcessAttributes">Security Description for the Process.</param>
        /// <param name="ThreadAttributes">Security Description for the Thread</param>
        /// <param name="InheritHandles">will this process inherit your handles?</param>
        /// <param name="ProcessCreationFlags">Creation Attributes. Note Detours Automatically adds CREATE_SUSPENDED and resumes it after it does its thing if you don't specify CREATE_SUSPENDED explicitly </param>
        /// <param name="Environment">The process's new environment. Pass string.empty or null to inherit yours</param>
        /// <param name="StartDirectory">The process's initial directory. Inherits yours if null or empty</param>
        /// <param name="lpStartupInfo">pointer to a start info structure. You can use <see cref="StartupInfo"/></param> if you like or make your own. See MSDN for how to fill this out
        /// <param name="ProcessInfoResult">Receives Win32 Handles and IDs for both the process and the Main Thread if successful.</param> You'll need to close them yourself when finished
        /// <param name="ForceLoadDlls">!IMPORTANT. This is a list of DLLs to force the process to load. Requires the DLL to link to Detours (or use build it a static C/C++ lib as a static) and call <seealso href="https://github.com/microsoft/Detours/wiki/DetourRestoreAfterWith">DetourRestoreAfterWith()</seealso> if <seealso href="https://github.com/microsoft/Detours/wiki/DetourIsHelperProcess">DetourIsHelperProcess()</seealso> returns false.  This c# routine is a wrapper for the Unicode <seealso href="https://github.com/Microsoft/Detours/wiki/DetourCreateProcessWithDllEx">DetourCreateProcessWithDllExW()</seealso>  If you do not want to force a DLL to be loaded, leave this null or with empty to not force any DLLs to be loaded.</param>
        /// <exception cref="OutOfMemoryException"> can happen if the wrapper can't allocate memory for the call</exception>
        /// <returns>Returns True if it Worked OK and False if it failed. Call <see cref="Marshal.GetLastWin32Error()"/> to see why the underlying CreateProcessW call did not work</returns>
        public static bool DetourCreateProcessWithDllEx(string TargetApplication,
            string Arguments,
            SecurityAttributes ProcessAttributes,
            SecurityAttributes ThreadAttributes,
            bool InheritHandles,
            CreateFlags ProcessCreationFlags,
            string Environment,
            string StartDirectory,
            [In] ref StartupInfo lpStartupInfo,
            out ProcessInformation ProcessInfoResult,
            List<string> ForceLoadDlls)
        {
            bool result = false;
            int MidArg;
            string[] MidArr;
            // points to Process Security, Thread Security and Environment
            IntPtr PAttrib_Ptr, TAttrib_Ptr, Env_Ptr;
            PAttrib_Ptr = TAttrib_Ptr  = IntPtr.Zero;

            if (ForceLoadDlls == null)
            {
                MidArg = 0;
                MidArr = null;
            }
            else
            {
                MidArg = ForceLoadDlls.Count;
                MidArr = ForceLoadDlls.ToArray();
            }
            if (string.IsNullOrEmpty(Environment))
            {
                Env_Ptr = IntPtr.Zero;
            }
            else
            {
                Env_Ptr = Marshal.StringToHGlobalUni(Environment);
            }
            try
            {
                
                PAttrib_Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SecurityAttributes)));
                TAttrib_Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SecurityAttributes)));
                // Out of memory exception should trigger if the StringToHGlobalUni() fails to allocate memory. That's the reason it's not checked here.

                if (PAttrib_Ptr == null)
                {
                    throw new OutOfMemoryException(string.Format(CultureInfo.InvariantCulture, StringMessages.DetourMarshalCreateProcessWithDll_OutOfMemory, "Process"));
                }
                if (TAttrib_Ptr == null)
                {
                    throw new OutOfMemoryException(string.Format(CultureInfo.InvariantCulture, StringMessages.DetourMarshalCreateProcessWithDll_OutOfMemory, "Process"));
                }

                Marshal.StructureToPtr(ProcessAttributes, PAttrib_Ptr, false);
                Marshal.StructureToPtr(ThreadAttributes, TAttrib_Ptr, false);

                result =  NativeMethods.DetourCreateProcessWithDllsW(TargetApplication,
                                                            Arguments,
                                                            IntPtr.Zero,
                                                            IntPtr.Zero,
                                                            InheritHandles,
                                                            (uint)ProcessCreationFlags,
                                                            Env_Ptr,
                                                            StartDirectory,
                                                            ref lpStartupInfo,
                                                            out ProcessInfoResult, 
                                                            MidArg,
                                                            MidArr,
                                                            IntPtr.Zero);
                return result;
            }
            finally
            {
                if (Env_Ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(Env_Ptr);
                }
                if (PAttrib_Ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(PAttrib_Ptr);
                }
                if (TAttrib_Ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(TAttrib_Ptr);
                }
                if (Env_Ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(Env_Ptr);
                }
            }
            
        }
    }

}
    namespace DebugDotNet.Win32.Internal
{ 
    internal static partial class  NativeMethods
    {
        [DllImport("detours.dll", SetLastError =true, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DetourCreateProcessWithDllsW(
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
[In] ref StartupInfo lpStartupInfo,
out ProcessInformation lpProcessInformation,
int NumberOfDlls,

string[] LoadDlls,
IntPtr ReplacementCreateProcessW);
    }
}
