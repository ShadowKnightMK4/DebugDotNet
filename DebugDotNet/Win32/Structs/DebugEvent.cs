using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Internal;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Tools;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static System.Globalization.CultureInfo;

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
        /// get a text friendly view.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder ret = new StringBuilder();
            ret.Append("{ DebugEvent, ");
            ret.AppendFormat(InvariantCulture, "Process Id {0}, ", dwProcessId);
            ret.AppendFormat(InvariantCulture, "Thread Id {0}, ", dwThreadId);
            ret.AppendFormat(InvariantCulture, "Event Type: {0}, ",  Enum.GetName(typeof(DebugEventType),dwDebugEventCode));
            switch (dwDebugEventCode)
            {
                case DebugEventType.CreateProcessDebugEvent:
                    ret.Append(CreateProcessInfo.ToString());
                    break;
                case DebugEventType.CreateThreadDebugEvent:
                    ret.Append(CreateThreadInfo.ToString());
                    break;
                case DebugEventType.ExceptionDebugEvent:
                    ret.Append(ExceptionInfo.ToString());
                    break;
                case DebugEventType.ExitProcessDebugEvent:
                    ret.Append(ExitProcessInfo.ToString());
                    break;
                case DebugEventType.ExitThreadDebugEvent:
                    ret.Append(ExitThreadInfo.ToString());
                    break;
                case DebugEventType.LoadDllDebugEvent:
                    ret.Append(LoadDllInfo.ToString());
                    break;
                case DebugEventType.OutputDebugStringEvent:
                    ret.Append(DebugStringInfo.ToString());
                    break;
                case DebugEventType.RipEvent:
                    ret.Append(RipInfo.ToString());
                    break;
                case DebugEventType.UnloadDllDebugEvent:
                    ret.Append(UnloadDllInfo.ToString());
                    break;
            }
            ret.Append("}");
            return ret.ToString();
        }


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
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 88, ArraySubType = UnmanagedType.U1)]
        byte[] debugInfo;






        /// <summary>
        /// gets the Exception Information 
        /// </summary>
        public ExceptionDebugInfo ExceptionInfo
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
                    var result = new ExceptionDebugInfo();

                    {
                        if (midresult.dwFirstChance != 0)
                        {
                            result.IsFirstChance = true;
                        }
                        else
                        {
                            result.IsFirstChance = false;
                        }

                        
                        ExceptionRecord StartPoint = new ExceptionRecord(midresult.ExceptionRecord);
                        result.TopLevelException = StartPoint;
                        


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
        public CreateThreadDebugInfo CreateThreadInfo
        {
            get { return SimpleGetStruct<CreateThreadDebugInfo>(); }
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

                        ret.ImageName = NativeMethods.GetFinalPathNameByHandle(midresult.hFile, FinalFilePathFlags.VolumeNameDos);
                        ret.ImageName = UnmangedToolKit.TrimPathProcessingConst(ret.ImageName);
                    }
                    else
                    {
                        ret.hFile = null;
                    }


                    if (midresult.hProcess != IntPtr.Zero)
                    {
                        ret.ProcessHandleRaw = midresult.hProcess;
                    }
                    else
                    {
                        ret.ProcessHandleRaw = IntPtr.Zero;
                    }

                    ret.ThreadHandleRaw = midresult.hThread;
                    ret.BaseOfImage = midresult.lpBaseOfImage;
                    ret.StartAddress = midresult.lpStartAddress;
                    ret.ThreadLocalBase = midresult.lpThreadLocalBase;
                    ret.DebugInfoSize = midresult.nDebugInfoSize;
                    ret.DebugInfoFileOffset = midresult.dwDebugInfoFileOffset;
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
        public ExitThreadDebugInfo ExitThreadInfo
        {
            get
            {
                return new ExitThreadDebugInfo(SimpleGetStruct<ExitThreadDebugInfo>().ExitCode);
            }
        }

        /// <summary>
        /// Get the EXIT_PROCESS_INFO struct
        /// </summary>
        public ExitProcessDebugInfo ExitProcessInfo
        {
            get
            {
                return new ExitProcessDebugInfo(SimpleGetStruct<EXIT_PROCESS_DEBUG_INFO_INTERNAL>().dwExitCode);
            }
        }


        /// <summary>
        /// Get the LoadDll info struct
        /// </summary>

        public LoadDllDebugInfo LoadDllInfo
        {
            get
            {
                IntPtr pointer = IntPtr.Zero;
                var structSize = Marshal.SizeOf(typeof(LoadDllDebugInfo));

                try
                {
                    pointer = Marshal.AllocHGlobal(structSize);
                    Marshal.Copy(debugInfo, 0, pointer, structSize);
                    var midresult = (LOAD_DLL_DEBUG_INFO_INTERNAL)Marshal.PtrToStructure(pointer, typeof(LOAD_DLL_DEBUG_INFO_INTERNAL));
                    var result = new LoadDllDebugInfo();

                    if (midresult.hFile != IntPtr.Zero)
                    {
                        result.FileHandle = new SafeFileHandle(midresult.hFile, true);
                        result.ImageName = NativeMethods.GetFinalPathNameByHandle(midresult.hFile, FinalFilePathFlags.VolumeNameDos);
                        result.WasBad = false;
                    }
                    else
                    {
                        result.FileHandle = null;
                        result.ImageName = string.Empty;
                        result.WasBad = true;
                    }

                    result.BaseDllAddress = midresult.lpBaseOfDll;
                    result.DebugInfoSize = midresult.nDebugInfoSize;
                    result.DebugInfoFileOffset = midresult.dwDebugInfoFileOffset;

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
        public UnloadDllDebugInfo UnloadDllInfo
        {
            get {
                return new UnloadDllDebugInfo(SimpleGetStruct<UNLOAD_DLL_DEBUG_INFO_INTERNAL>().lpBaseOfDll);
            }
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
        public OutputDebugStringInfo DebugStringInfo
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
                    var result = new OutputDebugStringInfo();

                    if (midresult.lpStringData != null)
                    {
                        if (midresult.nDebugStringLength == ushort.MaxValue)
                        {
                            // this will be large string and may not be the whole length.
                            result.DebugStringData = UnmangedToolKit.ExtractString(this.dwProcessId, midresult.lpStringData, IntPtr.Zero, UnmangedToolKit.UShortToBool(midresult.fUnicode));
                        }
                        else
                        {
                            if (midresult.fUnicode == 0)
                            {
                                result.DebugStringData = UnmangedToolKit.ExtractString(this.dwProcessId, midresult.lpStringData, (IntPtr)midresult.nDebugStringLength, UnmangedToolKit.UShortToBool(midresult.fUnicode));
                            }
                            else
                            {
                                result.DebugStringData = UnmangedToolKit.ExtractString(this.dwProcessId, midresult.lpStringData, (IntPtr)(midresult.nDebugStringLength /2), UnmangedToolKit.UShortToBool(midresult.fUnicode));
                            }
                            
                        }
                    }
                    else
                    {
                        result.DebugStringData = null;
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
                        ErrorType = (RipInfo.ErrorTypeEnum)midresult.dwType,
                        ErrorCode = midresult.dwError
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
            {
                return false;
            }
            else
            {
                if (obj is DebugEvent Data)
                {
                    return Equals(Data);
                }
                return false;
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


        /// <summary>
        /// compare the DebugEvent other with this one
        /// </summary>
        /// <param name="other">check against</param>
        /// <remarks>Specialzed Versi</remarks>
        /// <returns>true if they are the same</returns>
        public bool Equals(DebugEvent other)
        {
            if (other == null)
                return false;
            else
            {
                if (other.dwProcessId != this.dwProcessId)
                    return false;
                if (other.dwThreadId != this.dwThreadId)
                {
                    return false;
                }
                if (other.dwDebugEventCode != this.dwDebugEventCode)
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
    }

}
