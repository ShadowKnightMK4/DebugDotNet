using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DiverDotNetDiverExt.Win32.Diver.Internal
{

    /*
     *          DiverReader Gets notified of a resource handle being opened when the RaiseException() based function RaiseInformResource()
     *          
     *          
     *          The DiverDLL is generated to include a context constant that tells what type of resource it 's sended to the debugger
     *          
     *          The DiverReader Debugger Duplicates the Handle and gives itself Read and Write Access
     */
    /*
     * BOOL DuplicateHandle(
  HANDLE   hSourceProcessHandle,
  HANDLE   hSourceHandle,
  HANDLE   hTargetProcessHandle,
  LPHANDLE lpTargetHandle,
  DWORD    dwDesiredAccess,
  BOOL     bInheritHandle,
  DWORD    dwOptions
);
     */
    /// <summary>
    /// Native Imports to make DiverReader to Work
    /// </summary>
    internal class DiverImports
    {

        /// <summary>
        /// Used when receiving a Handle to a resource. This duplicates the handle into the Debugging Program to allow access to it.
        /// </summary>
        /// <param name="SourceProcess">The Process the Handle is sourced from</param>
        /// <param name="SourceHandle">The Handle to duplicate in the process of the Source Process</param>
        /// <param name="IsSocket">tells the routine if it's a WinSocket handle which requires a different way to duplicate </param>
        /// <returns>returns IntPtr.Zero on failure or a Raw Win32 Handle on OK</returns>
        public static IntPtr DuplicateHandleToSelf(IntPtr SourceProcess, IntPtr SourceHandle, bool IsSocket=false)
        {
            IntPtr ret = IntPtr.Zero;
            IntPtr Self;
            IntPtr HandlePtr = IntPtr.Zero;
            Self = Process.GetCurrentProcess().Handle;
            
            if (IsSocket)
            {
                throw new NotImplementedException("Socket Duplication not done yet");
            }
            else
            {

                try
                {
                    HandlePtr = Marshal.AllocHGlobal(sizeof(ulong));

                    if (DuplicateHandle(SourceProcess, SourceHandle, HandlePtr, ret, false, 0))
                    {
                        ret = Marshal.ReadIntPtr(HandlePtr);
                        return ret;
                    }
                    else
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                finally
                {
                    if (HandlePtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(HandlePtr);
                    }
                }
            }

        }
        [DllImport("kernel32.dll",SetLastError =true)]
        static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
                                                  IntPtr hSourceHandle,
                                                  IntPtr hTargetProcessHandle,
                                                  IntPtr TargetHandle,
                                                  bool bInheritHandle,
                                                  ulong dwOptions);
    }
}
