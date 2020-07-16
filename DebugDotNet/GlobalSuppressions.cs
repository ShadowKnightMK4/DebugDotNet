// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:DebugDotNet.Win32.Structs.CREATE_THREAD_DEBUG_INFO.lpThreadLocalBase")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:DebugDotNet.Win32.Structs.DEBUG_EVENT.dwProcessId")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Specifying a length in unmarshalled caused garbage readss. I want to add that back as freature and not need to worry about changing public api", Scope = "member", Target = "~M:DebugDotNet.Win32.Tools.UnmangedToolKit.ExtractLocalString(System.IntPtr,System.Int32,System.Boolean)~System.String")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "", Scope = "type", Target = "~T:DebugDotNet.Win32.Enums.ContinueStatus")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Contains Flags that can't be expressed in Int32 but can be in uint", Scope = "type", Target = "~T:DebugDotNet.Win32.Enums.ExceptionCode")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Personal Preference. That's also why the duplicat enum entries are there", Scope = "type", Target = "~T:DebugDotNet.Win32.Enums.ContinueStatus")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "<Pending>", Scope = "type", Target = "~T:DebugDotNet.Win32.Enums.ContinueStatus")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Public field I need for marshaling to c#", Scope = "member", Target = "~F:DebugDotNet.Win32.Structs.DebugEvent.dwProcessId")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Public field I need for marshaling to c#", Scope = "member", Target = "~F:DebugDotNet.Win32.Structs.DebugEvent.dwThreadId")]
