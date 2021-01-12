using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using DiverTraceApiCodeGen;
namespace DiverTraceApiCodeGen.NewVersion
{
    /// <summary>
    /// Contains code to contain a C/C++ function, its arguments, and can generate the attach function as well as the detour function
    /// </summary>
    public class NeoNativeFunction : NeoFunctionPiece
    {
        #region Enums
        public enum FunctionType
        {
            /// <summary>
            /// function just gets a reference to the routine and lets the C/C++ linker do the rest. Attach code is then in the generated function
            /// </summary>
            StaticLink = 0,
            /// <summary>
            /// The function is to be detoured upon library load. This causes the LoadLibraryXXX set to be detoured also.
            /// </summary>
            OnDemandLink = 1
        }
        #endregion

        /// <summary>
        /// This is the mode we are generating this function in. 
        /// </summary>
        public FunctionType LinkMode { get; set; } = FunctionType.StaticLink;

        
        #region static stuff

        /// <summary>
        /// Construct a list of NeoNativeTypeData args from the passed NeoNativeFunctionArg list.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static List<NeoNativeTypeData> ExtractArgumentDebugHints(List<NeoNativeFunctionArg> args)
        {
            if ((args == null) || (args.Count == 0))
            {
                return null;
            }
            else
            {
                List<NeoNativeTypeData> ret = new List<NeoNativeTypeData>();

                args.ForEach(p => {
                    ret.Add(p.DebugCodeGenHint);
                });
                return ret;
            }
        }

        public static List<string> ExtractArgumentNames(List<NeoNativeFunctionArg> args)
        {

            if ((args == null) || (args.Count == 0))
            {
                return null;
            }
            else
            {
                List<string> ret = new List<string>();

                args.ForEach(p => {
                    ret.Add(p.ArgName);
                });
                return ret;
            }
        }
        
        public static List<string> ExtractArgumentTypes(List<NeoNativeFunctionArg> args )
        {
            if ((args == null) || (args.Count == 0))
            {
                return null;
            }
            else
            {
                List<string> ret = new List<string>();

                args.ForEach(p => {
                    ret.Add(p.ArgType);
                });
                return ret;
            }
        }

        #endregion

        /// <summary>
        /// If set the Generated Function that we are detouring the original too is generated via diver protocol.
        /// </summary>
        public virtual bool ReplaceFuncWantDiver { get; set; } = true;

        

        /// <summary>
        /// Highest priority. This makes the stub just call the original function
        /// </summary>
        public virtual bool JustCallIt { get; set; } = false;

        /// <summary>
        /// if true then call is made to output debug string with the name of the original routine
        /// </summary>
        public virtual bool OutputDebugName { get; set; } = true;

        /// <summary>
        /// if true then we build a buffer and emit the arguments as a series of pointers
        /// </summary>
        public virtual bool OutputDebugArguments { get; set; } = true;
        /// <summary>
        /// For the DetourAttach code, this causes a function call to DetourIgnoreToSmall() to be generated.
        /// </summary>
        public virtual bool AttachFuncCallDetourIgnoreToSmall { get; set; } = true;

        /// <summary>
        /// Tells what Chanel for the attach function to emit debug strings too. Default of 0 will be OutputDebugStringW()
        /// </summary>
        public virtual int AttachFuncDebugStringChannel { get; set; } = 0;

        /// <summary>
        /// If an error happens when attempting to attach to the target function, we'll write to the specified channel otherwise we write each step's result to the channel
        /// </summary>
        public virtual bool AttachFuncOnlyLogErrors { get; set; } = true;


        /// <summary>
        /// Only applies to the <see cref="FunctionType.OnDemandLink"/>, this specifies a DLL string to check for.
        /// </summary>
        public virtual string DynamicLinkDllName { get; set; }

        /// <summary>
        /// Becomes the name of the Replacement routine that will be generated.
        /// </summary>
        public override string RoutineName
        {
            get
            {
                if (string.IsNullOrEmpty(base.RoutineName))
                {
                    if (string.IsNullOrEmpty(OriginalFunctionName) == false)
                    {
                        return "ReplacementDiver_" + OriginalFunctionName;
                    }
                    return null;
                }
                else
                {
                    return base.RoutineName;
                }
            }
            set
            {
                base.RoutineName = value;
            }
        }


        /// <summary>
        /// name of the routine that will contain the code to Detour the original function. If empty then resolves to "DetourAttachDetach_" being pasted at the beginning of the routine's name
        /// </summary>
        public virtual string AttachFunctionName
        {
            get
            {
                if (string.IsNullOrEmpty(AttachFunctionName_Backing))
                {
                    if (string.IsNullOrEmpty(OriginalFunctionName) == false)
                    {
                        return "DetourAttachDetach_" + OriginalFunctionName;
                    }
                    return null;
                }
                else
                {
                    return AttachFunctionName_Backing;
                }
            }
            set
            {
                AttachFunctionName_Backing = value;
            }
        }

        /// <summary>
        /// Contains the original function's name. For example "IsDebuggerPresent()"
        /// </summary>
        public virtual string OriginalFunctionName { get; set; }
        /// <summary>
        /// pointer that will be called as the original function for Example "IsDebuggerPresentPtr"
        /// </summary>
        public virtual string OriginalFunctionNamePtr 
        {
            get
            {
                if (string.IsNullOrEmpty( OriginalFunctionNamePtr_Backing))
                {
                    if (string.IsNullOrEmpty(OriginalFunctionName ) == false)
                    {
                        return "PtrTo_" + OriginalFunctionName;
                    }
                    else
                    {
                        return null;
                    }
                }
                return OriginalFunctionNamePtr_Backing;
            }
            set
            {
                OriginalFunctionNamePtr_Backing = value;
            }
        }


        /// <summary>
        /// Becomes the name of the typedef'd function pointer. If left to null or string.empty, it defaults to <see cref="OriginalFunctionName"/> with the value of "_PtrType" placed at the end minus the quotes
        /// </summary>
        public virtual string OriginalFunctionNamePtrType
        {
            get
            {
                if ( string.IsNullOrEmpty(OriginalFunctionNamePtrType_Backing))
                {
                    if (string.IsNullOrEmpty(OriginalFunctionName) == false)
                    {
                        return OriginalFunctionName + "_PtrType";
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return OriginalFunctionNamePtrType_Backing;
                }
            }
            set
            {
                OriginalFunctionNamePtrType_Backing = value;
            }
        }

        #region backing values
        private string OriginalFunctionNamePtrType_Backing;
        private string OriginalFunctionNamePtr_Backing;
        private string AttachFunctionName_Backing;
        #endregion

        /// <summary>
        /// Contains the Arguments that the original function expects, including parameter type info.
        /// </summary>
        public virtual List<NeoNativeFunctionArg> Arguments
        {
            get
            {
                if (BackingArguments == null)
                {
                    BackingArguments = new List<NeoNativeFunctionArg>();
                }
                return BackingArguments;
            }
        }
        private List<NeoNativeFunctionArg> BackingArguments;

        /// <summary>
        /// The functions's return value. The value <see cref="NativeFunctionArg.ArgType"/> defines it.
        /// </summary>
        /// <returns></returns>
        public NeoNativeFunctionArg ReturnValue { get; set; }


        /// <summary>
        /// Instructs how to emit the calling function and a few other things
        /// </summary>
        public CodeGen.EmitDeclareFunctionSpecs CallingConvention { get; set; } = CodeGen.EmitDeclareFunctionSpecs.WINAPI;
        /// <summary>
        /// generate the function that will attach to the original function. Can overwritten
        /// </summary>
        /// <returns></returns>
        public virtual string GenerateAttachFunction()
        {
            string easytype;
            if (string.IsNullOrEmpty(ReturnValue.ArgType))
            {
                easytype = "VOID";
            }
            else
            {
                easytype = ReturnValue.ArgType;
            }
            using (MemoryStream tmp = new MemoryStream())
            {
                switch (LinkMode)
                {
                    case FunctionType.StaticLink:
                        
                        break;
                    case FunctionType.OnDemandLink:
                        {
                            // not implemented but we catch it in the body. as there are plans to implement it 
                            // there's nothing to do here. at this point in time
                            break;
                        }
                    default:
                        {
                            throw new NotImplementedException(Enum.GetName(typeof(FunctionType), LinkMode));
                        }
                }


                CodeGen.EmitDeclareFunction(tmp, easytype, CallingConvention, AttachFunctionName, new List<string>() { "BOOL" }, new List<string>() { "AttachMode" });
                CodeGen.WriteLiteralNoIndent(tmp, GenerateAttachFunctionBody());
                return CodeGen.MemoryStreamToString(tmp);
            }
        }

        /// <summary>
        /// write the comments that will appear at the top of the attac hfunctions 
        /// </summary>
        /// <param name="target">write to this stream using <see cref="CodeGen.TargetEncoding"/></param>
        private void WriteAttachFunctionSettingComments(MemoryStream target)
        {
            if (this.LinkMode == FunctionType.OnDemandLink)
            {
                // ok;
            }
        }

        private void WriteDetourTypeDefAndName(MemoryStream Target)
        {
            CodeGen.EmitFunctionTypeDef(Target, ReturnValue.ArgType, CallingConvention, OriginalFunctionNamePtr, ExtractArgumentTypes(Arguments), ExtractArgumentNames(Arguments));
        }
             
        #region constants to reduce spelling errors
        const string DetourTransactionAttach_VarName = "Result";
        const string DetourTransactionAttach_VarType = "LONG";
        const string DetourTransactionNoError_Value = "NO_ERROR";
        const string DetourTransactionBegin = "DetourTransactionBegin";
        const string DetourTransactionCommit = "DetourTransactionCommit";
        const string DetourAttach = "DetourAttach";
        const string DetourSetIgnoreTooSmall = "DetourSetIgnoreTooSmall";
        const string DetourTransactionAbort = "DetourTransactionAbort";
        const string DetourDetach = "DetourDetach";
        const string DetourUpdateThread = "DetourUpdateThread";
        const string UnicodeBuffer = "DebugMessage";
        const string DEBUGGER_RESPONSE_VarType = "DEBUGGER_RESPONSE";
        const string DiverCppArgName = "VectorCppArgNames";
        const string DiverCppArgTypes = "VectorCppArgTypes";

        const string FuncCall_Result = "Result";
        const string DiverCall_Result = "DiverReturnReturnVal";

        /// <summary>
        /// name of the struct that is zero-out and passed as a pointer to the debugger
        /// </summary>
        const string DEBUGGER_RESPONSE_VarName = "DebugResponse";
        #endregion


    

        /// <summary>
        /// makes the attach function but not the title
        /// </summary>
        /// <returns></returns>
        private string GenerateAttachFunctionBody()
        {
            /// call the transaction routine passed with the passed arguments (and types if needed). Issues calls to emit code and TrasnactionAbort if TransactionRoutineToCall != TrascantionEnd
            /// tmp is where to write to
            /// Transaction routine is one of the DetourXXX things in thsi file.
            /// args is argtypes and not used
            /// Names are expressions / parameters for the args
            void CommonCodeGen_DetourTransactionSharedCode(MemoryStream tmp, string TransactionRoutineToCall, List<string> Args, List<string> Names)
            {
                CodeGen.EmitCallFunction(tmp, TransactionRoutineToCall, Args, Names, DetourTransactionAttach_VarName);

                CodeGen.WriteIf(tmp, string.Format(CultureInfo.InvariantCulture, "({0} != {1})", DetourTransactionAttach_VarName, DetourTransactionNoError_Value));
                CodeGen.WriteLeftBracket(tmp, true);
                if (AttachFuncOnlyLogErrors)
                {
                    CodeGen.WriteComment(tmp, "Code was generated to log errors");
                    CodeGen.EmitClearWideStreamStream(tmp, UnicodeBuffer);
                    List<string> MsgArgs = new List<string>();
                    MsgArgs.Add(string.Format(CultureInfo.InvariantCulture, "L\"The Call to the routine {0} failed with a result of \"", TransactionRoutineToCall));
                    MsgArgs.Add(DetourTransactionAttach_VarName);
                    CodeGen.EmitInsertStream(tmp, UnicodeBuffer, MsgArgs, false);
                    CodeGen.EmitCallOutputDebugString(tmp, UnicodeBuffer + CodeGen.GetStreamStringToStringPiece(), false);

                }
                else
                {
                    CodeGen.WriteComment(tmp, "Code was generated to not log when something worked");
                }

                // special case to ensure Detours cleans up anything it allocated on our behalf
                if (TransactionRoutineToCall != DetourTransactionBegin)
                {
                    CodeGen.EmitCallFunction(tmp, DetourTransactionAbort, null, null);
                }
                CodeGen.EmitReturnX(tmp, "FALSE");
                CodeGen.WriteRightBracket(tmp, true);
                CodeGen.WriteElse(tmp);
                CodeGen.WriteLeftBracket(tmp, true);
                if (AttachFuncOnlyLogErrors == false)
                {
                    CodeGen.EmitCallOutputDebugString(tmp, "The call to " + TransactionRoutineToCall + "() for " + OriginalFunctionName + " was OK", true);
                }
                else
                {
                    CodeGen.WriteComment(tmp, "Code Was generated to not log a string when the call worked");
                }
                CodeGen.WriteRightBracket(tmp, true);
            }
            
            using (MemoryStream tmp = new MemoryStream())
            {
                CodeGen.WriteNewLine(tmp);
                CodeGen.WriteLeftBracket(tmp, true);
                    WriteAttachFunctionSettingComments(tmp);
                CodeGen.EmitDeclareVariable(tmp, DetourTransactionAttach_VarType, DetourTransactionAttach_VarName, DetourTransactionNoError_Value);
                CodeGen.EmitDeclareWideStringStream(tmp, UnicodeBuffer, true);

               if (AttachFuncCallDetourIgnoreToSmall)
                {
                    CodeGen.WriteComment(tmp, "Code Generated to include a call to " + DetourSetIgnoreTooSmall);
                    CodeGen.EmitCallFunction(tmp, DetourSetIgnoreTooSmall, null, new List<string> {"TRUE"});
                }
                else
                {
                    CodeGen.WriteComment(tmp, "Code was generated to NOT include a call to " + DetourSetIgnoreTooSmall);
                    CodeGen.WriteComment(tmp, "The possibility exists that some functions to detour are to small to actually detour");
                }

                switch (LinkMode)
                {
                    case FunctionType.StaticLink:
                        {
                            CodeGen.WriteComment(tmp, "Static link chosen for " + OriginalFunctionName);
                            CodeGen.WriteComment(tmp, "This will end up in the import list for the generated DLL");
                            CodeGen.EmitAssignVariable(tmp, OriginalFunctionNamePtr, OriginalFunctionName);
                            break;
                        }
                    case FunctionType.OnDemandLink:
                        {
                            throw new NotImplementedException(Enum.GetName(typeof(FunctionType), LinkMode));
                        }
                    default:
                        {
                            throw new NotImplementedException(Enum.GetName(typeof(FunctionType), LinkMode));
                        }
                }
                

                CommonCodeGen_DetourTransactionSharedCode(tmp, DetourTransactionBegin, null, null);
                CommonCodeGen_DetourTransactionSharedCode(tmp, DetourUpdateThread, null, new List<string>() { "GetCurrentThread()" });

                List<string> AttachDetachArgs = new List<string>();
                AttachDetachArgs.Add(string.Format(CultureInfo.InvariantCulture, "&(PVOID&){0}", OriginalFunctionNamePtr));
                AttachDetachArgs.Add(string.Format(CultureInfo.InvariantCulture, "{0}", RoutineName));
                //CodeGen.EmitAssignVariable(Buffer, DetourResultValue, string.Format(CultureInfo.InvariantCulture, DetourAttachCall + "(&(PVOID&){0},  {1})", TypeDefName, TargetReplacement));
                CodeGen.WriteIf(tmp, "mode != FALSE");
                {
                    CodeGen.WriteLeftBracket(tmp, true);
                        CommonCodeGen_DetourTransactionSharedCode(tmp, DetourAttach, null, AttachDetachArgs);
                    CodeGen.WriteRightBracket(tmp, true);
                }
                CodeGen.WriteElse(tmp);
                CodeGen.WriteLeftBracket(tmp, true);
                CommonCodeGen_DetourTransactionSharedCode(tmp, DetourDetach, null, AttachDetachArgs);
                CodeGen.WriteRightBracket(tmp, true);



                CommonCodeGen_DetourTransactionSharedCode(tmp, DetourTransactionCommit, null, null);
                CodeGen.EmitReturnX(tmp, DetourTransactionAttach_VarName);

                CodeGen.WriteRightBracket(tmp, true);

                // close the routine
       

                return CodeGen.MemoryStreamToString(tmp);
            }
        }

        public enum DetourFunctionMode
        {
            /// <summary>
            /// Standard generation
            /// </summary>
            Normal = 0,
            /// <summary>
            /// Assuming the check for 'LoadLibraryA' passes, this includes code to attach to routines listed.  This is for On Demand Detouring 
            /// </summary>
            LoadLibraryA = 1,
            /// <summary>
            /// Assuming the check for 'LoadLibraryW' passes, this includes code to attach to routines listed.  This is for On Demand Detouring 
            /// </summary>
            LoadLibraryW = 2,
            /// <summary>
            /// Assuming the check for 'LoadLibraryExA' passes, this includes code to attach to routines listed.  This is for On Demand Detouring 
            /// </summary>
            LoadLibraryExA = 3,
            /// <summary>
            /// Assuming the check for 'LoadLibraryExW' passes, this includes code to attach to routines listed.  This is for On Demand Detouring 
            /// </summary>
            LoadLibraryExW = 4
        }


        public enum SpecialistMode
        {
            Normal = 1,
            NtSetInformationThread = 2,
            NtCreateThread = 3
        }

        /// <summary>
        /// Set ThreadHideFromDebugger (0x11) stripping from certain calls
        /// </summary>
        /// <param name="Mode"></param>
        protected void SetSpecialistMode(SpecialistMode Mode)
        {
            void NtSetInformationThreadSanityCheck()
            {
                // throws exception on failure.
                if (this.GetType() != typeof(NeoNtSetThreadInformation))
                {
                    throw new InvalidOperationException("Set Specialist Mode of NtSetInformationThread. Santity check failure for current object");
                }
            }
            AntiDebugMode = Mode;

            switch (Mode)
            {
                case SpecialistMode.Normal:
                    break;
                case SpecialistMode.NtSetInformationThread:
                    NtSetInformationThreadSanityCheck();
                    break;
                case SpecialistMode.NtCreateThread:
                default:
                    throw new NotImplementedException(Enum.GetName(typeof(SpecialistMode), Mode));
            }

        }

        /// <summary>
        /// contains if we are including code in the <see cref="GenerateDetourFunction"/> that will strip/fake
        /// calls when the ThreadHideFromDebugger (0x11) is used as an arugment for rectain reoutines.
        /// A sucesfull call when that argument blocks diver calls from being the debugging program.
        /// </summary>
        SpecialistMode AntiDebugMode = SpecialistMode.Normal;
        /// <summary>
        /// For the NeoLoadLibraryXXX stuff, this modifies how the detour function is genreated
        /// </summary>
        protected DetourFunctionMode DetourMode { get; set; } = DetourFunctionMode.Normal;
        /// <summary>
        /// Generate the function that the original will be detoured too.
        /// </summary>
        /// <returns></returns>
        public virtual string GenerateDetourFunction()
        {
            void NtSetThreadInformationDebugStrip(MemoryStream Output)
            {
                CodeGen.WriteIf(Output, string.Format(CultureInfo.InvariantCulture, "({0} && ThreadHideFromDebugger) == ThreadHideFromDebugger) 0", Arguments[1].ArgName));
                CodeGen.WriteLeftBracket(Output, true);
                {
                    CodeGen.WriteIf(Output, string.Format(CultureInfo.InvariantCulture, "(({0} == 0) && ({1} == NULL))", Arguments[3].ArgName, Arguments[2].ArgName));
                    CodeGen.WriteLeftBracket(Output, true);
                    CodeGen.WriteFormated(Output, "{0} = {0} & ~{1};", Arguments[1].ArgName, Arguments[1].ArgName, "ThreadHideFromDebugger");

                    CodeGen.WriteRightBracket(Output, true);
                    CodeGen.WriteRightBracket(Output, true);
                }
            }

            // declare a unicode string named unicode string and use MultiByteToWideChar() to convert to unicode
            void ConvertUnicodeString(MemoryStream Target, string AnsiName, string UnicodeName)
            {
                CodeGen.EmitDeclareVariable(Target, UnicodeName, "NULL");
                CodeGen.EmitDeclareVariable(Target, "CharChount", "0");
                CodeGen.WriteLeftBracket(Target, true);
                    CodeGen.WriteIf(Target, string.Format(CultureInfo.InvariantCulture, "{0} != NULL", AnsiName));
                    CodeGen.WriteLeftBracket(Target, true);
                    {
                        CodeGen.EmitDeclareVariable(Target, "int", "AnsiSize", "0");
                        List<string> Args = new List<string>() { "CP_ACP", "0", AnsiName, string.Format(CultureInfo.InvariantCulture, "strlen( {0} )", AnsiName), "NULL", "0"};
                        CodeGen.EmitCallFunction(Target, "MultiByteToWideChar", null, Args, "AnsiSize");

                        CodeGen.WriteIf(Target, "AnsiSize != 0");
                        CodeGen.WriteLeftBracket(Target, true);
                            CodeGen.EmitCallMalloc(Target, "(AnsiSize + 1)* sizeof(wchar_t)", UnicodeName, false);
                            CodeGen.WriteIf(Target, string.Format(CultureInfo.InvariantCulture, "{0} != NULL", UnicodeName));
                            CodeGen.WriteLeftBracket(Target, true);
                                CodeGen.EmitCallZeroMemory(Target, UnicodeName, "(AnsiSize + 1) * sizeof(wchar_t)", CodeGen.ZeroMemoryArg.ExactlyAsPassed);
                                Args[Args.Count - 1] = "AnsiSize";
                                CodeGen.EmitCallFunction(Target, "MultiByteToWideChar", null, Args);
                        CodeGen.WriteRightBracket(Target, true);
                    CodeGen.WriteRightBracket(Target, true);
                }
                    CodeGen.WriteRightBracket(Target, true);
                CodeGen.WriteRightBracket(Target, true);
            }

            void FreeUnicodeString(MemoryStream Target, string UnicodeName)
            {
                CodeGen.WriteIf(Target, string.Format(CultureInfo.InvariantCulture , "({0} != NULL)", UnicodeName));
                CodeGen.WriteLeftBracket(Target, true);
                CodeGen.EmitCallFree(Target, UnicodeName, false);
                CodeGen.WriteRightBracket(Target, true);
            }

            void EmitLoadLibraryStuffDetour(MemoryStream Target,  DetourFunctionMode Mode)
            {
                switch (Mode)
                {
                    case DetourFunctionMode.LoadLibraryA:
                    case DetourFunctionMode.LoadLibraryExA:
                        ConvertUnicodeString(Target, Arguments[0].ArgName, "UnicodeString");
                        break;
                }
            }

            void EmitLogMessageCall(MemoryStream Target, string msg)
            {

            }


            NeoNativeTypeData ExtractResourceType(NeoNativeTypeData T)
            {
                if (T.HasFlag(NeoNativeTypeData.ContextFileHandle))
                {
                    return NeoNativeTypeData.ContextFileHandle;
                }
                return 0;
            }
            void EmitResourceCallNotify(MemoryStream Target, NeoNativeFunctionArg DataType)
            {
                if (DataType.DebugCodeGenHint.HasFlag(NeoNativeTypeData.IsResource) == false)
                {
                   // its fine
     //               CodeGen.WriteCommentBlock(Target, "User indicated to generate a debugger resource call. User did not specify what *type* of resource");
                }
                else
                {
                    NeoNativeTypeData DType = ExtractResourceType(DataType.DebugCodeGenHint);
                    if (DType == 0)
                    {
                        CodeGen.WriteCommentBlock(Target, "User indicated to generate a diver resource call but failed to specify what ResourceContext to use");
                    }
                    else
                    {
                        string tmpName;
                        if (string.IsNullOrEmpty(ReturnValue.ArgName))
                        {
                            tmpName = this.OriginalFunctionName + "." + ReturnValue.ArgType + ".Resource" ;
                        }
                        else
                        {
                            tmpName = ReturnValue.ArgName;
                        }


                        EmitRaiseExceptionNotifyResourceCall( Target , "L\"" + tmpName + "\"", DType, true);
                    }

                }
            }
            string returnval;
            if ((ReturnValue == null) || (string.IsNullOrEmpty(ReturnValue.ArgType)))
            {
                returnval = null;
            }
            else
            {
                returnval = ReturnValue.ArgType;
            }
#if DEBUG
        if ((OriginalFunctionName.Contains("NtSetThreadInformation")))
             {
                Debugger.Break();
            }
#endif
            using (MemoryStream Output = new MemoryStream())
            {
                WriteDetourTypeDefAndName(Output);

                CodeGen.EmitDeclareFunction(Output, ReturnValue.ArgType, CallingConvention, this.RoutineName, ExtractArgumentTypes(Arguments), ExtractArgumentNames(Arguments));
                CodeGen.WriteNewLine(Output);
                CodeGen.WriteLeftBracket(Output, true);
                if (returnval != null)
                {
                    CodeGen.WriteComment(Output, "This Variable will contain the results of the function call if it gets called.");
                    CodeGen.EmitDeclareVariable(Output, returnval, FuncCall_Result);
                }
                else
                {
                    CodeGen.WriteComment(Output, "Normally below this is a variable that will hold the results of calling the function BUT the function does not return value");
                    CodeGen.WriteBeginCommentBlock(Output);
                    CodeGen.EmitDeclareVariable(Output, "VOID", FuncCall_Result);
                    CodeGen.WriteEndCommentBlock(Output);
                }

                CodeGen.WriteNewLine(Output);
                if (JustCallIt)
                {
                    CodeGen.WriteComment(Output, "JustCallIt Flag enabled. The only thing we generate was the call to this function");

                    if (AntiDebugMode == SpecialistMode.NtSetInformationThread)
                    {
                        CodeGen.WriteComment(Output, "SpecialCase is made for detouring NtSetInformationThread. User indicated that they still want the ThreadHideFromDebugger Val removed");
                        NtSetThreadInformationDebugStrip(Output);
                    }

                    if (returnval != null)
                    {

                        CodeGen.EmitCallFunction(Output, OriginalFunctionNamePtr, null, ExtractArgumentNames(Arguments), FuncCall_Result);
                    }
                    else
                    {
                        CodeGen.EmitCallFunction(Output, OriginalFunctionNamePtr, null, ExtractArgumentNames(Arguments));
                    }

                }

                CodeGen.WriteNewLine(Output);
                if (OutputDebugName)
                {
                    CodeGen.WriteComment(Output, "Code was asked to place a call to output the original name's ");
                    string msg = OriginalFunctionName + "'s detour was reached";
                    if (AttachFuncDebugStringChannel == 0)
                    {
                        CodeGen.EmitCallOutputDebugString(Output,msg, true);
                    }
                    else
                    {
                        CodeGen.WriteLiteralNoIndent(Output, EmitCallDiverOutputDebugMessage(msg, true, AttachFuncDebugStringChannel, true));
                    }
                }
                else
                {
                    CodeGen.WriteComment(Output, "Code was not generated to emit a call to show the function's name");
                }

                CodeGen.WriteNewLine(Output);
                if (OutputDebugArguments)
                {
                    CodeGen.WriteComment(Output, "The writing the debug arguments to the stream is to not supported yet. User asked for code to do this");
                }
                else
                {
                    CodeGen.WriteComment(Output, "User generated code to not write pointers to the debug arguments to the output stream");
                }


                if (ReplaceFuncWantDiver)
                {
                    CodeGen.WriteComment(Output, "Code was generated to include Diver Protocol");
                    CodeGen.EmitDeclareVariable(Output, DEBUGGER_RESPONSE_VarType, DEBUGGER_RESPONSE_VarName);
                    CodeGen.EmitCallZeroMemory(Output, DEBUGGER_RESPONSE_VarName, DEBUGGER_RESPONSE_VarType, CodeGen.ZeroMemoryArg.UseSizeOfTemplate);
                    CodeGen.EmitDeclareVector(Output, DiverCppArgName);
                    CodeGen.EmitDeclareVector(Output, DiverCppArgTypes);
                    CodeGen.EmitDeclareWideStringStream(Output, UnicodeBuffer);
                    CodeGen.EmitDeclareVariable(Output, "BOOL", "DiverFuncCallResult");


                    CodeGen.WriteComment(Output, "The format for these vectors are entry[0] is the number of remaining entries -1 and " + DiverCppArgName + "[X]'s type data is in " + DiverCppArgTypes + "[X]'s position");
                    if (Arguments.Count == 0)
                    {
                        CodeGen.EmitPushVectorValue(Output, DiverCppArgName, "0");
                        CodeGen.EmitPushVectorValue(Output, DiverCppArgTypes, "0");
                        CodeGen.WriteComment(Output, "In the case of 0 arguments. The vectors will contain a single value each that's equal to 0");
                    }
                    else
                    {
                        CodeGen.EmitPushVectorValue(Output, DiverCppArgName, Arguments.Count.ToString(CultureInfo.InvariantCulture));
                        CodeGen.EmitPushVectorValue(Output, DiverCppArgTypes, Arguments.Count.ToString(CultureInfo.InvariantCulture));
                        Arguments.ForEach(p => {
                            CodeGen.WriteComment(Output, "This tells the DiverReader that " + p.ArgName + " is a NeoNativeType of " + p.DebugCodeGenHint + "\r\n");
                            CodeGen.EmitPushVectorValue(Output, DiverCppArgName, "(ULONG_PTR)" + p.ArgName);
                            CodeGen.EmitPushVectorValue(Output, DiverCppArgTypes, "(ULONG_PTR)" + ((int)p.DebugCodeGenHint).ToString(CultureInfo.InvariantCulture)) ;
                        });
                    }


                    List<string> DiverCallArgs = new List<string>();
                    DiverCallArgs.Add("L" + CodeGen.AddQuotesIfNone(OriginalFunctionName));
                    DiverCallArgs.Add("" + DiverCppArgName);
                    DiverCallArgs.Add("" + DiverCppArgTypes);
                    DiverCallArgs.Add("(ULONG_PTR*&)" + "0"); // not used
                    DiverCallArgs.Add("(ULONG_PTR*)" + "0"); // return value hint not used
                    DiverCallArgs.Add("&" + DEBUGGER_RESPONSE_VarName);

                    CodeGen.EmitCallFunction(Output, "RaiseExceptionTrackFunc", null, DiverCallArgs, "DiverFuncCallResult");

                    CodeGen.WriteIf(Output, "DiverFuncCallResult == 0");
                    {
                        CodeGen.WriteLeftBracket(Output, true);
                        CodeGen.WriteComment(Output, "Debugger did not set response flag. We can't assume it understood it so we default to just call it ");
                        if (returnval != null)
                        {
                            switch (AntiDebugMode)
                            {
                                case SpecialistMode.Normal:
                                    break;
                                case SpecialistMode.NtSetInformationThread:
                                    NtSetThreadInformationDebugStrip(Output);
                                    break;
                                case SpecialistMode.NtCreateThread:
                                default:
                                    throw new NotImplementedException(Enum.GetName(typeof(SpecialistMode), AntiDebugMode));
                            }


                            CodeGen.EmitCallFunction(Output, OriginalFunctionNamePtr, null, ExtractArgumentNames(Arguments), "result");
                            CodeGen.EmitReturnX(Output, "result");

                            if (DetourMode != DetourFunctionMode.Normal)
                            {
                                CodeGen.WriteComment(Output, "Additional code setup to detour a LoadLibrary Call (at least one detoured function is in On Demand Mode");
                                switch (DetourMode)
                                {
                                    case DetourFunctionMode.LoadLibraryA:
                                        {
                                            break;
                                        }
                                    case DetourFunctionMode.LoadLibraryExA:
                                        {
                                            break;
                                        }
                                    case DetourFunctionMode.LoadLibraryExW:
                                        {
                                            break;
                                        }
                                    case DetourFunctionMode.LoadLibraryW:
                                        {
                                            break;
                                        }
                                    case DetourFunctionMode.Normal:
                                        {
                                            // should never actually be here
                                            break;
                                        }
                                    default:
                                        // someone didn't add a handler for this mode yet
                                        throw new NotImplementedException(Enum.GetName(typeof(DetourFunctionMode), DetourMode));
                                }

                            }
                        }
                        else
                        {
                            switch (AntiDebugMode)
                            {
                                case SpecialistMode.Normal:
                                    break;
                                case SpecialistMode.NtSetInformationThread:
                                    NtSetThreadInformationDebugStrip(Output);
                                    break;
                                case SpecialistMode.NtCreateThread:
                                default:
                                    throw new NotImplementedException(Enum.GetName(typeof(SpecialistMode), AntiDebugMode));
                            }
                            CodeGen.EmitCallFunction(Output, OriginalFunctionNamePtr, null, ExtractArgumentNames(Arguments));
                            CodeGen.EmitReturnX(Output, string.Empty);
                        }
                        CodeGen.WriteRightBracket(Output, true);
                    }
                    CodeGen.WriteElse(Output);
                    CodeGen.WriteLeftBracket(Output, true);
                    CodeGen.WriteSwitch(Output, DEBUGGER_RESPONSE_VarName + ".Flags");
                    CodeGen.WriteLeftBracket(Output, true);
                    CodeGen.WriteCase(Output, "ForceReturn");
                    {
                        CodeGen.WriteLeftBracket(Output, true);
                        if (returnval != null)
                        {
                            EmitResourceCallNotify(Output, ReturnValue);
                        }
                        if (returnval != null)
                        {
                            CodeGen.EmitReturnX(Output, "(" + returnval + ") " + DEBUGGER_RESPONSE_VarName + ".Arg1");
                        }
                        else
                        {
                            CodeGen.EmitReturnX(Output, string.Empty);
                        }


                        CodeGen.WriteBreak(Output);
                        CodeGen.WriteRightBracket(Output, true);
                    }
                    CodeGen.WriteRightBracket(Output, true);
                    CodeGen.WriteRightBracket(Output, true);


                    if (returnval != null)
                    {
                        switch (AntiDebugMode)
                        {
                            case SpecialistMode.Normal:
                                break;
                            case SpecialistMode.NtSetInformationThread:
                                NtSetThreadInformationDebugStrip(Output);
                                break;
                            case SpecialistMode.NtCreateThread:
                            default:
                                throw new NotImplementedException(Enum.GetName(typeof(SpecialistMode), AntiDebugMode));
                        }
                        CodeGen.EmitCallFunction(Output, OriginalFunctionNamePtr, null, ExtractArgumentNames(Arguments), "result");
                        if (returnval != null)
                        {
                            EmitResourceCallNotify(Output, ReturnValue);
                        }
                        CodeGen.EmitReturnX(Output, "result");
                    }
                    else
                    {
                        switch (AntiDebugMode)
                        {
                            case SpecialistMode.Normal:
                                break;
                            case SpecialistMode.NtSetInformationThread:
                                NtSetThreadInformationDebugStrip(Output);
                                break;
                            case SpecialistMode.NtCreateThread:
                            default:
                                throw new NotImplementedException(Enum.GetName(typeof(SpecialistMode), AntiDebugMode));
                        }
                        CodeGen.EmitCallFunction(Output, OriginalFunctionNamePtr, null, ExtractArgumentNames(Arguments));
                    }

                }
                else
                {
                    if (returnval != null)
                    {
                        switch (AntiDebugMode)
                        {
                            case SpecialistMode.Normal:
                                break;
                            case SpecialistMode.NtSetInformationThread:
                                NtSetThreadInformationDebugStrip(Output);
                                break;
                            case SpecialistMode.NtCreateThread:
                            default:
                                throw new NotImplementedException(Enum.GetName(typeof(SpecialistMode), AntiDebugMode));
                        }
                        CodeGen.EmitCallFunction(Output, OriginalFunctionName, null, ExtractArgumentNames(Arguments), FuncCall_Result);
                        if (returnval != null)
                        {
                            EmitResourceCallNotify(Output, ReturnValue);
                        }
                        CodeGen.EmitReturnX(Output, FuncCall_Result);
                    }
                    else
                    {
                        switch (AntiDebugMode)
                        {
                            case SpecialistMode.Normal:
                                break;
                            case SpecialistMode.NtSetInformationThread:
                                NtSetThreadInformationDebugStrip(Output);
                                break;
                            case SpecialistMode.NtCreateThread:
                            default:
                                throw new NotImplementedException(Enum.GetName(typeof(SpecialistMode), AntiDebugMode));
                        }
                        CodeGen.EmitCallFunction(Output, OriginalFunctionName, null, ExtractArgumentNames(Arguments));
                        CodeGen.EmitReturnX(Output, string.Empty);
                    }
                }




                CodeGen.WriteRightBracket(Output, true);
                CodeGen.WriteNewLine(Output);

                return CodeGen.MemoryStreamToString(Output);
            }
        }


    
        /// <summary>
        /// The stub for the static link mode can safely be placed output of a routine. It defines a typedef function pointer, variable and assigns the function to it
        /// </summary>
        /// <returns></returns>
        public string GenerateStaticFuncLink()
        {
            using (MemoryStream tmp = new MemoryStream())
            {
                CodeGen.EmitFunctionTypeDef(tmp, ReturnValue.ArgType, CodeGen.EmitDeclareFunctionSpecs.WINAPI, OriginalFunctionNamePtrType, ExtractArgumentTypes(Arguments), ExtractArgumentNames(Arguments));
                CodeGen.EmitDeclareVariable(tmp, OriginalFunctionNamePtrType, OriginalFunctionNamePtr, OriginalFunctionName);
                return CodeGen.MemoryStreamToString(tmp);
            }
        }
        /// <summary>
        /// returns a string containing the Attach function and the original
        /// </summary>
        /// <returns></returns>
        public override string GenerateFunction()
        {
            return GenerateAttachFunction() + GenerateDetourFunction();
        }
    }
}
