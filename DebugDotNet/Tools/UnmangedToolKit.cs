using System;using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using DebugDotNet.Win32.Internal;
using System.Globalization;
using System.Runtime.CompilerServices;
using DebugDotNet.Win32.Enums;

namespace DebugDotNet.Win32.Tools
{


    /// <summary>
    /// If its something to easy use with memory pointers and not related to an imported routine
    /// it goes here
    /// </summary>
    public static class UnmangedToolKit
    {



        /// <summary>
        /// Readies a StartInfo.Enviroment dictioanry to become a series of name=value;name=value
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception> happens when startinfo is null
        /// <returns>the result of this (ex.   "name1=value1;name2=value2"</returns>
        public static string EnviromentToStringReady(ProcessStartInfo StartInfo)
        {
            if (StartInfo == null)
            {
                throw new ArgumentNullException(nameof(StartInfo));
            }
            if (StartInfo.Environment.Keys.Count == 0)
            {
                return string.Empty;
            }
            else
            {

                StringBuilder ret = new StringBuilder();
                foreach (string key in StartInfo.Environment.Keys)
                {
                    ret.AppendFormat(CultureInfo.InvariantCulture, "{0}={1};", key, StartInfo.Environment[key]);
                }
                return ret.ToString();
            }
        }

        /// <summary>
        /// Get a process's name from an open handle to it.
        /// </summary>
        /// <param name="hProcess"></param>
        /// <returns>returms main modiule / starting process </returns>
        public static string GetProcessNameByHandle(IntPtr hProcess)
        {
            return NativeMethods.GetProcessNameByName(hProcess);
        }

        /// <summary>
        /// Implemented for <see cref="NativeMethods.GetFinalPathNameByHandle(IntPtr, FinalFilePathFlags)"/>. Moved here from NativeMethods.  Calls the Win32Api GetFinalPathByHandle()
        /// </summary>
        /// <param name="hFile">Raw Win32 to get file name from</param>
        /// <param name="Flags">flags that specifiy how GetFinalPathNameByHandle will function</param>
        /// <returns></returns>
        /// <remarks>This was moved in the source from the <see cref="NativeMethods"/> to here</remarks>
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
                    Result = (int)NativeMethods.GetFinalPathNameByHandleW(hFile, UnmanagedBlock, (UInt32)BlockSize.ToInt32(), (uint)Flags);
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

        /// <summary>
        /// Write a struct to a remote location of a certain type.
        /// </summary>
        /// <param name="ProcessHandle"></param>
        /// <param name="TargetAddress"></param>
        /// <param name="Struct"></param>
        /// <param name="StructType"></param>
        /// <remarks>From a certain point of view, this marshals the passed object to an umanaged buffer and then writes to th passed Process specified</remarks>
        public static unsafe void RemoteWriteToStructure(IntPtr ProcessHandle, IntPtr TargetAddress, object Struct, Type StructType=null)
        {
            IntPtr LocalAddress = IntPtr.Zero;
            int LocalSize;
            try
            {
                LocalSize = Marshal.SizeOf(Struct);
                LocalAddress = Marshal.AllocHGlobal(LocalSize);
                Marshal.StructureToPtr(Struct, LocalAddress, true);

                if (NativeMethods.WriteProcessMemory(ProcessHandle, TargetAddress, LocalAddress, LocalSize, out IntPtr BytesWrote) == false)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                if (LocalAddress != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(LocalAddress);
                }
            }

        }

        /// <summary>
        /// Read a block of memory from a remote process and return it as an instanced version of the target type if possible.
        /// </summary>
        /// <param name="ProcessHandle">The Win32 handle of the process</param>
        /// <param name="TargetAddress">target address to read from in the process.</param>
        /// <param name="StructType">The type of structure to instance. </param>
        /// <returns>an instance of the object if OK </returns>
        /// <exception cref="Win32Exception"> May occur if the remote memory can't be read for some reason</exception>
        /// <exception cref="InvalidOperationException">If the block of data can't be Marshalled. This exception may occur. Notice the </exception>
        /// <remarks>Any pointers to memory contained in the final structure will still be in the context the target process. You will likely experience FATAL EXCEPTIONS when marshaling remote strings that are in structs as pointers with this routine </remarks>
        public static unsafe object RemoteReadToStructure(IntPtr ProcessHandle, IntPtr TargetAddress, Type StructType)
        {
            IntPtr LocalAddress = IntPtr.Zero;
            int Size;
            IntPtr BytesRead;
            object ret;
            try
            {

                
                Size = Marshal.SizeOf(StructType);
                LocalAddress = Marshal.AllocHGlobal(Size);

                Memset((byte*)LocalAddress.ToPointer(), 0, (uint)Size);
                if (NativeMethods.ReadProcessMemory(ProcessHandle, TargetAddress, LocalAddress, Size, out BytesRead) == true)
                {
                    try
                    {
                        ret = Marshal.PtrToStructure(LocalAddress, StructType);
                    }
                    catch (Exception e)
                    {
                        ret = null;
                        throw new InvalidOperationException("Bad Marshalling", e);
                    }
                    return ret;
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }


            }
            finally
            {
                if (LocalAddress != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(LocalAddress);
                }
            }

        }

        /// <summary>
        /// Helper routine to translate a <see cref="ushort"/> variable to a true or false
        /// </summary>
        /// <param name="s">the value to check</param>
        /// <returns>true if s is not equal to zero. Returns false if s is equal to zero.</returns>
        public static bool UShortToBool(ushort s)
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
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (path.StartsWith("\\\\?\\", StringComparison.InvariantCulture) == true)
            {
                return path.Substring(4, path.Length - 4);
            }
            return path;
        }

        /// <summary>
        /// from pointer to string
        /// </summary>
        /// <param name="Base">pointer to the first character in the string</param>
        /// <param name="Size">Number of (ANSI bytes or UNICODE chars) to read in the string. set to zero to read until first null char</param>
        /// <param name="Unicode">tells if this pointer points to a Unicode string</param>
        /// <returns>returns a c# string that Base pointed to</returns>
        private static string ExtractLocalString(IntPtr Base, int Size, bool Unicode)
        {
            string ret;
            if ((Base == null) || (Base.ToInt32() == 0))
            {
                throw new ArgumentNullException(nameof(Base));
            }
            else
            {
                if (Size == 0)
                {
                    return ExtractLocalString(Base, Unicode);
                }
                else
                {
                    if (Unicode)
                    {
                        return Marshal.PtrToStringUni(Base, Size / 2);
                    }
                    else
                    {
                        return Marshal.PtrToStringAnsi(Base, Size);
                    }
                }
            }

        }

        
        /// <summary>
        /// Marshal either a Unicode or ANSI string in the local process memory to a managed string
        /// </summary>
        /// <param name="BaseAddress">memory block</param>   
        /// <param name="Unicode">set to non zero for Unicode string. Zero or ANSI</param>
        /// <returns>a managed Unicode string</returns>

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
        /// <param name="Unicode">True if Unicode otherwise false</param>
        /// <exception cref="InvalidOperationException"> can happen if the process is already quit. </exception>
        /// <returns>the read string if it worked</returns>
        public static unsafe string ExtractString(int TargetProcessId, IntPtr BaseAddress, IntPtr size, bool Unicode)
        {
            using (Process Target = Process.GetProcessById(TargetProcessId))
            {
                if (Target.HasExited)
                {
                    return null;
                }
                return ExtractString(Target.Handle, BaseAddress, size, Unicode);
            }
        }
        /// <summary>
        /// Extract a string from either self or a remote process specifying its length
        /// </summary>
        /// <param name="TargetProcess">Process to read from. Passing <seealso cref="Process.GetCurrentProcess()"/>.Handle is equal to calling <see cref="ExtractLocalString(IntPtr, int, bool)"/>  </param>
        /// <param name="BaseAddress">Address read from </param>
        /// <param name="size">size (may be zero for whole thing)</param>
        /// <param name="Unicode">non zero for Unicode</param>
        /// <returns>tring that was read read struct</returns>
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
                        // c# chars are 2 byte uncicode chars so the extra + should contain the null
                        LocalUnmanged = Marshal.AllocHGlobal(AllotSize + sizeof(char));
                        Memset((byte*)LocalUnmanged.ToPointer(), 0, (uint)AllotSize + sizeof(char));
                    }
                    else
                    {
                        AllotSize = size.ToInt32();
                        LocalUnmanged = Marshal.AllocHGlobal(AllotSize + 1);
                        Memset((byte*)LocalUnmanged.ToPointer(), 0, (uint)AllotSize +1);
                    }
  
                    

                    if (NativeMethods.ReadProcessMemory(TargetProcess, BaseAddress, LocalUnmanged, AllotSize, out IntPtr BytesRead) == true)
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
        /// Write a collection of Bytes to the Remote Process
        /// </summary>
        /// <param name="ProcessTarget">Raw Win32 Handle of the process</param>
        /// <param name="ProcessAddressLocation">Virtual Memoery Address in the ProcessTarget to write too</param>
        /// <param name="WriteThese">buffer of bytes to write</param>
        /// <param name="BytesWritten">contains number of bytes written</param>
        /// <exception cref="ArgumentException"> Is thrown if ProcessTarget equals <see cref="Win32DebugApi.InvalidHandleValue"/></exception>
        /// <exception cref="ArgumentNullException">Is thrown if you pass Null in ProcesAddressLocation or WriteThese</exception> 
        /// <exception cref="Win32Exception">Is thrown if the underlying call to the Native routine WriteProcessMemory that's wrapped <see cref="WriteProcessMemory(IntPtr, IntPtr, byte[], out IntPtr, bool)"/> </exception>
        public unsafe static void WriteProcessMemory(IntPtr ProcessTarget, IntPtr ProcessAddressLocation, byte[] WriteThese, out IntPtr BytesWritten)
        {
            if  ( (WriteThese == null) || (ProcessAddressLocation == IntPtr.Zero) || (ProcessAddressLocation == null))
            {
                throw new ArgumentNullException(nameof(WriteThese));
            }
            WriteProcessMemory(ProcessTarget, ProcessAddressLocation, WriteThese, out BytesWritten, true);
        }

        /// <summary>
        /// Write a 4 byte value to a remote process
        /// </summary>
        /// <param name="ProcessTarget">Handle to the process to write too</param>
        /// <param name="ProcessAddressLocation">pointer in the address space of the process to write too</param>
        /// <param name="Value">value to write</param>
        /// <remarks>
        ///     Remember, this is writing to pontially a different process that your currently executing process. Be carefull to not use a pointer whose context is your process in writing to the remote one. You may accidently (or not) crash it.
        /// </remarks>
        public unsafe static void WriteDWORD(IntPtr ProcessTarget, IntPtr ProcessAddressLocation, uint Value )
        {
            
            IntPtr _;
            byte[] Buff;
            Buff = BitConverter.GetBytes(Value);
            WriteProcessMemory(ProcessTarget, ProcessAddressLocation, Buff, out _);
        }
        /// <summary>
        /// Prive C# Wrapper for the Windows API of WriteProcessMemory.
        /// </summary>
        /// <param name="ProcessHandle">Win32 API Handle of the rpcoess to write too</param>
        /// <param name="ProcessAddressLocation">Virtual Memory address in ProcessHandle's menoery space to write to. </param>
        /// <param name="WhatToWrite">buffer of bytes to deposit there.</param>
        /// <param name="BytesWritten">will contain how many bytes written</param>
        /// <param name="ErrorThrow">Do you wish for an exception to be generated on Failure?</param>
        /// <exception cref="ArgumentException"> Is generated if ProcessHandle equals <see cref="Win32DebugApi.InvalidHandleValue"/> if ErrorThrow is set</exception>
        /// <exception cref="Win32Exception"> Is thrown if the called failed and ErrorThrow is set. Will contain message on error triggerd thanks to <see cref="Marshal.GetLastWin32Error"/> and <see cref="Win32Exception"/> contructor</exception>
        /// <returns>returns true if call worked, and false if it did not</returns>
        private unsafe static bool WriteProcessMemory(IntPtr ProcessHandle, IntPtr ProcessAddressLocation, byte[] WhatToWrite, out IntPtr BytesWritten, bool ErrorThrow)
        {
            bool Result;

            if (ProcessHandle == Win32DebugApi.InvalidHandleValue)
            {
                if (ErrorThrow)
                {
                    throw new ArgumentException(nameof(ProcessHandle) + " was the InvalidHandleValue. Not supported");
                }
                BytesWritten = IntPtr.Zero;
                return false;
            }
            fixed (byte* Ptr = WhatToWrite)
            {
                Result = NativeMethods.WriteProcessMemory(ProcessHandle, ProcessAddressLocation, (IntPtr)Ptr, WhatToWrite.Length, out BytesWritten);
            }

            if (!Result )
            {

                if (ErrorThrow)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// public caller access o read a process's memory. This version does not throw exceptions
        /// </summary>
        /// <param name="ProcessHandle">MSDN Win32 HANDLE for the process</param>
        /// <param name="ProcessAddressLocation">target to read in process</param>
        /// <param name="ret">will contain byte array of read</param>
        /// <param name="Size">how many bytes to read</param>
        /// <param name="SizeRead">will contain how many bytes read</param>
        /// <returns>true if it worked and throws exception if it did not</returns>
        public unsafe static bool ReadProcessMemoryNoThrow(IntPtr ProcessHandle, IntPtr ProcessAddressLocation, out byte[] ret, int Size, out int SizeRead)
        {
            return ReadProcessMemory(ProcessHandle, ProcessAddressLocation, out ret, Size, out SizeRead, true);
        }

            /// <summary>
            /// public caller access o read a process's memory
            /// </summary>
            /// <param name="ProcessHandle"></param>
            /// <param name="ProcessAddressLocation"></param>
            /// <param name="ret"></param>
            /// <param name="Size"></param>
            /// <param name="SizeRead"></param>
            /// <exception cref="ArgumentException">Occurs when ProcesHandle is invalid or ProcessAddressLocation is 0</exception>
            /// <exception cref="Win32Exception">Should the call false for some reason, this is thrown with the faul data</exception>
            /// <returns>true if it worked and throws exception if it did not</returns>
            /// <exception cref="ArgumentException">Occurs when ProcesHandle is invalid or ProcessAddressLocation is 0</exception>
            /// <exception cref="Win32Exception">Should the call false for some reason, this is thrown with the faul data</exception>
            public unsafe static void ReadProcessMemory(IntPtr ProcessHandle, IntPtr ProcessAddressLocation, out byte[] ret, int Size, out int SizeRead)
        {
            ReadProcessMemory(ProcessHandle, ProcessAddressLocation, out ret, Size, out SizeRead, true);
        }

        /// <summary>
        ///  versionf of internal import of ReadProcessMemory at MSDN
        /// </summary>
        /// <param name="ProcessHandle">The Win32 Handle of the Process to Read</param>
        /// <param name="ProcessAddressLocation">Address to Read from in the Process's memory</param>
        /// <param name="ret">will contain the results of the read in byte form</param>
        /// <param name="Size">specifies how many bytes to read</param>
        /// <param name="SizeRead">will contain how many bytes read (or just heck ret.Count) when done</param>
        /// <param name="ErrorThrow">if set and the routine would return false, its throws an error instread</param>
        /// <exception cref="ArgumentException">Occurs when ProcesHandle is invalid or ProcessAddressLocation is 0</exception>
        /// <exception cref="Win32Exception">Should the call false for some reason, this is thrown with the faul data</exception>
        /// <returns></returns>
        private unsafe static bool ReadProcessMemory(IntPtr ProcessHandle, IntPtr ProcessAddressLocation, out byte[] ret, int Size, out int SizeRead, bool ErrorThrow=false)
        {
            bool result;
            if (ProcessHandle == Win32DebugApi.InvalidHandleValue)
            {
                if (ErrorThrow)
                {
                    throw new ArgumentException(nameof(ProcessHandle) + " was the InvalidHandleValue. Not supported");
                }
                ret = null;
                SizeRead = 0;
                return false;
            }
            else
            {
               if (Size > 0)
                {
                    ret = new byte[Size];
                    IntPtr Bytesread = IntPtr.Zero;
                    fixed (byte* tmp = ret)
                    {
                        result = NativeMethods.ReadProcessMemory(ProcessHandle, ProcessAddressLocation, (IntPtr)tmp , Size, out Bytesread);
                        SizeRead = Bytesread.ToInt32();
                    }
                    if ( (ErrorThrow) && (!result))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    return result;
                }
               else
                {
                    if (ErrorThrow)
                    {
                        throw new ArgumentException(nameof(ProcessHandle) + " was the InvalidHandleValue. Not supported");
                    }
                    ret = null;
                    SizeRead = 0;
                    return false;
                }

            }
        }

        /// <summary>
        /// Zero a range of memory
        /// </summary>
        /// <param name="Target">Memory location in the calling process to Zero.</param>
        /// <param name="NumberOfBytes">number of bytes to set to zero.</param>
        public static unsafe void ZeroMemory(void *Target, int NumberOfBytes)
        {
            Memset((byte*)Target, 0, (uint)NumberOfBytes);
        }
        /// <summary>
        /// zero a range of values
        /// </summary>
        /// <param name="Target">Memory location in the calling process to Zero.</param>
        /// <param name="len">number of bytes to set to zero.</param>
        public static unsafe void ZeroMemory(IntPtr Target, uint len)
        {
            Memset((byte*)Target.ToPointer(), 0, (uint)len);
        }
        /// <summary>
        /// Set an Umanaged Memory Block of size len to a specific range of values
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
