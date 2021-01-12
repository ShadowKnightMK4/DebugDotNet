using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using static DiverTraceApiCodeGen.CodeGen;

namespace DiverTraceApiCodeGen
{
    public struct NativeFunctionArg : IEquatable<NativeFunctionArg>
    {

        public NativeFunctionArg(string ArgType, string ArgName, UnmanagedType DebugHint)
        {
            this.ArgName = ArgName;
            this.ArgType = ArgType;
            this.DebugCodeGenHint = DebugHint;
        }

        public NativeFunctionArg(string ArgType, UnmanagedType DebugHint)
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
        public UnmanagedType DebugCodeGenHint { get; set; }

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
        public static bool operator ==(NativeFunctionArg left, NativeFunctionArg right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// is left diffrent from right
        /// </summary>
        /// <param name="left">compare against right</param>
        /// <param name="right">compare against left</param>
        /// <returns>true if NOT equal / and different</returns>
        public static bool operator !=(NativeFunctionArg left, NativeFunctionArg right)
        {
            return !(left == right);
        }

        public bool Equals(NativeFunctionArg other)
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
    /// <summary>
    /// Contains a prototype and settings to specify code to emit.
    /// </summary>
    public class NativeFunctionClass
    {
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, " {{ Original={0}, Detour={1} }}", FunctionName, DetourFunctionName);
        }

        public enum LinkMode
        {
            /// <summary>
            /// The source is a direct assigment of the routine function. This triggers an import being built into the final DLL
            /// </summary>
            Static = 0,
            /// <summary>
            /// The DllMain routine will get a model handle to the library (ntdll or kernel32 only) and use GetProcAddress() to get a pointer
            /// </summary>
            LoadLibAtStart = 1,
            /// <summary>
            /// The LoadLibrary gang is detoured and then calls code to begin the detour if the right dll is loaded.
            /// </summary>
            LoadLibAsNeeded = 2
        }

        private LinkMode Choice = LinkMode.Static;

        /// <summary>
        /// Tell the generate how to source the original function
        /// </summary>
        public LinkMode HowToGetSourceFunction
        {
            get
            {
                return Choice;
            }
            set
            {
                Choice = value;
            }
        }

        /// <summary>
        /// Get a DetourAttachFunction with values setup to do that with this function
        /// </summary>
        /// <returns></returns>
        public DetourAttachFunction GenerateAttachFunction()
        {
            DetourAttachFunction ret = new DetourAttachFunction();
            ret.TemplateArgs[DetourAttachFunction.OriginalFunctionKey] = FunctionName;
            ret.TemplateArgs[DetourAttachFunction.RoutineNameKey] = "DetourAttach_" + FunctionName;
            ret.TemplateArgs[DetourAttachFunction.ReportDebugStringEntryKey] = "1";
            ret.TemplateArgs[DetourAttachFunction.ReportDebugStringDetourResultsKey] = "1";
            ret.TemplateArgs[DetourAttachFunction.ReplacementFunctionKey] = DetourFunctionName;
            ret.TemplateArgs[DetourAttachFunction.TypeDefVarName] = string.Empty;
            ret.TemplateArgs[DetourAttachFunction.TypeDefVarType] = string.Empty;
            ret.TemplateArgs[DetourAttachFunction.DetourSourceDllName] = this.SourceDll;

            switch (Choice)
            {

                case LinkMode.Static:
                    //ret.TemplateArgs[DetourAttachFunction.DetourOriginalSource] = "DirectAssigment";
                    ret.TemplateArgs[DetourAttachFunction.DetourOriginalSource] = DetourAttachFunction.DetourOriginalSourceDirectAssigment;
                    break;
                case LinkMode.LoadLibAtStart:
                    ret.TemplateArgs[DetourAttachFunction.DetourOriginalSource] = DetourAttachFunction.DetourOriginalSourceLoadLibraryOnStart;
                    break;
                case LinkMode.LoadLibAsNeeded:
                    ret.TemplateArgs[DetourAttachFunction.DetourOriginalSource] = DetourAttachFunction.DetourOriginalSourceLoadLibraryOnDemand;
                    break;
            }

            
            ret.TemplateArgs[DetourAttachFunction.DetourSourceDllName] = string.Empty;
            return ret;
        }
        /// <summary>
        /// describes how to emit the detour function
        /// </summary>
        public EmitStyles FuncStyle { get; set; }
        /// <summary>
        /// Base name of this function that is going to be detoured
        /// </summary>
        public string FunctionName { get; set; }
        /// <summary>
        /// if non-null this is the name of the function when emitting the detoured function. If null a default value of "Detoured_" + functionanme is used
        /// </summary>
        public string DetourFunctionName { 
            get
            {
                if (BackingDetourFuncName == null)
                {
                    return "Detoured_" + FunctionName;
                }
                else
                {
                    return BackingDetourFuncName;
                }
            }
            set
            {
                BackingDetourFuncName = value;
            }
        }

        private string BackingDetourFuncName;
        /// <summary>
        /// if non-null this is the name of the function when emitting a typedef function ptr. If null a default value of functionname + "_ptr" is used
        /// </summary>

        public string PreferredTypeDefName { get; set; }
        /// <summary>
        /// calling convention of this function. 
        /// </summary>
        public EmitDeclareFunctionSpecs CallingConvention { get; set; } = EmitDeclareFunctionSpecs.None;
        /// <summary>
        /// List of the function arguments
        /// </summary>
        public List<NativeFunctionArg> Arguments { get; }= new List<NativeFunctionArg>();
        /// <summary>
        /// return type for this function
        /// </summary>
        public NativeFunctionArg ReturnType { get; set; }

        /// <summary>
        /// This is the name of the dll to check for when during a <see cref="Choice"/> of <see cref=LinkMode.LoadLibAsNeeded"/>
        /// </summary>
        public string SourceDll { get; set; }
        private void ArgumentToString(ref List<string> ArgType, ref List<string> ArgNames)
        {
            List<string> TmpType = new List<string>(Arguments.Count);
            List<string> TmpName = new List<string>(Arguments.Count);

            Arguments.ForEach(p => { TmpType.Add(p.ArgType); TmpName.Add(p.ArgName); });

            ArgType = TmpType;
            ArgNames = TmpName;

        }

        /// <summary>
        /// the bool that holds a call to a RaiseExceptionTrackFunc() call in generated code
        /// </summary>
        const string DiverExceptionReturnVal = "DiverExceptionReturnVal";

        /// <summary>
        /// Name of the vector that will contain  a list of arguments for diver debug
        /// </summary>
        const string DiverCppArgName = "Arguments";
        /// <summary>
        /// Name of vector that will contain list of Arg Hints for diver debug
        /// </summary>
        const string DiverCppArgTypes = "ArgHints";

        /// <summary>
        /// if Diver code iss generated, this is a wstringstream that buffers for OutputDebugStringW()
        /// </summary>
        const string DebugMsgBufferStr = "DebugMsgBuffer";
        /// <summary>
        /// Name of varable that will contain the result of calling the target fuction (if any)
        /// </summary>
        const string DiverCppCallResultName = "result";
        /// <summary>
        /// is the struct type that is zero-out and passed as a pointer to the debugger
        /// </summary>
        const string DiverCppResponseStructType = "DEBUGGER_RESPONSE";
        /// <summary>
        /// name of the struct that is zero-out and passed as a pointer to the debugger
        /// </summary>
        const string DiverCppResponseStructName = "DebugResponse";

        /// <summary>
        /// Write a typedef with optionally a replacement name matching the stored function
        /// </summary>
        /// <param name="Output"></param>
        public void EmitTypedefFunction(Stream Output, string ForceName="")
        {
            if (string.IsNullOrEmpty(ForceName))
            {
                ForceName = FunctionName + "_ptr";
            }
            List<string> ArgTypes, ArgNames;
            ArgTypes = ArgNames = null;

            ArgumentToString(ref ArgTypes, ref ArgNames);
            EmitFunctionTypeDef(Output, ReturnType.ArgType, CallingConvention, ForceName, ArgTypes, ArgNames);
        }
        /// <summary>
        /// Write the function that will be the detour of the contained function 
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="SourceFunction">This is a variable that holds a source of the function from either assigment or GetProcCall()</param>
        public void EmitDetourFunction(Stream Output, string SourceFunction)
        {
            string FinalFuncName;
            string FinalTypeDefName;
            List<string> ArgTypes, ArgNames;
            ArgTypes = ArgNames = null;

            if (string.IsNullOrEmpty(SourceFunction))
            {
                if (string.IsNullOrEmpty(this.PreferredTypeDefName))
                {
                    FinalTypeDefName = FunctionName + "Ptr";
                }
                else
                {
                    FinalTypeDefName = PreferredTypeDefName;
                }
            }
            else
            {
                FinalTypeDefName = SourceFunction;
            }


            if (string.IsNullOrEmpty( SourceFunction))
            {
                SourceFunction = FunctionName;
            }

            if (string.IsNullOrEmpty(DetourFunctionName))
            {
                FinalFuncName = "Detoured_" + FunctionName; 
            }
            else
            {
                FinalFuncName = DetourFunctionName;
            }
            ArgumentToString(ref ArgTypes, ref ArgNames);
            EmitDeclareFunction(Output, ReturnType.ArgType, CallingConvention, FinalFuncName, ArgTypes, ArgNames);
            WriteNewLine(Output);

            WriteLeftBracket(Output, true);
            if (string.IsNullOrEmpty(ReturnType.ArgType) == false)
            {
                EmitDeclareVariable(Output, ReturnType.ArgType, "result");
            }
            else
            {
                WriteBeginCommentBlock(Output);
                WriteLiteralNoIndent(Output, "Function does not return  value. Declaring a variable to hold nothing is pointless here");
                WriteEndCommentBlock(Output);
            }
            if (FuncStyle.HasFlag(EmitStyles.DirectCall))
            {
                WriteComment(Output, "Function was generated to be a quick small stop that just calls the original function");
                if (string.IsNullOrEmpty(ReturnType.ArgType) == false)
                {
                    //EmitCallFunction(Output, this.FunctionName, ArgTypes, ArgNames, "result");
                    EmitCallFunction(Output, FinalTypeDefName, ArgTypes, ArgNames, "result");
                    EmitReturnX(Output, "result");
                }
                else
                {
                    EmitCallFunction(Output, FinalTypeDefName, ArgTypes, ArgNames, string.Empty);
                    EmitReturnX(Output, string.Empty);
                }

            }
            else
            {


                if (FuncStyle.HasFlag(EmitStyles.IncludeDiverCode))
                {
                    WriteComment(Output, "Code was generated to include diver protocol. ");
                    EmitDeclareVariable(Output, DiverCppResponseStructType, DiverCppResponseStructName);
                    EmitDeclareVector(Output, DiverCppArgName);
                    EmitDeclareVector(Output, DiverCppArgTypes);
                    EmitCallZeroMemory(Output, DiverCppResponseStructName, DiverCppResponseStructType);
                }

                if (FuncStyle.HasFlag(EmitStyles.DebugStringNameOnly) || FuncStyle.HasFlag(EmitStyles.DebugStringArgs) || (FuncStyle.HasFlag(EmitStyles.DebugStringNameRet)))
                {
                    if (ArgNames.Count != 0)
                    {
                        WriteComment(Output, "This variable buffers Unicode Strings to send to OutputDebugStringW\\r\\n");
                        EmitDeclareWideStringStream(Output, "DebugMsgBuffer", false);
                    }
                    else
                    {
                        WriteBeginCommentBlock(Output);
                        WriteNewLine(Output);
                        WriteComment(Output, "The Variable DebugMsgBuffer is used to buffer Arguments send to OutputDebugStringW().");
                        WriteComment(Output, "With no Arguments defined for this detour routine. The Variable is not needed");
                        EmitDeclareWideStringStream(Output, "DebugMsgBuffer", false);
                        WriteEndCommentBlock(Output);
                        WriteNewLine(Output);
                    }
                }

                if (FuncStyle.HasFlag(EmitStyles.DebugStringNameOnly))
                {
                    EmitCallOutputDebugString(Output, FunctionName + " reached OK.\\r\\n", true);
                }

                if (FuncStyle.HasFlag(EmitStyles.DebugStringArgs))
                {
                    List<string> ArgBufferStrings = new List<string>();
                    if (ArgNames.Count != 0)
                    {
                        WriteComment(Output, "Pack the argument list into an Unicode Code string and make a call to OutpuDebugStringW()");
                        ArgNames.ForEach(p => ArgBufferStrings.Add("(" + p + ")"));
                        ArgBufferStrings.Add("endl");
                        EmitInsertStream(Output, DebugMsgBufferStr, ArgBufferStrings, false);

                        ArgBufferStrings.Clear();
                        EmitCallOutputDebugString(Output, DebugMsgBufferStr + CodeGen.GetStreamStringToStringPiece(), false);
                        EmitClearWideStreamStream(Output, DebugMsgBufferStr);
                    }
                    else
                    {
                        WriteComment(Output, "Code was generated to pack arguments into unicode string but function requires no arguments. Skipping that");
                    }
                }

                if (FuncStyle.HasFlag(EmitStyles.IncludeDiverCode))
                {
                    // emit code to put pointers to args in a vector
                    CodeGen.WriteComment(Output, "DiverCode is emitted. This emits a buffer and a call to raise a special exeption that allows the debugger to modify arguments");
                    CodeGen.WriteCommentBlock(Output, "The Bool Below stores the call to the RaiseExceptionTrackFunc() that diver protocal uses");
                    CodeGen.EmitDeclareVariable(Output, "BOOL", DiverExceptionReturnVal, "FALSE");

                    CodeGen.WriteComment(Output, "The format of the arguments that the debugger will get is from left to right i.e. the Source code difination left to right");
                    CodeGen.WriteComment(Output, "The number of  entries in the vector pairs will always be equal to the number of arguments plus 1");
                    CodeGen.WriteComment(Output, "The first element in the vector is always the number of remaining arguments.");

                    CodeGen.EmitPushVectorValue(Output, DiverCppArgName, Arguments.Count.ToString(CultureInfo.InvariantCulture ));
                    CodeGen.EmitPushVectorValue(Output, DiverCppArgTypes, Arguments.Count.ToString(CultureInfo.InvariantCulture));
                    for (int step = 0; step < Arguments.Count; step++)
                    {
                        CodeGen.EmitPushVectorValue(Output, DiverCppArgName, "(ULONG_PTR)" + "&" + Arguments[step].ArgName, false);
                    }

                    for (int step = 0; step < Arguments.Count; step++)
                    {
                        CodeGen.EmitPushVectorValue(Output, DiverCppArgTypes, "(ULONG_PTR)" + ((int) Arguments[step].DebugCodeGenHint).ToString(CultureInfo.InvariantCulture), false);
                    }


                    List<string> Args = new List<string>();
                    Args.Add("L" + CodeGen.AddQuotesIfNone(FunctionName));
                    Args.Add("" + DiverCppArgName);
                    Args.Add("" + DiverCppArgTypes);
                    Args.Add("(ULONG_PTR*)&" + DiverCppCallResultName);
                    Args.Add("(ULONG_PTR*)" + "0"); // return value hint not used
                    Args.Add("&" + DiverCppResponseStructName);

                    EmitCallFunction(Output, "RaiseExceptionTrackFunc", null, Args, DiverExceptionReturnVal); 

                    WriteIf(Output, DiverExceptionReturnVal + " == FALSE");
                    {
                        WriteLeftBracket(Output, true);
                        WriteComment(Output, "Debugger Did not Set the seen it val in the DEUGGER_RESONSE struct datatype. The attached debugger (if any) likely does not understand our exception");
                        WriteComment(Output, "So We just call the routine, assign the value and return");
                        if (string.IsNullOrEmpty(ReturnType.ArgType) == false)
                        {
                            EmitCallFunction(Output, FinalTypeDefName, null, ArgNames, DiverCppCallResultName);
                            EmitReturnX(Output, DiverCppCallResultName);
                        }
                        else
                        {
                            EmitCallFunction(Output, FinalTypeDefName, null, ArgNames, string.Empty);
                            EmitReturnX(Output, string.Empty);
                        }

                        WriteRightBracket(Output, true);
                    }
                    WriteComment(Output, "If we reach here. We need to examine the debugger response struct for modifications");


                    WriteIf(Output, DiverCppResponseStructName + ".Flags == ForceReturn");
                    WriteLeftBracket(Output, true);
                    if (string.IsNullOrEmpty(ReturnType.ArgType) == false)
                    {
                        EmitReturnX(Output, string.Format(CultureInfo.InvariantCulture, "(({0}){1})",  ReturnType.ArgType, DiverCppResponseStructName + ".Arg1"));
                        
                    }
                    else
                    {
                        EmitReturnX(Output, string.Empty);
                    }
                    WriteRightBracket(Output, true);

                    WriteComment(Output, "Debugger was free to change the arguments during the exception All that's lest is to call the function with the arguments");
                    WriteComment(Output, "TODO: Modify generator to copy arguments to new memory so that we guard access access violations ");

                    if (string.IsNullOrEmpty(ReturnType.ArgType) == false)
                    {
                        EmitCallFunction(Output, FinalTypeDefName, ArgTypes, ArgNames, "result");
                        EmitReturnX(Output, "result");
                    }

                    else
                    {
                        EmitCallFunction(Output, this.FunctionName, ArgTypes, ArgNames, string.Empty);
                        EmitReturnX(Output, string.Empty);
                    }

                }
                else
                {
                    if (string.IsNullOrEmpty(ReturnType.ArgType) == false)
                    {
                        EmitCallFunction(Output, this.FunctionName, ArgTypes, ArgNames, "result");
                        EmitReturnX(Output, "result");
                    }
                    else
                    {
                        EmitCallFunction(Output, this.FunctionName, ArgTypes, ArgNames, string.Empty);
                        EmitReturnX(Output, string.Empty);
                    }
                }



            }


            
            WriteRightBracket(Output, true);
        }
        
        [Flags]
        public enum EmitStyles
        {
            /// <summary>
            /// Just call the true function. This is overwritten by any other flags
            /// </summary>
            DirectCall = 1,
            /// <summary>
            /// Include code that raises diver exceptions to give debugger a change to modify the function call
            /// </summary>
            IncludeDiverCode = 2,
            /// <summary>
            /// OutputDebugStringW() is called with "FunctionName" was reached
            /// </summary>
            DebugStringNameOnly = 4,
            /// <summary>
            /// Debug string include arg specs
            /// </summary>
            DebugStringArgs = 8,
            /// <summary>
            /// Includes reture value
            /// </summary>
            DebugStringNameRet = 16,

            /// <summary>
            /// IMPORTANT. DO NOT USE for general code. This assumes one of the LoadLibraryXXX routines is being called and adds code to call a function to trigger DetourAttach() stuff on load. Setting this flag outside of that can cause exceptions and incorrect code to be triggered
            /// </summary>
           
            LoadLibraryCheckInternal = 32,

        }
    }
}
