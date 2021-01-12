using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Tools;
using DebugDotNet.Win32.Diver;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace DebugDotNet.Win32.DiverOld
{
    /*
     * This file implements the c# classes to allow DebugDotNet to understand the Diver Protocal
     * 
     */

    public enum DEBUGGER_RESPONSE_FLAGS : uint
{
	// no special modifications
	NoResponse = 0,
	// Detoured Function does not call original and instead returns struct.Arg1
	ForceReturn = 2

};
/// Debugger may also modify arguments already passes as seen fit.
/*typedef struct DEBUGGER_RESPONSE
{
    // Set to sizeof(DEBUGGER_RESPONSE) by detoured function
    DWORD SizeOfThis;
    // Set to sizeof(VOID*) By detoured function
    DWORD SizeOfPtr;
    // must be set by debugger to indicate that this was seen and modidied.
    BOOL DebuggerSeenThis;
    // Flags to change stuff
    DEBUGGER_RESPONSE_FLAGS Flags;
    // Argument for certain flags
    DWORD Arg1;
};
*/
/// <summary>
/// struct to communicate with the diver dll with
/// </summary>
[StructLayout(LayoutKind.Sequential)]
    public struct DiverDebugResponse
    {

        public UInt32 StructSize;
         public UInt32 SizeOfPtr;

        public bool DebuggerSeenThis;

        public DEBUGGER_RESPONSE_FLAGS ResponseFlags;

        public UInt32 Arg1;
    }
    /// <summary>
    /// This is list of all 'exceptions' the diver DLL will use to communicate with a debugger.
    /// Some require the debugger to modify a native pointer / data struct to indicate if it worked.
    /// The type of DiverExceptionList will be specified under <see cref="DebugEvent.ExceptionInfo"/>'s <see cref="ExceptionDebugInfo.TopLevelException"/> 's <see cref="ExceptionRecord.ExceptionCode"/>
    /// </summary>
#pragma warning disable CA1028 // Enum Storage should be Int32
    public enum DiverExceptionList: uint
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        /// <summary>
        /// 
        /// This is thrown by the DiverDll using SEH to indicate that it is offering to communicate native function arguments and allow modification of targed functions
        /// and calls to the Debugger via this protocol.  
        /// 
        /// Exception Arguments located at <see cref="ExceptionRecord.ExceptionInformation"/> 
        /// <list type="table">
        /// <listheader>Argument format for list in <see cref="ExceptionRecord.ExceptionInformation"/>'s list structure
        /// </listheader>
        /// <item>
        /// <term>Item at Position 0</term>
        /// <Description>Argument 0 will contain the version of the Diver Protocol that the DLL is using to communicate </Description>
        /// </item>
        /// <item>
        /// <term>Item at position 1</term>
        /// <description>Argument 1 is a pointer to a 4 byte integer that the DLL expects your debugger to set if it understood the message. You can use <see cref="DiverException.SetDiverHandledFlag(bool)"/> as an easy way to set it. If you don't do this the DLL may turn off any dicer output it provides</description>
        /// </item>
        /// </list>
        /// </summary>
        DiverComSetVersion = 0xEFFFFFFE,


        /// <summary>
        /// This is thrown by the Diver DLL using SEH to indicate a function that it has detoured has been called.
        /// This is thrown before the function call and your debugger gets a chance to inspect things and make modifications if needed.
        /// Be sure to call the <see cref="DiverException.SetDiverHandledFlag(bool)"/> function to inform the DLL that you made changes
        /// <list type="table">
        ///     <listheader>Argument format for list in <see cref="ExceptionRecord.ExceptionInformation"/>'s list structure</listheader>
        ///     <item>
        ///         <term>Item at position 0</term>
        ///         <description>Argument 0 is a pointer to a 4 byte integer that the DLL expects your debugger to set if it understood the message. You can use <see cref="DiverException.SetDiverHandledFlag(bool)"/> as an easy way to set it. If you don't do this, any changes you to data structs could be ignored. This is a memory location in the Debugger Response structure</description>
        ///     </item>
        ///     <item>
        ///         <term>Item at position 1</term>
        ///         <description>This points to a C++ vector containing pointers to arguments if any. The ULONG_PTR at position[0] in this vector specifies the remaining arguments in the list. If it is zero then the function has no arguments. Indices of [X] here match the type hints locate at [X]</description>
        ///     </item>
        ///     <item>
        ///         <term>Item at position 2</term>
        ///         <description>This points to a C++ vector containing a list of values that give hints to the arguments if any. The first value is always the number of remaining arguments. If it is zero then the function has no arguments. </description>
        ///     </item>
        ///     <item>
        ///         <term>Item as position 3</term>
        ///         <description>a pointer to a return value (this is allocated on the native stack) IMPORTANT This is not fully implemented and is currently meaningless</description>
        ///     </item>
        ///     <item>
        ///         <term>Item at position 4</term>
        ///         <description>UNUSED. Currently not used. Ment to be a type hint for the return value</description>
        ///     </item>
        ///     <item>
        ///         <term>Item as position 5</term>
        ///         <description>This will be a pointer to a Native Structure of type DEBUGGER_RESPONSE. You can modify function behavior here.</description>
        ///     </item>
        ///     <item>
        ///         <term>Item as position 6</term>
        ///         <description>This will point to an Unicode string that is the name of the function that was detoured.</description>
        ///     </item>
        ///     <item>
        ///         <term>Item at position 7</term>
        ///         <description>This specifies the number of characters in the Unicode string specified at 6. This can be zero if no string was passed during throwning the exception</description>
        ///     </item>
        /// </list>
        /// </summary>
        DiverComTrackFuncCall = 0xEFFFFFFD,
    }


    /// <summary>
    /// Is thrown when trying to modify a debugged process  with <see cref="DiverTools"/> and the exception is not actually a diver message
    /// </summary>
    public class NotDiverMessage : InvalidOperationException
    { 
        /// <summary>
        /// Make instance of this with default message
        /// </summary>
        public NotDiverMessage() : base("The Exception Code passed to a Diver Communcation function is not a Diver Message" )
        {
            
        }

        public NotDiverMessage(ExceptionCode Code) : base("The Exception Code " + Code.ToString() + " is not a diver exception")
        {

        }
        public NotDiverMessage(string Arg): base("The Exception Code " + Arg + "passed to a Diver Communcation function is not a Diver Message" )
        {

        }

        public NotDiverMessage(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// The Base Class that implements some Diver stuff
    /// </summary>
    public static class DiverTools
    {
        #region Hard Coded stuff subject to change

        #region DiverExceptionList.DiverComSetVersion Array locations
        ///<summary>
        ///the entry in the list that we write to during an <see cref="DiverExceptionList.DiverComSetVersion"/> exception to say we understood it
        ///</summary> 
        public const int DiverSetVersionResponseWrite = 1;
        /// <summary>
        ///  the entry in the list that contains the version information specified during a <see cref="DiverExceptionList.DiverComSetVersion"/> execpiton
        /// </summary>
        public const int DiverSetVersionVersionData = 0;
        #endregion


        #region DiverExceptionList.DiverComResponse Array Locations
        /// the entry in the list that we write to during an <see cref="DiverExceptionList.DiverComTrackFuncCall"/> exception to say we understood it
        public const int DiverTrackFunctionResonseWrite = 0;
        /// <summary>
        /// Points to the entry in the list during a  an <see cref="DiverExceptionList.DiverComTrackFuncCall"> that will contain the Debug response struct ptr</see>
        /// </summary>
        public const int DiverTrackFunctionDebugStruct = 6;

        #endregion


        #endregion
        /// <summary>
        /// Tool to see if an exception code from a <see cref="ExceptionDebugInfo"/> is a diver message
        /// </summary>
        /// <param name="Code"></param>
        /// <returns>true if the passed value is defined in the <see cref="DiverExceptionList"/> enum</returns>
        public static bool IsDiverMessage(ExceptionCode Code)
        {
            switch (Code)
            {
                case (ExceptionCode)DiverExceptionList.DiverComSetVersion:
                    return true;
                case (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall:
                    return true;
            }
            
            return false;
        }

    

    }
}

namespace DebugDotNet.Win32.Diver
{


    /// <summary>
    /// This class operates on top of a <see cref="DebugEvent"/> that contains an event of type <see cref="DebugEvent.ExceptionInfo"/>
    /// The class reads data from the passed event
    /// </summary>
    public class DiverException
    {
        private DebugEvent ThisEvent;
        /// <summary>
        /// Interpret the passed <see cref="DebugEvent"/> in the Diver Protocol context
        /// </summary>
        /// <param name="InterpetEvent"></param>
        public DiverException(DebugEvent InterpetEvent)
        {
            if (InterpetEvent == null)
            {
                throw new ArgumentNullException(nameof(InterpetEvent));
            }
            ThisEvent = InterpetEvent;
        }

        /// <summary>
        /// gets the debugger response struct (if any) or sets it
        /// </summary>
        public DiverDebugResponse? DebuggerResponse
        { 
            get
            {
                ExceptionRecord Info = ThisEvent.ExceptionInfo.TopLevelException;
                switch (Info.ExceptionCode)
                {
                    case (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall:
                        {
                            DiverDebugResponse ret = new DiverDebugResponse();
                            return (DiverDebugResponse) UnmangedToolKit.RemoteReadToStructure(Process.GetProcessById(ThisEvent.dwProcessId).Handle, new IntPtr(ThisEvent.ExceptionInfo.TopLevelException.ExceptionInformation[DiverTools.DiverTrackFunctionDebugStruct]), typeof(DiverDebugResponse));
                            
                        }
                    default:
                        return null;
                }
            }
            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException("Can't set to null");
                }
                else
                {
                    ExceptionRecord Info = ThisEvent.ExceptionInfo.TopLevelException;
                    switch (Info.ExceptionCode)
                    {
                        case (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall:
                            {
                                Marshal.StructureToPtr(value, new IntPtr(Info.ExceptionInformation[DiverTools.DiverTrackFunctionDebugStruct]), false);
                                break;
                            }
                        default:
                            new InvalidOperationException("can't assign in this mode");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Set the flag to indicate to the DiverDll that the exception was seen and stuff was possibly modified
        /// </summary>
        ///<param name="Status">True means your debugger understood and responded to the diver message</param>
        ///<exception cref="InvalidOperationException"> may occur should the debugged process already quit due to c call of <see cref="Process.GetProcessById(int)"/></exception>
        public void SetDiverHandledFlag(bool Status)
        {
            uint StatusAsFlag;
            if (Status)
            {
                StatusAsFlag = 1;
            }
            else
            {
                StatusAsFlag = 0;
            }
            ExceptionDebugInfo Info = ThisEvent.ExceptionInfo;
            if (DiverTools.IsDiverMessage(Info.TopLevelException.ExceptionCode) == false)
            {
                throw new NotDiverMessage(Info.TopLevelException.ExceptionCode);
            }
            else
            {
                switch (Info.TopLevelException.ExceptionCode)
                {
                    case (ExceptionCode)DiverExceptionList.DiverComSetVersion:
                        {
                            UnmangedToolKit.WriteDWORD(Process.GetProcessById(ThisEvent.dwProcessId).Handle, new IntPtr(Info.TopLevelException.ExceptionInformation[DiverTools.DiverSetVersionResponseWrite]), StatusAsFlag);
                            break;
                        }
                    case (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall:
                        {
                            UnmangedToolKit.WriteDWORD(Process.GetProcessById(ThisEvent.dwProcessId).Handle, new IntPtr(Info.TopLevelException.ExceptionInformation[DiverTools.DiverTrackFunctionResonseWrite]), StatusAsFlag);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Does the passed <see cref="DebugEvent"/> contain a diver message
        /// </summary>
        public bool ContainsDiverMessage
        {
            get
            {
                if (ThisEvent.dwDebugEventCode != DebugEventType.ExceptionDebugEvent)
                {
                    return false;
                }
                return DiverTools.IsDiverMessage(ThisEvent.ExceptionInfo.TopLevelException.ExceptionCode);
            }
        }

        public string DiverMsgGetFunctionName
        {
            get
            {
                if (ContainsDiverMessage)
                {
                    ExceptionDebugInfo Info = ThisEvent.ExceptionInfo;
                    if (Info.TopLevelException.ExceptionCode == (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall)
                    {
                        string test;
                        if (Info.TopLevelException.ExceptionInformation[7] == 0)
                        {
                            return string.Empty;
                        }
                        else
                        {
                            return UnmangedToolKit.ExtractString(ThisEvent.dwProcessId,new IntPtr( Info.TopLevelException.ExceptionInformation[6]), new IntPtr( Info.TopLevelException.ExceptionInformation[7] ), true);
                        }
                    }
                }
                return string.Empty;
            }
        }
        /// <summary>
        /// During a <see cref="DiverExceptionList.DiverComSetVersion"/> exception, this returns the protocal the diver dll will use
        /// </summary>
        /// <returns>returns the value of <see cref="ExceptionRecord.ExceptionInformation"/> at position <see cref="DiverTools.DiverSetVersionVersionData"/> if the current exception is <see cref="DiverExceptionList.DiverComSetVersion"/> </returns>
        public uint DiverMsgGetVersion
        {
            get
            {
                if (ContainsDiverMessage)
                {
                    ExceptionDebugInfo Info = ThisEvent.ExceptionInfo;
                    if (Info.TopLevelException.ExceptionCode == (ExceptionCode)DiverExceptionList.DiverComSetVersion)
                {
                        return Info.TopLevelException.ExceptionInformation[DiverTools.DiverSetVersionVersionData];
                    }
                }
                return 0;
            }
        }

        
    }
}
