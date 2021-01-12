using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace DiverTraceApiCodeGen.NewVersion
{
    /// <summary>
    /// The abstract class the NeoFunction Classs build on.
    /// </summary>
    public abstract class NeoFunctionPiece
    {
        /// <summary>
        /// Becomes the name of the routine that will be Generated
        /// </summary>
        public virtual string RoutineName { get; set; }

        /// <summary>
        /// Generate this classe's function and return it in string form
        /// </summary>
        /// <returns>Return a string containing the C/C++ representation of this class's output function</returns>
        public abstract string GenerateFunction();

        
        /// <summary>
        /// In the detoured function, emit a call to tell the debugger a resource was just opened. 
        /// </summary>
        /// <param name="Target">emit to this stream using <see cref="CodeGen.TargetEncoding"/></param>
        /// <param name="VariableName">name of the variable that's a win32 handle.</param>
        /// <param name="ResourceType">Describes the resource</param>
        public static void EmitRaiseExceptionNotifyResourceCall(Stream Target, string VariableName, NeoNativeTypeData  ResourceType, bool ForceEmit=false)
        {
            if (ForceEmit)
            {
                CodeGen.EmitCallFunction(Target, "RaiseExceptionNotifyResource", null, new List<string>() { VariableName, ((int)ResourceType).ToString(CultureInfo.InvariantCulture) });
            }
        }

        /// <summary>
        /// Emits a call to the DiverFunction 'RaiseExceptionSpecialDebugMessage' which can be used to send 'filtered' strings to the debugger if it understands it
        /// </summary>
        /// <param name="Target">emit the string to <see cref="CodeGen.TargetEncoding"/></param>
        /// <param name="MessageArgument">this is the unicode string or C/C++ wchar_t* string to emit. </param>
        /// <param name="FallBackToDebugString">if specified and the debugger does replay, we readmit the string as call to OutputDebugStringW</param>
        /// <param name="Channel">The specific channel to use. With the exception of '0' which means use OutputDebugStringW instread of the normal rougine</param>
        /// <param name="AddQuotes">quote the MessageArgument if there are no -quotes. use FALSE if the MessageArgument is a variable rathar than a literal</param>
        public static string EmitCallDiverOutputDebugMessage(string MessageArgument, bool FallBackToDebugString, int Channel, bool AddQuotes=false)
        {
            if (AddQuotes)
            {
                MessageArgument = CodeGen.AddQuotesIfNone(MessageArgument);
            }

            using (MemoryStream Ret = new MemoryStream())
            {
                CodeGen.EmitCallFunction(Ret, "RaiseExceptionSpecialDebugMessage", null, new List<string>() { MessageArgument, Channel.ToString(CultureInfo.InvariantCulture), CodeGen.BoolToCPP(FallBackToDebugString) });
                return CodeGen.MemoryStreamToString(Ret);
            }
        }


        const string EmitDiverTrackFuncCallTemplate = "BOOL DiverResult = RaiseExceptionTrackFunc ( L\"{0}\", {1}, {2}, {3}, {4}, {5} )";
        /*
         * 	BOOL WINAPI RaiseExceptionTrackFunc(const wchar_t* FuncName,
		vector<ULONG_PTR>& ArgPtrs,
		vector<ULONG_PTR>& TypeHint,
		ULONG_PTR* RetVal,
		ULONG_PTR* RetHint,
		DEBUGGER_RESPONSE* Debugger)
         */
        /// <summary>
        /// Emit a call to the Diver function RaiseExceptionTrackFunc
        /// <param name="Output">Emit to this thing using <see cref="CodeGen.TargetEncoding"/></param>
        /// <param name="FuncName">name of the function we are reported in the call. For Example "IsDebuggerPresent" </param>
        /// <param name="VectorArgValues">the name of the vector argument that will become the argument containing pointers to the arguments</param>
        /// <param name="VectorArgHints">the name of the vector argument that will contain type information for the arguments</param>
        /// <param name="DebuggerResponseVarName">name of the debugger response structure value</param>
        /// <remarks> The routine that is called in C/C++ generated code has this prototype: 
        /// 
        /// BOOL WINAPI RaiseExceptionTrackFunc(const wchar_t* FuncName,
		/// vector(ULONG_PTR)& ArgPtrs,
		/// vector(ULONG_PTR)& TypeHint,
		/// ULONG_PTR* RetVal,
        /// ULONG_PTR* RetHint,
		/// DEBUGGER_RESPONSE* Debugger)
        /// </remarks>
        /// </summary>
  
        public static void EmitCallDiverTrackFunc(Stream Output, string FuncName, string VectorArgValues, string VectorArgHints, string DebuggerResponseVarName, bool DeclareDiverVariable)
        {
            CodeGen.WriteCommentBlock(Output, "Calling diver tracker function for " + FuncName);
            if (DeclareDiverVariable)
            {
                CodeGen.EmitDeclareVariable(Output, "BOOL", "DiverResult", "FALSE");
                CodeGen.WriteFormated(Output, EmitDiverTrackFuncCallTemplate, FuncName, VectorArgValues, VectorArgHints, "0", "0", DebuggerResponseVarName);
            }
            else
            {
                CodeGen.EmitCallFunction(Output, "RaiseExceptionTrackFunc", null, new List<string>() {FuncName, VectorArgValues, VectorArgHints, "0", "0", DebuggerResponseVarName }, "DiverResult");
            }
        }

        /// <summary>
        /// Calls the <see cref="GenerateFunction"/> and writes it to the target stream using the passed format specified in target. This will typically be <see cref="CodeGen.TargetEncoding"/>/>
        /// </summary>
        /// <param name="Target">Write to this stream</param>
        /// <param name="Format">Using this encoding. Passing null means use <see cref="CodeGen.TargetEncoding"/></param>
        public void EmitFunctionToStream(Stream Target, Encoding Format)
        {
            byte[] ret;
            if (Format == null)
            {
                Format = CodeGen.TargetEncoding;
            }

            if (Target == null)
                throw new ArgumentNullException(nameof(Target));
            if (Format == null)
                throw new ArgumentNullException(nameof(Format));
            else
                ret= Format.GetBytes(GenerateFunction());

            Target.Write(ret, 0, ret.Length);

        }
    }
}
