using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


/*
 * The struct defination is 2 ways.
 * 
 * The 'Prepped' c# style one are named directly after their MSDN equivelent name in Camel case
 * 
 * Example: MSDN DEBUG_EVENT  =>  DebugEvent
 * 
 * The  Internal / unprepped ones look like
 * DebugEventInternal.  
 * 
 * 
 * This file contains the implemnation of the internal struct side of things and this not generally ment for general use
 * 
 */
namespace DebugDotNet.Win32.Internal
{    /// <summary>
     /// Specifies a specific start routine to a Win32 Thread that accepts one argument
     /// </summary>
     /// <param name="lpThreadParameter"></param>
     /// <returns>results of the thread</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "The internal structs care about byte location. I can't seem to figure how to do this with fields.")]

    [Obsolete("This parameter points to the virtual address in the debugged process. C# compains when marshaling this sometimes to the struct we use")]
    internal delegate uint PThreadStartRoutine(IntPtr lpThreadParameter);


  

    /// <summary>
    /// The in memory structer that we eventual convert to <see cref="ExitThreadDebugInfo"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct EXIT_THREAD_DEBUG_INFO_INTERNAL
    { 
        /// <summary>
        /// the exit code for the thread
        /// </summary>
        public uint dwExitCode;

    }



        /// <summary>
        ///The in memory form of <see cref="CreateThreadDebugInfo"/> structure before any processing
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
    internal struct CREATE_THREAD_DEBUG_INFO_INTERNAL
    {
        /// <summary>
        ///  the raw hThread handle. Resolves to <see cref="CreateThreadDebugInfo.ThreadHandle"/>
        /// </summary>
        public IntPtr hThread;
        /// <summary>
        /// The pointer to the start of the TLS memory block. Resolves to <see cref="CreateThreadDebugInfo.ThreadLocalBaseStart"/>
        /// </summary>
        public IntPtr lpThreadLocalBase;
        /// <summary>
        /// the entry point of the thread in the vitual address space of the process being debugged. This Resolves to <see cref="CreateThreadDebugInfo.StartRoutineAddress"/>
        /// </summary>
        public IntPtr lpStartAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct THREADENTRY32
    {
        /// <summary>
        /// Make in instance of the struct with the size entry already set
        /// </summary>
        /// <param name="_">Not used. C# complains about explicite parameterless struct construct(). </param>
        public THREADENTRY32(int _)
        {
            dwSize = (uint) Marshal.SizeOf(typeof(THREADENTRY32));
            cntUsage = th32ThreadId = th32OwnerProcessID = tpBasePri = tpDeltaPri = dwFlags = 0;
        }
        public uint dwSize;
        public uint cntUsage;
        public uint th32ThreadId;
        public uint th32OwnerProcessID;
        public uint tpBasePri;
        public uint tpDeltaPri;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EXIT_PROCESS_DEBUG_INFO_INTERNAL
    {
        public uint dwExitCode;
    }


    /// <summary>
    /// The internal direct memory struct of RipInfo is made from.
    /// <see cref="RipInfo"/>
    /// </summary>

   [StructLayout(LayoutKind.Sequential)]
    internal struct RIP_INFO_INTERNAL : IEquatable<RIP_INFO_INTERNAL>
    {
        /// <summary>
        /// from MSDN RIP_INFO:   The error that caused the RIP debug event.  Resolves to <seealso cref="RipInfo.ErrorCode"/>
        /// </summary>
        public uint dwError;

        /// <summary>
        /// contains the value that specifies the type off error that thappens.  Resolvesto <seealso cref="RipInfo.ErrorType"/>
        /// </summary>
        public uint dwType;

        /// <summary>
        /// returns if the obj is equal to this
        /// </summary>
        /// <param name="CheckMe">thing to check to check</param>
        /// <returns></returns>
        public bool Equals(RIP_INFO_INTERNAL CheckMe)
        {
            {
                if (CheckMe.dwError != dwError)
                    return false;

                if (CheckMe.dwType != dwType)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// returns if the obj is equal to this
        /// </summary>
        /// <param name="obj">obj to check</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj.GetType() == typeof(RIP_INFO_INTERNAL))
                {
                    RIP_INFO_INTERNAL cmp = (RIP_INFO_INTERNAL)obj;
                    if (cmp.dwError != dwError)
                        return false;

                    if (cmp.dwType != dwType)
                        return false;


                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// return a hashcode for this RIP_INFO_INTERNAL struct
        /// </summary>
        /// <returns>the hash</returns>
        public override int GetHashCode()
        {
            return dwError.GetHashCode() + dwType.GetHashCode();
        }

        /// <summary>
        /// return if left is equal to right
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(RIP_INFO_INTERNAL left, RIP_INFO_INTERNAL right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// return if left is not equal to right
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(RIP_INFO_INTERNAL left, RIP_INFO_INTERNAL right)
        {
            return !(left == right);
        }

 
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct UNLOAD_DLL_DEBUG_INFO_INTERNAL
    {
        /// <summary>
        /// The base address of the dll that was unloaded
        /// </summary>
        public IntPtr lpBaseOfDll { get; set; }

        /// <summary>
        /// return an instance of <see cref="UnloadDllDebugInfo"/> make from this struct
        /// </summary>
        /// <returns></returns>
        public UnloadDllDebugInfo GetPublic()
        {
            return new UnloadDllDebugInfo(lpBaseOfDll);
        }
    }
       


    /// <summary>
    /// The internal direct memory struct that <seealso cref="CreateProcessDebugInfo"/> is made from
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CREATE_PROCESS_DEBUG_INFO_INTERNAL : IEquatable<CREATE_PROCESS_DEBUG_INFO_INTERNAL>
    {
        /// <summary>
        /// If non-zero is a Win32 Handle the the file this process is created from. Caller is responsbile for closing it when done. Resolves to <see cref="CreateProcessDebugInfo.hFile"/>
        /// </summary>
        public IntPtr hFile;
        /// <summary>
        /// If non-zero then it is handle to the process being debugged. Resolves to <see cref="CreateProcessDebugInfo.ProcessHandleRaw"/>
        /// </summary>
        public IntPtr hProcess;
        /// <summary>
        /// Handle to the starting thread of the prcess debug debugged. Resolves to <see cref="CreateProcessDebugInfo.ThreadHandleRaw"/>
        /// </summary>
        public IntPtr hThread;
        /// <summary>
        /// Pointet to the image base. Resolves to <see cref="CreateProcessDebugInfo.BaseOfImage"/>
        /// </summary>
        public IntPtr lpBaseOfImage;
        /// <summary>
        /// Points to the locatation when the debug info is stored in the file <see cref="CreateProcessDebugInfo.DebugInfoFileOffset"/>
        /// </summary>
        public uint dwDebugInfoFileOffset;
        /// <summary>
        /// How big is this block of debug info <see cref="CreateProcessDebugInfo.DebugInfoSize"/>
        /// </summary>
        public uint nDebugInfoSize;
        /// <summary>
        /// Points to thread local storage <see cref="CreateProcessDebugInfo.ThreadLocalBase"/>
        /// </summary>
        public IntPtr lpThreadLocalBase;

        /// <summary>
        /// Points to Proces Entry point. Does not have equivelent in <see cref="CreateProcessDebugInfo"/>
        /// </summary>
        public IntPtr lpStartAddress;
        /// <summary>
        /// pointer null terminated string (ANSI or UNICODE) if non zero . <see cref="CreateProcessDebugInfo.ImageName"/>
        /// </summary>
        public IntPtr lpImageName;
        /// <summary>
        /// is this unicode (non zero) or ansi (zero).  Not needed in <see cref="CreateProcessDebugInfo"/> final function
        /// </summary>
        public ushort fUnicode;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj is CREATE_PROCESS_DEBUG_INFO_INTERNAL)
                {
                    
                }
                return false;
            }
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(CREATE_PROCESS_DEBUG_INFO_INTERNAL left, CREATE_PROCESS_DEBUG_INFO_INTERNAL right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CREATE_PROCESS_DEBUG_INFO_INTERNAL left, CREATE_PROCESS_DEBUG_INFO_INTERNAL right)
        {
            return !(left == right);
        }

        public bool Equals(CREATE_PROCESS_DEBUG_INFO_INTERNAL other)
        {
            return base.Equals(other);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct EXCEPTION_DEBUG_INFO_INTERNAL
    {
        public EXCEPTION_RECORD_INTERNAL ExceptionRecord;
        public uint dwFirstChance;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OUTPUT_DEBUG_STRING_INTERNAL
    {
        /// <summary>
        /// points to a block of memory contained the string data. 
        /// </summary>
        public IntPtr lpStringData;
        /// <summary>
        /// if zero then the string is an ANSI code string. Otherwise it is a Unicode stringe
        /// </summary>
        public ushort fUnicode;
        /// <summary>
        /// Contains the length of the string in chas
        /// </summary>
        public ushort nDebugStringLength;
    }



    [StructLayout(LayoutKind.Sequential)]
    internal struct EXCEPTION_RECORD_INTERNAL 
    {
        public uint ExceptionCode;
        public uint ExceptionFlags;
        public IntPtr ExceptionRecord;
        public IntPtr ExceptionAddress;
        public uint NumberParameters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15, ArraySubType = UnmanagedType.U4)] 
        public uint[] ExceptionInformation;
        
        public ExceptionRecord Convert()
        {
            ExceptionRecord ret = new ExceptionRecord(this);
            return ret;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LOAD_DLL_DEBUG_INFO_INTERNAL
    {
        public IntPtr hFile;
        public IntPtr lpBaseOfDll;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpImageName;
        public ushort fUnicode;


        /// <summary>
        /// convert this to the convention version
        /// </summary>
        /// <returns></returns>
        public LoadDllDebugInfo ToLoadDllDebugInfo()
        {
            LoadDllDebugInfo result = new LoadDllDebugInfo();
            if (hFile != null)
            {
                result.FileHandle = new SafeFileHandle(hFile, true);
                result.ImageName = NativeMethods.GetFinalPathNameByHandle(hFile, FinalFilePathFlags.FileNameNormalized);
            }
            else
            {
                result.FileHandle = null;
            }
            result.BaseDllAddress = lpBaseOfDll;


            result.DebugInfoSize = nDebugInfoSize;
            result.DebugInfoFileOffset = dwDebugInfoFileOffset;

            return result;
        }
    }

}
