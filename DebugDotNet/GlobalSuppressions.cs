// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:DebugDotNet.Win32.Structs.CREATE_THREAD_DEBUG_INFO.lpThreadLocalBase")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "<Pending>", Scope = "member", Target = "~F:DebugDotNet.Win32.Structs.DEBUG_EVENT.dwProcessId")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Specifying a length in unmarshalled caused garbage readss. I want to add that back as freature and not need to worry about changing public api", Scope = "member", Target = "~M:DebugDotNet.Win32.Tools.UnmangedToolKit.ExtractLocalString(System.IntPtr,System.Int32,System.Boolean)~System.String")]
