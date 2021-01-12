using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DiverTraceApiCodeGen.NewVersion
{

    public class NewLoadLibraryCommon : NeoLoadLibraryExW
    {
        protected NeoNativeFunction IntLoadLibraryExW { get; set; }
        /// <summary>
        /// Tell this class where to get the LoadLibraryEx() routine. The <see cref="GenerateFunction"/> refers to this when making the code call
        /// </summary>
        /// <param name="LoadLibraryExW"></param>
        public void SetLoadLibraryEx(NeoNativeFunction LoadLibraryExW)
        {
            IntLoadLibraryExW = LoadLibraryExW;
        }
        public NewLoadLibraryCommon(): base()
        {
            
            Arguments.Insert(0, new NeoNativeFunctionArg("int", "CallerStringType", NeoNativeTypeData.I4));
        }
        
        /// <summary>
        /// Emit the Common Branch for The Dynamic LoadLibraryXXX() routines which branches back to LoadLibraryExW() version that takes an ID which will specify the string to pass to the diver dll
        /// </summary>
        /// <returns></returns>
        public override string GenerateFunction()
        {
            using (MemoryStream Fn = new MemoryStream())
            {
                CodeGen.WriteComment(Fn, "This routine is the common branch code for the LoadLibraryXXXX() modes we Detour. The CallerStringType defines the string we pass to the DiverFuncCall");

                CodeGen.EmitDeclareFunction(Fn, "HMODULE", CodeGen.EmitDeclareFunctionSpecs.WINAPI, RoutineName, ExtractArgumentTypes(Arguments), ExtractArgumentNames(Arguments));
                CodeGen.WriteNewLine(Fn);
                CodeGen.WriteLeftBracket(Fn, true);
                CodeGen.EmitDeclareVariable(Fn, "wchar_t*", "Caller", "0");
                CodeGen.WriteSwitch(Fn, "CallerStringType");
                CodeGen.WriteLeftBracket(Fn, true);
                CodeGen.WriteCase(Fn, "6");
                CodeGen.WriteLeftBracket(Fn, true);
                    CodeGen.WriteComment(Fn, "This case is for LoadLibraryA");
                    CodeGen.EmitAssignVariable(Fn, "Caller", "L\"LoadLibraryA\"");
                    CodeGen.WriteBreak(Fn);
                CodeGen.WriteRightBracket(Fn, true);

                CodeGen.WriteCase(Fn, "4");
                CodeGen.WriteLeftBracket(Fn, true);
                    CodeGen.WriteComment(Fn, "This case is for LoadLibraryW");
                    CodeGen.EmitAssignVariable(Fn, "Caller", "L\"LoadLibraryW\"");
                    CodeGen.WriteBreak(Fn);
                CodeGen.WriteRightBracket(Fn, true);

                CodeGen.WriteCase(Fn, "1");
                CodeGen.WriteLeftBracket(Fn, true);
                    CodeGen.WriteComment(Fn, "This case is for LoadLibraryExA");
                    CodeGen.EmitAssignVariable(Fn, "Caller", "\"LoadLibraryExA\"");
                    CodeGen.WriteBreak(Fn);
                CodeGen.WriteRightBracket(Fn, true);


                CodeGen.WriteCase(Fn, "2");
                CodeGen.WriteLeftBracket(Fn, true);
                    CodeGen.WriteComment(Fn, "This case is for LoadLibraryExW");
                    CodeGen.EmitAssignVariable(Fn, "Caller", "\"LoadLibraryExW\"");
                    CodeGen.WriteBreak(Fn);
                CodeGen.WriteRightBracket(Fn, true);
                CodeGen.WriteLiteral(Fn, "default:");
                CodeGen.WriteLeftBracket(Fn, true);
                    CodeGen.WriteComment(Fn, "This case is the default should someone call with unexpcted value");
                    CodeGen.EmitAssignVariable(Fn, "Caller", "\"DiverLoadLibraryCommonPath\"");
                    CodeGen.WriteBreak(Fn);
                CodeGen.WriteRightBracket(Fn, true);





                CodeGen.WriteRightBracket(Fn, true);


                if (this.JustCallIt)
                {
                    CodeGen.EmitDeclareVariable(Fn, ReturnValue.ArgType, "result");
                    CodeGen.EmitCallFunction(Fn, IntLoadLibraryExW.OriginalFunctionNamePtr, null, ExtractArgumentTypes(IntLoadLibraryExW.Arguments), "result");
                    CodeGen.EmitReturnX(Fn,"result");
                }
                throw new NotImplementedException();
                //TODO Work on this part of the commonLoadlIbraryCode
                CodeGen.WriteRightBracket(Fn, true);
                return CodeGen.MemoryStreamToString(Fn);
            }
        }
    }




    public class NeoLoadLibraryExW : NeoNativeFunction
    {
        public NeoLoadLibraryExW()
        {
            OriginalFunctionName = "LoadLibraryExW";

            Arguments.Add(new NeoNativeFunctionArg("LPCWSTR", "lpLibFileName", NeoNativeTypeData.LPWStr));
            Arguments.Add(new NeoNativeFunctionArg("HANDLE", "hFile", NeoNativeTypeData.U4));
            Arguments.Add(new NeoNativeFunctionArg("DWORD", "dwFlags", NeoNativeTypeData.LPWStr));

            ReturnValue = new NeoNativeFunctionArg("HMODULE", NeoNativeTypeData.U4);

            LinkMode = FunctionType.StaticLink;
            DetourMode = DetourFunctionMode.LoadLibraryExW;

        }

    }
        public class NeoLoadLibraryExA: NeoNativeFunction
    {
        public NeoLoadLibraryExA()
        {
            OriginalFunctionName = "LoadLibraryExA";

            Arguments.Add(new NeoNativeFunctionArg("LPCSTR", "lpLibFileName", NeoNativeTypeData.LPWStr));
            Arguments.Add(new NeoNativeFunctionArg("HANDLE", "hFile", NeoNativeTypeData.U4));
            Arguments.Add(new NeoNativeFunctionArg("DWORD", "dwFlags", NeoNativeTypeData.LPWStr));

            ReturnValue = new NeoNativeFunctionArg("HMODULE", NeoNativeTypeData.U4);

            LinkMode = FunctionType.StaticLink;
            DetourMode = DetourFunctionMode.LoadLibraryExA;

        }
    }

    public class NeoLoadLibraryA: NeoNativeFunction
    {
        public NeoLoadLibraryA()
        {
            OriginalFunctionName = "LoadLibraryA";
            
            Arguments.Add(new NeoNativeFunctionArg("LPCSTR", "lpLibFileName" , NeoNativeTypeData.LPStr));
            ReturnValue = new NeoNativeFunctionArg("HMODULE", NeoNativeTypeData.U4);

            LinkMode = FunctionType.StaticLink;
            DetourMode = DetourFunctionMode.LoadLibraryA;
        }

    }


    public class NeoLoadLibraryW : NeoNativeFunction
    {
        public NeoLoadLibraryW()
        {
            OriginalFunctionName = "LoadLibraryW";

            Arguments.Add(new NeoNativeFunctionArg("LPCWSTR", "lpLibFileName", NeoNativeTypeData.LPWStr));
            ReturnValue = new NeoNativeFunctionArg("HMODULE", NeoNativeTypeData.U4);

            LinkMode = FunctionType.StaticLink;
            DetourMode = DetourFunctionMode.LoadLibraryW;
        }

    }
}
