using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Internal;
using Microsoft.Win32.SafeHandles;

/*
 *
 */
namespace DebugDotNet.Win32.Structs
{


    /// <summary>
    /// Exit Thread struct as returned via DebugEvent.ExitThread; 
    /// triggers when a thread ends
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EXIT_THREAD_DEBUG_INFO : IEquatable<EXIT_THREAD_DEBUG_INFO>
    {
        /// <summary>
        /// the exit code for the thread
        /// </summary>
        public uint dwExitCode;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj is EXIT_PROCESS_DEBUG_INFO)
                {
                    return Equals(obj);
                }
                return false;
            }
        }

        public override int GetHashCode()
        {
            return dwExitCode.GetHashCode();
        }

        public static bool operator ==(EXIT_THREAD_DEBUG_INFO left, EXIT_THREAD_DEBUG_INFO right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EXIT_THREAD_DEBUG_INFO left, EXIT_THREAD_DEBUG_INFO right)
        {
            return !(left == right);
        }

        public bool Equals(EXIT_THREAD_DEBUG_INFO other)
        {
            if (other == null)
                return false;
            else
                return (other.dwExitCode == dwExitCode);
        }
    }


    /// <summary>
    /// EXIT_PROCESS_DEBUG_INFO as retured via DebugEvent.ExitProcess
    /// triggers when a process being debugged ends
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EXIT_PROCESS_DEBUG_INFO
    {
        /// <summary>
        /// the exit code received when the process exited
        /// </summary>
        public readonly uint dwExitCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LOAD_DLL_DEBUG_INFO
    {
        /// <summary>
        /// a Handle to the DLL in question or 0 if there was en error.
        /// </summary>
        public SafeFileHandle hFile;
        /// <summary>
        /// the base address of the dll
        /// </summary>
        public IntPtr lpBaseOfDll;
        /// <summary>
        /// the offset into the debug info of the dll
        /// </summary>
        public uint dwDebugInfoFileOffset;
        /// <summary>
        /// the debug info size
        /// </summary>
        public uint nDebugInfoSize;
        /// <summary>
        /// a string that specifies the dll's name. 
        /// for this freindly version this is derhived on hFile also being valid.
        /// </summary>
        public string lpImageName;
        /// <summary>
        /// set to True if the string could name be read (or a problem happend)
        /// </summary>
        public bool WasBad;
    }

    /// <summary>
    /// a RIP_INFO Debug Event Struct after it has been converted from RIP_INFO_Internal
    /// </summary>
    public struct RipInfo
    {
        /// <summary>
        /// possible error code
        /// </summary>
        public uint dwError;
        /// <summary>
        /// Specifies the eror that happend
        /// </summary>
        public ErrorType dwType;
        /// <summary>
        /// What kind of error
        /// </summary>
        public enum ErrorType
        {
            /// <summary>
            /// only dwError is set
            /// </summary>
            ONLY_ERROR_SET = 0,
            /// <summary>
            /// Indicates that potentially invalid data was passed to the function, but the function completed processing. 
            /// </summary>
            SLE_WARNING = 3,
            /// <summary>
            /// indicates that invalid data was passed to the function, but the error probably will not cause the application to fail. 
            /// </summary>
            SLE_MINORERROR = 2,
            /// <summary>
            /// Indicates that invalid data was passed to the function that failed. This caused the application to fail. 
            /// </summary>
            SLE_ERROR = 1,
        }
    }


    /// <summary>
    /// Processed Results from a <see cref="CREATE_PROCESS_DEBUG_INFO_INTERNAL"/> class in <see cref="DebugEvent.CreateProcessInfo"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateProcessDebugInfo
    {
        public SafeFileHandle hFile;
        public IntPtr hProcess;
        public IntPtr hThread;
        public IntPtr lpBaseOfImage;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpThreadLocalBase;
        public PTHREAD_START_ROUTINE lpStartAddress;
        public string lpImageName;
    }

    public struct EXCEPTION_ACCESS_VIOLATION_PARAMETERS
    {
        /// <summary>
        /// what kind of violation did the target do
        /// </summary>
        public BaseAccessViolation BaseViolation;
        /// <summary>
        /// where did the violation occure in virtual memory land of the target
        /// </summary>
        public IntPtr EventAddressLocation;
    }
    public struct EXCEPTION_RECORD
    {
        public uint ExceptionCode;
        public string ExceptionMessage;
        public bool CanContinueException;
        public IntPtr ExceptionRecord;
        public IntPtr ExceptionAddress;
        public uint NumberParameters;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15, ArraySubType = UnmanagedType.U4)] public uint[] ExceptionInformation;

        /// <summary>
        /// returns the EXCEPTION_ACCESS_VIOLATE paramaters as a structure.
        /// </summary>
        public EXCEPTION_ACCESS_VIOLATION_PARAMETERS AccessViolation
        {
            get
            {
                if (ExceptionCode != (uint)EXCEPTION_CODE.EXCEPTION_ACCESS_VIOLATION)
                {
                    throw new InvalidCastException("Attempt to get Access Violation Arguments in record when that's not the exception");
                }
                EXCEPTION_ACCESS_VIOLATION_PARAMETERS ret = new EXCEPTION_ACCESS_VIOLATION_PARAMETERS
                {
                    BaseViolation = (BaseAccessViolation)ExceptionInformation[0],
                    EventAddressLocation = new IntPtr(ExceptionInformation[1])
                };
                return ret;
            }
        }


    }


    [StructLayout(LayoutKind.Sequential)]
    public struct EXCEPTION_DEBUG_INFO
    {
        public EXCEPTION_RECORD ExceptionRecord;
        public uint dwFirstChance;

    }

    /// <summary>
    /// struct that contains a string received from the Program being debugged class.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OutputDebugStringInfo: IEquatable<OutputDebugStringInfo>
    {
        /// <summary>
        /// The what was emitted or null if some went wrong in retrieved the data. Already Unicode
        /// </summary>
        public string lpDebugStringData { get; set; }
        /// <summary>
        /// Compare OUTPUT_DEBUG_STRING_INFO against this object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is OutputDebugStringInfo)
            {
                return Equals(obj);
            }
            return false;
        }

        /// <summary>
        /// return the hash code for is OUTPUT_DEBING_STRING struct
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return lpDebugStringData.GetHashCode();
        }

        /// <summary>
        /// equal check
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(OutputDebugStringInfo left, OutputDebugStringInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// not equal check
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(OutputDebugStringInfo left, OutputDebugStringInfo right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compare the underlying strings
        /// </summary>
        /// <param name="other"></param>
        /// <returns>true if equal otherwise false</returns>
        public bool Equals(OutputDebugStringInfo other)
        {
            return (other.lpDebugStringData == lpDebugStringData);
        }
    }


    /// <summary>
    /// The Processed version of a UNLOAD_DLL_DEBUG_INFO event.  There is no processing needed"/>
    /// </summary>

    [StructLayout(LayoutKind.Sequential)]
    public struct UNLOAD_DLL_DEBUG_INFO : IEquatable<UNLOAD_DLL_DEBUG_INFO>
    {
        /// <summary>
        /// The base address of the dll that was unloaded
        /// </summary>
        public IntPtr lpBaseOfDll { get; set; }

        /// <summary>
        /// compare an object and this
        /// </summary>
        /// <param name="obj">the obj</param>
        /// <returns>true if equal otherwise false</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj is UNLOAD_DLL_DEBUG_INFO)
                {
                    return Equals(obj);
                }
                return false;
            }
        }

        /// <summary>
        /// get hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return lpBaseOfDll.GetHashCode();
        }

        /// <summary>
        /// equal 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(UNLOAD_DLL_DEBUG_INFO left, UNLOAD_DLL_DEBUG_INFO right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// not equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(UNLOAD_DLL_DEBUG_INFO left, UNLOAD_DLL_DEBUG_INFO right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(UNLOAD_DLL_DEBUG_INFO other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return (other.lpBaseOfDll == lpBaseOfDll);
            }
        }
    }


    /// <summary>
    /// CREATE_THREAD_DEBUG_INFO as its returned via DebugEvent.CreateThread;
    /// Triggers when a thread is started
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_THREAD_DEBUG_INFO
    {
        /// <summary>
        /// Thread Handle
        /// </summary>
        public IntPtr ThreadHandle { get; set; }
        /// <summary>
        /// the TLS memory block
        /// </summary>
        public IntPtr ThreadLocalBaseStart { get; set; }
        /// <summary>
        /// the entry point of the thread   
        /// </summary>
        public PTHREAD_START_ROUTINE StartRoutineAddress { get; set; }


        /// <summary>
        /// return a Hash Code for each item in this struct
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ThreadHandle.GetHashCode() + ThreadLocalBaseStart.GetHashCode() + StartRoutineAddress.GetHashCode();
        }

        /// <summary>
        /// compare a CREATE_THREAD_DEBUG_INFO against another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj is CREATE_THREAD_DEBUG_INFO)
                {
                    return Equals((CREATE_THREAD_DEBUG_INFO)obj);
                }
                return false;
            }
        }

        /// <summary>
        /// is this equal to CREATE_THREAD_DEBUG_INFO
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(CREATE_THREAD_DEBUG_INFO obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj.StartRoutineAddress != StartRoutineAddress)
                    return false;
                if (obj.ThreadHandle != ThreadHandle)
                    return false;
                if (obj.ThreadLocalBaseStart != ThreadLocalBaseStart)
                    return false;
                return true;
            }
        }

        public static bool operator ==(CREATE_THREAD_DEBUG_INFO left, CREATE_THREAD_DEBUG_INFO right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CREATE_THREAD_DEBUG_INFO left, CREATE_THREAD_DEBUG_INFO right)
        {
            return !(left == right);
        }
    }
}
