# DebugDotNet
Bringing some Win32 DEBUG_EVENT handling to c#


This in intended to be a stand alone version of something I toy with from time to time.  Started with looking at the pinvoke for various parts of the Windows Debugger Routines and structs on pinvoke.net and that included some example code on how to read the DebugEvent structure and introduced me to the world of Marshaling from Windows Native back to .NET.  The stuff added is intended to make looking at Debug Events a little more C# friendly than a straight 1 to 1 conversion of the DEBUG_EVENT struct parts the example provided. 


The class named DebuggerWorkerThread is the general implementation that that does the message pump of WaitForDebugEvent() and ContinueDebugEvent() on MSDN. EventDebugger offers c# event subscribed to be updated when you want. 


See quickstart.txt under DebugDotNet to a small read in using this library.
http://www.pinvoke.net/default.aspx/Structures/DEBUG_EVENT.html was the starting point I had for the main DebugEvent class.



