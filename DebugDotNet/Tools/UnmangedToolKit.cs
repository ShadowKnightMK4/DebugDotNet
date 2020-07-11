using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace DebugDotNet.Win32.Tools
{


    /// <summary>
    /// If its something to easy use with memory pointers and not related to an imported routine
    /// it goes here
    /// </summary>
    public static class UnmangedToolKit
    {

        /// <summary>
        /// making a ushort as a true false
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public   static bool UShortToBool(ushort s)
        {
            if (s != 0)
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// If the passed path has "\\\\?\\" as a prefix part, remove it
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string TrimPathProcessingConst(string path)
        {
            if (path  == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (path.StartsWith("\\\\?\\", StringComparison.InvariantCulture) == true)
            {
                return path.Substring(4, path.Length - 5);
            }/*
            if (path.Length > 3)
            {
                if (path[0] == '\\')
                {
                    if (path[1] == '\\')
                    {
                        if (path[2] == '?')
                        {
                            if (path[3] == '\\')
                            {
                                return path.Substring(4, path.Length - 5);
                            }
                        }
                    }
                }
            }*/
            return path;
        }

        /// <summary>
        /// from pointer to string
        /// </summary>
        /// <param name="Base">poitner to the first cfharin the struct</param>
        /// <param name="NotUsed">hold over from when this specified strength length.</param>
        /// <param name="Unicode">tells if this pointer points to a unicode string</param>
        /// <returns></returns>
        private static string ExtractLocalString(IntPtr Base, int NotUsed, bool Unicode)
        {
            return ExtractLocalString(Base, Unicode);
        }

        /// <summary>
        /// Marshal either a unicode or ansi string in the local process memory to a managed string
        /// </summary>
        /// <param name="BaseAddress">memory block</param>   
        /// <param name="Unicode">set to non zero for unicode string. Zero or ansi</param>
        /// <returns>a managed unicode string</returns>
        
        private static unsafe string ExtractLocalString(IntPtr BaseAddress, bool Unicode)
        {
            string ret;

            // we are reading from self.
            if ((BaseAddress == null) || (BaseAddress.ToInt32() == 0))
            {
                throw new ArgumentNullException(nameof(BaseAddress));
            }
            // so I says 'self?'. Yes I do

            if (Unicode)
            {
                  ret = Marshal.PtrToStringUni(BaseAddress);
            }
            else
            {
                    ret = Marshal.PtrToStringAnsi(BaseAddress);
            }
            return ret;
            
        }

        /// <summary>
        /// Wraps Process.GetProcessByID() into a call read a string from the specified target process
        /// </summary>
        /// <param name="TargetProcessId">specifies ID of the process to read from</param>
        /// <param name="BaseAddress">base address in the TARGET's Virtual Address Space</param>
        /// <param name="size">not used. ignored.</param>
        /// <param name="Unicode">True if unicode otherwise false</param>
        /// <returns>the read string if it worked</returns>
        public static unsafe string ExtractString(int TargetProcessId, IntPtr BaseAddress, IntPtr size, bool Unicode)
        {
            using (Process Target = Process.GetProcessById(TargetProcessId))
            {
                return ExtractString(Target.Handle, BaseAddress, size, Unicode);
            }
        }
        /// <summary>
        /// Extract a string from either self or a remote process specifying its length
        /// </summary>
        /// <param name="TargetProcess">Process to read from. Using Process.GetCurrentProcess.Handle is the same as ExtractLocalString()</param>
        /// <param name="BaseAddress">Address to check</param>
        /// <param name="size">size (may be zero for whole thing)</param>
        /// <param name="Unicode">non zero for unicode</param>
        /// <returns>tghe read struct</returns>
        public static unsafe string ExtractString(IntPtr TargetProcess, IntPtr BaseAddress, IntPtr size, bool Unicode)
        {
            string ret;
            IntPtr LocalUnmanged = IntPtr.Zero;
            try
            {
                if (TargetProcess == Process.GetCurrentProcess().Handle)
                {
                    ret = ExtractLocalString(BaseAddress, size.ToInt32(), Unicode);
                }
                else
                {
                    int AllotSize;
                    if (Unicode.Equals(0) == false)
                    {
                        AllotSize = size.ToInt32() * sizeof(char);
                    }
                    else
                    {
                        AllotSize = size.ToInt32();
                    }
                    LocalUnmanged = Marshal.AllocHGlobal(AllotSize);
                    Memset((byte*)LocalUnmanged.ToPointer(), 0, (uint)AllotSize);

                    IntPtr BytesRead;
                    if (NativeMethods.ReadProcessMemory(TargetProcess, BaseAddress, LocalUnmanged, AllotSize, out BytesRead) == true)
                    {
                        ret = ExtractLocalString(LocalUnmanged, BytesRead.ToInt32(), Unicode);
                    }
                    else
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                return ret;
            }
            finally
            {
                if (LocalUnmanged != IntPtr.Zero)
                    Marshal.FreeHGlobal(LocalUnmanged);
            }
        }


        /// <summary>
        /// Zero a range of memory
        /// </summary>
        /// <param name="Target">memory location</param>
        /// <param name="NumberOfBytes">number of bytes</param>
        public static unsafe void ZeroMemory(void *Target, int NumberOfBytes)
        {
            Memset((byte*)Target, 0, (uint)NumberOfBytes);
        }
        /// <summary>
        /// zero a reange of values
        /// </summary>
        /// <param name="target"></param>
        /// <param name="len"></param>
        public static unsafe void ZeroMemory(IntPtr target, uint len)
        {
            Memset((byte*)target.ToPointer(), 0, (uint)len);
        }
        /// <summary>
        /// Set memory to specific range of values.
        /// </summary>
        /// <param name="target">target based address in the current procecess's Virtual Memory</param>
        /// <param name="c">char to fill it with</param>
        /// <param name="len">how many bytyes to fill</param>
        public static unsafe void Memset(byte* target, byte c, uint len)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (len == 0)
                return; // set nothing
            else
                while (len > 0)
                {
                    *target = c;
                    len--;
                }
        }
    }
}
