using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DiverTraceApiCodeGen.NewVersion
{
    /// <summary>
    /// NeoNativeTypeData is the enum Neo CodeGen System uses to generate values to pass
    /// pass to the DiverDll.
    /// </summary>
    public enum NeoNativeTypeData
    {
        /// <summary>
        /// Pointer to something. Can be comebinged with Ptr2 for a double pointer
        /// </summary>
        IsPtr1 = 1,
        /// <summary>
        /// Pointer to something. Can be comebinged with Ptr1 for a double pointer
        /// </summary>
        IsPtr2 = 2,

        /// <summary>
        /// Tells diver that the value is a resource. Resources need a context type (see the context stuff in this enum) and call to tell the debugger about this will resource + type are genereated. This enum is stripped before being sent.
        /// </summary>
        IsResource = 4,

        /// <summary>
        /// Unsigned 4 byte value
        /// </summary>
        U4 = 8,
        /// <summary>
        /// Signed 4 byte value
        /// </summary>
        I4 = 16,

        /// <summary>
        /// Unicode string that's null terminated
        /// </summary>
        LPWStr = 32,

        /// <summary>
        /// Ansi string that's null terminated
        /// </summary>
        LPStr = 64,

        /// <summary>
        /// A 4 byte value containing 0 or 1. See the WIN32 Api BOOL data type.
        /// </summary>
        Bool = 128,

        /// <summary>
        /// Contains a structure of an unspecified length. PROTOTYPE
        /// </summary>
        LPStruct = 256,


        /// <summary>
        /// The data value or return type is a file handle.
        /// </summary>
        ContextFileHandle = 512,
        ContextThreadHandle = 1024
    }

    /// <summary>
    /// The native function return type and args used by the NeoRoutines
    /// </summary>
    public struct NeoNativeFunctionArg : IEquatable<NeoNativeFunctionArg>
    {

        
        public NeoNativeFunctionArg(string ArgType, string ArgName, NeoNativeTypeData DebugHint)
        {
            this.ArgName = ArgName;
            this.ArgType = ArgType;
            this.DebugCodeGenHint = DebugHint;
        }

        public NeoNativeFunctionArg(string ArgType, NeoNativeTypeData DebugHint)
        {
            this.ArgName = string.Empty;
            this.ArgType = ArgType;
            this.DebugCodeGenHint = DebugHint;
        }
        /// <summary>
        /// The string literal that will become the C/C++ Arg Type
        /// </summary>
        public string ArgType { get; set; }
        /// <summary>
        /// The string literal that will become the C/C++ Arg Name
        /// </summary>
        public string ArgName { get; set; }
        /// <summary>
        /// Hint for the code generate on how to make the code / deal with this type. This will be passed back to the Debugger during a TrackFunc Exeception
        /// </summary>
        public NeoNativeTypeData DebugCodeGenHint { get; set; }

        
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else
            {
                if (obj is NativeFunctionArg arg)
                {
                    return Equals(arg);
                }
                return false;
            }
        }
        /// <summary>
        /// case a hash of this structure
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ArgName.GetHashCode() + ArgType.GetHashCode() + DebugCodeGenHint.GetHashCode();
        }

        /// <summary>
        /// is left equal to right
        /// </summary>
        /// <param name="left">compare against right</param>
        /// <param name="right">compare against left</param>
        /// <returns>true if equal</returns>
        public static bool operator ==(NeoNativeFunctionArg left, NeoNativeFunctionArg right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// is left diffrent from right
        /// </summary>
        /// <param name="left">compare against right</param>
        /// <param name="right">compare against left</param>
        /// <returns>true if NOT equal / and different</returns>
        public static bool operator !=(NeoNativeFunctionArg left, NeoNativeFunctionArg right)
        {
            return !(left == right);
        }

        public bool Equals(NeoNativeFunctionArg other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (other.ArgName != ArgName)
                {
                    return false;
                }

                if (other.ArgType != ArgType)
                {
                    return false;
                }

                if (other.DebugCodeGenHint != DebugCodeGenHint)
                {
                    return false;
                }

                return true;
            }
        }


    }
}
