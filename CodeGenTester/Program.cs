using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiverTraceApiCodeGen;
using DiverTraceApiCodeGen.NewVersion;

using DiverApiCodeGen;

namespace CodeGenTester
{
    class Program
    {
        static void Main(string[] args)
        {
            NeoDllMainFunction Test = new NeoDllMainFunction();

            
            NeoNativeFunction IsDebuggerPresent = new NeoNativeFunction();
            NeoNativeFunction CreateFileA = new NeoNativeFunction();
            NeoNtSetThreadInformation NtSetThreadInformation = new NeoNtSetThreadInformation();

            NtSetThreadInformation.StripHideFromDebugger = true;

            Test.StandardInclude.Add("windows.h");
            Test.StandardInclude.Add("iostream");
            Test.StandardInclude.Add("sstream");
            Test.ProjectIncludes.Add("stdafx.h");


            Test.NameSpaceList.Add("std");
            /*
             * HANDLE CreateFileA(
  LPCSTR                lpFileName,
  DWORD                 dwDesiredAccess,
  DWORD                 dwShareMode,
  LPSECURITY_ATTRIBUTES lpSecurityAttributes,
  DWORD                 dwCreationDisposition,
  DWORD                 dwFlagsAndAttributes,
  HANDLE                hTemplateFile
);
             */
            
            CreateFileA.ReturnValue = new NeoNativeFunctionArg("HANDLE",NeoNativeTypeData.U4 | NeoNativeTypeData.ContextFileHandle | NeoNativeTypeData.IsResource);
            CreateFileA.OriginalFunctionName = "CreateFileA";
            CreateFileA.Arguments.Add(new NeoNativeFunctionArg("LPCSTR", "lpFileName", NeoNativeTypeData.LPStr));
            CreateFileA.Arguments.Add(new NeoNativeFunctionArg("DWORD", "dwDesiredAccess", NeoNativeTypeData.U4));
            CreateFileA.Arguments.Add(new NeoNativeFunctionArg("DWORD", "dwShareMode", NeoNativeTypeData.U4));
            CreateFileA.Arguments.Add(new NeoNativeFunctionArg("LPSECURITY_ATTRIBUTES", "lpSecurityAttributes", NeoNativeTypeData.LPStruct));
            CreateFileA.Arguments.Add(new NeoNativeFunctionArg("DWORD", "dwCreationDisposition", NeoNativeTypeData.U4));
            CreateFileA.Arguments.Add(new NeoNativeFunctionArg("DWORD", "dwCreationDisposition", NeoNativeTypeData.U4));
            CreateFileA.Arguments.Add(new NeoNativeFunctionArg("HANDLE", "hTemplateFile", NeoNativeTypeData.U4));

            CreateFileA.LinkMode = NeoNativeFunction.FunctionType.StaticLink;

            IsDebuggerPresent.OriginalFunctionName = "IsDebuggerPresent";
            IsDebuggerPresent.ReturnValue = new NeoNativeFunctionArg("BOOL", NeoNativeTypeData.Bool);

            //Test.DetourThese.Add(IsDebuggerPresent);
            //Test.DetourThese.Add(CreateFileA);
            Test.DetourThese.Add(NtSetThreadInformation);
            
            using (var Fn = System.IO.File.OpenWrite("H:\\Users\\Thoma\\Desktop\\output.cpp"))
            {
                string ret = Test.GenerateDiverSource();

                byte[] Data = Encoding.ASCII.GetBytes(ret);

                Fn.Write(Data, 0, Data.Length );
            }
            
            Console.ReadLine();
        }
    }
}



