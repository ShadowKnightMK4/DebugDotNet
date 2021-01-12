Diver is the Dll that gets injected into a target process to provide feedback to a debugger.

Ideal final build processs is to launch the DiverApiCodeGen.exe app pointing the an xml file that describes each routine to 
include in the build and then finish with a normal Visual Studio Compile with build step.


Currently, It's all hardcoded as more of a proof of concept. It Defaults to Detouring CreateFileA 
and IsDebuggerPresent