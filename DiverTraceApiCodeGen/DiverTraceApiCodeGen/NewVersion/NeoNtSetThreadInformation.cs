using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiverTraceApiCodeGen.NewVersion
{
    /// <summary>
    /// A specialized version for NTDLL.DLL's NtSetThreadInformation. Fixed arguments, name and return value subject to MSDN's documentation of NtSetThreadInformation
    /// </summary>
    public class NeoNtSetThreadInformation : NeoNativeFunction
    {
        /// <summary>
        /// If set we generate our NT related stuff to be defined with the function.
        /// </summary>
        public bool GenerateSingleNtStuff { get; set; } = true;

        /// <summary>
        /// If set we test for the case of a valid Hide thread from debugger. If so we strip the flag call and pass it to the original
        /// </summary>
        public bool StripHideFromDebugger { get; set; } = true;
        public NeoNtSetThreadInformation(): base()
        {
            OriginalFunctionName = "NtSetThreadInformation";


            ReturnValue = new NeoNativeFunctionArg("DWORD", NeoNativeTypeData.U4) ;
            

            NeoNativeFunctionArg ThreadHandle = new NeoNativeFunctionArg();
            NeoNativeFunctionArg ThreadInformationClass = new NeoNativeFunctionArg();
            NeoNativeFunctionArg ThreadInformation = new NeoNativeFunctionArg();
            NeoNativeFunctionArg ThreadInformationLength = new NeoNativeFunctionArg();



            ThreadHandle.ArgName = "ThreadHandle";
            ThreadHandle.ArgType = "HANDLE";
            ThreadHandle.DebugCodeGenHint = NeoNativeTypeData.ContextThreadHandle | NeoNativeTypeData.U4;

            ThreadInformationClass.ArgName = "ThreadInformationClass";
            ThreadInformationClass.ArgType = "THREADINFOCLASS";
            ThreadHandle.DebugCodeGenHint = NeoNativeTypeData.U4;



            ThreadInformation.ArgName = "ThreadInformation";
            ThreadInformation.ArgType = "PVOID";
            ThreadInformation.DebugCodeGenHint = NeoNativeTypeData.IsPtr1 | NeoNativeTypeData.U4 ;


            ThreadInformationLength.ArgName = "ThreadInfomationLength";
            ThreadInformationLength.ArgType = "ULONG";
            ThreadInformationLength.DebugCodeGenHint = NeoNativeTypeData.U4;

            Arguments.Add(ThreadHandle);
            Arguments.Add(ThreadInformationClass);
            Arguments.Add(ThreadInformation);
            Arguments.Add(ThreadInformationLength);
        }


        /// <summary>
        /// Generate the attach fuction and the detoured function.
        /// </summary>
        /// <returns></returns>
        public override string GenerateFunction()
        {
            if (StripHideFromDebugger == false)
            {
                return base.GenerateFunction();
            }
            else
            {
                SetSpecialistMode(SpecialistMode.NtSetInformationThread);
                string ret = base.GenerateFunction();
                SetSpecialistMode(SpecialistMode.Normal);
                return ret;
            }
        }

        public override string GenerateDetourFunction()
        {
            throw new NotImplementedException();
            if (StripHideFromDebugger == false)
            {
                return base.GenerateDetourFunction();
            }
            else
            {
                SetSpecialistMode(SpecialistMode.NtSetInformationThread);
                string ret = base.GenerateDetourFunction();
                SetSpecialistMode(SpecialistMode.Normal);
                return ret;
            }
        }

    }
}
