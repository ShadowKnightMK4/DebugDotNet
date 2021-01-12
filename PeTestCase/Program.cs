using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebugDotNet;
using DebugDotNet.Win32.Debugger;
using System.Diagnostics;
using DebugDotNet.Win32.Structs;
using DebugDotNet.Win32.Enums;
using DebugDotNet.Win32.Symbols;
using DebugDotNet.Win32.Threads;
namespace PeTestCase
{
    class Program
    {
        static bool Close = false;
        static bool FirstBreakPointHit = false;
        static EventDebugger Test;
        static List<ExceptionCode> Trigged = new List<ExceptionCode>();
        static void Main(string[] args)
        {
            Process T = new Process
            {
                //StartInfo = new ProcessStartInfo("C:\\Windows\\system32\\notepad.exe")
                StartInfo = new ProcessStartInfo("C:\\Users\\Thoma\\source\\repos\\DebugDotNet\\DebugDotNet\\Debug\\TestFollowCreateFile.exe")
                //StartInfo = new ProcessStartInfo("C:\\WINDOWS\\system32\\cmd.exe")
            //    StartInfo = new ProcessStartInfo("C:\\Users\\Thoma\\source\\repos\\DebugDotNet\\DebugDotNet\\DebugTestApps\\Debug\\TestCaseNullReference.exe")
            };


            
            Test = new EventDebugger(T, DebugDotNet.Win32.Enums.DebuggerCreationSetting.CreateWithDebug);

            Test.OutputDebugStringEvent += Test_OutputDebugStringEvent;
            Test.LoadDllDebugEvent += Test_LoadDllDebugEvent;
            Test.CreateProcessEvent += Test_CreateProcessEvent;
            Test.ExceptionDebugEvent += Test_ExceptionDebugEvent;
            Test.ExitProcessEvent += Test_ExitProcessEvent;
            Test.EndDebugProcessOnQuit = false;
            Test.MonitorOnly = false;
            Test.DebugSpawnedProceses = true;
            Test.AddForceLoadDllRange(new List<string> { "C:\\Users\\Thoma\\source\\repos\\DebugDotNet\\DebugDotNet\\Debug\\TestDllLoad.dll" });

            Test.TrackingModules = true;


            Test.Start();
     
       
            Test.Dispose();
        }



        private static void Test_ExitProcessEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine("Process " + EventData.dwProcessId + "exited with value of " + EventData.ExitProcessInfo.ExitCode);
            
            foreach (ExceptionCode Code in Trigged)
            {
                Console.WriteLine(Code.ToString());
            }
            


        }

        private static void Test_ExceptionDebugEvent(ref DebugEvent EventData, ref ContinueStatus Response)
        {

            var ExceptionData = EventData.ExceptionInfo;
            Trigged.Add(ExceptionData.TopLevelException.ExceptionCode);
            Console.WriteLine(ExceptionData.TopLevelException.ExceptionCode);
            Response = ContinueStatus.DBG_CONTINUE;
            switch (ExceptionData.TopLevelException.ExceptionCode)
            {
                case ExceptionCode.ExceptionIntDivideByZero:
                    Console.WriteLine("Dividision by zero!");
                    break;
                case ExceptionCode.ExceptionBreakpoint:
                    {
                        if (!FirstBreakPointHit)
                        {
                            FirstBreakPointHit = true;
                            Response = ContinueStatus.DBG_CONTINUE;
                        }
                        else
                        {
                            Response = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                        }
                        break;
                    }
                default:
                    Response = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                    break;
            }


          
          
            
        }

        private static void Test_CreateProcessEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine(EventData.CreateProcessInfo.ImageName);
            using (DebugHelpLibrary Test = new DebugHelpLibrary(EventData.CreateProcessInfo.ProcessHandleRaw, null, false))
            {
                Console.WriteLine("Test code for dbg help called");
            }

            Console.WriteLine("Enumerating Threads");
            
            Console.WriteLine("Test code cleanup is ok");
        }

        private static void Test_LoadDllDebugEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine(EventData.LoadDllInfo.ImageName);
            
            
        }

        private static void Test_OutputDebugStringEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine(EventData.DebugStringInfo.DebugStringData);
            var result = DebugDotNetThreads.GetProcessThreads(Process.GetProcessById(EventData.dwProcessId));
        }


    }
}
