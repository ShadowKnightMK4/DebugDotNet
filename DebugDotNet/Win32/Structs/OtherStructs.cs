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
    /// When a Call to <see cref="NativeMethods.CreateProcessW(string, string, IntPtr, IntPtr, bool, uint, IntPtr, string, ref STARTUPINFO, out ProcessInformation)"/> returns. This structure is filled out with information. This corasponds with Win32 Api structure at https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-process_information for more info
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessInformation : IEquatable<ProcessInformation>
    {
        /// <summary>
        /// The Raw Handle Value for the process. User Should call <see cref="NativeMethods.CloseHandle(IntPtr)"/> when finished.
        /// </summary>
        public IntPtr ProcessHandleRaw { get; set; }
        /// <summary>
        /// The Raw Handle Value for the main Thread. User Should call <see cref="NativeMethods.CloseHandle(IntPtr)"/> when finished.
        /// </summary>
        public IntPtr ThreadHandleRaw { get; set; }
        /// <summary>
        /// A number that specifies the ID of thie process
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// A number that specifies the ID of the main thread
        /// </summary>
        public int ThreadMainId { get; set; }

        /// <summary>
        /// Compare an arbitrary object with this <see cref="ProcessInformation"/> struct
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
                if (obj is ProcessInformation information)
                {
                    return Equals(information);
                }
                return false;
            }
        }
        /// <summary>
        /// get hash code of this structure's elements
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ProcessId.GetHashCode() + ThreadMainId.GetHashCode() + ProcessHandleRaw.GetHashCode() + ThreadHandleRaw.GetHashCode();
        }

        /// <summary>
        /// Are left and right the same
        /// </summary>
        /// <param name="left">left side to compare </param>
        /// <param name="right">right side to compare</param>
        /// <returns>true if they are same</returns>
        public static bool operator ==(ProcessInformation left, ProcessInformation right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// if left differetn from right
        /// </summary>
        /// <param name="left">left side to compare </param>
        /// <param name="right">right side to compare</param>
        /// <returns>true if different</returns>
        public static bool operator !=(ProcessInformation left, ProcessInformation right)
        {
            return !(left == right);
        }

        /// <summary>
        /// compare two <see cref="ProcessInformation"/> structs
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ProcessInformation other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (other.ProcessId != ProcessId)
                {
                    return false;
                }

                if (other.ThreadMainId != ThreadMainId)
                {
                    return false;
                }
                if (other.ProcessHandleRaw != ProcessHandleRaw)
                {
                    return false;
                }

                if (other.ThreadHandleRaw != ThreadHandleRaw)
                {
                    return false;
                }
                return true;
            }
        }
    }



    /// <summary>
    /// Exit Thread struct as returned via DebugEvent.ExitThread; 
    /// triggers when a thread ends
    /// </summary>
    public struct ExitThreadDebugInfo : IEquatable<ExitThreadDebugInfo>
    {
        /// <summary>
        /// make instance of this with specified exit code
        /// </summary>
        /// <param name="ExitCode">code that the process returned after exiting</param>
        public ExitThreadDebugInfo(uint ExitCode)
        {
            this.ExitCode = ExitCode;
        }
        /// <summary>
        /// the exit code for the thread
        /// </summary>
        public uint ExitCode { get; set; }

        /// <summary>
        /// return if an object is equal to this
        /// </summary>
        /// <param name="obj">check this one</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj is ExitThreadDebugInfo info)
                {
                    return Equals(info);
                }
                return false;
            }
        }

        /// <summary>
        /// get a hash code of this struct
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ExitCode.GetHashCode();
        }

        /// <summary>
        /// Return if left is equal to right
        /// </summary>
        /// <param name="left">one</param>
        /// <param name="right">another</param>
        /// <returns>true if one equals another</returns>
        public static bool operator ==(ExitThreadDebugInfo left, ExitThreadDebugInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// is left not equal to right
        /// </summary>
        /// <param name="left">one</param>
        /// <param name="right">another</param>
        /// <returns>true if not equal</returns>
        public static bool operator !=(ExitThreadDebugInfo left, ExitThreadDebugInfo right)
        {
            return !(left == right);
        }

        /// <summary>
        /// is this equal to other
        /// </summary>
        /// <param name="other">check against this one</param>
        /// <returns>true if equal</returns>
        public bool Equals(ExitThreadDebugInfo other)
        {
            if (other == null)
                return false;
            else
                return (other.ExitCode == ExitCode);
        }
    }


    /// <summary>
    /// EXIT_PROCESS_DEBUG_INFO as retured via DebugEvent.ExitProcess
    /// triggers when a process being debugged ends
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ExitProcessDebugInfo : IEquatable<ExitProcessDebugInfo>
    {
        /// <summary>
        /// make a struct with this exit code
        /// </summary>
        /// <param name="ExitCode"></param>
        public ExitProcessDebugInfo(uint ExitCode)
        {
            this.ExitCode = ExitCode;
        }
        /// <summary>
        /// the exit code received when the process exited
        /// </summary>
        public uint ExitCode { get; set; }

        /// <summary>
        /// return if an obj is equal to this 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj is ExitProcessDebugInfo info)
                {
                    return Equals(info);
                }
                return false;
            }
        }

        /// <summary>
        /// is this equal to Obj
        /// </summary>
        /// <param name="Obj">check against</param>
        /// <returns>true if equal</returns>
        public bool Equals(ExitProcessDebugInfo Obj)
        {
            if (Obj == null)
                return false;
            else
            {
                if (Obj.ExitCode != ExitCode)
                    return false;
                else
                    return true;
            }
        }

        /// <summary>
        /// get a hashcode of the elements
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ExitCode.GetHashCode();
        }


        /// <summary>
        /// are left and right the same
        /// </summary>
        /// <param name="left">left side</param>
        /// <param name="right">right side</param>
        /// <returns>true if equal</returns>
        public static bool operator ==(ExitProcessDebugInfo left, ExitProcessDebugInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// are left and right different
        /// </summary>
        /// <param name="left">left side</param>
        /// <param name="right">right side</param>
        /// <returns>true if NOT equal</returns>
        public static bool operator !=(ExitProcessDebugInfo left, ExitProcessDebugInfo right)
        {
            return !(left == right);
        }


    }

    /// <summary>
    /// Public struct that contains a LoadDllDebugInfo Event Data
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LoadDllDebugInfo : IEquatable<LoadDllDebugInfo>
    {
        /// <summary>
        /// a Handle to the DLL in question or 0 if there was en error.
        /// </summary>
        public SafeFileHandle FileHandle { get; set; }
        /// <summary>
        /// the base address of the dll in the debugged process's virtual memory
        /// </summary>
        public IntPtr BaseDllAddress { get; set; }
        /// <summary>
        /// the offset into the debug info of the dll
        /// </summary>
        public uint DebugInfoFileOffset { get; set; }
        /// <summary>
        /// the debug info size
        /// </summary>
        public uint DebugInfoSize { get; set; }
        /// <summary>
        /// A string that specifies the dll's name. 
        /// This is resolved by placing a call to <see cref="NativeMethods.GetFinalPathNameByHandle(IntPtr, FinalFilePathFlags)"/>, assuming the <see cref="LOAD_DLL_DEBUG_INFO_INTERNAL.hFile"/> (An Internal struct receives a valid handle to the Dll's file location 
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// set to True if the string could name be read (or a problem happend)
        /// </summary>
        public bool WasBad { get; set; }

        /// <summary>
        /// Is this object equal to this LoadDllDebugStruct
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is LoadDllDebugInfo))
            {
                return false;
            }
            return Equals((LoadDllDebugInfo)obj);

        }

        /// <summary>
        /// Return a hash of the struct's items
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return DebugInfoFileOffset.GetHashCode() +
                FileHandle.GetHashCode() +
                BaseDllAddress.GetHashCode() +
                ImageName.GetHashCode() +
                DebugInfoSize.GetHashCode() +
                WasBad.GetHashCode();
        }

        /// <summary>
        /// Compare 2 structs as equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(LoadDllDebugInfo left, LoadDllDebugInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare 2 Struct as not equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(LoadDllDebugInfo left, LoadDllDebugInfo right)
        {
            return !(left == right);
        }


        
        /// <summary>
        /// Is this equal to other
        /// </summary>
        /// <param name="other">check aginst this</param>
        /// <returns>true if equal</returns>
        public bool Equals(LoadDllDebugInfo other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                {
                    if (other.DebugInfoFileOffset != DebugInfoFileOffset)
                        return false;
                    if (other.FileHandle != FileHandle)
                        return false;
                    if (other.BaseDllAddress != BaseDllAddress)
                        return false;
                    if (other.DebugInfoSize != DebugInfoSize)
                        return false;
                    if (other.WasBad != WasBad)
                        return false;
                    if (other.ImageName != ImageName)
                        return false;
                }
                return true;
            }

        }
    }

    /// <summary>
    /// a RIP_INFO Debug Event means the victim died outside of debugger control. This struct  is the result after it has been converted from  <see cref="RIP_INFO_INTERNAL"/>
    /// </summary>
    public struct RipInfo : IEquatable<RipInfo>
    {
        /// <summary>
        /// Error code 
        /// </summary>
        public uint ErrorCode { get; set; }
        /// <summary>
        /// Additioanl Error Info / what kind of error. see <see cref="ErrorTypeEnum"/>  or look on MSDN for RIP_INFO for more info.
        /// </summary>
        public ErrorTypeEnum ErrorType { get; set; }
        /// <summary>
        /// Enum to assist finding what type of error
        /// </summary>
        public enum ErrorTypeEnum
        {
            /// <summary>
            /// only <see cref="ErrorCode"/>  is set.
            /// </summary>
            OnlyErrorSet = 0,
            /// <summary>
            /// Indicates that potentially invalid data was passed to the function, but the function completed processing. 
            /// </summary>
            SleWarning = 3,
            /// <summary>
            /// indicates that invalid data was passed to the function, but the error probably will not cause the application to fail. 
            /// </summary>
            SleMinorError = 2,
            /// <summary>
            /// Indicates that invalid data was passed to the function that failed. This caused the application to fail. 
            /// </summary>
            SleFatalError = 1,
        }


        /// <summary>
        /// Compare this object with another
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(RipInfo obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj.ErrorCode != ErrorCode)
                {
                    return false;
                }
                if (obj.ErrorType != ErrorType)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Compare an object with this
        /// </summary>
        /// <param name="obj">the thing to compare</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj is RipInfo info)
                {
                    return Equals(info);
                }
                return false;
            }
        }
        
        /// <summary>
        /// Get a hash of this struct's elements
        /// </summary>
        /// <returns>hash of each element</returns>
        public override int GetHashCode()
        {
            return ErrorCode.GetHashCode() + ErrorType.GetHashCode();
        }

        /// <summary>
        /// is left the same as right
        /// </summary>
        /// <param name="left">left side</param>
        /// <param name="right">right side</param>
        /// <returns>returns true if equal</returns>
        public static bool operator ==(RipInfo left, RipInfo right)
        {
            return left.Equals(right);
        }
        /// <summary>
        /// is left the NOT the same as right
        /// </summary>
        /// <param name="left">left side</param>
        /// <param name="right">right side</param>
        /// <returns>returns true if NOT equal</returns>
        public static bool operator !=(RipInfo left, RipInfo right)
        {
            return !(left == right);
        }


    }


    /// <summary>
    /// Processed Results from a <see cref="CREATE_PROCESS_DEBUG_INFO_INTERNAL"/> class in <see cref="DebugEvent.CreateProcessInfo"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateProcessDebugInfo : IEquatable<CreateProcessDebugInfo>
    {
        /// <summary>
        /// Safe Handle Open the file that was the source for what we are debugging
        /// </summary>
        public SafeFileHandle hFile { get; set; }
        /// <summary>
        /// Win32 Handle to the Process
        /// </summary>
        public IntPtr ProcessHandleRaw { get; set; }
        /// <summary>
        /// Win32 Handle to the main Thread
        /// </summary>
        public IntPtr ThreadHandleRaw { get; set; }

        /// <summary>
        /// Pointer to the base memory location of the process in THAT process's virtual memory.
        /// </summary>
        public IntPtr BaseOfImage { get; set; }
        /// <summary>
        /// Offset to contained debug into in the process's file
        /// </summary>
        public uint DebugInfoFileOffset { get; set; }
        /// <summary>
        /// size of the debug info in the file
        /// </summary>
        public uint DebugInfoSize { get; set; }
        /// <summary>
        /// Pointer to the Thread Local Storage Location in the process's memory
        /// </summary>
        public IntPtr ThreadLocalBase { get; set; }
        /// <summary>
        /// The Program's Entry Point (NOTE this is in The Process's Virtual Address Space.
        /// </summary>
        public IntPtr StartAddress { get; set; }
        /// <summary>
        /// Path to the program we loaded. Can be null if we can't get the process's location
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// Compare any object with this struct
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            else
            {
                if (obj is CreateProcessDebugInfo info)
                {
                    return Equals(info);
                }
                return false;
            }
        }

        /// <summary>
        /// get a hashcode for each of the elements
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.DebugInfoFileOffset.GetHashCode() +
                this.hFile.GetHashCode() +
                this.ProcessHandleRaw.GetHashCode() +
                this.ThreadHandleRaw.GetHashCode() +
                this.BaseOfImage.GetHashCode() +
                this.BaseOfImage.GetHashCode() +
                this.ImageName.GetHashCode() +
                this.StartAddress.GetHashCode() +
                this.ThreadLocalBase.GetHashCode() +
                this.DebugInfoSize.GetHashCode();

        }

        /// <summary>
        /// is left equal to right
        /// </summary>
        /// <param name="left">one</param>
        /// <param name="right">another</param>
        /// <returns>returns true if equal</returns>
        public static bool operator ==(CreateProcessDebugInfo left, CreateProcessDebugInfo right)
        {
            return left.Equals(right);
        }


        /// <summary>
        /// is left != to right
        /// </summary>
        /// <param name="left">one</param>
        /// <param name="right">another</param>
        /// <returns>returns true if NOT equal</returns>
        public static bool operator !=(CreateProcessDebugInfo left, CreateProcessDebugInfo right)
        {
            return !(left == right);
        }

        /// <summary>
        /// is other equal to this one
        /// </summary>
        /// <param name="other">compare this to other</param>
        /// <returns>true if equal</returns>
        public bool Equals(CreateProcessDebugInfo other)
        {
            if (other == null)
                return false;
            else
            {
                {
                    if (DebugInfoFileOffset != other.DebugInfoFileOffset)
                        return false;
                    if (hFile != other.hFile)
                        return false;
                    if (ProcessHandleRaw != other.ProcessHandleRaw)
                        return false;
                    if (ThreadHandleRaw != other.ThreadHandleRaw)
                        return false;
                    if (BaseOfImage != other.BaseOfImage)
                        return false;
                    if (StartAddress != other.StartAddress)
                        return false;
                    if (ThreadLocalBase != other.ThreadLocalBase)
                        return false;
                    if (DebugInfoSize != other.DebugInfoSize)
                        return false;
                    if (ImageName != other.ImageName)
                        return false;
                }
                return true;
            }
        }
    }


    
    /// <summary>
    /// A Processes Exception Record
    /// </summary>
    public struct ExceptionRecord : IEquatable<ExceptionRecord>
    {

        /// <summary>
        /// Walk through the exception list (if the Other is not null and build a list of the linked exceptions)
        /// </summary>
        /// <param name="other">pointer to <see cref="EXCEPTION_RECORD_INTERNAL.ExceptionRecord"/></param>
        /// <param name="ChainWalk">reference to list to build the chain of exceptions</param>
        static internal void FetchNestedRecord(IntPtr other, ref List<ExceptionRecord> ChainWalk)
        {
            EXCEPTION_RECORD_INTERNAL tmp = (EXCEPTION_RECORD_INTERNAL)Marshal.PtrToStructure(other, typeof(EXCEPTION_RECORD_INTERNAL));
            FetchNestedRecord(tmp, ref ChainWalk);
        }
        /// <summary>
        /// make the chain
        /// </summary>
        /// <param name="other">we start with this one</param>
        /// <param name="ChainWalk">and add the ones we find to this one</param>
        internal static void FetchNestedRecord(EXCEPTION_RECORD_INTERNAL other, ref List<ExceptionRecord> ChainWalk)
        {
            ChainWalk = new List<ExceptionRecord>();
            if (other.ExceptionRecord != IntPtr.Zero)
            {
                IntPtr WalkPtr = other.ExceptionRecord;
                while (WalkPtr != IntPtr.Zero)
                {
                    ChainWalk.Add(new ExceptionRecord(WalkPtr));
                    WalkPtr = ChainWalk[ChainWalk.Count - 1].ExceptionAddress;
                }
            }
            
        }

        /// <summary>
        /// constructor used when walking the exception change and making .NET structs containing the data
        /// </summary>
        /// <param name="StartPoint"></param>
        internal ExceptionRecord(IntPtr StartPoint)
        {
            EXCEPTION_RECORD_INTERNAL StartingPoint = (EXCEPTION_RECORD_INTERNAL)Marshal.PtrToStructure(StartPoint, typeof(EXCEPTION_RECORD_INTERNAL));
            this.ExceptionAddress = StartingPoint.ExceptionAddress;
            this.ExceptionCode = (ExceptionCode)StartingPoint.ExceptionCode;
            this.ExceptionInformation =  new List<uint>(StartingPoint.ExceptionInformation);
            this.NestedRecordVal = new List<ExceptionRecord>();
            ExceptionMessage = string.Empty;
            if (StartingPoint.ExceptionFlags != 0)
            {
                this.CanContinueException = ExceptionFlagType.Continuable;
            }
            else
            {
                this.CanContinueException = ExceptionFlagType.NonContinuable;
            }
          //  FetchNestedRecord(StartingPoint, ref this.NestedRecord);
        }
        /// <summary>
        /// make a EXCEPTION_RECORD from the Base Class
        /// </summary>
        /// <param name="StartingPoint"></param>
        internal ExceptionRecord(EXCEPTION_RECORD_INTERNAL StartingPoint)
        {
            this.ExceptionAddress = StartingPoint.ExceptionAddress;
            this.ExceptionCode = (ExceptionCode)StartingPoint.ExceptionCode;
            ExceptionInformation = new List<uint>();
            ExceptionInformation.AddRange(StartingPoint.ExceptionInformation);
            /*
            Array.Copy(StartingPoint.ExceptionInformation, 0, ExceptionInformation, 0, StartingPoint.NumberParameters); */
            this.NestedRecordVal = new List<ExceptionRecord>();
            ExceptionMessage = string.Empty;
            if (StartingPoint.ExceptionFlags != 0)
            {
                this.CanContinueException = ExceptionFlagType.Continuable;
            }
            else
            {
                this.CanContinueException = ExceptionFlagType.NonContinuable;
            }
            
            if (StartingPoint.ExceptionRecord != IntPtr.Zero)
            {
                IntPtr Stepper = StartingPoint.ExceptionRecord;
                while (Stepper != IntPtr.Zero)
                {
                    FetchNestedRecord(Stepper, ref NestedRecordVal);
                    Stepper = IntPtr.Zero;
                }
            }
           // FetchNestedRecord(StartingPoint, ref this.NestedRecord);
        }
        /// <summary>
        /// The type of exception. NOTICE: It is not restricted to the enum. Things may throw exceptions outside of that.
        /// </summary>
        public ExceptionCode ExceptionCode { get; set; }
        /// <summary>
        /// Possible message for the exception if any,
        /// </summary>
        public string ExceptionMessage { get; set; }
        /// <summary>
        /// Tells if the exception can be continued ( or not)
        /// </summary>
        public ExceptionFlagType CanContinueException { get; set; }
        /// <summary>
        /// Nested exceptions (if any) or null if none
        /// </summary>
        public List<ExceptionRecord> NestedRecord
        {
            get
            {
                return NestedRecordVal;
            }
        }

        private List<ExceptionRecord> NestedRecordVal;
        /// <summary>
        /// Where the excecption happened
        /// </summary>
        public IntPtr ExceptionAddress { get; set; }
        /// <summary>
        /// number of arguments is folding into ExceptionInformation.Length
        /// </summary>
        public int NumberParameters { get { return ExceptionInformation.Count; } }
        /// <summary>
        /// Arguments to the Exception (if any). Virtual Memomory Addresses (if any) are in the context of the process that triggered the event
        /// </summary>
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 15, ArraySubType = UnmanagedType.U4)] public uint[] ExceptionInformation ;
        public List<uint> ExceptionInformation { get; private set; }


        
  
        
        /// <summary>
        /// compare this <see cref="ExceptionRecord"/> and any other object
        /// </summary>
        /// <param name="obj">compare with</param>
        /// <returns>true if the same and false if not</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj is ExceptionRecord record)
                {
                    return Equals(record);
                }
                return false;
            }
        }

        /// <summary>
        /// get a hash code of this class's memebers
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int tmp;
            if (NestedRecord != null)
            {
                tmp = NestedRecord.GetHashCode();
            }
            else
            {
                tmp = 0;
            }
            return CanContinueException.GetHashCode() +
                ExceptionAddress.GetHashCode() +
                ExceptionCode.GetHashCode() +
                NumberParameters.GetHashCode() + tmp;

        }


        /// <summary>
        /// Is left thesame as right
        /// </summary>
        /// <param name="left">left side of compare</param>
        /// <param name="right">right side of compare</param>
        /// <returns></returns>
        public static bool operator ==(ExceptionRecord left, ExceptionRecord right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// is left different to right
        /// </summary>
        /// <param name="left">left of compare</param>
        /// <param name="right">right of compare</param>
        /// <returns>return true if different (Not the same)</returns>
        public static bool operator !=(ExceptionRecord left, ExceptionRecord right)
        {
            return !(left == right);
        }

        /// <summary>
        /// is other equal to this
        /// </summary>
        /// <param name="other">compare against</param>
        /// <returns>true if identical</returns>
        public bool Equals(ExceptionRecord other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (other.CanContinueException != CanContinueException)
                {
                    return false;
                }
                if (other.ExceptionAddress != ExceptionAddress)
                {
                    return false;
                }

                if (other.ExceptionCode != ExceptionCode)
                {
                    return false;
                }

                if (other.ExceptionMessage != ExceptionMessage)
                {
                    return false;
                }

                if (other.NumberParameters != NumberParameters)
                {
                    return false;
                }

                for (int step =0; step < other.ExceptionInformation.Count; step++)
                {
                    if (other.ExceptionInformation[step] != ExceptionInformation[step])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }


    /// <summary>
    /// Processed from <see cref="EXCEPTION_RECORD_INTERNAL"/>, this contains data describing an exception event
    /// </summary>
    public struct ExceptionDebugInfo : IEquatable<ExceptionDebugInfo>
    {
        /// <summary>
        /// The main exception that this contains.
        /// </summary>
        public ExceptionRecord TopLevelException { get; set; }

        /// <summary>
        /// Gets a list of other nested exception records (if any) or null if there is non
        /// </summary>
        public List<ExceptionRecord> Chain
        {
            get
            {
                return TopLevelException.NestedRecord;
            }
        }

        /// <summary>
        /// If true then this is the first time this exception has been see. if false then it has been seen by your debugger first.
        /// </summary>
        public bool IsFirstChance { get; set; }

        /// <summary>
        /// is any object the same as this instance of <see cref="ExceptionDebugInfo"/>
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
                if (obj is ExceptionDebugInfo info)
                {
                    return Equals(info);
                }
                return false;
            }

        }

        /// <summary>
        /// return a hash code of this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int tmp = 0;
            if (Chain != null)
            {
                tmp = Chain.GetHashCode();
            }
            return tmp + TopLevelException.GetHashCode() + IsFirstChance.GetHashCode();
        }

        /// <summary>
        /// are left and right the same?
        /// </summary>
        /// <param name="left">left value</param>
        /// <param name="right">right value</param>
        /// <returns>true if they are</returns>
        public static bool operator ==(ExceptionDebugInfo left, ExceptionDebugInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Is left different from right
        /// </summary>
        /// <param name="left">one to consider</param>
        /// <param name="right">another to conosider </param>
        /// <returns>returns true if different</returns>
        public static bool operator !=(ExceptionDebugInfo left, ExceptionDebugInfo right)
        {
            return !(left == right);
        }

        /// <summary>
        /// is other the same as thsi one
        /// </summary>
        /// <param name="other">the object to compare against</param>
        /// <returns>returns true if matching</returns>
        public bool Equals(ExceptionDebugInfo other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (other.IsFirstChance != IsFirstChance)
                {
                    return false;
                }
                if (other.TopLevelException != TopLevelException)
                {
                    return false;
                }
                if ( ((other.Chain == null) && (Chain != null)) ||
                    ((other.Chain !=  null) && (Chain == null))
                      )
                {
                    return false;
                }

                if (other.Chain.Count != Chain.Count)
                {
                    return false;
                }
                
                for (int step = 0; step < other.Chain.Count;step++)
                {
                    if (other.Chain[step] != Chain[step])
                    {
                        return false;
                    }
                }

                return true;
}
        }
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
        public string DebugStringData { get; set; }
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
            return DebugStringData.GetHashCode();
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
            return (other.DebugStringData == DebugStringData);
        }
    }


    /// <summary>
    /// Processing from The Internal version just sets the variable.
    /// </summary>
    public struct UnloadDllDebugInfo : IEquatable<UnloadDllDebugInfo>
    {
        /// <summary>
        /// make a struct with this lpBaseOfDll
        /// </summary>
        /// <param name="lpBaseOfDll"></param>
        public UnloadDllDebugInfo(IntPtr lpBaseOfDll)
        {
            BaseDllAddress = lpBaseOfDll;
        }

        /// <summary>
        /// The base address of the dll that was unloaded in the address space of the Process being debugged
        /// </summary>
        public IntPtr BaseDllAddress { get; set; }

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
                if (obj is UnloadDllDebugInfo)
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
            return BaseDllAddress.GetHashCode();
        }

        /// <summary>
        /// equal 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(UnloadDllDebugInfo left, UnloadDllDebugInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// not equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(UnloadDllDebugInfo left, UnloadDllDebugInfo right)
        {
            return !(left == right);
        }

       

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(UnloadDllDebugInfo other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return (other.BaseDllAddress == BaseDllAddress);
            }
        }
    }


    /// <summary>
    /// CREATE_THREAD_DEBUG_INFO as its returned via DebugEvent.CreateThread;
    /// Triggers when a thread is started
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CreateThreadDebugInfo : IEquatable<CreateThreadDebugInfo>
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
        public IntPtr StartRoutineAddress { get; set; }


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
                if (obj is CreateThreadDebugInfo CreateThreadInfo)
                {
                    return Equals(CreateThreadInfo);
                }
                return false;
            }
        }

        /// <summary>
        /// is this equal to CREATE_THREAD_DEBUG_INFO
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Equals(CreateThreadDebugInfo obj)
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
        /// <summary>
        /// is left  the same as right
        /// </summary>
        /// <param name="left">check this</param>
        /// <param name="right">against that one</param>
        /// <returns>true if same</returns>
        public static bool operator ==(CreateThreadDebugInfo left, CreateThreadDebugInfo right)
        {
            return left.Equals(right);
        }
        /// <summary>
        /// is left NOT the same as right
        /// </summary>
        /// <param name="left">check this</param>
        /// <param name="right">against that one</param>
        /// <returns>true if different</returns>
        public static bool operator !=(CreateThreadDebugInfo left, CreateThreadDebugInfo right)
        {
            return !(left == right);
        }

 
    }
}
