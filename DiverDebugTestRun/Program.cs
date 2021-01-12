using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebugDotNet.Win32.Debugger;
using DebugDotNet.Win32.Tools;
using DebugDotNet.Win32.Diver;
using DebugDotNet.Win32.Enums;

namespace DiverDebugTestRun
{
    class Program
    {
        const uint FuncCallException =  0xFFFFFFFD;
        const  uint FuncCallDiverCheck = 0xFFFFFFFE;
        static void Main(string[] args)
        {
         const string DiverLoc = "C:\\Users\\Thoma\\source\\repos\\DebugDotNet\\DebugDotNet\\bin\\Debug\\netstandard2.0\\DebugDotNetDiver.Dll";
         
            Process Target = new Process();
            Target.StartInfo = new ProcessStartInfo();
            Target.StartInfo.FileName = "C:\\Windows\\system32\\notepad.exe";
            //Target.StartInfo.FileName = "C:\\HOSP\\WINMAIN.EXE";
            Console.WriteLine("Loading and running Notepad.exe at System32\\notepad.exe with diver dll");
            EventDebugger Tester = new EventDebugger(Target, DebugDotNet.Win32.Enums.DebuggerCreationSetting.CreateWithDebug);

            Tester.AddForceLoadDllRange( new List<string>() { DiverLoc });

            Tester.CreateProcessEvent += Tester_CreateProcessEvent;
            Tester.ExceptionDebugEvent += Tester_ExceptionDebugEvent;
              Tester.OutputDebugStringEvent += Tester_OutputDebugStringEvent;
            Tester.LoadDllDebugEvent += Tester_LoadDllDebugEvent;
            Tester.Start();
        }

        private static void Tester_LoadDllDebugEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            if (EventData.LoadDllInfo.ImageName.Contains("diver"))
            {
                Console.WriteLine("Hit Diver dll");
            }
        }

        private static void Tester_OutputDebugStringEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            Console.WriteLine(EventData.DebugStringInfo.DebugStringData);
        }

        private static void Tester_ExceptionDebugEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            var Info = EventData.ExceptionInfo;
            if (EventData.IsDiverException())
            {
                switch (Info.TopLevelException.ExceptionCode)
                {
                    case (ExceptionCode) DiverExceptionList.DiverComSetVersion:
                        {
                            Console.WriteLine("Diver Protocol requested. Version number \"" + EventData.GetDiverVersionInfo() + "\"");
                            EventData.SetDiverHandledValue(true);
                            break;
                        }
                    case (ExceptionCode)DiverExceptionList.DiverComTrackFuncCall:
                        {
                            string FuncNamel = UnmangedToolKit.ExtractString(EventData.dwProcessId,
                                                                                                 new IntPtr(Info.TopLevelException.ExceptionInformation[Diver.DiverMessageTrackFuncSourceName]),
                                                                                                 new IntPtr(Info.TopLevelException.ExceptionInformation[Diver.TrackFuncSourceNameSize]),
                                                                                                 true);

                            Console.WriteLine("Diver Function Call..... Setting up Args");
                            Console.WriteLine("The function is " + FuncNamel);
                            if (FuncNamel.Equals("IsDebuggerPresent"))
                            {
                                Console.WriteLine("We are telling the IsDebuggerPresent() Function to report no debuggers here.");
                                DiverDebugResponse DiverResponse = (DiverDebugResponse)EventData.GetDiverDebugResponse();
                                DiverResponse.Arg1 = 0;
                                DiverResponse.DebuggerSeenThis = true;
                                DiverResponse.ResponseFlags = DebuggerResponseFlags.ForceReturn;
                                EventData.UpdateDiverDebugResponseStruct(DiverResponse);
                            }
                            EventData.SetDiverHandledValue(true);
                            Console.WriteLine("....Returning control the debugged process");


                            break;
                        }
                }


            }
            else
            {
                switch (Info.TopLevelException.ExceptionCode)
                {
                    case DebugDotNet.Win32.Enums.ExceptionCode.ExceptionBreakpoint:
                        Response = DebugDotNet.Win32.Enums.ContinueStatus.DBG_CONTINUE;
                        break;
                    default:
                        Response = DebugDotNet.Win32.Enums.ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;
                        break;
                }
            }
        }

        private static void Tester_CreateProcessEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref DebugDotNet.Win32.Enums.ContinueStatus Response)
        {
            var Info = EventData.CreateProcessInfo;
            Console.WriteLine(Info.ImageName + " Started as Process #" + EventData.dwProcessId);
            

        }

        static Dictionary<int, IntPtr> XRef = new Dictionary<int, IntPtr>();




    }
}
