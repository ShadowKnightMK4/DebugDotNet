using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Internal;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Tools;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDotNet.Win32.Structs
{
    /// <summary>
    /// Contains a single DebugEvent struct. This class is responsible for holding the byte[] that
    /// holds the DEBUG_EVENT as seen in MSDN and also making instances of Non Internal class/structs of the events structs when asked for
    /// from the particular event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DebugEvent : IEquatable<DebugEvent>
    {
        /// <summary>
        /// return a hashgcode of this class's members
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return dwDebugEventCode.GetHashCode() + dwProcessId.GetHashCode() + dwThreadId.GetHashCode();
        }


        /// <summary>
        /// contains the type of struct that this escapolates
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>")]
        public DebugEventType dwDebugEventCode;
        /// <summary>
        /// the process this event triggered from
        /// </summary>
        public int dwProcessId;
        /// <summary>
        /// the thread this event triggered from
        /// </summary>
        public int dwThreadId;






        /// <summary>
        /// Raw byte information of the event
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
        byte[] debugInfo;






        /// <summary>
        /// gets the Exception Information 
        /// </summary>
        public EXCEPTION_DEBUG_INFO Exception
        {
            get
            {
                IntPtr pointer = IntPtr.Zero;
                var structSize = Marshal.SizeOf(typeof(EXCEPTION_DEBUG_INFO_INTERNAL));

                try
                {
                    pointer = Marshal.AllocHGlobal(structSize);
                    Marshal.Copy(debugInfo, 0, pointer, structSize);
                    var midresult = (EXCEPTION_DEBUG_INFO_INTERNAL)Marshal.PtrToStructure(pointer, typeof(EXCEPTION_DEBUG_INFO_INTERNAL));
                    var result = new EXCEPTION_DEBUG_INFO
                    {
                        dwFirstChance = midresult.dwFirstChance,
                        ExceptionRecord = new EXCEPTION_RECORD()
                    };

                    {
                        if (midresult.ExceptionRecord.ExceptionFlags == (uint)ExceptionFlags.EXCEPTION_NONCONTINUABLE)
                        {
                            result.ExceptionRecord.CanContinueException = false;
                        }
                        else
                        {
                            result.ExceptionRecord.CanContinueException = true;
                        }

                        result.ExceptionRecord.ExceptionAddress = midresult.ExceptionRecord.ExceptionAddress;
                        result.ExceptionRecord.ExceptionInformation = midresult.ExceptionRecord.ExceptionInformation;
                        result.ExceptionRecord.NumberParameters = midresult.ExceptionRecord.NumberParameters;
                        result.ExceptionRecord.ExceptionMessage = string.Empty; // TODO fill this out

                    }
                    return result;
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pointer);
                    }
                }
            }
        }

        /// <summary>
        /// Get the CREATE_THREAD_DEBUG_INFO struct
        /// </summary>
        public CREATE_THREAD_DEBUG_INFO CreateThread
        {
            get { return SimpleGetStruct<CREATE_THREAD_DEBUG_INFO>(); }
        }

        /// <summary>
        /// Get the CREATE_PROCESS_DEBUG_INFO struct
        /// </summary>
        public CreateProcessDebugInfo CreateProcessInfo
        {
            get
            {
                IntPtr pointer = IntPtr.Zero;
                var structSize = Marshal.SizeOf(typeof(CREATE_PROCESS_DEBUG_INFO_INTERNAL));

                try
                {
                    pointer = Marshal.AllocHGlobal(structSize);
                    Marshal.Copy(debugInfo, 0, pointer, structSize);
                    var midresult = (CREATE_PROCESS_DEBUG_INFO_INTERNAL)Marshal.PtrToStructure(pointer, typeof(CREATE_PROCESS_DEBUG_INFO_INTERNAL));
                    var ret = new CreateProcessDebugInfo();

                    if (midresult.hFile != IntPtr.Zero)
                    {
                        ret.hFile = new SafeFileHandle(midresult.hFile, true);

                        ret.lpImageName = NativeMethods.GetFinalPathNameByHandle(midresult.hFile, FinalFilePathFlags.VOLUME_NAME_DOS);
                        ret.lpImageName = UnmangedToolKit.TrimPathProcessingConst(ret.lpImageName);
                    }
                    else
                    {
                        ret.hFile = null;
                    }


                    if (midresult.hProcess != IntPtr.Zero)
                    {
                        ret.hProcess = midresult.hProcess;
                    }
                    else
                    {
                        ret.hProcess = IntPtr.Zero;
                    }

                    ret.hThread = midresult.hThread;
                    ret.lpBaseOfImage = midresult.lpBaseOfImage;
                    ret.lpStartAddress = midresult.lpStartAddress;
                    ret.lpThreadLocalBase = midresult.lpThreadLocalBase;
                    ret.nDebugInfoSize = midresult.nDebugInfoSize;
                    ret.dwDebugInfoFileOffset = midresult.dwDebugInfoFileOffset;
                    return ret;
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pointer);
                    }
                }
            }
        }


        /// <summary>
        /// Get the ExitThread Struct
        /// </summary>
        public EXIT_THREAD_DEBUG_INFO ExitThread
        {
            get
            {
                return SimpleGetStruct<EXIT_THREAD_DEBUG_INFO>();
            }
        }

        /// <summary>
        /// Get the EXIT_PROCESS_INFO struct
        /// </summary>
        public EXIT_PROCESS_DEBUG_INFO ExitProcess
        {
            get
            {
                return SimpleGetStruct<EXIT_PROCESS_DEBUG_INFO>();
            }
        }


        /// <summary>
        /// Get the LoadDll info struct
        /// </summary>

        public LOAD_DLL_DEBUG_INFO LoadDll
        {
            get
            {
                IntPtr pointer = IntPtr.Zero;
                var structSize = Marshal.SizeOf(typeof(LOAD_DLL_DEBUG_INFO));

                try
                {
                    pointer = Marshal.AllocHGlobal(structSize);
                    Marshal.Copy(debugInfo, 0, pointer, structSize);
                    var midresult = (LOAD_DLL_DEBUG_INFO_INTERNAL)Marshal.PtrToStructure(pointer, typeof(LOAD_DLL_DEBUG_INFO_INTERNAL));
                    var result = new LOAD_DLL_DEBUG_INFO();

                    if (midresult.hFile != IntPtr.Zero)
                    {
                        result.hFile = new SafeFileHandle(midresult.hFile, true);
                        result.lpImageName = NativeMethods.GetFinalPathNameByHandle(midresult.hFile, FinalFilePathFlags.VOLUME_NAME_DOS);
                        result.WasBad = false;
                    }
                    else
                    {
                        result.hFile = null;
                        result.lpImageName = string.Empty;
                        result.WasBad = true;
                    }

                    result.lpBaseOfDll = midresult.lpBaseOfDll;
                    result.nDebugInfoSize = midresult.nDebugInfoSize;
                    result.dwDebugInfoFileOffset = midresult.dwDebugInfoFileOffset;

                    return result;
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pointer);
                    }
                }
            }
        }

        /// <summary>
        /// Get the UNLOAD_DLL_DEBUG_INFO struct
        /// </summary>
        public UNLOAD_DLL_DEBUG_INFO UnloadDll
        {
            get { return SimpleGetStruct<UNLOAD_DLL_DEBUG_INFO>(); }
        }

        /// <summary>
        /// if No special processing is needed i.e. strings ect..., the public struct properties get routed ot this.
        /// </summary>
        /// <typeparam name="T">The type of the Struct we are fetching</typeparam>
        /// <returns>a struct Marshaled from the internal buffer with the sizeof(T) </returns>
        private T SimpleGetStruct<T>() where T : struct
        {
            IntPtr pointer = IntPtr.Zero;
            var structSize = Marshal.SizeOf(typeof(T));
            try
            {
                pointer = Marshal.AllocHGlobal(structSize);
                Marshal.Copy(debugInfo, 0, pointer, structSize);
                var ret = (T)Marshal.PtrToStructure(pointer, typeof(T));
                return ret;
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pointer);
                }
            }
        }
        /// <summary>
        /// Get the DebugInfo for the struct
        /// </summary>
        public OUTPUT_DEBUG_STRING_INFO DebugString
        {

            get
            {
                IntPtr pointer = IntPtr.Zero;
                var structSize = Marshal.SizeOf(typeof(OUTPUT_DEBUG_STRING_INTERNAL));

                try
                {
                    pointer = Marshal.AllocHGlobal(structSize);
                    Marshal.Copy(debugInfo, 0, pointer, structSize);
                    // The OUTPUT_DEBUG_STRING INTERNAL is the true struct received via DEBUG_EVENT
                    // OUTPUT_DEBUG_STRING_INFO() is the friendly version we return that already has the data setup for use by the caller


                    OUTPUT_DEBUG_STRING_INTERNAL
                     midresult = (OUTPUT_DEBUG_STRING_INTERNAL)Marshal.PtrToStructure(pointer, typeof(OUTPUT_DEBUG_STRING_INTERNAL));
                    var result = new OUTPUT_DEBUG_STRING_INFO();

                    if (midresult.lpStringData != null)
                    {
                        result.lpDebugStringData = UnmangedToolKit.ExtractString(this.dwProcessId, midresult.lpStringData, (IntPtr)midresult.nDebugStringLength, UnmangedToolKit.UShortToBool(midresult.fUnicode));
                    }
                    else
                    {
                        result.lpDebugStringData = null;
                    }
                    return result;
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pointer);
                    }
                }
            }
        }



        /// <summary>
        /// Get a RIP_INFO struct
        /// </summary>
        public RipInfo RipInfo
        {
            get
            {
                IntPtr pointer; 
                var structSize = Marshal.SizeOf(typeof(RIP_INFO_INTERNAL));
                pointer = Marshal.AllocHGlobal(structSize);
                Marshal.Copy(debugInfo, 0, pointer, structSize);
                try
                {
                    RIP_INFO_INTERNAL midresult = (RIP_INFO_INTERNAL)Marshal.PtrToStructure(pointer, typeof(RIP_INFO_INTERNAL));
                    var result = new RipInfo
                    {
                        dwType = (RipInfo.ErrorType)midresult.dwType,
                        dwError = midresult.dwError
                    };
                    return result;
                }
                finally
                {
                    if (pointer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pointer);
                    }
                }
            }
        }

        /// <summary>
        /// return if the 2 objects are equal. Does not check the byte[] array and the DEBUG_EVENT properties
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            else
            {
                DebugEvent e = (DebugEvent)obj;
                if (e.dwProcessId != this.dwProcessId)
                    return false;
                if (e.dwThreadId != this.dwThreadId)
                {
                    return false;
                }
                if (e.dwDebugEventCode != this.dwDebugEventCode)
                {
                    return false;
                }
                else
                {
                    // TODO: comare the sub structs of each thing
                }
                return true;
            }
        }

        /// <summary>
        /// Are this two instances the same
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(DebugEvent left, DebugEvent right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Are this two instances not the same
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(DebugEvent left, DebugEvent right)
        {
            return !(left == right);
        }

        public bool Equals(DebugEvent other)
        {
            return other.Equals(this);
        }
    }

}
