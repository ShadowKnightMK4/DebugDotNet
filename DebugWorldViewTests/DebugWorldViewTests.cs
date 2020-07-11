using Microsoft.VisualStudio.TestTools.UnitTesting;
using DebugDotNet.Win32.Debugger;
using System.IO;
using System;
using System.Diagnostics;
using DebugDotNet.Win32.Enums;
namespace DebugWorldViewTests
{
    [TestClass]
    public class DebugWorkerThreadTestCases
    {
        [TestMethod]
        public void DoNothing()
        {
        }


        bool OkToQuit = false;
        /// 
        /// <summary>
        /// Launch notepad from the windows directory and watch events in the console 
        /// </summary>


        [TestMethod]
        public void DebugNotepadWithOutput()
        {
            string TargetName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe");
            System.Diagnostics.Process Target = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo(TargetName)
            };
            DebugDotNet.Win32DebugApi.DebugPriv = true;
            EventDebugger TestCase = new EventDebugger(Target, DebugDotNet.Win32.Enums.DebuggerCreationSettings.RunProgramThenAttach);
            TestCase.OutputDebugStringEvent += TestCase_OutputDebugStringEvent;
            TestCase.CreateProcessEvent += TestCase_CreateProcessEvent;
            TestCase.ExitProcessEvent += TestCase_ExitProcessEvent;
            
            TestCase.Start();
            while (!OkToQuit)
            {
                TestCase.DispatchEvents();
            }

        }

        private void TestCase_ExitProcessEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref ContinueStatus Response)
        {
            Debug.Write(string.Format("Process ID {0} has exited with a return code of {1}. ", EventData.dwProcessId, EventData.ExitProcessInfo));
        }

        private void TestCase_CreateProcessEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref ContinueStatus Response)
        {
            Debug.Write(string.Format("Process ID {0} created from PE file at {1}.", EventData.dwProcessId, EventData.CreateProcessInfo.lpImageName));
        }

        private void TestCase_OutputDebugStringEvent(ref DebugDotNet.Win32.Structs.DebugEvent EventData, ref ContinueStatus Response)
        {
            Debug.Write(string.Format("Process ID {0} reports this => {1}.", EventData.dwProcessId, EventData.DebugStringInfo.lpDebugStringData));
            OkToQuit = true;
            
        }
    }
}
