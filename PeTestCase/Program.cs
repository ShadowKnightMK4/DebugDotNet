using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebugDotNet;
using DebugDotNet.Win32.Debugger;
using System.Diagnostics;
namespace PeTestCase
{
    class Program
    {
        static bool Close = false;
        static void Main(string[] args)
        {
            Process T = new Process
            {
                StartInfo = new ProcessStartInfo("C:\\Windows\\system32\\notepad.exe")
            };

            EventDebugger Test = new EventDebugger(T, DebugDotNet.Win32.Enums.DebuggerCreationSettings.RunProgramThenAttach);
            Test.OutputDebugStringEvent += Test_OutputDebugStringEvent;
            Test.LoadDllDebugEvent += Test_LoadDllDebugEvent;
            Test.CreateProcessEvent += Test_CreateProcessEvent;
            Test.ExceptionDebugEvent += Test_ExceptionDebugEvent;
            Test.EndDebugProcessOnQuit = false;
            Test.MonitorOnly = true;
            Test.Start();
            while (!Close)
            {
                continue;
            }
            Test.Dispose();
        }

        private static void Test_ExceptionDebugEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Response = DebugDotNet.Win32.Enums.ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
        }

        private static void Test_CreateProcessEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine(EventData.CreateProcessInfo.lpImageName);
        }

        private static void Test_LoadDllDebugEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine(EventData.LoadDllInfo.lpImageName);
        }

        private static void Test_OutputDebugStringEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine(EventData.DebugStringInfo.lpDebugStringData);
        }


    }
}
