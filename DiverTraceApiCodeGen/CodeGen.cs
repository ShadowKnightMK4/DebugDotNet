using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace DiverTraceApiCodeGen
{
    /// <summary>
    /// DiverTraceApi CodeGen Emitting for the Trace Api.  Routine Name protocal.
    /// WriteXX() => lower level stuff that can be solved via a single line or statement.
    /// EmitXXX() => Highler Level Stuff that can be a bit complex.
    /// </summary>
    public static class CodeGen
    {
        /// <summary>
        /// Specifies the encoding this will use.
        /// </summary>
        public static Encoding TargetEncoding { get; set; } = Encoding.ASCII;
        /// <summary>
        /// if true certain Emit and Write calls get an auto WriteEndLine() called afterwards
        /// </summary>
        public static bool AutoLn { get; set; } = true;
        /// <summary>
        /// Specifies the default calling convention if non specified to <see cref="EmitDeclareFunction(string, EmitDeclareFunctionSpecs, string, List{string}, List{string})"/> and kin
        /// </summary>

        public static EmitDeclareFunctionSpecs DefaultCallingConvention
        {
            get
            {
                return BackingCallingConvention;
            }
            set
            {
                switch (value)
                {
                    case EmitDeclareFunctionSpecs.FASTCALL:
                        BackingCallingConvention = EmitDeclareFunctionSpecs.FASTCALL;
                        break;
                    case EmitDeclareFunctionSpecs.CDECL:
                        BackingCallingConvention = EmitDeclareFunctionSpecs.CDECL;
                        break;
                    case EmitDeclareFunctionSpecs.WINAPI:
                        BackingCallingConvention = EmitDeclareFunctionSpecs.WINAPI;
                        break;
                    default: throw new InvalidOperationException("This value must be a calling conversion only");
                }
            }
        }

        static EmitDeclareFunctionSpecs BackingCallingConvention = EmitDeclareFunctionSpecs.WINAPI;
        #region Templates
        /// <summary>
        /// used for single line comments <see cref="EmitComment(Stream, string)"/>
        /// </summary>
        static string SingleLineCommentTemplate = "// {0}";
        /// <summary>
        /// the /* for the start of a comment block <see cref="EmitBeginCommentBlock(Stream)"/>
        /// </summary>
        static string BeginCommentBlockPart = "/* ";
        /// <summary>
        /// the */ for the end of a comment block <see cref="EmitEndCommentBlock(Stream)"/>
        /// </summary>
        static string EndCommentBlockPart = " */ ";
        /// <summary>
        /// Used for a porential multipline comment <see cref="EmitCommentBlock(Stream, string)"/>
        /// </summary>
        static string CommentBlockTemplate = "/* {0} */";
        /// <summary>
        /// When a New Line is required we write this. <see cref="AutoLn"/> and <see cref="WriteNewLine(Stream)"/>
        /// </summary>
        static string EndLine = "\n";
        /// <summary>
        /// Return template <see cref="EmitReturnX(Stream, string)"/>
        /// </summary>
        static string ReturnLiteral = "return {0}";
        /// <summary>
        /// Template for variable with no assigment <see cref="DeclareVariable(Stream, string, string, string)"/>
        /// </summary>
        static string DeclareVariableNoAssigment = "{0} {1};";
        /// <summary>
        /// Template for variable with assigment <see cref="DeclareVariable(Stream, string, string, string)"/>
        /// </summary>
        static string DeclareVariableWithAssigment = "{0} {1} = {2};";
        /// <summary>
        /// Template for including a file in the standard include paths
        /// </summary>
        static string StandardIncludeTemplate = "#include <{0}>";
        /// <summary>
        /// TEmplate or includiung a file in project specified or relative include paths
        /// </summary>
        static string ProjectIncludeTemplate = "#include \"{0}\"";


        /// <summary>
        /// Template for if statement that evals to true.
        /// </summary>
        static string IfNonZero = "if ({0}}";
        #endregion

        #region bottom level emit
        /// <summary>
        /// Write the string uses as the new line <see cref="EndLine"/>. Default is '\n'
        /// </summary>
        /// <param name="Output">target stream</param>
        public static void WriteNewLine(Stream Output)
        {
            WriteLiteral(Output, EndLine);
        }

        /// <summary>
        /// Call <see cref="WriteNewLine(Stream)"/> if <see cref="AutoLn"/> is set to true
        /// </summary>
        /// <param name="Output">target stream</param>
        static void NewLineAuto(Stream Output)
        {
            if (AutoLn)
            {
                WriteNewLine(Output);
            }
        }
        /// <summary>
        /// Write x to the stream in <see cref="TargetEncoding"/>. This forms the root of the other routines.
        /// </summary>
        /// <param name="Output">Write To this strema</param>
        /// <param name="x">what to write</param>
        public static void WriteLiteral(Stream Output, string x)
        {
            byte[] Data = TargetEncoding.GetBytes(x);
            Output.Write(Data, 0, Data.Length);
        }

        /// <summary>
        /// Write a formatted string to Output with up to 6 arguments.
        /// </summary>
        /// <param name="Output">Write to this stream</param>
        /// <param name="Template"><see cref="string.Format(IFormatProvider?, string, object?)"/> template to use</param>
        /// <param name="arg1">defaults to "" </param>
        /// <param name="arg2">defaults to ""</param>
        /// <param name="arg3">defaults to ""</param>
        /// <param name="arg4">defaults to ""</param>
        /// <param name="arg5">defaults to ""</param>
        /// <param name="arg6">defaults to ""</param>
        public static void WriteFormated(Stream Output, string Template, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "", string arg5 = "", string arg6 = "")
        {
            WriteLiteral(Output, string.Format(Template, arg1, arg2, arg3, arg4, arg5, arg6));
        }
        #endregion

        #region comment emitting
            /// <summary>
            /// Emit a Single line C/C++ comment 
            /// </summary>
            /// <param name="Output">Write to this stream</param>
            /// <param name="Comment">literal to emit</param>
            public static void EmitComment(Stream Output, string Comment)
            {
                WriteFormated(Output, SingleLineCommentTemplate, Comment);
                if (AutoLn)
                {     
                    WriteLiteral(Output, EndLine);
                }
            }

        /// <summary>
        /// Emit in the form of /* Comment */ 
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="Comment"></param>
            public static void EmitCommentBlock(Stream Output, string Comment)
            {
                  WriteFormated(Output, CommentBlockTemplate, Comment);
               if (AutoLn)
               {
                   WriteLiteral(Output, EndLine);
               }
            }

            /// <summary>
            /// Write /* to the stream
            /// </summary>
            /// <param name="Output"></param>
            public static void EmitBeginCommentBlock(Stream Output)
            {
                WriteLiteral(Output, BeginCommentBlockPart);
            }

        /// <summary>
        /// Write */ to the stream
        /// </summary>
        /// <param name="Output"></param>

            public static void EmitEndCommentBlock(Stream Output)
            {
                WriteLiteral(Output, EndCommentBlockPart);
            }

        #endregion
        #region Return Emitting
        /// <summary>
        /// write "Return "X"" to the stream
        /// </summary>
        /// <param name="Output">target stream</param>
        /// <param name="X">X is written exactly as it s. Should one need quotes, add them</param>
                public static void EmitReturnX(Stream Output, string X)
                {
                    WriteFormated(Output, ReturnLiteral, X);
                    NewLineAuto(Output);

                }
        #endregion

        #region Variable Declaration
        /// <summary>
        /// Declare variable Name of type Type. If Value is specified, we declare with assigment
        /// </summary>
        /// <param name="Output">Write to this</param>
        /// <param name="Type">Placed in the place the variable's type is.</param>
        /// <param name="Name">Placed where the variable's name is </param>
        /// <param name="Value">if "" or <see cref="string.Empty"/> we do not assign a value, otherwise we assign the value</param>
            public static void DeclareVariable(Stream Output, string Type, string Name, string Value="")
            {
                if (Value != string.Empty)
                {
                    WriteFormated(Output, DeclareVariableWithAssigment, Type, Name, Value);
                }
                else
                {
                    WriteFormated(Output, DeclareVariableNoAssigment, Type, Name);
                }
                NewLineAuto(Output);
            }
        #endregion

        #region Include Routines Writers
        public enum EmitIncludeChoice
        {
            Standard = 0,
            Project = 1
        }

        /// <summary>
        /// Emit an include for a standard search path non quoted #include statement
        /// </summary>
        /// <param name="Output">Write to this</param>
        /// <param name="File">File to include</param>
        /// <remarks>resolves to the non quoted version. <see cref="StandardIncludeTemplate"/></remarks>
        public static void EmitStandardInclude(Stream Output, string File)
        {
            EmitInclude(Output, File, EmitIncludeChoice.Standard);
        }

        /// <summary>
        /// Emit an include for a project specific search path quoted #include statement
        /// </summary>
        /// <param name="Output">Write to this</param>
        /// <param name="File">File to include</param>
        /// <remarks>resolves to the non quoted version. <see cref="ProjectIncludeTemplate"/></remarks>
        public static void EmitProjectInclude(Stream Output, string File)
        {
            EmitInclude(Output, File, EmitIncludeChoice.Project);
        }
        /// <summary>
        /// Emit an include file. with the specified template
        /// </summary>
        /// <param name="Output">Write here</param>
        /// <param name="File">include this file.</param>
        /// <param name="Style">specifies the template to use</param>
        public static void EmitInclude(Stream Output, string File, EmitIncludeChoice Style)
        {
            switch (Style)
            {
                case EmitIncludeChoice.Project:
                    WriteFormated(Output, ProjectIncludeTemplate, File);
                    break;
                case EmitIncludeChoice.Standard:
                    WriteFormated(Output, StandardIncludeTemplate, File);
                    break;
                default:
                    // safety net incase someone force feeds invalue choice
                    throw new NotImplementedException(Enum.GetName(typeof(EmitIncludeChoice), Style));
            }
            NewLineAuto(Output);

        }
        #endregion

        #region If Statements
        /// <summary>
        /// Write the start of an If statement block.
        /// </summary>
        /// <param name="Output">Write to this</param>
        /// <param name="Eval">This is  literal, should it eval to true at runtime the code after this is ran</param>
        /// <remarks>IMPORTANT! Does not emit codeblock stuff</remarks>
            public static void WriteIfTrue(Stream Output, string Eval)
            {
                WriteFormated(Output, IfNonZero, Eval);
                NewLineAuto(Output);
            }
        #endregion


        #region Function Emitting

        #region Function Emitting Template Stuff
            static string DllImport = "__declspec(dllimport)";
            static string DllExport = "__declspec(dllexport)";
            static string WINAPI = "WINAPI";
            static string CDECL = "_cdecl";
            static string INLINE = "inline";
            static string FASTCALL = "FASTCALL";
            static string VariableArgStuffix = "...";
        /// <summary>
        /// template for start of a prototyping a C/C++ non function ptr name
        /// </summary>
        static string FunctionProtoTypeBeginTemplate = "{0} {1} {2} {3} (";
        /// <summary>
        /// Template for prototyping a C/C++ function pointer 
        /// </summary>
        static string FunctionProtoTypePtrBeginTemplate = "{0} ({1}* {2}) {3} (";
        static string TypeDefBit = "typedef ";
        /// <summary>
        /// This collection is used to specifiy how the privite <see cref="EmitDeclareFunctionInternal(Stream, string, EmitDeclareFunctionSpecs, string, string, bool, List{string}, List{string}, EmitDeclareArgHandling, string)"/> emits its code
        /// </summary>
        [Flags]
        internal enum EmitDeclareArgHandling
        {
            /// <summary>
            /// Normal handling, Keep both the name and the types
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Drop Function Names from the emit
            /// </summary>
            DropNames = 2,
            /// <summary>
            /// Drop Function Types from the emit
            /// </summary>
            DropTypes = 4,

            /// <summary>
            /// disables the calling convention check. Purpose is because the public routine already done it
            /// </summary>
            DisableFunctionSpecsCheck = 8
            

        }
        #endregion

            /// <summary>
            /// Flags to modify how the Function Emiting routines generate the function prototypes
            /// </summary>
        [Flags]
            public enum EmitDeclareFunctionSpecs
            {
            /// <summary>
            /// Emits nothing
            /// </summary>
                None = 0,
                /// <summary>
                /// Emits    __declspec(dllimport)
                /// </summary>
                DllImport = 1,
                /// <summary>
                /// Emits __declpec(dllexport)
                /// </summary>
                DllExport = 2,
                /// <summary>
                /// Emits WINAPI calling type
                /// </summary>
                WINAPI = 4,
                /// <summary>
                /// Emits CDECL Calling type
                /// </summary>
                CDECL = 8,
                /// <summary>
                /// Emits FASTCALL Calline Type
                /// </summary>
                FASTCALL = 16,
                /// <summary>
                /// Emits INLINE Modifer
                /// </summary>
                INLINE = 32,
                /// <summary>
                /// includes the Variable arg thing . . . at the end.
                /// </summary>
                VariableArgs = 64

            }

        /// <summary>
        /// Extracts calling conversion if there.  Should multiple be defined (WHY????) it extracts in the order of CDECL, FASTCALL, WINAPI
        /// </summary>
        /// <param name="x">the enum to check</param>
        /// <returns>returns the calling conversion if there or <see cref="DefaultCallingConvention"/> if it can't find it</returns>
        public static EmitDeclareFunctionSpecs ExtractCallingConversion(EmitDeclareFunctionSpecs x)
        {
            if (x.HasFlag(EmitDeclareFunctionSpecs.CDECL))
                return EmitDeclareFunctionSpecs.CDECL;
            if (x.HasFlag(EmitDeclareFunctionSpecs.FASTCALL))
                return EmitDeclareFunctionSpecs.FASTCALL;
            if (x.HasFlag(EmitDeclareFunctionSpecs.WINAPI))
                return EmitDeclareFunctionSpecs.WINAPI;
            return DefaultCallingConvention;
        }

        /// <summary>
        /// Validate a ArgType and ArgName list passed to an emit function. A return value of true should mean that the EmitDeclareFunction stuff should be able to handle the lists ok
        /// </summary>
        /// <param name="ArgType">the argtype list to check</param>
        /// <param name="ArgName">the argname list to check</param>
        /// <returns>returns false under these conditions:
        /// (if other ArgType or ArgName is null but not both;
        /// if ArgType.Count != ArgName.Count)
        /// 
        /// returns true under these condtions:
        /// (if ArgType and ArgName are both null;
        /// if (argtype.count == argname.count))
        /// </returns>
        static bool ValidateArgTypeName(ref List<string> ArgType, ref List<string> ArgName)
        {
            if ( (ArgType == null) ^ (ArgName == null) )
            {
                return false;
            }
            else
            {
                if ((ArgType == null) && (ArgName == null))
                {
                    return true;
                }
                else
                {
                    if (ArgType.Count != ArgName.Count)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        /// <summary>
        /// check for contrary values and return true if ok or fale if none. 
        /// </summary>
        /// <param name="x">the value to check</param>
        /// <param name="ThrowOnFalse">if set and the routine would return false, we throw in InvalidOperationError with the problem specified in English</param>
        /// <returns>true if all tests passed or false (if ThrowOnFalse is NOT set) if a test failed</returns>
        /// <exception cref="InvalidOperationException"> if the test failed and ThrowOnFalse=true</exception>
        public static bool ValidateFunctionSpecsEnum(EmitDeclareFunctionSpecs x,  bool ThrowOnFalse=false, [CallerMemberName] string Caller = "")
        {
            if (x == EmitDeclareFunctionSpecs.None)
                return true;
            if (EmitFunctionHasMultipleCallingConventions(x))
            {
                if (ThrowOnFalse)
                {
                    throw new InvalidOperationException(Caller + ": Can't specifiy multiple Calling Conventions");
                }
                return false;
            }
            else
            {
                if (x.HasFlag(EmitDeclareFunctionSpecs.INLINE))
                {
                    if (x.HasFlag(EmitDeclareFunctionSpecs.DllExport) || (x.HasFlag(EmitDeclareFunctionSpecs.DllImport) ))
                    {
                        throw new InvalidOperationException(Caller + "can't specifiy DllExport or DllImport with inline");
                    }
                }
                if (x.HasFlag(EmitDeclareFunctionSpecs.DllExport) && (x.HasFlag(EmitDeclareFunctionSpecs.DllImport)))
                {
                    if (ThrowOnFalse)
                    {
                        throw new InvalidOperationException(Caller + ": Can't specifiy both DllExport and DllImport");
                    }
                    return false;
                }

                if (x.HasFlag(EmitDeclareFunctionSpecs.VariableArgs) && (x.HasFlag(EmitDeclareFunctionSpecs.CDECL) == false))
                {
                    if (ThrowOnFalse)
                    {
                        throw new InvalidOperationException(Caller + ": Function with variable number of arguments must be specified as CDECL in the calling convention");
                    }
                }

                return true;

            }
        }
        /// <summary>
        /// helper to new if the passed value has multiple calltypes currentling set (like CDECL and FASTCALL, ect...
        /// </summary>
        /// <param name="x"></param>
        /// <returns>returns true if the enum has more than one calling convention set (cdecl, fastcall, winapi)</returns>
        public static bool EmitFunctionHasMultipleCallingConventions(EmitDeclareFunctionSpecs x)
        {
            int ProtocalCount = 0;
            if (x.HasFlag(EmitDeclareFunctionSpecs.CDECL))
            {
                ProtocalCount++;
            }

            if (x.HasFlag(EmitDeclareFunctionSpecs.FASTCALL))
            {
                ProtocalCount++;
            }

            if (x.HasFlag(EmitDeclareFunctionSpecs.WINAPI))
            {
                ProtocalCount++;
            }
            return ProtocalCount > 1;
        }
        /// <summary>
        /// Emit a typedef for the function. 
        /// </summary>
        /// <param name="Output">Write to here with <see cref="TargetEncoding"/></param>
        /// <param name="ReturnType">Unmodified.  This goes in the spot the return type contains. Can leave blank for \"void\"</param>
        /// <param name="CallerMods">modifies how the function will be emitted <see cref="EmitDeclareFunctionSpecs"/></param>
        /// <param name="Name">Unmodified. This will go in the spot that defines the name of this function / typedef </param>
        /// <param name="ArgTypes">Specify The Argument Types. May leave null for no arguments</param>
        /// <param name="ArgNames">Specifies the nae of the arguments. May leave null for no arguments</param>
        /// <remarks>ArgType.Count and ArgName.Count must match if neither of them are null. </remarks>
        public static void EmitFunctionTypeDef(Stream Output, string ReturnType, EmitDeclareFunctionSpecs CallerMods, string Name, List<string> ArgTypes, List<string> ArgNames)
        {
            if (CallerMods != EmitDeclareFunctionSpecs.None)
            {
                try
                {
                    ValidateFunctionSpecsEnum(CallerMods,true);
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllExport))
                    {
                        throw new InvalidOperationException("Can't export a C/C++ typedef");
                    }
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllImport))
                    {
                        throw new InvalidOperationException("Can't import a C/C++ typedef");
                    }
                }
                catch (InvalidOperationException e)
                {
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllExport))
                    {
                        throw new InvalidOperationException("Can't export a C/C++ typedef", e);
                    }
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllImport))
                    {
                        throw new InvalidOperationException("Can't import a C/C++ typedef" , e);
                    }
                }

                if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.INLINE))
                {
                    throw new InvalidOperationException("Emiting inline prefix unsupported for typedef statement");
                }

            }
            else
            {
                CallerMods = EmitDeclareFunctionSpecs.WINAPI;
            }

            EmitDeclareFunctionInternal(Output, ReturnType, CallerMods, Name, "typedef ", true, ArgTypes, ArgNames, EmitDeclareArgHandling.DropNames | EmitDeclareArgHandling.DisableFunctionSpecsCheck);

        }
        /// <summary>
        /// internal for the public EmitFunction routines
        /// </summary>
        /// <param name="Output">emit to this stream in <see cref="TargetEncoding"/></param>
        /// <param name="ReturnType">Literal. Gets placed in the spot that specifies the return type. Can specify string.empty for void</param>
        /// <param name="CallerMods">modifies how the calling conversion is generated. See <see cref="EmitDeclareFunctionSpecs"/> for more details</param>
        /// <param name="Name">This gets placed in the spot that specifies the name of this function ptr or function prototype</param>
        /// <param name="Prefix">Intendted for the typedef codegen. This specifies a literal that gets written to Output first before this routine writes anything else</param>
        /// <param name="IsFunctionPtr">if set we use the <see cref="FunctionProtoTypePtrBeginTemplate"/> template instead of the <see cref="FunctionProtoTypeBeginTemplate"/></param>
        /// <param name="ArgTypes">This list of strings goes into the Argument list for this function. These specifiy ArgType and line up with ArgName at the same index. (ArgType [x] should match with ArgName [x]). It Can be null if the routine does not need any arguments.</param>
        /// <param name="ArgNames">This list of strings gets emitting into the argument name slot for this function. each ArgNames [x] value is paired with ArgType [x] also. Can be null.</param>
        /// <param name="ArgHandling">Modifies Arg Handling. See the enum <see cref="EmitDeclareArgHandling"/> for specifies</param>
        /// <param name="BlameMe">using CallermemberName to default to who called this function. Exceptions genereated by this will point to blame this function. Defaults to caller</param>
        /// <exception cref="InvalidOperationException"> can happen if either CallerModes or the ArgType + ArgName do not pass validation. <see cref="ValidateArgTypeName(ref List{string}, ref List{string})"/> and <see cref="ValidateFunctionSpecsEnum(EmitDeclareFunctionSpecs, bool, string)"/></exception> 
        static void EmitDeclareFunctionInternal(Stream Output,
                                                string ReturnType,
                                                EmitDeclareFunctionSpecs CallerMods,
                                                string Name,
                                                string Prefix,
                                                bool IsFunctionPtr,
                                                List<string> ArgTypes,
                                                List<string> ArgNames,
                                                EmitDeclareArgHandling ArgHandling,
                                                [CallerMemberName]
                                                    string BlameMe="")
        {
            StringBuilder CallingStr = new StringBuilder();
            EmitDeclareFunctionSpecs TargetCallingConvention;
            if (!ArgHandling.HasFlag(EmitDeclareArgHandling.DisableFunctionSpecsCheck))
            {
                try
                {
                    if (ValidateFunctionSpecsEnum(CallerMods, true))
                    {
                        throw new ArgumentException("Validation for EmitDeclareFunctionSpecs failed in " + BlameMe + ".");
                    }
                }
                catch (InvalidOperationException e)
                {
                    throw new ArgumentException("Validation for EmitDeclareFunctionSpecs failed in " + BlameMe + ".", e);
                }
            }

            if (ValidateArgTypeName(ref ArgTypes, ref ArgNames) == false)
            {
                throw new InvalidOperationException("The Emit Function ArgType and ArgName failed validation in " + BlameMe + ".");
            }
            TargetCallingConvention = ExtractCallingConversion(CallerMods);

            WriteLiteral(Output, Prefix);

            if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllExport))
            {
                CallingStr.Append(DllExport);
            }
            else
            {
                if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllImport))
                {
                    CallingStr.Append(DllImport);
                }
                else
                {
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.INLINE))
                    {
                        CallingStr.Append(INLINE);
                    }
                }
            }

            if (CallingStr.Length != 0)
            {
                CallingStr.Append(" ");
            }

            switch (TargetCallingConvention)
            {
                case EmitDeclareFunctionSpecs.CDECL:
                    CallingStr.Append(CDECL);
                    break;
                case EmitDeclareFunctionSpecs.FASTCALL:
                    CallingStr.Append(FASTCALL);
                    break;
                case EmitDeclareFunctionSpecs.WINAPI:
                    CallingStr.Append(WINAPI);
                    break;
                }

            if (ReturnType == string.Empty)
            {
                ReturnType = "void";
            }
            if (!IsFunctionPtr)
            {
                WriteFormated(Output, FunctionProtoTypeBeginTemplate, ReturnType,
                    CallingStr.ToString(),
                    Name,
                    string.Empty

                    );

            }
            else
            {
                WriteFormated(Output, FunctionProtoTypePtrBeginTemplate, ReturnType,
                    CallingStr.ToString(),
                    Name,
                    string.Empty);
            }

            if ( ( ((ArgTypes == null) && (ArgNames == null)) == false) )
            {
                string aType, aName;
                // contains arguments
                for (int step = 0; step < ArgTypes.Count; step++)
                {
                    if (ArgHandling.HasFlag( EmitDeclareArgHandling.Normal) )
                    {
                        aType = ArgTypes[step];
                        aName = ArgNames[step];
                    }
                    else
                    {
                       if (ArgHandling.HasFlag(EmitDeclareArgHandling.DropTypes) )
                        {
                            aType = string.Empty;
                        }
                       else
                        {
                            aType = ArgTypes[step];
                        }

                        if (ArgHandling.HasFlag(EmitDeclareArgHandling.DropNames))
                        {
                            aName = string.Empty;
                        }
                        else
                        {
                            aName = ArgNames[step];
                        }
                    }
                    WriteFormated(Output, "{0} {1}", aType, aName);
                    if ( (step + 1) != ArgTypes.Count)
                    {
                        WriteLiteral(Output, ",");
                    }
                }
            }
            if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.VariableArgs))
            {
                WriteLiteral(Output, ", " + VariableArgStuffix);
            }

            WriteLiteral(Output, ")");

        }   
        #endregion
    }
}
