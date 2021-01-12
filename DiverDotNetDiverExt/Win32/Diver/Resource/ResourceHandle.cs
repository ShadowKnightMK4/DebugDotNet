using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using DebugDotNet.Win32.Tools;
using DiverDotNetDiverExt.Win32.Diver.Enums;


namespace DiverDotNetDiverExt.Win32.Diver.Resource
{
    /// <summary>
    /// An instance of a Handle a DiverDLL has sent to the Debugger. We diferentiation types
    /// as theire as possibly different wiondows api routines to get verious properties of this type.
    /// </summary>
    public class ResourceHandle 
    {

        ~ResourceHandle()
        {
            bool Closed = true;
            if ( ResourceType.HasFlag(ResourceType.Process) ||
                 ResourceType.HasFlag(ResourceType.Pipe) || 
                 ResourceType.HasFlag(ResourceType.File)
                 )
            {
                CloseHandle(RawHandleBacking);
                Closed = true;
            }

            if (!Closed)
            {
                if (ResourceType.HasFlag(ResourceType.RegistryKey))
                {
                    if (RegCloseKey(RawHandleBacking) == 0)
                    {
                        Closed = true;
                    }
                }
            }
        }
         
        /// <summary>
        /// For files, and things makable by CreateFile() and its get, returns the object's location
        /// </summary>
        /// <returns>For File System Based resources this returns the win32 file path to it. ## for Processes this returns the path to the starting exe's filename + location</returns>
        public string GetResourceName()
        {
            if (ResourceType.HasFlag(ResourceType.File))
            {
                return UnmangedToolKit.GetFinalPathNameByHandle(RawHandle, DebugDotNet.Win32.Enums.FinalFilePathFlags.FileNameNormalized);
            }
            
            if (ResourceType.HasFlag(ResourceType.Process))
            {
                return UnmangedToolKit.GetProcessNameByHandle(RawHandle);
            }

            return null;

        }
        public ResourceHandle(IntPtr SourceProcess, IntPtr Handle, ResourceType Type)
        {
            RawHandleBacking = Marshal.AllocHGlobal(4);
            if (DuplicateHandle(SourceProcess, Handle, Process.GetCurrentProcess().Handle, RawHandleBacking, 1, true, 2)) 
            {

            }
            else
            {
                throw new ArgumentException("Could not duplicate the remote handle into the process.", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            ResourceType = Type;
        }
        /// <summary>
        /// The raw handle for this type.
        /// </summary>
        protected IntPtr RawHandle  => RawHandleBacking;

        private IntPtr RawHandleBacking;

        /// <summary>
        /// Contains the resource type
        /// </summary>
        protected ResourceType ResourceType { get; set; }

 

        [DllImport("kernel32.dll", SetLastError =true)]
         static extern bool DuplicateHandle(IntPtr SourceHandleProcessHandle, IntPtr SourceHandle, IntPtr TargetProcessHandle, IntPtr PtrToTargetHandle, ulong dwAcessFlags, bool InheritHandle, uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("Advapi32.dll")]
        static extern int RegCloseKey(IntPtr Key);

    }



}
