using DebugDotNet.Win32.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace DebugDotNet.Win32.Enums
{

    /// <summary>
    /// Settings to fill out <see cref="StartupInfo.wShowWindow"/> value
    /// </summary>
    [Flags]
#pragma warning disable CA1028 // Enum Storage should be Int32
    public enum ShowWindowSettings : short
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        /// <summary>
        /// Minimizes a window, even if the thread that owns the window is not responding.This flag should only be used when minimizing windows from a different thread. 
        /// </summary>
        ForceMinnimize = 11,


        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        Hide = 0,

        /// <summary>
        /// //Maximizes the specified window. 
        /// </summary>
        Maximize = 3,

        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        Minimize = 6,

        /// <summary>
        /// Activates and displays the window.If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window. 
        /// </summary>
        Restore = 9,

        /// <summary>
        ///Activates the window and displays it in its current size and position.
        /// </summary>
        Show = 5,

        /// <summary>
        /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
        /// </summary>
        ShowDefault = 10,

        /// <summary>
        ///  Activates the window and displays it as a maximized window.
        /// </summary>
        ShowMaximized = Maximize,

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        ShowMinimized = 2,

        /// <summary>
        /// Displays the window as a minimized window.This value is similar to SW_SHOWMINIMIZED, except the window is not activated. 
        /// </summary>
        ShowMinimizedNoActivate = 7,


        /// <summary>
        /// Displays the window in its current size and position.This value is similar to SW_SHOW, except that the window is not activated. 
        /// </summary>
        ShowNa = 8,


        /// <summary>
        /// Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated. 
        /// </summary>
        ShowNoActivate = 4,


        /// <summary>
        /// Normal Window
        /// </summary>
        ShowNormal = 1
    }



    /// <summary>
    /// Flags used in the <see cref="StartupInfo.dwFlags"/> member
    /// </summary>
    [Flags]
    public enum StartInfoFlags 
    {
        /// <summary>
        /// Indicates that the cursor is in feedback mode for two seconds after CreateProcess is called. The Working in Background cursor is displayed (see the Pointers tab in the Mouse control panel utility).
        /// If during those two seconds the process makes the first GUI call, the system gives five more seconds to the process. If during those five seconds the process shows a window, the system gives five more seconds to the process to finish drawing the window.
        /// The system turns the feedback cursor off after the first call to GetMessage, regardless of whether the process is drawing.
        /// </summary>
        StartInfoForceOnFeedBack = 0x00000040,

        /// <summary>
        /// Indicates that the feedback cursor is forced off while the process is starting. The Normal Select cursor is displayed.
        /// </summary>
        StartInfoForceOffFeedBack = 0x00000080,

        /// <summary>
        /// Indicates that any windows created by the process cannot be pinned on the taskbar.
        /// This flag must be combined with STARTF_TITLEISAPPID.
        /// </summary>
        StartInfoPreventPinning = 0x00002000,

        /// <summary>
        /// Indicates that the process should be run in full-screen mode, rather than in windowed mode.
        /// This flag is only valid for console applications running on an x86 computer.
        /// </summary>
        StartInfoRunFullscreen = 0x00000020,

        /// <summary>
        /// The lpTitle member contains an AppUserModelID. This identifier controls how the taskbar and Start menu present the application, and enables it to be associated with the correct shortcuts and Jump Lists. Generally, applications will use the SetCurrentProcessExplicitAppUserModelID and GetCurrentProcessExplicitAppUserModelID functions instead of setting this flag. For more information, see Application User Model IDs.
        /// If STARTF_PREVENTPINNING is used, application windows cannot be pinned on the taskbar. The use of any AppUserModelID-related window properties by the application overrides this setting for that window only.
        /// This flag cannot be used with STARTF_TITLEISLINKNAME.
        /// </summary>
        StartInfoTitleIsAppId = 0x00001000,



        /// <summary>
        /// The lpTitle member contains the path of the shortcut file (.lnk) that the user invoked to start this process. This is typically set by the shell when a .lnk file pointing to the launched application is invoked. Most applications will not need to set this value.
        /// This flag cannot be used with STARTF_TITLEISAPPID. 
        /// </summary>
        StartInfoTitleIsLinkName = 0x00000800,


        /// <summary>
        /// The command line came from an untrusted source. For more information, see MSDN
        /// </summary>
        StartInfoUntrustedSource = 0x00008000,

        /// <summary>
        /// The dwXCountChars and dwYCountChars members contain additional information.
        /// </summary>
        StartInfoUseCountChars = 0x00000008,

        /// <summary>
        /// The dwFillAttribute member contains additional information.
        /// </summary>
        StartInfoUseFillAttribute = 0x00000010,

        /// <summary>
        /// The hStdInput member contains additional information.
        /// This flag cannot be used with STARTF_USESTDHANDLES.
        /// </summary>
        StatInfoUseHotKey = 0x00000200,

        /// <summary>
        /// The dwX and dwY members contain additional information.
        /// </summary>
        StartInfoUsePositon = 0x00000004,

        /// <summary>
        /// The wShowWindow member contains additional information.
        /// </summary>
        StartInfoUseShowWindow = 0x00000001,

        /// <summary>
        /// The dwXSize and dwYSize members contain additional information.
        /// </summary>
        StartInfoUseSize = 0x00000002,
        /// <summary>
        /// The hStdInput, hStdOutput, and hStdError members contain additional information.
        /// If this flag is specified when calling one of the process creation functions, the handles must be inheritable and the function's bInheritHandles parameter must be set to TRUE. For more information, see Handle Inheritance.
        /// If this flag is specified when calling the GetStartupInfo function, these members are either the handle value specified during process creation or INVALID_HANDLE_VALUE.
        /// Handles must be closed with CloseHandle when they are no longer needed.
        /// This flag cannot be used with STARTF_USEHOTKEY.
        /// </summary>
        StartInfoUseStdHandles = 0x00000100
    }



    /// <summary>
    /// Specifies how DebugWorkerThread class is begin debugging the target process.
    /// </summary>
    public enum DebuggerCreationSetting
    {
        /// <summary>
        /// Attach to running existing program
        /// </summary>
        AttachRunningProgram = 1,
        /// <summary>
        /// spawn program then attach (same as launching program first) then using AttachRunningProgram
        /// </summary>
        RunProgramThenAttach = 2,
        /// <summary>
        /// Create the process explicity with the worker thread with the debug flag passed
        /// </summary>
        CreateWithDebug = 3
    };

    /// <summary>
    /// Process Creation Flags used by Detours and Debug Process
    /// </summary>
    [Flags]
    public enum CreateFlags : int
    {
        /// <summary>
        /// Launch with no Debug flag
        /// </summary>
        DoNotDebug = 0x00000000,
        /// <summary>
        /// Launch with debugging this process plus any processors it spawns
        /// </summary>
        DebugProcessAndChild = 0x00000001,
        /// <summary>
        /// Debug *just* this process
        /// </summary>
        DebugOnlyThisProcess = 0x00000002,
        /// <summary>
        /// If the target is a console app, this forces a new console rather than using the one (you) have
        /// </summary>
        ForceNewConsole = 0x00000010
    }

    /// <summary>
    /// from MSDN Exception Debug Events, this is the type of exception that happened
    /// </summary>
    public enum ExceptionCode : UInt32
    {
        /// <summary>
        /// CTRL-C Pressed in a console app
        /// </summary>
        DebugConsoleControlC = 0x40010005,
        /// <summary>
        /// The thread tried to read from or write to a virtual address for which it does not have the appropriate access.  
        /// </summary>
        ExceptionAccessViolation = 0xC0000005,
        
        /// <summary>
        /// The thread tried to access an array element that is out of bounds and the underlying hardware supports bounds checking. 
        /// </summary>
        ExceptionArrayBoundsExceeded = 0xC000008C,
        
        
        /// <summary>
        /// A breakpoint was encountered. 
        /// </summary>
        ExceptionBreakpoint = 0x80000003,
        
        /// <summary>
        ///The thread tried to read or write data that is misaligned on hardware that does not provide alignment. For example, 16-bit values must be aligned on 2-byte boundaries; 32-bit values on 4-byte boundaries, and so on.  
        /// </summary>
        ExceptionDataTypeMisalignment = 0x80000002,

        /// <summary>
        ///  One of the operands in a floating-point operation is denormal. A denormal value is one that is too small to represent as a standard floating-point value. 
        /// </summary>
        ExceptionFltDenormalOperand = 0xC000008D,
        

        /// <summary>
        /// The thread tried to divide a floating-point value by a floating-point divisor of zero. 
        /// </summary>
        ExceptionFltDivideByZero = 0xC000008E,

        /// <summary>
        /// The result of a floating-point operation cannot be represented exactly as a decimal fraction. 
        /// </summary>
        ExceptionFltInexactResult = 0xC000008F,

        /// <summary>
        /// This exception represents any floating-point exception not included in this list. 
        /// </summary>
        ExceptionFltInvalidOperation = 0xC0000090,

        /// <summary>
        /// The exponent of a floating-point operation is greater than the magnitude allowed by the corresponding type. 
        /// </summary>
        ExceptionFloatOverflow = 0xC0000091,

        /// <summary>
        /// The stack overflowed or underflowed as the result of a floating-point operation. 
        /// </summary>
        ExceptionFloatStackCheck = 0xC0000092,

        /// <summary>
        ///The exponent of a floating-point operation is less than the magnitude allowed by the corresponding type.  
        /// </summary>
        ExceptionFloatUnderflow = 0xC0000093,

        /// <summary>
        /// The thread tried to execute an invalid instruction. 
        /// </summary>
        ExceptionIllegalInstruction = 0xC000001D,

        /// <summary>
        /// The thread tried to access a page that was not present, and the system was unable to load the page. For example, this exception might occur if a network connection is lost while running a program over the network. 
        /// </summary>
        ExceptionInPageError = 0xC0000006,

        /// <summary>
        /// The thread tried to divide an integer value by an integer divisor of zero. 
        /// </summary>
        ExceptionIntDivideByZero = 0xC000009,

        /// <summary>
        /// The result of an integer operation caused a carry out of the most significant bit of the result. 
        /// </summary>
        ExceptionIntOverflow = 0xC0000095,

        /// <summary>
        /// An exception handler returned an invalid disposition to the exception dispatcher. Programmers using a high-level language such as C should never encounter this exception.
        /// </summary>
        ExceptionInvalidDispostion = 0xC0000026,

        /// <summary>
        /// The thread tried to continue execution after a noncontinuable exception occurred. 
        /// </summary>
        ExceptionNonContinuableException = 0xC0000025,

        /// <summary>
        /// The thread tried to execute an instruction whose operation is not allowed in the current machine mode. 
        /// </summary>
        ExceptionPrivInstruction = 0xC0000096,

        /// <summary>
        /// A trace trap or other single-instruction mechanism signaled that one instruction has been executed.
        /// </summary>
        ExceptionSingleStep = 0x80000004,

        /// <summary>
        /// The thread used up its stack.
        /// </summary>
        ExceptionStackOverflow = 0xC00000FD
        
    }

    /// <summary>
    /// Specifies what type of Event is contained within the DebugEvent class struct
    /// </summary>
    public enum DebugEventType 
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
        DebugContinue = DBG_CONTINUE,

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
    /// What Type of Exception happened (not limited to just these)
    /// </summary>
    public enum ExceptionFlagType
    {
        /// <summary>
        /// It's ok to continue running the program
        /// </summary>
        Continuable = 0x0,
        /// <summary>
        /// Program can't continue from here.
        /// </summary>
        NonContinuable = 0x1
    }

    /// <summary>
    /// Describes A Memory Access violation
    /// </summary>
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
        DEP = 8,
        /// <summary>
        /// Something that DebugDotNet does not reconize. 
        /// </summary>
        Other = -1,
    }

    /// <summary>
    ///  Flags related to GetFinalFileNameByHandle()
    /// </summary>
    [Flags]
    public enum FinalFilePathFlags
    {
        /// <summary>
        /// Returns the normalized finalname. Also is the default.
        /// </summary>
        FileNameNormalized = 0x0,
        /// <summary>
        /// Return the path with the drive letter. This is the default.
        /// </summary>
        VolumeNameDos = FileNameNormalized,
        /// <summary>
        /// Returns the opened (not normalized) filename
        /// </summary>
        FileNameOpened = 0x8,
        /// <summary>
        /// Returned string contains the path with a volume GUID path instead of the drive name
        /// </summary>
        VolumeNameGuid = 0x1,
        /// <summary>
        /// returns string contains no volume information
        /// </summary>
        VolumeNameNone = 0x4,
        /// <summary>
        /// returns the string containing the volume's device path.
        /// </summary>
        VolumeNameNt = 0x2
    }

}
