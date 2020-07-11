using System;
using System.Collections.Generic;
using System.Text;

namespace DebugDotNet.Win32.Enums
{
    /// <summary>
    /// from MSDN Exception Debug Events, this is the type of exception that happened
    /// </summary>
    public enum EXCEPTION_CODE : uint
    {
        /// <summary>
        /// The thread tried to read from or write to a virtual address for which it does not have the appropriate access.  
        /// </summary>
        EXCEPTION_ACCESS_VIOLATION = 0xC0000005,

        /// <summary>
        /// The thread tried to access an array element that is out of bounds and the underlying hardware supports bounds checking. 
        /// </summary>
        EXCEPTION_ARRAY_BOUNDS_EXCEEDED = 0xC000008C,

        /// <summary>
        /// A breakpoint was encountered. 
        /// </summary>
        EXCEPTION_BREAKPOINT = 0x80000003,

        /// <summary>
        ///The thread tried to read or write data that is misaligned on hardware that does not provide alignment. For example, 16-bit values must be aligned on 2-byte boundaries; 32-bit values on 4-byte boundaries, and so on.  
        /// </summary>
        EXCEPTION_DATATYPE_MISALIGNMENT = 0x80000002,

        /// <summary>
        ///  One of the operands in a floating-point operation is denormal. A denormal value is one that is too small to represent as a standard floating-point value. 
        /// </summary>
        EXCEPTION_FLT_DENORMAL_OPERAND = 0xC000008D,

        /// <summary>
        /// The thread tried to divide a floating-point value by a floating-point divisor of zero. 
        /// </summary>
        EXCEPTION_FLT_DIVIDE_BY_ZERO = 0xC000008E,

        /// <summary>
        /// The result of a floating-point operation cannot be represented exactly as a decimal fraction. 
        /// </summary>
        EXCEPTION_FLT_INEXACT_RESULT = 0xC000008F,

        /// <summary>
        /// This exception represents any floating-point exception not included in this list. 
        /// </summary>
        EXCEPTION_FLT_INVALID_OPERATION = 0xC0000090,

        /// <summary>
        /// The exponent of a floating-point operation is greater than the magnitude allowed by the corresponding type. 
        /// </summary>
        EXCEPTION_FLT_OVERFLOW = 0xC0000091,

        /// <summary>
        /// The stack overflowed or underflowed as the result of a floating-point operation. 
        /// </summary>
        EXCEPTION_FLT_STACK_CHECK = 0xC0000092,

        /// <summary>
        ///The exponent of a floating-point operation is less than the magnitude allowed by the corresponding type.  
        /// </summary>
        EXCEPTION_FLT_UNDERFLOW = 0xC0000093,

        /// <summary>
        /// The thread tried to execute an invalid instruction. 
        /// </summary>
        EXCEPTION_ILLEGAL_INSTRUCTION = 0xC000001D,

        /// <summary>
        /// The thread tried to access a page that was not present, and the system was unable to load the page. For example, this exception might occur if a network connection is lost while running a program over the network. 
        /// </summary>
        EXCEPTION_IN_PAGE_ERROR = 0xC0000006,

        /// <summary>
        /// The thread tried to divide an integer value by an integer divisor of zero. 
        /// </summary>
        EXCEPTION_INT_DIVIDE_BY_ZERO = 0xC000009,

        /// <summary>
        /// The result of an integer operation caused a carry out of the most significant bit of the result. 
        /// </summary>
        EXCEPTION_INT_OVERFLOW = 0xC0000095,

        /// <summary>
        /// An exception handler returned an invalid disposition to the exception dispatcher. Programmers using a high-level language such as C should never encounter this exception.
        /// </summary>
        EXCEPTION_INVALID_DISPOSITION = 0xC0000026,

        /// <summary>
        /// The thread tried to continue execution after a noncontinuable exception occurred. 
        /// </summary>
        EXCEPTION_NONCONTINUABLE_EXCEPTION = 0xC0000025,

        /// <summary>
        /// The thread tried to execute an instruction whose operation is not allowed in the current machine mode. 
        /// </summary>
        EXCEPTION_PRIV_INSTRUCTION = 0xC0000096,

        /// <summary>
        /// A trace trap or other single-instruction mechanism signaled that one instruction has been executed.
        /// </summary>
        EXCEPTION_SINGLE_STEP = 0x80000004,

        /// <summary>
        /// The thread used up its stack.
        /// </summary>
        EXCEPTION_STACK_OVERFLOW = 0xC00000FD
        
    }

    /// <summary>
    /// Specifies what type of Event is contained within the DebugEvent class struct
    /// </summary>
    public enum DebugEventType : uint
    {
        /// <summary>
        /// contains a RIP_EVENT_INTERNAL
        /// </summary>
        RipEvent = 9,
        /// <summary>
        /// contains an OUTPUT_DEBUG_STRING_INTERNAL
        /// </summary>
        OutputDebugStringEvent = 8,
        /// <summary>
        /// contains an UNLOAD_DLL_DEBUG_EVENT_INTERNAL
        /// </summary>
        UnloadDllDebugEvent = 7,
        /// <summary>
        /// contains a LOAD_DLL_DEBUG_EVENT_INTERNAL
        /// </summary>
        LoadDllDebugEvent = 6,
        /// <summary>
        /// contains an EXIT_PROCESS_DEBUG_EVENT_INTERNAL
        /// </summary>
        ExitProcessDebugEvent = 5,
        /// <summary>
        /// contains an EXIT_THREAD_DEBUG_EVENT_INTERNAL
        /// </summary>
        ExitThreadDebugEvent = 4,
        /// <summary>
        /// contains a CREATE_PROCESS_DEBUG_EVENT_INTERNAL
        /// </summary>
        CreateProcessDebugEvent = 3,
        /// <summary>
        /// contains a CREATE_THREAD_DEBUG_EVENT_INTERNAL
        /// </summary>
        CreateThreadDebugEvent = 2,
        /// <summary>
        /// contains an EXCEPTION_DEBUG_EVENT_INTERNAL
        /// </summary>
        ExceptionDebugEvent = 1,
    }


    /// <summary>
    /// Specifies how to continue with the ContinueDebugger Native Routines.
    /// Defines constants that match up with arguments to the ContinueDebugEvent  native function
    /// either written format is fine they are the same value. They're there for user code readability
    /// </summary>
    public enum ContinueStatus : uint
    {
        /// <summary>
        ///  For exception events, this tells Windows that the event was handled. For all other events this continues the process
        /// </summary>
        DBG_CONTINUE = 0x00010002,
        /// <summary>
        ///  For exception events, this tells Windows that the event was handled. For all other events this continues the process
        /// </summary>
        DebugContinue = 0x00010002,

        /// <summary>
        /// For exception events, tell Windows that your debugger code did not handle the exception
        /// </summary>
        DBG_EXCEPTION_NOT_HANDLED = 0x80010001,
        /// <summary>
        /// For exception events, tell Windows that your debugger code did not handle the exception
        /// </summary>
        DebugExceptionNotHandled = DBG_EXCEPTION_NOT_HANDLED,
        /// <summary>
        /// defination from MSDN: Supported in Windows 10, version 1507 or above, this flag causes dwThreadId to replay the existing breaking event after the target continues. By calling the SuspendThread API against dwThreadId, a debugger can resume other threads in the process and later return to the breaking. 
        /// </summary>
        DBG_REPLY_LATER = 0x40010001,
        /// <summary>
        /// defination from MSDN: Supported in Windows 10, version 1507 or above, this flag causes dwThreadId to replay the existing breaking event after the target continues. By calling the SuspendThread API against dwThreadId, a debugger can resume other threads in the process and later return to the breaking. 
        /// </summary>
        DebugReplyLater = DBG_REPLY_LATER,
    }

    /// <summary>
    /// Descripes how the exception can be dealt with
    /// </summary>
    enum ExceptionFlags
    {
        /// <summary>
        /// It's ok to continue running the program
        /// </summary>
        EXCEPTION_CONTINUEABLE = 0x0,
        /// <summary>
        /// Program can't continue from here.
        /// </summary>
        EXCEPTION_NONCONTINUABLE = 0x1
    }

    public enum BaseAccessViolation
    {
        /// <summary>
        /// Target read something it should not have
        /// </summary>
        Read = 0,
        /// <summary>
        /// Target wrote to something it should not have
        /// </summary>
        Write = 1,
        /// <summary>
        /// Target trigged a Data Execution Prevention event
        /// </summary>
        DEP = 8
    }

    /// <summary>
    ///  Flags related to GetFinalFileNameByHandle()
    /// </summary>
    [Flags]
    internal enum FinalFilePathFlags
    {
        FILE_NAME_NORMALIZED = 0x0,
        VOLUME_NAME_DOS = FILE_NAME_NORMALIZED,
        FILE_NAME_OPENED = 0x8,
        VOLUME_NAME_GUID = 0x1,
        VOLUME_NAME_NONE = 0x4,
        VOLUME_NAME_NT = 0x2
    }

}
