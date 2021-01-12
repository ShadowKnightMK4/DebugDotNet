using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Tools;
using DiverDotNetDiverExt;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DebugDotNet.Win32.Diver
{
    /*
     * This class contains routines to interpret the Diver Communication Exceptions
     */

    /// <summary>
    /// Ways to modify how the Detoured Function will change exception
    /// </summary>
    [Flags]
    public enum DebuggerResponseFlags : uint
    {
        /// <summary>
        ///  no special modifications are need to code execution
        /// </summary>
        NoResponse = 0,
        /// <summary>
        ///  Detoured Function does not call original and instead returns struct.Arg1 unmodified 
        /// </summary>
        ForceReturn = 2

    };


    /// <summary>
    /// This is list of all 'exceptions' the diver DLL will use to communicate with a debugger.
    /// Some require the debugger to modify a native pointer / data struct to indicate if it worked.
    /// The type of DiverExceptionList will be specified under <see cref="DebugEvent.ExceptionInfo"/>'s <see cref="ExceptionDebugInfo.TopLevelException"/> 's <see cref="ExceptionRecord.ExceptionCode"/>
    /// </summary>
#pragma warning disable CA1028 // Enum Storage should be Int32
    public enum DiverExceptionList : uint
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        /// <summary>
        /// This is very much like output debug string but offers a little more functionality
        /// <list type="table">
        /// <listheader>Argument format for list in <see cref="ExceptionRecord.ExceptionInformation"/>'s list structure </listheader>
        /// <item>
        /// <term>Item at position 0</term>
        /// <description>This is a pointer to a 4 byte value that your debugger will set if it understood this exception. If you don't set this to non-zero the message is sent as OutputDebugString() </description>
        /// </item>
        /// <item>
        /// <term>Item at position 1</term>
        /// <description>This is a number that specifies a 'channel' to place the message in, assuming your debugger supports it. IF the DLL uses channel 0, you'll be processing an <see cref="OutputDebugStringInfo"/> event instead of this. The value itself has no special meaning</description>
        /// </item>
        /// <item>
        /// <term> Item at position 2 </term>
        /// <description>This is a pointer to an Unicode string in the context of the debugged process's memory if non-zero </description> mn
        /// </item>
        /// <item>
        /// <term>Item at position 3 </term>
        /// <description>This is the length in characters of the string in question or is zero if the string is null (or does not have any characters)</description>
        /// </item>
        /// </list>
        /// </summary>

        DiverCom_OutputDebugChannel = 0,


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

    [StructLayout(LayoutKind.Sequential)]
    public struct DiverDebugResponse
    {
        /// <summary>
        /// For compatibility purposes. This is set to the size of the structure in bytes before being sent to debugger
        /// </summary>
        public UInt32 StructSize { get; set; }
        /// <summary>
        /// For compatibility purposes. This is set to the size of a poitner in the debugged program in bytes
        /// </summary>
        public UInt32 SizeOfPtr { get; set; }

        /// <summary>
        /// To use the diver DLL, this is required to be set to non-zero when finshed handling a diver exception
        /// </summary>
        public bool DebuggerSeenThis { get; set; }
        /// <summary>
        /// Control the response of the routine with this flag set
        /// </summary>

        public DebuggerResponseFlags ResponseFlags;
        /// <summary>
        /// An argument that is dependant on the ResponseFlags value for use
        /// </summary>
        public UInt32 Arg1;
    }


    /// <summary>
    /// This is trigged if Diver routines try to access <see cref="DebugEvent"/> stuff while said event is not a DiverMessage
    /// </summary>
    public class NotDiverException: Exception
    {
        /// <summary>
        /// Make an instance of this exception with the message. 
        /// </summary>
        /// <param name="message">describes error that happened</param>
        public NotDiverException(string message) : base(message)
        {
        }
        /// <summary>
        /// Make an instance of this exception with the message. 
        /// </summary>
        /// <param name="message">describes error that happened</param>
        /// <param name="innerException">Contains inner error</param>
        public NotDiverException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// make an instance with no messaging
        /// </summary>
        public NotDiverException()
        {

        }
    }
    /// <summary>
    /// This triggered when attempting to access message pameters for a diver message when the wrong message is contained.
    /// For example, accessing <see cref="DiverExceptionList.DiverComSetVersion"/> routines while handling a <see cref="DiverExceptionList.DiverComTrackFuncCall"/> will trigger this
    /// </summary>
    public class WrongDiverMessageException: Exception
    {
        /// <summary>
        /// Make an instance of this exception with the message. 
        /// </summary>
        /// <param name="message">describes error that happened</param>
        public WrongDiverMessageException(string message) : base(message)
        {
        }
        /// <summary>
        /// Make an instance of this exception with no messaging
        /// </summary>
        public WrongDiverMessageException()
        {
        }
        /// <summary>
        /// Make an instance of this exception with the message. 
        /// </summary>
        /// <param name="message">describes error that happened</param>
        /// <param name="innerException">Contains inner error</param>
        public WrongDiverMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    /// <summary>
    /// Implements interpreting diver stuff as extensions to the <see cref="DebugEvent"/> class
    /// </summary>
    public static class Diver
    {
        /// <summary>
        /// enum to define results of ensuring a <see cref="DebugEvent"/> contains the appropriate Diver Message
        /// </summary>
        internal enum CheckResult
        {
            /// <summary>
            /// Contained event is not an exception
            /// </summary>
            NotException = 0,
            /// <summary>
            /// contained event is not a diver message
            /// </summary>
            NotDiverMessage = 1,
            /// <summary>
            /// contained event is not the expected diver message
            /// </summary>
            InvalidDiverMessage = 2,
            /// <summary>
            /// contained event is a full match with the expected diver message
            /// </summary>
            FullMatch = 3
        }

        /// <summary>
        /// examines the passed message ind throws an error if there's a program. This is the common checking code for the public extentions
        /// </summary>
        /// <param name="result"></param>
        /// <param name="Target"></param>
        /// <param name="_1">The this argument for the message</param>
        internal static void ThrowAnyBadDiverMessage(this DebugEvent _1, CheckResult result, DiverExceptionList Target)
        {
            string MsgString;
            if (Target == 0)
            {
                MsgString = "Any Diver Message";
            }
            else
            {
                MsgString = Enum.GetName(typeof(DiverExceptionList), Target);
            }
            switch (result)
            {
                case CheckResult.FullMatch:
                    return;
                case CheckResult.NotException:
                case CheckResult.NotDiverMessage:
                    throw new NotDiverException(DiverMsgs_En.DetourDiverMessageNonExistent);
                case CheckResult.InvalidDiverMessage:
                    throw new WrongDiverMessageException(string.Format(CultureInfo.CurrentCulture, DiverMsgs_En.DetourDiverMessageMismatchMessage, MsgString));

                default:
                    throw new NotImplementedException(DiverMsgs_En.DetourDiverMessageNotImplemented);
            }

        }

        /// <summary>
        /// calls <see cref="VerifyException"/> and <see cref="ThrowAnyBadDiverMessage(DebugEvent, CheckResult, DiverExceptionList)"/>
        /// </summary>
        /// <param name="that"></param>
        /// <param name="Target"></param>
        internal static void VerifyAndCheck(DebugEvent that, DiverExceptionList Target)
        {
            ThrowAnyBadDiverMessage(that, VerifyException(that, Target), Target);
        }
        /// <summary>
        /// used to verify diver messages are correct when getting info
        /// </summary>
        /// <param name="that"></param>
        /// <param name="Target"></param>
        /// <returns></returns>
        internal static CheckResult VerifyException(DebugEvent that, DiverExceptionList Target)
        {
            if (that == null)
            {
                return CheckResult.NotException;
            }
            if (that.dwDebugEventCode != DebugEventType.ExceptionDebugEvent)
            {
                return CheckResult.NotException;
            }

            var Info = that.ExceptionInfo;
            if (Diver.Codes.Contains((DiverExceptionList) Info.TopLevelException.ExceptionCode)== false)
            {
                return CheckResult.NotDiverMessage;
            }

            if (Info.TopLevelException.ExceptionCode != (ExceptionCode) Target)
            {
                return CheckResult.InvalidDiverMessage;
            }
            return CheckResult.FullMatch;

        }

        #region argument constant aids for diver messages
        /// <summary>
        /// Current Diver protocol always sets the first argument to the the 4 dword pointer for the debuger to set
        /// </summary>
        private const int CommonMsgDebugFlagSet = 0;

        /// <summary>
        /// during a <see cref="DiverExceptionList.DiverComSetVersion"/> call, this argument will contain the version of the communcation protocal the Diver DLL is using
        /// </summary>
        public const int DiverMessageVersionGetData = 1;

        /// <summary>
        /// during a <see cref="DiverExceptionList.DiverComSetVersion"/> call, this argument will contain the a pointer to a 4 byte value that yoru debugger will set to non zero if you understood the exception
        /// </summary>
        public const int DiverMessageVersionDebugFlagPointer = CommonMsgDebugFlagSet;

        /// <summary>
        /// This points to a 4 byte value that your debugger will set if your code understands the exception.
        /// </summary>
        public const int DiverMessageTrackFuncDebuggerResponseSet = 1;
        /// <summary>
        /// This is the position of a c++ vector containing pointers to arguments passed in the call during a <see cref="DiverExceptionList.DiverComTrackFuncCall"/>. First entry is how many entries are left. A value of X means there are X-1 entries left in the array. 
        /// </summary>
        public const int DiverMessageTrackFuncVectorArgPtrs = 2;
        /// <summary>
        /// This is the position of a c++ vector containing argument hint data during a <see cref="DiverExceptionList.DiverComTrackFuncCall"/>. First entry is how many entries are left. A value of X means there are X-1 entries left in the array. 
        /// </summary>
        public const int DiverMessageTrackFuncVectorArgHint = 3;
        /// <summary>
        /// This is the position of a Return  value in the <see cref="DiverExceptionList.DiverComTrackFuncCall"/> argument list. IMPORANT THIS IS NOT IMPLEMENTED FULLY.
        /// </summary>
        public const int DiverMessageTrackFuncRetHint = 4;
        /// <summary>
        /// This is the position of the <see cref="DiverDebugResponse"/> pointer in the Exception Arguments list during a <see cref="DiverExceptionList.DiverComTrackFuncCall"/> event
        /// </summary>
        public const int DiverMessageTrackFuncDebugStructPosition = 5;
        /// <summary>
        /// This is the position of Unicode string char pointer that is the function's name in the Exception Arguments list during a <see cref = "DiverExceptionList.DiverComTrackFuncCall" /> event
        /// </summary>
        public const int DiverMessageTrackFuncSourceName = 6;

        /// <summary>
        /// This is the length in chars of the <see cref="DiverMessageTrackFuncSourceName"/> string or 0 if there was no supplied string
        /// </summary>
        public const int TrackFuncSourceNameSize = 7;
        #endregion

        /// <summary>
        /// When adding a new exception to diver protocol. Don't forget to add it here
        /// </summary>
        private static readonly List<DiverExceptionList> Codes = new List<DiverExceptionList>() { DiverExceptionList.DiverComSetVersion, DiverExceptionList.DiverComTrackFuncCall };


        
        /// <summary>
        /// internal version of <see cref="GetDiverDebugResponse(DebugEvent)"/>. Allows diabling the check if diver exception
        /// </summary>
        /// <param name="that">the event to check</param>
        /// <param name="Check">true means we call <see cref="IsDiverException(DebugEvent)"/> first</param>
        /// <returns>returns the structure or null on nothing to return</returns>
        internal static DiverDebugResponse? GetDiverDebugResponse(this DebugEvent that, bool Check=false)
        {
            if (Check)
            {
                VerifyAndCheck(that, DiverExceptionList.DiverComTrackFuncCall);
                if (!that.IsDiverException())
                {
                    return null;
                }
                if (that.ExceptionInfo.TopLevelException.ExceptionCode != (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall)
                {
                    return null;
                }
            }
            if (that.ExceptionInfo.TopLevelException.ExceptionCode == (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall)
            {
                return (DiverDebugResponse)UnmangedToolKit.RemoteReadToStructure(Process.GetProcessById(that.dwProcessId).Handle, new IntPtr(that.ExceptionInfo.TopLevelException.ExceptionInformation[DiverMessageTrackFuncDebugStructPosition]), typeof(DiverDebugResponse));
            }
            return null;
        }

        /// <summary>
        /// return the <see cref="DiverDebugResponse"/> struct that is for thar <see cref="DiverExceptionList.DiverComTrackFuncCall"/> call or null if this does not contain an exception of that type
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public static DiverDebugResponse? GetDiverDebugResponse(this DebugEvent that)
        {
            return GetDiverDebugResponse(that, true);
        }
        
        
        /// <summary>
        /// This writes the passed <see cref="DiverDebugResponse"/> struct back into the debugged process's memory. indeicated by that
        /// </summary>
        /// <param name="that"></param>
        /// <param name="Updated">The structure to write back into the memory of the process that triggerd this event</param>
        public static void UpdateDiverDebugResponseStruct(this DebugEvent that, DiverDebugResponse Updated)
        {
            if (that != null)
            {
                VerifyAndCheck(that, DiverExceptionList.DiverComTrackFuncCall);
                UnmangedToolKit.RemoteWriteToStructure(Process.GetProcessById(that.dwProcessId).Handle, new IntPtr(that.ExceptionInfo.TopLevelException.ExceptionInformation[DiverMessageTrackFuncDebugStructPosition]), Updated, typeof(DiverDebugResponse));
            }
        }


        /// <summary>
        /// During a <see cref="DiverExceptionList.DiverComSetVersion"/> event, this returns the data that is the version of the protocal to use
        /// </summary>
        /// <param name="that"></param>
        /// <returns></returns>
        public static uint GetDiverVersionInfo(this DebugEvent that)
        {
            if (that != null)
            {
                VerifyAndCheck(that, DiverExceptionList.DiverComTrackFuncCall);
                if (IsDiverException(that) == true)
                {
                    if (that.ExceptionInfo.TopLevelException.ExceptionCode == (ExceptionCode)DiverExceptionList.DiverComSetVersion)
                    {
                        return that.ExceptionInfo.TopLevelException.ExceptionInformation[Diver.DiverMessageVersionGetData];
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Updates the 4 byte value to indicate that your debugger understood the message
        /// </summary>
        /// <param name="That"></param>
        /// <param name="Handled"></param>
        public static void SetDiverHandledValue(this DebugEvent That, bool Handled)
        {
            if (IsDiverException(That))
            {
                if (Handled)
                {
                    UnmangedToolKit.WriteDWORD(Process.GetProcessById(That.dwProcessId).Handle, new IntPtr(That.ExceptionInfo.TopLevelException.ExceptionInformation[CommonMsgDebugFlagSet]), 1);
                }
                else
                {
                    UnmangedToolKit.WriteDWORD(Process.GetProcessById(That.dwProcessId).Handle, new IntPtr(That.ExceptionInfo.TopLevelException.ExceptionInformation[CommonMsgDebugFlagSet]), 0);
                }
            }
            else
            {
                ThrowAnyBadDiverMessage(That, CheckResult.NotDiverMessage, 0);
            }
        }
        /// <summary>
        /// Diver Extension for DebugEvent. Check if the exception (if any) contained is a Diver Message
        /// </summary>
        /// <param name="That">The DebugEvent to check</param>
        /// <returns>true if That contains an exception and that exception is a DiverMessage. (There's an internal list it checks)</returns>
        public static bool IsDiverException(this DebugEvent That)
        {
            if (That == null)
            {
                return false;
            }
            else
            {
                if (That.dwDebugEventCode != Enums.DebugEventType.ExceptionDebugEvent)
                {
                    return false;
                }
                return Codes.Contains((DiverExceptionList)That.ExceptionInfo.TopLevelException.ExceptionCode);
            }
        }
    }

}
