using System;
using System.Collections.Generic;
using System.Text;

namespace DiverDllCodeGenVarTypes
{
    /// <summary>
    /// Used as common storage for variables. 
    /// This is used to keep DiverCodeGen Variable Const and DebugDotNetDiverReader same values for
    /// easy of use.
    /// </summary>
    [Flags]
    public enum DiverVarEnumTypes
    {
        /// <summary>
        /// If set, the resource triggers an DiverNotifyResourceAccess Exception to notify the debugger of the resource
        /// Should Resource Validation be set then it triggers only if it works and DiverNotifyResourceNotLocated exception on falure
        /// </summary>
        ValueTriggersResourceSignal = 1,
        /// <summary>
        /// If set, Values of null after the original detoured routine are treated as failure
        /// </summary>
        ValidTriggerBadNull = 2,
        /// <summary>
        /// If set, values of the Win32 INVALID_HANDLE_VALUE after calling the original routine are treated as bad
        /// </summary>
        ValidTriggerBadInvalidHandleValue = 4,
        /// <summary>
        /// A value of False (or null) after calling the original routine means it did not work
        /// </summary>
        ValidTriggerBadFalse = 8,
        /// <summary>
        /// A valid of true (or 1) means the call did not work
        /// </summary>
        ValidTriggerBadTrue = 16,
        /// <summary>
        /// A valid of -1 means it did not work.
        /// </summary>
        ValidTriggerBadNeg1 = 32,

        /// <summary>
        /// Unsigned 1 byte value
        /// </summary>
        U1 = 64,
        /// <summary>
        /// Signed 1 byte value
        /// </summary>
        S1 = 128,
        /// <summary>
        /// 2 byte signed value 
        /// </summary>

        U2 = 256,

        S2 = 512,

        S4 = 1024,

        U4 = 2048,

        /// <summary>
        /// Belongs to something returned by CreateFile and kin
        /// </summary>
        ResourceIsWin32FileHandle = 4096,

        /// <summary>
        /// Belongs to something make buy opening a Socket
        /// </summary>
        ResourceIsWin32Socket = 0x2000,

        /// <summary>
        /// Belongs to something opened in the registry
        /// </summary>
        ResourceIsWin32RegHandle = 0x4000,

        /// <summary>
        /// The CreateProcess Flags
        /// </summary>
        ProcessOpenFlags = 0x8000,
        /// <summary>
        /// Generic Access Flags
        /// </summary>
        GenericOpenFlags = 0x10000,

        /// <summary>
        /// Share Access Flags
        /// </summary>
        GenericShareFlags = 0x20000,




         




    }
}
