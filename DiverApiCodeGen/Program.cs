using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiverTraceApiCodeGen;
using System.Xml.Serialization;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace DiverConsoleAppCompiler
{
    class Program
    {
        static string SourceFile = "null.xml";
        static string TargetFile = "null.cpp";
        private static DiverXmlDictionary Extractor(MemoryStream testTream)
        {
            XmlSerializer In = new XmlSerializer(typeof(DiverXmlDictionary));
            return (DiverXmlDictionary)In.Deserialize(testTream);
        }
        static void Convert(DiverXmlDictionary x, Stream Out)
        {
            XmlSerializer Output = new XmlSerializer(typeof(DiverXmlDictionary));
            Output.Serialize(Out, x);
        }


        static void Convert(DetourAttachFunction x, Stream Out)
        {
            XmlSerializer Output = new XmlSerializer(typeof(DetourAttachFunction));
            Output.Serialize(Out, x);
        }
        static ConsoleMode Mode = ConsoleMode.GenerateCodeFromXml;
        enum ConsoleMode
        {
            Exit = 0,
            GenerateXmlTemplate = 1,
            GenerateCodeFromXml = 2


        }

        static void Usage()
        {

            Console.WriteLine("DiverApiCodeGen.exe [Flags] Source [Target]");
        }

        static void ExtactArgs(string[] args)
        {
#if DEBUG
            if (args.Length == 0)
            {
                args = new string[2];
                args[0] = "H:\\Users\\Thoma\\Desktop\\compressed.txt";
                //args[1] = "C:\\Users\\Thoma\\source\\repos\\DebugDotNet\\DebugDotNet\\NativeHelperSource\\DebugDotNetDiver\\output.cpp";
                args[1] = "H:\\Users\\Thoma\\Desktop\\output.cpp";
            }
#endif

            if (args.Length < 2)
            {
                Console.WriteLine("Not enough args.");
                Mode = ConsoleMode.Exit;
            }
            else
            {
                SourceFile = args[0];
                TargetFile = args[1];
                Mode = ConsoleMode.GenerateCodeFromXml;
            }
        }


        static int Main(string[] args)
        {
            ExtactArgs(args);
            switch (Mode)
            {
                case ConsoleMode.Exit:
                    return -1;
                case ConsoleMode.GenerateCodeFromXml:
                    {
                        Console.WriteLine("CodeGen Diver Tool");
                        Console.WriteLine("Making DetourCode from " + SourceFile);
                        Console.WriteLine("to Target file " + TargetFile);

                        using (var Source = File.OpenRead(SourceFile))
                        {
                            using (var Target = File.OpenWrite(TargetFile))
                            {
                                Target.SetLength(0);

                                var Detours = new DetoursCodeGen(false);

                                Detours.StandardIncludes.Add("windows.h");
                                Detours.StandardIncludes.Add("iostream");
                                Detours.StandardIncludes.Add("sstream");
                                Detours.ProjectIncludes.Add("stdafx.h");


                                Detours.UsingNameSpaces.Add("std");


                                NativeFunctionClass IsDebuggerPresent = new NativeFunctionClass
                                {
                                    CallingConvention = CodeGen.EmitDeclareFunctionSpecs.WINAPI,
                                    FuncStyle = NativeFunctionClass.EmitStyles.DebugStringArgs | NativeFunctionClass.EmitStyles.DebugStringNameOnly | NativeFunctionClass.EmitStyles.DebugStringNameRet | NativeFunctionClass.EmitStyles.IncludeDiverCode,
                                    FunctionName = "IsDebuggerPresent",
                                    ReturnType = new NativeFunctionArg(),
                                    SourceDll = "Kernel32.dll"
                                   
                                };
                                IsDebuggerPresent.Arguments.Clear();

                                IsDebuggerPresent.ReturnType = new NativeFunctionArg("BOOL", UnmanagedType.Bool);

                                IsDebuggerPresent.HowToGetSourceFunction = NativeFunctionClass.LinkMode.Static;

                                NativeFunctionClass CreateFileA = new NativeFunctionClass
                                {
                           
                                    CallingConvention = CodeGen.EmitDeclareFunctionSpecs.WINAPI,
                                    FuncStyle = NativeFunctionClass.EmitStyles.DebugStringArgs | NativeFunctionClass.EmitStyles.DebugStringNameOnly | NativeFunctionClass.EmitStyles.DebugStringNameRet | NativeFunctionClass.EmitStyles.IncludeDiverCode,
                                    FunctionName = "CreateFileA",
                                    ReturnType = new NativeFunctionArg(),
                                    SourceDll = "Kernel32.dll"
                                };
                                
                                CreateFileA.Arguments.Clear();

                                {
                                    NativeFunctionArg functionArg1 = new NativeFunctionArg();
                                    functionArg1.ArgName = "lpFileName";
                                    functionArg1.ArgType = "LPCSTR";
                                    functionArg1.DebugCodeGenHint = UnmanagedType.LPStr;
                                    CreateFileA.Arguments.Add(functionArg1);

                                    NativeFunctionArg functionArg2 = new NativeFunctionArg();
                                    functionArg2.ArgName = "dwDesiredAccess";
                                    functionArg2.ArgType = "DWORD";
                                    functionArg2.DebugCodeGenHint = UnmanagedType.U4;
                                    CreateFileA.Arguments.Add(functionArg2);

                                    NativeFunctionArg functionArg3 = new NativeFunctionArg();
                                    functionArg3.ArgName = "dwShareMode";
                                    functionArg3.ArgType = "DWORD";
                                    functionArg3.DebugCodeGenHint = UnmanagedType.U4;
                                    CreateFileA.Arguments.Add(functionArg3);

                                    NativeFunctionArg functionArg4 = new NativeFunctionArg();
                                    functionArg4.ArgName = "lpSecurityAttributes";
                                    functionArg4.ArgType = "LPSECURITY_ATTRIBUTES";
                                    functionArg4.DebugCodeGenHint = UnmanagedType.LPStruct;
                                    CreateFileA.Arguments.Add(functionArg4);

                                    NativeFunctionArg functionArg5 = new NativeFunctionArg();
                                    functionArg5.ArgName = "dwCreationDisposition";
                                    functionArg5.ArgType = "DWORD";
                                    functionArg5.DebugCodeGenHint = UnmanagedType.U4;
                                    CreateFileA.Arguments.Add(functionArg5);


                                    NativeFunctionArg functionArg6 = new NativeFunctionArg();
                                    functionArg6.ArgName = "dwFlagsAndAttributes";
                                    functionArg6.ArgType = "DWORD";
                                    functionArg6.DebugCodeGenHint = UnmanagedType.U4;
                                    CreateFileA.Arguments.Add(functionArg6);


                                    NativeFunctionArg functionArg7 = new NativeFunctionArg();
                                    functionArg7.ArgName = "TemplateFile";
                                    functionArg7.ArgType = "HANDLE";
                                    functionArg7.DebugCodeGenHint = UnmanagedType.U4;
                                    CreateFileA.Arguments.Add(functionArg7);


                                }

                                CreateFileA.ReturnType = new NativeFunctionArg("HANDLE", UnmanagedType.U4);

                                Detours.WantPerThreadDetourAttach = false;
                                Detours.FunctionList.Add(IsDebuggerPresent);
                                Detours.FunctionList.Add(CreateFileA);
                                Detours.DllMainCodeGen = new DllMainRoutine();
                                Detours.NeedPerfectAttach = true;
                                Detours.EmitCodeLecacy(Target);

                            }
                            break;
                        }
                    }
                    
            }

            return 0;
        }
    }
}
