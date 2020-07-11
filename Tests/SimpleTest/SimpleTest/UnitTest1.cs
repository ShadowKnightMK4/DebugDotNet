using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DebugDotNet;
using DebugEventDotNet.Root;

namespace SimpleTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestDebugWorldView()
        {
            Console.WriteLine("Creating DebugWorldView to debug notepad.exe");
            DebugWorldView TestRun = new DebugWorldView("C:\\WINDOWS\\system32\\notepad.exe", DebugEventWorkerThread.CreationSetting.RunProgramThenAttach );

            Console.WriteLine("Starting process");
            TestRun.Start();

            while (TestRun.IsDisposed == false)
            {
                
            }
        }
    }
}
