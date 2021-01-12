using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Configuration;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DiverTraceApiCodeGen
{



    /// <summary>
    /// CodeGen's Primary purpose is to exist as a way to emit a subset of C/C++ code with an arbitrary encoding <see cref="TargetEncoding"/> to any passed valid <see cref="Stream"/>
    /// 
    /// Naming Scheme
    /// Routines that reduce to writing a single C/C++ statement will usually be found under WriteXXX()
    /// Routines that require a bit more though will usually be found under EmitXXXX()
    /// 
    /// Routines that allow 
    /// 
    /// </summary>
    public static partial class CodeGen
    {
        #region Function Argument Validation stuff to make C# code analyze happy
            /// <summary>
            /// pass through value when null is passed to an EmitFunctionXXX. so C# code Analyze shuts it
            /// </summary>
            readonly static List<string> EmptyList = new List<string>();
        #endregion
        #region Configuration
        /// <summary>
        /// This specifies the Encoding that CodeGen's will use for ALL of its routines. when writing to a <see cref="Stream"/>
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
                    default: throw new InvalidOperationException(MessageStrings.InvalidCallConvSetMsg);
                }
            }
        }

        static EmitDeclareFunctionSpecs BackingCallingConvention = EmitDeclareFunctionSpecs.WINAPI;

        /// <summary>
        /// Indented for Debug purposes. This being true causes the root routines of <see cref="WriteLiteral"/> and <see cref="WriteLiteralNoIndent"/> to also write their output to a Console using Console.Write(). Default is true if DEBUG is #defined
        /// </summary>
#if DEBUG
        public static bool DebugEchoConsole { get; set; } = true;
#else
      public static bool DebugEchoConsole { get; set; } = false;
#endif

        #endregion

        // Templates are bits of string pieces that the routines in CodeGen use -> typically with string.Format()
        #region Templates

        /// <summary>
        /// Template for <see cref="WriteCase(Stream, string)"/> 
        /// </summary>
        const string CaseFuncTemplate = "case {0}:";

        /// <summary>
        /// Template for <see cref="WriteSwitch(Stream, string)"/>
        /// </summary>

        const string SwitchFuncTemplate = "switch ( {0} )";

        /// <summary>
        /// Template to emit inserting a value in a c++ stream.   Used by <see cref="EmitInsertStream"/>
        /// </summary>
        const string OperatorShiftLeftStreamTemplate = "{0} << {1}";
        /// <summary>
        /// Template used to chain inserting a value in a C++ stream. Used by <see cref="EmitInsertStream(Stream, string, List{string}, bool)"/>
        /// </summary>
        const string OperatorShiftLeftStreamTemplatePart = " << {0}";

        /// <summary>
        /// Mainly for <see cref="AddQuotesIfNone(string)"/>. This is a zero length string enclosed in quotes.
        /// </summary>
        const string EmptyQuoted = "\"\"";
        /// <summary>
        /// Temple for <see cref="AddQuotesIfNone(string)"/>
        /// </summary>
        const string QuotedStringTemplate = "\"{0}\"";

        /// <summary>
        /// Template to issue  a call to a vector's.push_back() routine Used by <see cref="EmitPushVectorValue(Stream, string, string, bool)"/>
        /// </summary>
        const string VectorPushBackTemplate = "{0}.push_back({1})";

        /// <summary>
        /// template for pragma lib. <see cref="WritePragmaLib(Stream, string)"/>
        /// </summary>
        const string PragmaTemplate = "#pragma comment(lib , \"{0}\" )";
        /// <summary>
        /// TEmplate for namespace command. <see cref="WriteUsingNameSpace(Stream, string)"/><see cref=""/>
        /// </summary>
        const string UsingNameSpaceTemplate = "using namespace {0};";
        /// <summary>
        /// used for single line comments <see cref="WriteComment(Stream, string)"/>
        /// </summary>
        const string SingleLineCommentTemplate = "// {0}";
        /// <summary>
        /// the /* for the start of a comment block <see cref="WriteBeginCommentBlock(Stream)"/>
        /// </summary>
        const string BeginCommentBlockPartLiteral = "/* ";
        /// <summary>
        /// the */ for the end of a comment block <see cref="WriteEndCommentBlock(Stream)"/>
        /// </summary>
        const string EndCommentBlockPartLiteral = " */ ";
        /// <summary>
        /// Used for a potential multiple comment <see cref="WriteCommentBlock(Stream, string)"/>
        /// </summary>
        const string CommentBlockTemplate = "/* {0} */";
        /// <summary>
        /// When a New Line is required we write this. <see cref="AutoLn"/> and <see cref="WriteNewLine(Stream)"/>
        /// </summary>
        const string EndLineLiteral = "\r\n";
        /// <summary>
        /// Return template <see cref="EmitReturnX(Stream, string)"/>
        /// </summary>
        const string ReturnLiteral = "return {0}";
        /// <summary>
        /// Template for variable with no assignment <see cref="EmitDeclareVariable(Stream, string, string, string)"/>
        /// </summary>
        const string DeclareVariableNoAssigment = "{0} {1}";
        /// <summary>
        /// Template for variable with assignment <see cref="EmitDeclareVariable(Stream, string, string, string)"/>
        /// </summary>
        const string DeclareVariableWithAssigment = "{0} {1} = {2}";
        /// <summary>
        /// Template for including a file in the standard include paths
        /// </summary>
        const string StandardIncludeTemplate = "#include <{0}>";
        /// <summary>
        /// TEmplate or including a file in project specified or relative include paths
        /// </summary>
        const string ProjectIncludeTemplate = "#include \"{0}\"";


        /// <summary>
        /// Template for if statement that evaluates to true.
        /// </summary>
        const string IfNonZero = "if ({0})";

        /// <summary>
        /// Template for <see cref="WriteDefinePreProcessorValue(Stream, string, string)"/> for no known value for a #define
        /// </summary>
        const string DefinePreProcessorNoVal = "#define {0}";
        /// <summary>
        /// Template for <see cref="WriteDefinePreProcessorValue(Stream, string, string)"/> for a known value for a #define
        /// </summary>
        const string DefinePreProessorKnownValue = "#define {0} ({1})";
        /// <summary>
        /// template for <see cref="WritePreProcessorIfdefCodeBlock(Stream, string)"/>
        /// </summary>
        const string DoesPreProcessorValueExist = "#ifdef {0}";
        /// <summary>
        /// Template for "#endif"
        /// </summary>
        const string EndPreProcessorRegion = "#endif";

        /// <summary>
        /// private variable that tracks indent level. 
        /// </summary>
        static int IndentLevelBacking = 0;
        /// <summary>
        /// We Indent with this char (a tab)
        /// </summary>
        const char IndentChar = '\t';

        /// <summary>
        /// buffer that's generated on <see cref="IndentLevel"/> change
        /// </summary>
        static string IndentBuffer = string.Empty;

        /// <summary>
        /// Specify a level to indent code with (0 means no indent). Regenerates <see cref="IndentBuffer"></> (an internal string) on assignment
        /// ArgumentOutOfRangeException() is thrown if 0 <= 0
        /// </summary>
        public static int IndentLevel
        {
            get
            {
                return IndentLevelBacking;
            }
            set
            {
                if (value < 0)
                {
                    throw new InvalidOperationException(MessageStrings.IndentLessZeroMessage);
                }
                IndentLevelBacking = value;
                StringBuilder ret = new StringBuilder();
                for (int stepper = 0; stepper < IndentLevelBacking; stepper++)
                {
                    ret.Append(IndentChar);
                }
                IndentBuffer = ret.ToString();
            }
        }
        #endregion


        // routines in this region serve as code foundation for the other stuff in this class
        #region Bottom Level Stream Emitting

        #region WriteIndent(), WriteLiteral() and WriteFormmatted()


        /// <summary>
        /// Write a formatted string to Output with up to 6 arguments. cares about <see cref="IndentLevel"/>
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
            WriteLiteral(Output, string.Format(CultureInfo.InvariantCulture, Template, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Write a formatted string to Output with up to 6 arguments.
        /// </summary>
        /// <param name="Output">Write to this stream</param>
        /// <param name="ForceIndent">Forces indent any if  true</param>
        /// <param name="Template"><see cref="string.Format(IFormatProvider?, string, object?)"/> template to use</param>
        /// <param name="arg1">defaults to "" </param>
        /// <param name="arg2">defaults to ""</param>
        /// <param name="arg3">defaults to ""</param>
        /// <param name="arg4">defaults to ""</param>
        /// <param name="arg5">defaults to ""</param>
        /// <param name="arg6">defaults to ""</param>
        public static void WriteFormattedNoIndent(Stream Output, bool ForceIndent, string Template, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "", string arg5 = "", string arg6 = "")
        {
            if (ForceIndent)
            {
                WriteFormated(Output, Template, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            else
            {
                WriteLiteralNoIndent(Output, string.Format(CultureInfo.InvariantCulture, Template, arg1, arg2, arg3, arg4, arg5, arg6), false);
            }
        }

        /// <summary>
        /// Write the current indent level to the target stream with <see cref="TargetEncoding"/> This is not dependent on <see cref="WriteLiteral(Stream, string)"/>
        /// </summary>
        /// <param name="Output">Write to this string using <see cref="TargetEncoding"/></param>
        public static void WriteIndent(Stream Output)
        {
            byte[] Stuff = TargetEncoding.GetBytes(IndentBuffer);
            if (Output == null)
            {
                throw new ArgumentNullException(nameof(Output));
            }
            Output.Write(Stuff, 0, Stuff.Length);
        }


        /// <summary>
        /// Write x to the stream in <see cref="TargetEncoding"/>. This forms the root of the other routines. 
        /// </summary>
        /// <param name="Output">Write to this string using <see cref="TargetEncoding"/></param>
        /// <param name="x">what to write to this stream</param>
        /// <remarks>All the other routines eventually boil down to calling this</remarks>
        public static void WriteLiteralNoIndent(Stream Output, string x, bool IndentAnyway = false)
        {
#if DEBUG
            if (x != null)
            {
                if (x.Contains("\0"))
                {
                    Debugger.Break();
                }
            }
#endif
            if (IndentAnyway)
            {
                WriteLiteral(Output, x);
                return;
            }
            if (Output == null)
            {
                throw new ArgumentNullException(nameof(Output));
            }
            byte[] Data = TargetEncoding.GetBytes(x);
            if (DebugEchoConsole)
            {
                //Console.Write(x);
                Console.Write(x);
            }

            Output.Write(Data, 0, Data.Length);
        }

        /// <summary>
        /// Write x to the stream in <see cref="TargetEncoding"/>. This forms the root of the other routines and includes <see cref="IndentLevel"/>.  amount of tabs as prefix
        /// </summary>
        /// <param name="Output">Write to this string using <see cref="TargetEncoding"/></param>
        /// <param name="x">what to write to this stream</param>
        /// <remarks>All the other routines eventually boil down to calling this</remarks>
        public static void WriteLiteral(Stream Output, string x)
        {

            if (Output == null)
            {
                throw new ArgumentNullException(nameof(Output));
            }
#if DEBUG

#endif

            x = IndentBuffer + x;
            WriteLiteralNoIndent(Output, x, false);
            /*
            if (DebugEchoConsole)
            {
                Console.Write(x.Replace("\r", "\\r\r").Replace("\n", "\\\n"));
            }
            byte[] Data = TargetEncoding.GetBytes(x);
            Output.Write(Data, 0, Data.Length);*/
        }

#endregion

#region Writing Symbols and Spaces
        /// <summary>
        /// Write the string uses as the new line <see cref="EndLineLiteral"/>. Default is '\r\n'
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
        public static void WriteNewLine(Stream Output)
        {
            WriteLiteralNoIndent(Output, EndLineLiteral);
        }

        /// <summary>
        /// Write { to the stream,  Auto Adds New line if AutoLn is set
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
        public static void WriteLeftBracket(Stream Output)
        {
            WriteLiteral(Output, "{");
            NewLineAuto(Output);
        }

        /// <summary>
        /// Write { to the stream,  Auto Adds New line if AutoLn is set. Increasing <see cref="IndentLevel"/> by 1 after writing the bracket
        /// </summary>
        /// <param name="Output">Write to this using <see cref="TargetEncoding"/></param>
        /// <param name="IndentTickUp">If true, we Increase <see cref="IndentLevel"/> by 1 after writing the {</param>
        public static void WriteLeftBracket(Stream Output, bool IndentTickUp)
        {
            WriteLeftBracket(Output);
            if (IndentTickUp)
            {
                IndentLevel++;
            }

        }

        /// <summary>
        /// Write { to the stream,  Auto Adds New line if AutoLn is set
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
        public static void WriteRightBracket(Stream Output)
        {
            WriteLiteral(Output, "}");
            NewLineAuto(Output);
        }

        /// <summary>
        /// Write } to the stream,  Auto Adds New line if  <see cref="AutoLn"/> is set. We decrease the Indent before Writing the } symbol
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
        /// <param name="DecreaseIndent"> If set we decrease indent level by 1 before writing symbol</param>
        public static void WriteRightBracket(Stream Output, bool DecreaseIndent)
        {
            if (DecreaseIndent)
            {
                IndentLevel--;
            }
            WriteRightBracket(Output);

        }
        /// <summary>
        /// Call <see cref="WriteNewLine(Stream)"/> if <see cref="AutoLn"/> is set to true
        /// </summary>
        /// <param name="Output">target stream</param>
        /// <remarks>Used by certain routines for a bit easier to read coding.</remarks>
        public static void NewLineAuto(Stream Output)
        {
            if (AutoLn)
            {
                WriteNewLine(Output);
            }
        }



        /// <summary>
        /// Write ';' to the stream without caring about <see cref="IndentLevel"/>
        /// </summary>
        /// <param name="Output">Write to this string using <see cref="TargetEncoding"/></param>
        public static void WriteSemiColon(Stream Output)
        {
            WriteLiteralNoIndent(Output, ";");
        }


#endregion
#endregion

#region comment emitting
        /// <summary>
        /// Emit a Single line C/C++ comment using the <see cref="SingleLineCommentTemplate"/>. (aka "// " ) It will always emit a new line if your literal does not have the '\r\n' and the end. It will emit an extra lien if <see cref="AutoLn"/>"/> is set
        /// </summary>
        /// <param name="Output">Write to this stream</param>
        /// <param name="Comment">Literal. This is exactly what the comment will be. </param>
        public static void WriteComment(Stream Output, string Comment)
        {
            WriteFormated(Output, SingleLineCommentTemplate, Comment);
            if (Comment != null)
            {
                if (Comment.Length != 0)
                {
                    if (Comment.EndsWith(EndLineLiteral, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        WriteNewLine(Output);
                    }
                }
            }
                
             NewLineAuto(Output);
        }

        /// <summary>
        /// Emit a potentially multi lined signed comment  the form of /* Comment */. Uses <see cref="CommentBlockTemplate"/>  and will emit a NewLine at the end if <see cref="AutoLn"/> is set
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
        /// <param name="Comment">Literal. This is exactly what the comment will be. </param>
        public static void WriteCommentBlock(Stream Output, string Comment)
        {
            WriteFormated(Output, CommentBlockTemplate, Comment);
            if (AutoLn)
            {
                WriteLiteral(Output, EndLineLiteral);
            }
        }

            /// <summary>
            /// Write "/*" to the stream.    Uses the private <see cref="BeginCommentBlockPartLiteral"/> string
            /// Respects <see cref="IndentLevel"/>
            /// CALLER should terminate it by Calling <see cref="WriteEndCommentBlock(Stream)"/>
            /// </summary>
            /// 
            /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
            /// <remarks> This just writes '/* ' to the Output Stream</remarks>
          public static void WriteBeginCommentBlock(Stream Output)
            {
                WriteLiteral(Output, BeginCommentBlockPartLiteral);
            }

            /// <summary>
            /// Write "*/" to the stream     Uses the <see cref="EndCommentBlockPartLiteral"/> string
            /// </summary>
            /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
            /// <remarks> just writes ' */' to the stream</remarks>
            public static void WriteEndCommentBlock(Stream Output)
            {
                WriteLiteral(Output, EndCommentBlockPartLiteral);
            }

#endregion
#region Return Emitting
            /// <summary>
            /// Emit return "x" to Output. x is not modified at all. This adds a semi colon at the end, respects <see cref="IndentLevel"/> and adds a new line if <see cref="AutoLn"/> is set
            /// </summary>
            /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
            /// <param name="X">X is written exactly as it s. Should one need quotes, add them</param>
            /// <remarks> To return from a C/C++ void function pass either String.Empty or null. This will return</remarks>
            public static void EmitReturnX(Stream Output, string X)
            {
               if (string.IsNullOrEmpty(X) == false)
               {
                    WriteFormated(Output, ReturnLiteral, X);
               }
               else
               {
                    WriteFormated(Output, ReturnLiteral, string.Empty);
               }
                    
                WriteLiteralNoIndent(Output, ";");
                NewLineAuto(Output);
            }

#endregion

#region Variable Declaring and Assigning

        /// <summary>
        /// Assign Literal to name. Respects <see cref="AutoLn"/>
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
        /// <param name="Name">variable name to assign too.</param>
        /// <param name="Literal">C/C++ expression to assign to variable being declared</param>
        /// <remarks>Caller should have written to Output that already declared the variable <see cref="EmitDeclareVariable(Stream, string, string, string)"/></remarks>
        public static void EmitAssignVariable(Stream Output, string Name, string Literal)
        {
            if (Literal == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MessageStrings.NonNullAndNonEmptyStringNeeded, nameof(Literal)));
            }
            else
            {
                if (Literal.Length == 0)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MessageStrings.NonNullAndNonEmptyStringNeeded, nameof(Literal)));
                }
            }

            WriteFormated(Output, EmitAssignVariableTemplate, Name, Literal);
            WriteSemiColon(Output);
            NewLineAuto(Output);
        }
        
        /// <summary>
        /// Declare variable Name of type 'Type'. If Value is not string.empty, we declare with assignment instead. Auto adds newline if <see cref="AutoLn"/> is true and always adds ';' after this call.  Uses templates <see cref="DeclareVariableNoAssigment"/> and <see cref="DeclareVariableWithAssigment"/>
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
        /// <param name="Type">Placed in the place the variable's type is.</param>
        /// <param name="Name">Placed where the variable's name is </param>
        /// <param name="Value">if "" or <see cref="string.Empty"/> we do not assign a value, otherwise we assign the value</param>
        public static void EmitDeclareVariable(Stream Output, string Type, string Name, string Value="")
            {
                if (string.IsNullOrEmpty(Value) == false)
                {
                    WriteFormated(Output, DeclareVariableWithAssigment, Type, Name, Value);
                }
                else
                {
                    WriteFormated(Output, DeclareVariableNoAssigment, Type, Name);
                }
                WriteSemiColon(Output);
                NewLineAuto(Output);
            }
#endregion

#region Preprocessor If and Defines
            /// <summary>
            /// Write an #ifdef that starts a block of code that is included if defined <see cref="DoesPreProcessorValueExist"/> for its template. This respects <see cref="IndentLevel"/> and always follows with a <see cref="WriteNewLine(Stream)"/> before returning
            /// </summary>
            /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
            /// <param name="CheckThisDefine">This is written after the #ifdef statement verbatim</param>
            public static void WritePreProcessorIfdefCodeBlock(Stream Output, string CheckThisDefine)
            {
                WriteFormated(Output, DoesPreProcessorValueExist, CheckThisDefine);
                WriteNewLine(Output);
            }

            /// <summary>
            /// write "#endif" part of a code block <see cref="EndPreProcessorRegion"/>  for its template. This respects <see cref="IndentLevel"/> and always follows with a <see cref="WriteNewLine(Stream)"/> before returning
            /// </summary>
            /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
            public static void WritePreProcessorEndIfCodeBlock(Stream Output)
            {
                    WriteLiteral(Output, EndPreProcessorRegion);
                    WriteNewLine(Output);
            }

            /// <summary>
            /// #define  a value for the Preprocessor named Name with a value of Value. <see cref="DefinePreProessorKnownValue"/> and <see cref="DefinePreProcessorNoVal"/> for the templates
            /// </summary>
            /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/></param>
            /// <param name="Name">goes in the name side</param>
            /// <param name="Value">specifies the value. Use null to not set a value</param>
            public static void WriteDefinePreProcessorValue(Stream Output, string Name, string Value=null)
            {
                if (Value == null)
                {
                    WriteFormated(Output, DefinePreProcessorNoVal, Name);
                }
                else
                {
                    WriteFormated(Output, DefinePreProessorKnownValue, Name, Value);
            }
            }
#endregion
#region Include and Pragma

        /// <summary>
        /// Write a pragma lib comment with the specified library. <see cref="PragmaTemplate"/>. Inserts a new line if <see cref="AutoLn"/> is true and respects <see cref="IndentLevel"/>
        /// </summary>
        /// <param name="Output">write to this using <see cref="TargetEncoding"/></param>
        /// <param name="library">include this library. The Template already has quotes</param>
        public static void WritePragmaLib(Stream Output, string library)
        {
            WriteFormated(Output, PragmaTemplate, library);
            NewLineAuto(Output);
        }
        /// <summary>
        /// Specifies which Template to use with <see cref="EmitInclude(Stream, string, EmitIncludeChoice)"/>
        /// </summary>
         enum EmitIncludeChoice
        {
            /// <summary>
            /// Include Statement generated with <see cref="StandardIncludeTemplate"/> as base
            /// </summary>
            Standard = 0,
            /// <summary>
            /// Include Statement generated with <see cref="ProjectIncludeTemplate"/> as base
            /// </summary>
            Project = 1
        }

        /// <summary>
        /// Emit an include for a standard search path non quoted #include statement. Inserts a new line if <see cref="AutoLn"/> is true and respects <see cref="IndentLevel"/>
        /// </summary>
        /// <param name="Output">write to this using <see cref="TargetEncoding"/></param>
        /// <param name="File">File to include. Search Bath is the normal compile include locations</param>
        /// <remarks>resolves to the non quoted version. <see cref="StandardIncludeTemplate"/></remarks>
        public static void EmitStandardInclude(Stream Output, string File)
        {
            EmitInclude(Output, File, EmitIncludeChoice.Standard);
        }

        /// <summary>
        /// Emit an include for a project specific search path quoted #include statement
        /// </summary>
        /// <param name="Output">write to this using <see cref="TargetEncoding"/></param>
        /// <param name="File">File to include. Search with is project specific file locations</param>
        /// <remarks>resolves to the non quoted version. <see cref="ProjectIncludeTemplate"/></remarks>
        public static void EmitProjectInclude(Stream Output, string File)
        {
            EmitInclude(Output, File, EmitIncludeChoice.Project);
        }
        /// <summary>
        /// Emit an include file. with the specified template
        /// </summary>
        /// <param name="Output">write to this using <see cref="TargetEncoding"/></param>
        /// <param name="File">include this file.</param>
        /// <param name="Style">specifies the template to use</param>
         static void EmitInclude(Stream Output, string File, EmitIncludeChoice Style)
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
                    // safety net encase someone force feeds an unsupported choice
                    throw new NotImplementedException(Enum.GetName(typeof(EmitIncludeChoice), Style));
            }
            NewLineAuto(Output);

        }
#endregion

#region Break Keyword
            /// <summary>
            /// Write a break statement followed by a semicolon. Auto Newlines if <see cref="AutoLn"/>
            /// </summary>
            /// <param name="Output">Write to this output using <see cref="TargetEncoding"/> </param>
            public static void WriteBreak (Stream Output)
            {
                WriteLiteral(Output, "break");
               WriteSemiColon(Output);
                NewLineAuto(Output);
            }
#endregion
#region Switch Statement
        /// <summary>
        /// Write switch ( X ) to output where X is VarToCheck.  Auto Newlines if <see cref="AutoLn"/> is set
        /// 
        /// IMPORTANT! Does not emit C/C++ brackets to go with it.
        /// </summary>
        /// <param name="Output">write to stream using <see cref="TargetEncoding"/> </param>
        /// <param name="VarToCheck">Literal, This is the C/C++ integer variable or constant to check. It gets placed where 'X 'goes </param>
        public static void WriteSwitch(Stream Output, string VarToCheck)
            {
                WriteFormated(Output, SwitchFuncTemplate, VarToCheck);
                NewLineAuto(Output);
            }

        /// <summary>
        /// Write a case ( X) to output where X is number to test for. Auto Newlines  if <see cref="AutoLn"/> is set
        /// 
        /// IMPORTANT!  C/C++ expects this keyword in a switch statement block.  This just writes the case part
        /// </summary>
        /// <param name="Output">write to stream using <see cref="TargetEncoding"/> </param>
        /// <param name="CaseVal">This is the integer</param>
        /// <param name="CaseVal">Literal, This is the C/C++ integer variable or constant to write after 'case'. It gets placed where 'X 'goes </param>
        public static void WriteCase(Stream Output, string CaseVal)
            {
            WriteFormated(Output, CaseFuncTemplate, CaseVal);
            NewLineAuto(Output);
            }
#endregion
#region If Statements
        /// <summary>
        /// Write the start of an If statement block.
        /// </summary>
        /// <param name="Output">Write to this</param>
        /// <param name="Eval">This is  literal, should it evaluate to true at runtime the code after this is ran</param>
        /// <remarks>IMPORTANT! Does not emit code block stuff</remarks>
        public static void WriteIf(Stream Output, string Eval)
            {
                WriteFormated(Output, IfNonZero, Eval);
                NewLineAuto(Output);
            }
#endregion

#region Name Space Routines
            /// <summary>
            /// Write a using namespace "Name" command. Uses <see cref="UsingNameSpaceTemplate"/> and will add a semi color. Also adds a newline if <see cref="AutoLn"/> is true
            /// </summary>
            /// <param name="Output"></param>
            /// <param name="Name"></param>
            public static void WriteUsingNameSpace(Stream Output, string Name)
            {
                WriteFormated(Output, UsingNameSpaceTemplate, Name);
                
                WriteNewLine(Output);
            }
#endregion
#region Function Prototype Emitting

#region Function Emitting Template Stuff
            const string DllImport = "__declspec(dllimport)";
            const string DllExport = "__declspec(dllexport)";
            const string WINAPI = "WINAPI";
            const string CDECL = "_cdecl";
            const string INLINE = "inline";
            const string FASTCALL = "FASTCALL";
            const string VariableArgStuffix = "...";


        /// <summary>
        /// template used when a <see cref="EmitDeclareFunctionSpecs"/> calling conversion is nothing or unspecified
        /// </summary>
        const string FunctionProtoTypeBeginNoConvTemplate = "{0} {1} {2} (";
        /// <summary>
        /// template for start of a prototyping a C/C++ function that's not a function pointer
        /// </summary>
        const string FunctionProtoTypeBeginTemplate = "{0} {1} {2} {3} (";
        /// <summary>
        /// Template for prototyping a C/C++ function pointer 
        /// </summary>
        const string FunctionProtoTypePtrBeginTemplate = "{0} ({1}* {2}) {3} (";
        /// <summary>
        /// typedef blob that gets emitted as a prefix when emitting  typedef function pointer. 
        /// </summary>
        const string TypeDefBit = "typedef ";

        /// <summary>
        /// Template used when we are assigning something to a previously declared variable
        /// </summary>
        const string EmitAssignVariableTemplate = "{0} = {1}";

        /// <summary>
        /// This collection is used to specify how the private <see cref="EmitDeclareFunctionInternal(Stream, string, EmitDeclareFunctionSpecs, string, string, bool, List{string}, List{string}, EmitDeclareArgHandling, string)"/> emits its code
        /// </summary>
        [Flags]
        internal enum EmitDeclareArgHandling
        {
            /// <summary>
            /// Normal handling, Keep both the name and the types. ArgType and ArgName must be null or have matching counts
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Drop Function Names from the emit. ArgName MayBe null.
            /// </summary>
            DropNames = 2,
            /// <summary>
            /// Drop Function Types from the emit. ArgType may be null
            /// </summary>
            DropTypes = 4,

            /// <summary>
            /// disables the calling convention check. Purpose is because the public routine already done it
            /// </summary>
            DisableFunctionSpecsCheck = 8,

            /// <summary>
            /// Drops return value parts and calling convention stuff from the function call emit. ArgType may be null Calling a function with <see cref="EmitCallFunction(Stream, string, List{string}, List{string})"/> specifies this argument
            /// </summary>
            DropDeclareStuff = 16

            

        }
#endregion

            /// <summary>
            /// Flags to modify how the Function Emitting routines generate the function prototypes
            /// </summary>
        [Flags]
            public enum EmitDeclareFunctionSpecs
            {
            /// <summary>
            /// Emits nothing in the Calling Convention type
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
                /// Emits WINAPI calling type "stdcall"
                /// </summary>
                WINAPI = 4,
                /// <summary>
                /// Emits CDECL calling type "_cdecl"
                /// </summary>
                CDECL = 8,
                /// <summary>
                /// Emits FASTCALL calling Type _fastcall
                /// </summary>
                FASTCALL = 16,
                /// <summary>
                /// Emits INLINE Modifier inlinbe
                /// </summary>
                INLINE = 32,
                /// <summary>
                /// includes the Variable Argument thing '. . .' at the end.
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
        /// Validate a ArgType and ArgName list passed to an emit function. A return value of true should mean that the EmitDeclareFunction stuff should be able to handle the lists OK.
        /// </summary>
        /// <param name="ArgType">list of strings that correspond to Argument Types to validate. ArgType[X] = is ArgName[X]'s type. Both are strings</param>
        /// <param name="ArgName">list of strings that correspond to Argument Name to validate.ArgName[X]'s type is ArgType[X]'s value. Both are strings</param>
        /// <param name="Mode">Used to distinguish valid modes  For most cases, <see cref="EmitDeclareArgHandling.Normal"/> is what is needed</param>
        /// <returns>returns false under these conditions for <see cref="EmitDeclareArgHandling.Normal"/>
        /// <list type="table">
        /// FOR NORMAL:
        /// (if other ArgType or ArgName is null but not both;
        /// if ArgType.Count != ArgName.Count)
        /// 
        /// returns true under these conditions:
        /// (if ArgType and ArgName are both null;
        /// if (argtype.count == argname.count))
        /// </list>
        /// For all other cases:
        /// Validation is considerably relaxed <see cref="EmitDeclareFunctionInternal(Stream, string, EmitDeclareFunctionSpecs, string, string, bool, List{string}, List{string}, EmitDeclareArgHandling, string)"/> does its own validation and edge cases then
        /// SPECIAL CASE for <see cref="EmitCallFunction(Stream, string, List{string}, List{string})"></see> (ArgTypeNullOk is true) allows ArgType to be null.
        /// </returns>
        static bool ValidateArgTypeName(ref List<string> ArgType, ref List<string> ArgName, EmitDeclareArgHandling Mode)
        {
            if (Mode.HasFlag(EmitDeclareArgHandling.Normal))
            {
                if (((ArgType == null) ^ (ArgName == null)))
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
            else
            {
                if (Mode.HasFlag(EmitDeclareArgHandling.DropNames) && (Mode.HasFlag(EmitDeclareArgHandling.DropTypes)))
                {
                    return false;
                }
                else
                {
                    if (Mode.HasFlag(EmitDeclareArgHandling.DropDeclareStuff))
                    {
                        return true;
                    }
                    if (Mode.HasFlag(EmitDeclareArgHandling.DropNames) ^ (Mode.HasFlag(EmitDeclareArgHandling.DropTypes)))
                    {
                        return true;
                    }
                }

                return false;

            }
        }

        public static void WriteElse(Stream Output)
        {
            WriteLiteral(Output, "else");
            NewLineAuto(Output);
        }

        /// <summary>
        /// check for contrary values and return true if OK or FALSE if not. 
        /// </summary>
        /// <param name="x">the value to check</param>
        /// <param name="ThrowOnFalse">if set and the routine would return false, we throw in InvalidOperationError with the problem specified in English</param>
        /// <returns>true if all tests passed or false (if ThrowOnFalse is NOT set) if a test failed</returns>
        /// <exception cref="InvalidOperationException"> if the test failed and ThrowOnFalse=true</exception>
        public static bool ValidateFunctionSpecsEnum(EmitDeclareFunctionSpecs x,  bool ThrowOnFalse=false, [CallerMemberName] string Caller = "")
        {
            if (x == EmitDeclareFunctionSpecs.None)
                return true;
            if (HasMultiCallingConventions(x))
            {
                if (ThrowOnFalse)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, MessageStrings.ValidateFunctionSpecsEnumToManyCalling, Caller));
                }
                return false;
            }
            else
            {
                if (x.HasFlag(EmitDeclareFunctionSpecs.INLINE))
                {
                    if (x.HasFlag(EmitDeclareFunctionSpecs.DllExport) || (x.HasFlag(EmitDeclareFunctionSpecs.DllImport) ))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, MessageStrings.ValidateFunctionSpecsEnumNoInlineDllStuff, Caller));
                    }
                }
                if (x.HasFlag(EmitDeclareFunctionSpecs.DllExport) && (x.HasFlag(EmitDeclareFunctionSpecs.DllImport)))
                {
                    if (ThrowOnFalse)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, MessageStrings.ValidateFunctionSpecsEnumDupDllStuff, Caller));
                    }
                    return false;
                }

                if (x.HasFlag(EmitDeclareFunctionSpecs.VariableArgs) && (x.HasFlag(EmitDeclareFunctionSpecs.CDECL) == false))
                {
                    if (ThrowOnFalse)
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, MessageStrings.ValidateFunctionSpecsEnumCDeclForVargs, Caller));
                    }
                }

                return true;

            }
        }
        /// <summary>
        /// helper to new if the passed value has multiple call types currently set (like CDECL and FASTCALL, etc . . .
        /// </summary>
        /// <param name="x">the value to check against</param>
        /// <returns>returns true if the enum has more than one calling convention set (cdecl, fastcall, winapi)</returns>
        public static bool HasMultiCallingConventions(EmitDeclareFunctionSpecs x)
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
        /// Emit a call to the passed function.
        /// </summary>
        /// <param name="Output">Write to This stream with <see cref="TargetEncoding"/> format</param>
        /// <param name="Name">Goes in the spot that is function name</param>
        /// <param name="ArgType">IGNORED in emitting code to call a function but is passed to internal EmitFunction </param>
        /// <param name="ArgName">Names of the function arguments.</param>
        /// <param name="VariableAssign">if not blank, the function call is emitted as an assignment to this previously declared variable <see cref="EmitDeclareVariable(Stream, string, string, string)"/></param>
        /// <remarks> This call like the other EmitFunction stuff route to an internal function that handles it. Said function does not need the argument for this mode</remarks>
        /// <example>
        /// List of Strings ArgNames = { "hWnd", "szApp", "szOtherStuff", "hIcon" };
        /// 
        /// 
        /// 
        /// 
        /// CodeGen.EmitFunctionCall(TargetOutput,  "ShellAboutW", null, ArgNames);
        /// 
        /// Result should look like:
        /// ShellAboutW(hWnd, szApp, szOtherStuff, hIcon);
        /// </example>
        public static void EmitCallFunction(Stream Output, string Name, List<string> ArgType, List<string> ArgName, string VariableAssign="")
        {
            if (ArgType == null)
            {
                ArgType = EmptyList;
            }

            if (ArgName == null)
            {
                ArgName = EmptyList;
            }

            if (string.IsNullOrEmpty(VariableAssign))
            {
                EmitDeclareFunctionInternal(Output, string.Empty, EmitDeclareFunctionSpecs.None, Name, string.Empty, false, ArgType, ArgName, EmitDeclareArgHandling.DropTypes | EmitDeclareArgHandling.DropDeclareStuff);
            }
            else
            {
                WriteLiteral(Output, VariableAssign + "= ");
                EmitDeclareFunctionInternal(Output, string.Empty, EmitDeclareFunctionSpecs.None, Name, string.Empty, false, ArgType, ArgName, EmitDeclareArgHandling.DropTypes | EmitDeclareArgHandling.DropDeclareStuff);
            }
            
            WriteSemiColon(Output);
            NewLineAuto(Output);

        }
        /// <summary>
        /// Calls <see cref="EmitDeclareFunction"/>and places a semicolon at the end  
        /// </summary>
        /// <param name="Output">see <see cref="EmitDeclareFunction(Stream, string, EmitDeclareFunctionSpecs, string, List{string}, List{string})"/></param>
        /// <param name="ReturnType">see <see cref="EmitDeclareFunction(Stream, string, EmitDeclareFunctionSpecs, string, List{string}, List{string})</param>
        /// <param name="CallingConvention">see <see cref="EmitDeclareFunction(Stream, string, EmitDeclareFunctionSpecs, string, List{string}, List{string})</param>
        /// <param name="Name">see <see cref="EmitDeclareFunction(Stream, string, EmitDeclareFunctionSpecs, string, List{string}, List{string})</param>
        /// <param name="ArgType">see <see cref="EmitDeclareFunction(Stream, string, EmitDeclareFunctionSpecs, string, List{string}, List{string})</param>
        /// <param name="ArgName">see <see cref="EmitDeclareFunction(Stream, string, EmitDeclareFunctionSpecs, string, List{string}, List{string})</param>
        public static void EmitDeclareFunctionPrototype(Stream Output, string ReturnType, EmitDeclareFunctionSpecs CallingConvention, string Name, List<string> ArgType, List<string> ArgName)
        {
            EmitDeclareFunction(Output, ReturnType, CallingConvention, Name, ArgType, ArgName);
            WriteSemiColon(Output);
        }
        /// <summary>
        /// Declare a function (without semicolon) 
        /// </summary>
        /// <param name="Output">Write to here with <see cref="TargetEncoding"/></param>
        /// <param name="ReturnType">Literal. Is placed in the return value spot. string.empty defaults to "void"</param>
        /// <param name="CallingConvention">controls the emitting of certain settings between ReturnType and Name. see <see cref="EmitDeclareFunctionSpecs"/> for details
        /// <param name="Name">Literal. Is placed in the name spot.</param>
        /// <param name="ArgTypes">Specify The Argument Types. May leave null for no arguments</param>
        /// <param name="ArgNames">Specifies the nae of the arguments. May leave null for no arguments</param>
        public static void EmitDeclareFunction(Stream Output, string ReturnType, EmitDeclareFunctionSpecs CallingConvention, string Name,  List<string> ArgType, List<string> ArgName)
        {
            if (CallingConvention != EmitDeclareFunctionSpecs.None)
            {
                try
                {
                    ValidateFunctionSpecsEnum(CallingConvention, true);
                }
                catch (InvalidOperationException e)
                {
                    throw new InvalidOperationException(MessageStrings.ValidateFunctionSpecsEnumFailMsg, e);
                }
            }


            if (ValidateArgTypeName(ref ArgType, ref ArgName, EmitDeclareArgHandling.Normal) == false)
            {
                throw new ArgumentException(MessageStrings.ValidateArgFailureMsg);
            }

            if (ArgType == null)
            {
                ArgType = new List<string>();
            }

            if (ArgName == null)
            {
                ArgName = new List<string>();
            }
            EmitDeclareFunctionInternal(Output, ReturnType, CallingConvention, Name, string.Empty, false, ArgType, ArgName, EmitDeclareArgHandling.DisableFunctionSpecsCheck);
        }
        /// <summary>
        /// Emit a typedef for a function pointer 
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
                        throw new InvalidOperationException(MessageStrings.EmitTypeDefBadMsg1); 
                    }
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllImport))
                    {
                        throw new InvalidOperationException(MessageStrings.EmitTypeDefBadMsg2);
                    }
                }
                catch (InvalidOperationException e)
                {
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllExport))
                    {
                        throw new InvalidOperationException(MessageStrings.EmitTypeDefBadMsg1, e);
                    }
                    if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.DllImport))
                    {
                        throw new InvalidOperationException(MessageStrings.EmitTypeDefBadMsg2, e);
                    }
                }

                if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.INLINE))
                {
                    throw new InvalidOperationException(MessageStrings.EmitTypeDefBadMsgInline);
                }

            }
            else
            {
                CallerMods = EmitDeclareFunctionSpecs.WINAPI;
            }

            if (ArgTypes == null)
            {
                // spin
                ArgTypes = EmptyList;
            }

            if (ArgNames == null)
            {
                // spin
                ArgNames = EmptyList;
            }

            EmitDeclareFunctionInternal(Output, ReturnType, CallerMods, Name, TypeDefBit, true, ArgTypes, ArgNames, EmitDeclareArgHandling.DropNames | EmitDeclareArgHandling.DisableFunctionSpecsCheck);
            WriteSemiColon(Output);
            NewLineAuto(Output);

        }
        /// <summary>
        /// internal for the public EmitFunction routines. The public routines check a few things and call this to perform the work needed
        /// </summary>
        /// <param name="Output">emit to this stream in <see cref="TargetEncoding"/></param>
        /// <param name="ReturnType">Literal. Gets placed in the spot that specifies the return type. Can specify string.empty for "void"</param>
        /// <param name="CallerMods">modifies how the calling conversion is generated. See <see cref="EmitDeclareFunctionSpecs"/> for more details</param>
        /// <param name="Name">This gets placed in the spot that specifies the name of this function pointer or prototype</param>
        /// <param name="Prefix">Intended for the typedef CodeGen parts. This specifies a literal that gets written to Output first before this routine writes anything else</param>
        /// <param name="IsFunctionPtr">if set we use the <see cref="FunctionProtoTypePtrBeginTemplate"/> template instead of the <see cref="FunctionProtoTypeBeginTemplate"/></param>
        /// <param name="ArgTypes">This list of strings goes into the Argument list for this function. These specify ArgType and line up with ArgName at the same index. (ArgType [x] should match with ArgName [x]). It Can be null if the routine does not need any arguments.</param>
        /// <param name="ArgNames">This list of strings gets emitting into the argument name slot for this function. each ArgNames [x] value is paired with ArgType [x] also. Can be null.</param>
        /// <param name="ArgHandling">This modifies how Arguments are handled. See the enum <see cref="EmitDeclareArgHandling"/> for specifies"/></param>
        /// <param name="BlameMe">using CallermemberName to default to who called this function. Exceptions generated by this will point to blame this function. Defaults to caller</param>
        /// <exception cref="InvalidOperationException"> can happen if either CallerModes or the ArgType + ArgName do not pass validation. <see cref="ValidateArgTypeName(ref List{string}, ref List{string})"/> and <see cref="ValidateFunctionSpecsEnum(EmitDeclareFunctionSpecs, bool, string)"/></exception> 
        internal static void EmitDeclareFunctionInternal(Stream Output,
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
            // tracks the WalkThru of the Argument List. No Argument List or if they are of count 0, this stays null
            int step = 0;
            // we translate CallerMods into this eventually
            StringBuilder CallingStr = new StringBuilder();

            // we extract a single calling convention to this
            EmitDeclareFunctionSpecs TargetCallingConvention;

            // used to contain our stopping point in ArgTypes or ArgNames,  may be either ArgTypes.Count or ArgName.Count
            int ArgStep;

            // intended if the caller has already verified arguments. This disables OUR check
            if (!ArgHandling.HasFlag(EmitDeclareArgHandling.DisableFunctionSpecsCheck))
            {
                try
                {
                    if (ValidateFunctionSpecsEnum(CallerMods, true) != true)
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MessageStrings.ValidateCallerFuncFailMsg, BlameMe));
                    }
                }
                catch (InvalidOperationException e)
                {
                    //throw new ArgumentException("Validation for EmitDeclareFunctionSpecs failed in " + BlameMe + ".", e);
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MessageStrings.ValidateCallerFuncFailMsg, BlameMe), e);
                }
                if (ValidateArgTypeName(ref ArgTypes, ref ArgNames, ArgHandling) == false)
                {
                    //throw new InvalidOperationException("The Emit Function ArgType and ArgName failed validation in " + BlameMe + ".");
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MessageStrings.ValidateCallerFuncFailMsg, BlameMe));
                }
            }


            // if DropDeclareStuff is set we skip unneeded calculations
            if (ArgHandling.HasFlag(EmitDeclareArgHandling.DropDeclareStuff) == false)
            {
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

                if (string.IsNullOrEmpty(ReturnType))
                {
                    ReturnType = "void";
                }
            }


            if (!IsFunctionPtr)
            {
                if (CallingStr.Length != 0)
                {
                    WriteFormated(Output, FunctionProtoTypeBeginTemplate, ReturnType,
                        CallingStr.ToString(),
                        Name,
                        string.Empty

                        );
                }
                else
                {
                    WriteFormated(Output,  FunctionProtoTypeBeginNoConvTemplate,
                        Name,
                        string.Empty

                        );

                }
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
                // we handle arguments

                string aType, aName;
              
                if ((ArgTypes.Count != 0) ^ (ArgNames.Count  != 0))
                {
                    ArgStep = Math.Max(ArgTypes.Count, ArgNames.Count);
                }
                else
                {
                    ArgStep = Math.Min(ArgTypes.Count, ArgNames.Count);
                }

                // contains arguments
                for (step = 0; step < ArgStep; step++)
                {
                    if (ArgHandling.HasFlag( EmitDeclareArgHandling.Normal) )
                    {
                        aType = ArgTypes[step];
                        aName = ArgNames[step];
                        WriteFormattedNoIndent(Output, false, "{0} {1}", aType, aName);
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

                        if (string.IsNullOrEmpty(aType) == false)
                        {
                            WriteFormattedNoIndent(Output, false, "{0}", aType);

                            if (string.IsNullOrEmpty(aName) == false)
                            {
                                WriteFormattedNoIndent(Output, false, " {0}", aName);
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(aName) == false)
                            {
                                WriteFormattedNoIndent(Output, false, "{0}", aName);
                            }
                        }

                        
                    }

                   
                    if ( (step + 1) != ArgStep)
                    {
                        WriteLiteralNoIndent(Output, ",");
                    }
                }
            }
            if (CallerMods.HasFlag(EmitDeclareFunctionSpecs.VariableArgs))
            {
                if (step != 0)
                {
                    WriteLiteralNoIndent(Output, ", " + VariableArgStuffix,false);
                }
                else
                {
                    WriteLiteralNoIndent(Output, VariableArgStuffix, false);
                }
            }

            WriteLiteralNoIndent(Output, ")", false);

        }
#endregion

#region Specific Function Emits


        /// <summary>
        /// Emit the code that handles a detoured function based on settings.
        /// </summary>
        /// <param name="Output">Output to here</param>
        /// <param name="DebuggerResponseVarName">name of the C/C++ DEBUGGER_RESPONSE struct</param>
        public static void EmitDiverDetourTrackFuncBody(Stream Output, string DebuggerResponseVarName)
        {
            NewLineAuto(Output);
            // Function Body Bracket
            {
                WriteLeftBracket(Output, true);

                WriteIf(Output, DebuggerResponseVarName + ".DebuggerSeenIt != 0 ");
                {
                    WriteLeftBracket(Output, true);


                    WriteSwitch(Output, DebuggerResponseVarName + ".Flags");
                    {
                        WriteLeftBracket(Output, true);


                        WriteCase(Output, "ForceReturn");
                        {
                            WriteLeftBracket(Output, true);
                            EmitReturnX(Output, DebuggerResponseVarName + ".Arg1");
                            WriteBreak(Output);

                            WriteRightBracket(Output, true);
                            
                        }
                        WriteRightBracket(Output, true);
                    }
                    WriteRightBracket(Output, true);
                }
                WriteRightBracket(Output, true);
            }
  

        }



        /// <summary>
        /// Emit  a call to the CRT free() function with optional skipping call if the Varaible evals to null
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="Variable"></param>
        /// <param name="NullFreeGuard"></param>
        public static void EmitCallFree(Stream Output, string Variable, bool NullFreeGuard=true)
        {
            if (string.IsNullOrEmpty(Variable))
            {
                throw new ArgumentNullException(nameof(Variable));
            }
            if (NullFreeGuard)
            {
                WriteIf(Output, string.Format(CultureInfo.InvariantCulture, "(({0}) != 0)", Variable));
                WriteLeftBracket(Output, true);
                    EmitCallFunction(Output, "free", null, new List<string>() { Variable });
                WriteRightBracket(Output, true);
            }
            else
            {
                EmitCallFunction(Output, "free", null, new List<string>() { Variable });
            }
        }
        /// <summary>
        /// Emit a call to the CRT malloc routine and assign to variable
        /// </summary>
        /// <param name="Output">Write to this buffer using <see cref="TargetEncoding"/></param>
        /// <param name="SizeExpression">this is pastes as the argument to malloc</param>
        /// <param name="AssignToo">a C/C++ variable name to assign to</param>
        /// <param name="DeclareAssign">if true we declare the variable during the call</param>
        public static void EmitCallMalloc(Stream Output, string SizeExpression, string AssignToo, bool DeclareAssign=false)
        {
            if (string.IsNullOrEmpty(AssignToo))
            {
                throw new ArgumentNullException(nameof(AssignToo));
            }
            if (DeclareAssign)
            {
                EmitDeclareVariable(Output, "SIZE_T", AssignToo, string.Format(CultureInfo.InvariantCulture, "malloc( ({0}) )", SizeExpression));
            }
            else
            {
                EmitCallFunction(Output, "malloc", null, new List<string>() { SizeExpression }, AssignToo);
            }
        }

        public enum ZeroMemoryArg
        {
            /// <summary>
            /// pastes it within a sizeof(x) command
            /// </summary>
            UseSizeOfTemplate = 0,
            /// <summary>
            /// passed value is used as ZeroMemory Arg
            /// </summary>
            ExactlyAsPassed = 1
        }
        /// <summary>
        /// Emit a call to Windows Api ZeroMemory
        /// </summary>
        /// <param name="Output">write to this stream</param>
        /// <param name="Target">Target variable. the '&' symbol is added automatically</param>
        /// <param name="SizeOfArg">gets set as argument 2. in  a C/C++ sizeof() call</param>
        /// <example> 
        /// EmitZeroMemory(Output,  TargetPtr, DWORD);
        /// 
        /// results in something like
        /// ZeroMemory(&TargetPtr, sizeof(DWORD));  
        /// 
        /// being written to the output 
        /// </example>
        public static void EmitCallZeroMemory(Stream Output, string Target, string SizeOfArg, ZeroMemoryArg TemplateType= ZeroMemoryArg.UseSizeOfTemplate)
        {
            string ZeroMemoryTemplate;
            switch (TemplateType)
            {
                case ZeroMemoryArg.ExactlyAsPassed:
                    ZeroMemoryTemplate = "{0}";
                    break;
                case ZeroMemoryArg.UseSizeOfTemplate:
                    ZeroMemoryTemplate = "sizeof({0})";
                    break;
                default:
                    ZeroMemoryTemplate = "{0}";
                    break;
            }
            EmitCallFunction(Output, "ZeroMemory",null, new List<string>() { "&"+Target , string.Format(CultureInfo.InvariantCulture, ZeroMemoryTemplate, SizeOfArg) });
        }

        


        /// <summary>
        /// Call OutputDebugStringW
        /// </summary>
        /// <param name="Output">Write to this stream using <see cref="TargetEncoding"/> format</param>
        /// <param name="Value">Literal. Goes in the argument slot. </param>
        /// <param name="AddQuotes">if set to true and value does not start and stop with a quote, we add them. SET TO WRITE if your Value is indented to be a quoted literal rather than a wchar_t* string</param>
        /// <remarks> if you're using a variable rather than a literal const wchar_t* string, keep AddQuotes to false</remarks>
        public static void EmitCallOutputDebugString(Stream Output, string Value, bool AddQuotes=false)
        {
           if (AddQuotes)
            {
                Value = AddQuotesIfNone(Value);
                
            }
           if (Value.StartsWith("\"", StringComparison.InvariantCultureIgnoreCase))
            {
                if (Value.EndsWith("\"", StringComparison.InvariantCultureIgnoreCase))
                {
                    Value = "L" + Value; // tells visual studio C++ to use unicode strings
                }
            }
             
            EmitCallFunction(Output, "OutputDebugStringW", null, new List<string> { Value });
        }
#endregion
#region Specific Variable Declares

        /// <summary>
        /// reset a wide string to empty courtesy of https://stackoverflow.com/questions/2848087/how-to-clear-stringstream
        /// </summary>
        /// <param name="Output"></param>
        /// <param name="StreamName"></param>
        public static void EmitClearWideStreamStream(Stream Output, string StreamName )
        {
            WriteLiteral(Output, string.Format(CultureInfo.InvariantCulture, "{0}.str ( L\"\" )", StreamName));
            WriteSemiColon(Output);
            NewLineAuto(Output);
            WriteLiteral(Output, string.Format(CultureInfo.InvariantCulture, "{0}.clear()", StreamName));
            WriteSemiColon(Output);
            NewLineAuto(Output);
        }

        /// <summary>
        /// Write an insert operator on the passed stream with the literal.  Unlike the one at a time version of <see cref="EmitInsertStream(Stream, string, string, bool)"/>, this one finishes the emit with a semicolon and allows multiple insert writes to be written in a single call
        /// </summary>
        /// <param name="Output">We emit code to this C# stream using <see cref="TargetEncoding"/></param>
        /// <param name="StreamName">C/C++ stream to write too</param>
        /// <param name="Literals">list of literals to emit</param>
        /// <param name="AddQuotes">adds quotes to each entry if true</param>
        public static void EmitInsertStream(Stream Output, string StreamName, List<string> Literals, bool AddQuotes)
        {
            if ( (Literals==null) || (Literals.Count == 0))
            {
                // its fine. There's nothing to output
                return;
            }
            else
            {
                if (AddQuotes)
                {
                    for (int step =0; step < Literals.Count;step++)
                    {
                        Literals[step] = AddQuotesIfNone(Literals[step]);
                    }
                }

                WriteLiteralNoIndent(Output, IndentBuffer, false);
                WriteFormattedNoIndent(Output, false, OperatorShiftLeftStreamTemplate, StreamName, Literals[0]);

                for (int step = 1; step < Literals.Count;step++)
                {
                    WriteFormattedNoIndent(Output, false, OperatorShiftLeftStreamTemplatePart, Literals[step]);
                }

                WriteSemiColon(Output);
                NewLineAuto(Output);
            }
        }
        /// <summary>
        /// Write an insert operator on the passed stream with the literal. Caller is responsible for semi-colon when done. uses template <see cref="OperatorShiftLeftStreamTemplate"></see>
        /// {0} SHIFT_LEFT {1}
        ///
        /// </summary>
        /// <param name="Output">Emit to this stream using <see cref="TargetEncoding"/> </param>
        /// <param name="StreamName">name of the variable stream to write to (this is in C/C++ content NOT C#</param>
        /// <param name="Literal">Literal, unmodified but gets quotes if <paramref name="AddQuotes"/> is true</param>
        /// <param name="AddQuotes">If set and Literal does not start and stop with a " symbol, they are added</param>
        /// <remarks> CALLER IS required to write a semi-colon or use <see cref="WriteSemiColon(Stream)"/> when done</remarks>
        public static void EmitInsertStream(Stream Output, string StreamName, string Literal, bool AddQuotes)
        {
            if (AddQuotes)
            {
                Literal = AddQuotesIfNone(Literal);
            }

            WriteFormated(Output, OperatorShiftLeftStreamTemplate, StreamName, Literal);


        }
        /// <summary>
        /// Declare a variable of type wstringstream in the stream
        /// </summary>
        /// <param name="Output">Write to this using <see cref="TargetEncoding"/></param>
        /// <param name="StreamName">name for this variable / stream </param>
        /// <param name="NameSpacePrefix">if set we emit std::wstringstream  instead of wstringstream </param>
        public static void EmitDeclareWideStringStream(Stream Output, string StreamName, bool NameSpacePrefix = false)
        {
            if (!NameSpacePrefix)
                EmitDeclareVariable(Output, "wstringstream", StreamName);
            else
                EmitDeclareVariable(Output, "std::wstringstream", StreamName);
        }
        /// <summary>
        /// Diver Helper, Adds call to push_back() the passed value to the passed vector.
        /// </summary>
        /// <param name="Output">write to this using the <see cref="TargetEncoding"/> encoding</param>
        /// <param name="VectorName">vector in question</param>
        /// <param name="VectorValue">literal to pass to push to the vector's backs</param>
        /// <param name="AddQuotes">if True and string is not quoted, adds them</param>
        public static void EmitPushVectorValue(Stream Output, string VectorName, string VectorValue, bool AddQuotes = false)
        {
            if (AddQuotes)
            {
                VectorValue = AddQuotesIfNone(VectorValue);
            }
            WriteFormated(Output, VectorPushBackTemplate, VectorName, VectorValue);
            WriteSemiColon(Output);
            NewLineAuto(Output);
        }
        /// <summary>
        /// Helper for Detours. Declares a C++ vector containing a list of TypeOfVector .   Does not assign anything to it and adds semi-color if <see cref="AutoLn"/> is true
        /// </summary>
        /// <param name="Output">Write to this</param>
        /// <param name="Name">name of the vector</param>
        /// <param name="TypeOfVector">The type of Vector to Define. Should Resolve to STring literal that's a valid C/C++ style. Defaults to ULONG_PTR</param>
        /// <param name="NameSpacePrefix">forced std:: at the beginning of the name if true</param>
        public static void EmitDeclareVector(Stream Output, string Name, string TypeOfVector = "ULONG_PTR", bool NameSpacePrefix = false)
        {
            string FinalVarType;
            if (NameSpacePrefix)
            {
                FinalVarType = string.Format(CultureInfo.InvariantCulture, "std::vector<{0}>", TypeOfVector);
                
            }
            else
            {
                FinalVarType = string.Format(CultureInfo.InvariantCulture, "vector<{0}>", TypeOfVector);
            }
            EmitDeclareVariable(Output, FinalVarType, Name);
        }
#endregion

#region Common tools


        /// <summary>
        /// Helper Tool.  returns "TRUE" if value is true and "FALSE" if value is false. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns> returns "TRUE" if value is true and "FALSE" if value is false. </returns>
        public static string BoolToCPP(bool value)
        {
            if (value)
            {
                return "TRUE";
            }
            else
            {
                return "FALSE";
            }
        }
        /// <summary>
        /// Utility routine. Some routines use an internal memorystream buffer before converting to string. This dumps the contents of the buffer back into string form with <see cref="TargetEncoding"/> formatting
        /// </summary>
        /// <param name="Source">the source to read from</param>
        /// <returns></returns>
        /// <remarks>Most of the code in <see cref="DiverApiCodeGen"/>, <see cref="DiverTraceApiCodeGen"/> assumes that the encoding will always be <see cref="CodeGen.TargetEncoding"/></remarks>
        public static string MemoryStreamToString(MemoryStream Source)
        {

            if (Source == null)
            {
                throw new ArgumentNullException(nameof(Source));
            }
            Source.Flush();
            var old = Source.Position;
            Source.Position = 0;

            byte[] ret = new byte[Source.Length];
            Source.Read(ret, 0, (int)Source.Length );

            Source.Position = old;
            return TargetEncoding.GetString(ret);
        }

        /// <summary>
        /// return the stuff to paste after a Wide String Stream to get a wchar_t* to the internal buffer
        /// </summary>
        /// <returns>.str().c_str()</returns>
        public static string GetStreamStringToStringPiece()
        {
            return ".str().c_str()";
        }
        /// <summary>
        /// Add quotes if there are no quotes
        /// </summary>
        /// <param name="String">The string to encase in quotes if there are non</param>
        /// <returns>returns "String" encases in quotes if there are none</returns>
         public static string AddQuotesIfNone(string Str)
        {
            if (Str == null)
            {
                return EmptyQuoted;
            }
            else
            {
                if (Str.StartsWith("\"", StringComparison.OrdinalIgnoreCase) == false)
                {
                    if (Str.EndsWith("\"", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        return string.Format( CultureInfo.InvariantCulture, QuotedStringTemplate, Str);
                    }
                }
                return Str;
            }
        }
#endregion
    }
}
