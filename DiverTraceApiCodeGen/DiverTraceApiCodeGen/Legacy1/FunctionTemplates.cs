using Microsoft.SqlServer.Server;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DiverTraceApiCodeGen
{

    
    public class DynamicDetourName: FunctionPiece
    {
        /// <summary>
        /// list of functions that will be attached should a LoadLibary() call load a dll matching their name and they have a get proc for that routine
        /// </summary>
        public List<NativeFunctionClass> OnDemandLinks = new List<NativeFunctionClass>();
        protected override string GetBuild()
        {
            using (var Output = new MemoryStream())
            {
                List<string> RoutineNamesToCall = new List<string>(); 
                for (int step = 0; step < OnDemandLinks.Count; step++)
                {
                    DetourAttachFunction Specialize = OnDemandLinks[step].GenerateAttachFunction();
                    Specialize.TemplateArgs[DetourAttachFunction.DetourSourceDllName] = OnDemandLinks[step].SourceDll;
                    CodeGen.WriteLiteralNoIndent(Output, Specialize.GetFinalBuild());
                    RoutineNamesToCall.Add(Specialize[RoutineNameKey]);

                    OnDemandLinks[step].EmitDetourFunction(Output, Specialize[RoutineNameKey]);
                }
                    CodeGen.EmitDeclareFunction(Output, null, CodeGen.EmitDeclareFunctionSpecs.WINAPI, "DemandLinkDetour", null, null);
                CodeGen.WriteNewLine(Output);
                CodeGen.WriteLeftBracket(Output, true);
                foreach (string Routine in RoutineNamesToCall)
                {
                    CodeGen.EmitCallFunction(Output,Routine, null, null);
                }
                    
                CodeGen.WriteRightBracket(Output, true);
                Output.Position = 0;
                byte[] ret = new byte[Output.Length];
                Output.Read(ret, 0, (int)Output.Length);
                return CodeGen.TargetEncoding.GetString(ret);
            }
        }
    }
    
    /// <summary>
    /// A function we generate that follows a set path except for bits and pieces and minimal (if any) modifcations
    /// </summary>
    public abstract class FunctionPiece
    {
        /// <summary>
        /// Corresponds to Title of function being generated
        /// </summary>
        public static readonly string RoutineNameKey = "RoutineName";

        /// <summary>
        /// Contains a list of modifiable values (if any) that effect what the template will generate. See class documentation for what whey key value does
        /// </summary>
        public DiverXmlDictionary TemplateArgs
        {
            get
            {
                return BackingTemplateArgs;
            }
        }

        /// <summary>
        /// Internal backup of <see cref="TemplateArgs"/>
        /// </summary>
        protected internal DiverXmlDictionary BackingTemplateArgs { get; } = new DiverXmlDictionary();

        /// <summary>
        /// Access to <see cref="TemplateArgs"/>
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public string this[string Index]
        {
            get
            {
                return BackingTemplateArgs[Index];
            }
            set
            {
                BackingTemplateArgs[Index] = value;
            }

        }

        /// <summary>
        /// if True it should be added. If false it should not be added
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Get the final string ready to emit
        /// </summary>
        /// <returns></returns>
        public string GetFinalBuild()
        {
            if (Enabled == false)
                return string.Empty;

            return GetBuild();
        }

        /// <summary>
        /// get Intermittent build. Subclasses overwrite this. CodeGen use uses GetFinalBuild()
        /// </summary>
        /// <returns></returns>
        protected abstract string GetBuild();


        /// <summary>
        /// return a List of keys the template contains to modify
        /// </summary>
        /// <returns></returns>
        public virtual List<string> GetTemplateArgList()
        {
            return TemplateArgs.Keys.ToList();
        }

    }


    /// <summary>
    /// If we are sourcing from LoadLibrary and GetProcAddress() one of these specifies data structs and will 
    /// generate a function to assign those
    /// </summary>
    public class DynamicLoadDetouringDataStruct : DetourAttachCommonConst
    {
        public DynamicLoadDetouringDataStruct()
        {
            TemplateArgs.Add(RoutineNameKey, "DynamicLoaderGuard");
            throw new NotImplementedException();
        }

        /// <summary>
        /// This specifies the mode to link to the Routine With
        /// <list type="table">
        /// <listheader>Valid Options</listheader>
        /// <item>
        /// <term>Default</term>
        /// <description>Default Setting is Static Import </description>
        /// </item>
        /// <item>
        /// <term>Static</term>
        /// <description>This means the pointer to the original function is gotten from direct assignment. The C++ linker does the rest. You may be required to Add static libraries while compiling</description>
        /// </item>
        /// <item>
        /// <term>StartLoadLibrary</term>
        /// <description>We Dynamically Invoke LoadLibary at the start. We do spawn thread using __beginthread() in order to attempt to avoid deadlock</description>
        /// </item>
        /// <item>
        /// <term>OnDemand</term>
        /// <description>If at least one routine is specified, we overwrite the LoadLibraryXX() stuff to trigger a detour of the routine when the module is loaded. Requires <see cref="OnDemandLibraryKey"/> to be the name of the dll to load out for.</description>
        /// </item>
        /// </list>
        /// 
        /// </summary>
        public const string LinkModeKey = "LinkMode";

        /// <summary>
        /// Only needed for when <see cref="LinkModeKey"/> is set to OnDemand. This specifies the library to check for when Loading Stuff.
        /// </summary>
        public const string OnDemandLibraryKey = "OnDemandLibrary";
        /// <summary>
        /// contains "DllName" as the key and a list of routines to source as data
        /// </summary>
        private Dictionary<string, List<string>> DllData;

        private enum SourceMode
        { 
            Default = 0,
            StaticLink = Default,
            StartingLoadLibrary = 1,
            OnDemandLoadLibrary = 2
        }

        protected override string GetBuild()
        {
            string RoutineName;
            string TargetPtrForDetour;
            SourceMode mode = SourceMode.Default;
            #region Tanslating Key plugin stuff to locals

            try
            {
                TargetPtrForDetour = BackingTemplateArgs[DetourAttachCommonConst.PointerContainerNameKey];
                if (string.IsNullOrEmpty(TargetPtrForDetour))
                {
                    throw new InvalidOperationException("The name of the variable to store the pointer to the routinal routine is not defined. This is required.");
                }
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidOperationException("The name of the variable to store the pointer to the routinal routine is not defined. This is required.", e);
            }

              try
            {
                RoutineName = BackingTemplateArgs[RoutineNameKey];
                
            }
            catch (KeyNotFoundException)
            {
                RoutineName = "SourceFor" + TargetPtrForDetour;
            }
            #endregion

            using (MemoryStream Buffer = new MemoryStream())
            {
                CodeGen.WriteComment(Buffer, "This routine Generates a routine to Assign Routine Stuff to a pointer");
                
                CodeGen.EmitDeclareFunction(Buffer, "void", CodeGen.EmitDeclareFunctionSpecs.WINAPI, RoutineName, null, null);
                CodeGen.WriteLeftBracket(Buffer);
                    switch (mode)
                    {
                        case SourceMode.OnDemandLoadLibrary:
                        CodeGen.WriteComment(Buffer, "Mode is OnDemandLoadLibrary. If at least one routine is defined with this mode, LoadLibrary() team will be detoured to check if this routine is in the target dll when loaded. If so, it will be detoured");

                        break;
                    case SourceMode.StartingLoadLibrary:
                        CodeGen.WriteComment(Buffer, "Mode is StartLoadLibrary. This means that if your source for the routine is ntdll.dll or kernel32.dll, it will be dynamically loaded in the dllmain procedure. ");
                        CodeGen.WriteComment(Buffer, "CAUTION: Take care when calling LoadLibrary() in DllMain to advoid a deadlock. In Modern Windows kernel32.dll and ntdll.dll are always loaded.");
                        CodeGen.WriteComment(Buffer, "If the source for the routine is neither of those, it will be treated as onLoadLibrary mode instread");
                        break;
                    case SourceMode.StaticLink:
                        CodeGen.WriteComment(Buffer, "Mode is StaticLink. This just assigns the RoutineName to the target typedef'd pointer and lets C++ linker do the rest. You made need to add an import library when compiling.");

                        break;
                    default: throw new NotImplementedException(Enum.GetName(typeof(SourceMode), mode));
                    }
                CodeGen.WriteRightBracket(Buffer);
                Buffer.Position = 0;
                byte[] ret = new byte[Buffer.Length];
                Buffer.Read(ret, 0, (int)Buffer.Length);
                return CodeGen.TargetEncoding.GetString(ret);
            }
        }
    }

    /// <summary>
    /// Contains a function that will be Detoured via Detours's Transaction API
    /// </summary>
    public class DetourAttachFunction : DetourAttachDetach
    {
        public DetourAttachFunction()
        {
            SetClassMode(ClassMode.AttachMode);
        }


    }


    /// <summary>
    /// Contains a function that will Detach via the Detours API a function previously done with <see cref="DetourAttachAttach"/>
    /// </summary>
    public class DetourDetachFunction : DetourAttachDetach
    {
        public DetourDetachFunction()
        {
            SetClassMode(ClassMode.DetachMode);
        }
    }


    /// <summary>
    /// some common consts for <see cref="DetourAttach"/>, <see cref="DetourDetach"/> , <see cref="DetourAttachDetach"/> and <see cref="DynamicLoadDetouringDataStruct"/>
    /// </summary>
    public abstract class DetourAttachCommonConst : FunctionPiece
    {
        protected override string GetBuild()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// suffix that does at the end of the <see cref="DiverCppWideStreamVar"/> to get it as a wchar_t* UnicodeString
        /// </summary>
        protected const string WideStringStreamToWcharPtr = ".str().c_str()";
        /// <summary>
        /// "DetourAttach" Function name for c# auto stuff
        /// </summary>
        protected const string DetourAttach = "DetourAttach";
        /// <summary>
        /// "DetourDetach" Function Name for c# auto stuff
        /// </summary>
        protected const string DetourDetach = "DetourDetach";
        /// <summary>
        /// "GetCurrentThread" Function name for c# auto stuff
        /// </summary>
        protected const string GetCurrentThread = "GetCurrentThread";
        /// <summary>
        /// "DetourUpdateThead" Function Name for c# auto stuff
        /// </summary>
        protected const string DetourUpdateThread = "DetourUpdateThread";
        /// <summary>
        /// "DetourTransactionCommit" Function Name for c# auto stuff
        /// </summary>
        protected const string DetourTransactionCommit = "DetourTransactionCommit";
        /// <summary>
        /// "DetourSetIgnoreTooSmall" Function Name for c# auto stuff
        /// </summary>
        protected const string DetourSetIgnoreTooSmall = "DetourSetIgnoreTooSmall";
        /// <summary>
        /// Name of DetourTransactionAbort for c# auto stuff
        /// </summary>
        protected const string DetourTransactionAbort = "DetourTransactionAbort";
        /// <summary>
        /// Name of DetourTransactionBegin() for C# auto complete stuff
        /// </summary>
        protected const string DetourTransactionBegin = "DetourTransactionBegin";

        /// <summary>
        /// Ends up being the name of the variable we declare to collect Unicode strings for sending to OuptuDebugStringW()
        /// </summary>
        protected const string DiverCppWideStreamVar = "MsgBuffer";

        /// <summary>
        /// Name of LONG variable that stores results of calls toe Detour Transaction API calls
        /// </summary>
        protected const string DetourResultValue = "DetourResultValue";
        /// <summary>
        /// Variable name or Name of the function that the code will be detouring
        /// </summary>
        public const string OriginalFunctionKey = "TargetFunctionPtr";

        /// <summary>
        /// This becomes a typedef'd variable that will point to the original function sourced via direct import or GetProcCall
        /// </summary>
        public const string PointerContainerNameKey = "PointerContainerNameKey";

        public const string TypeDefVarName = PointerContainerNameKey;
        public const string TypeDefVarType = "TypeDefVarType";
        /// <summary>
        /// Variable name or name of the function that future calls will call instead of the original
        /// </summary>
        public const string ReplacementFunctionKey = "DetourFunctionReplacement";

        /// <summary>
        /// if set to "1" we return false on failure
        /// </summary>
        public const string AttachRequiredKey = "FatalIfCannotAttach";

        /// <summary>
        /// if set to "1" the generated function makes a call to OutputDebugStringW() on entry with its name
        /// </summary>
        public const string ReportDebugStringEntryKey = "ReportDebugStringCall";

        /// <summary>
        /// Says how to source the function
        /// string.empty or "" means direct import aka ReportDebugStringEntryKey's variable is assigned to the function name
        /// </summary>
        public const string OriginalSourceLocation = "OriginalSourceLocation";
        /// <summary>
        /// if set to "1" the generated function makes  a call to tell detours to ignore if the target's code is to small to work with
        /// </summary>
        public const string DetourIgnoreIfToSmallKey = "DetourIgnoreIfToSmall";

    }


    /// <summary>
    /// Contains the common code between classes of <see cref="DetourAttach"/> and <see cref="DetourDetach"/>. Behaves as <see cref="DetourAttach"/> unless specified in the protected routine
    /// </summary>
    public class DetourAttachDetach :DetourAttachCommonConst
    {
        /// <summary>
        /// Enum to specify the mode this class operates in
        /// </summary>
        protected enum ClassMode
        {
            DetachMode = 1,
            AttachMode = 2
        }

        /// <summary>
        /// Contains mode this class is operating in. See <see cref="ClassMode"/>
        /// </summary>
        ClassMode Mode = ClassMode.AttachMode;
        protected void SetClassMode(ClassMode NewMode)
        {
            Mode = NewMode;
        }
        /// <summary>
        /// controls how to get the original source of the routine we are detouring if non null.
        /// Options are:
        /// <list type="number">
        /// <listheader>Valid Options</listheader>
        /// <term>"DirectAssigment</term>
        /// <description>the Typedef'd variable is assigned directly from the function name. <example> IsDebuggerPresentPtr = IsDebuggerPresent</example></description>
        /// <term>DllImport</term>
        /// <description>The Typedef'd varaible is soureced via a call to LoadLibrary and a GetProcAddress</description>
        /// </list>
        /// </summary>
        /// <remarks>the list options are case insensitive</remarks>
        public const string DetourOriginalSource = "DetourOriginalSource";

        /// <summary>
        /// Set DetourOriginalSoucceKey to this for Direct Assigement Mode
        /// </summary>
        public const string DetourOriginalSourceDirectAssigment = "DirectAssigment";
        /// <summary>
        /// Set DetourOriginalSourceKEy to this for LoadLibraryStart Mode (works only for ntdll.dll and kernel32.dll)
        /// </summary>
        public const string DetourOriginalSourceLoadLibraryOnStart = "LoadLibraryStart";
        /// <summary>
        /// SetDetourOriginalSourceKey to this for loadlibraryXXX mode
        /// </summary>
        public const string DetourOriginalSourceLoadLibraryOnDemand = "LoadLibraryOnDemand";

        /// <summary>
        /// Allows one to specify a dll to load from when attaching if it's not already loaded.
        /// </summary>
        public const string DetourSourceDllName = "DetourSourceDllName";
        /// <summary>
        /// currently unused
        /// </summary>
        public const string ReportDebugStringDetourResultsKey = "ReportDebugStringDetourResults";


        public DetourAttachDetach()
        {
            // name of the routine to emit
            TemplateArgs.Add(RoutineNameKey, "DetourAttachSingleFunction");
            // variable or expression (like GetProcAddress()) that returns the function to detour
            TemplateArgs.Add(OriginalFunctionKey, string.Empty);
            // pointer to the function that is the detour
            TemplateArgs.Add(ReplacementFunctionKey, string.Empty);
            // function indicates failure if we cannot attach to this one
            TemplateArgs.Add(AttachRequiredKey, "1");
            // function reports OutputDebugStringCallW() with message showing we detouring target with replacement
            TemplateArgs.Add(ReportDebugStringEntryKey, "1");
            // function indicates an outputdebugstringW() trace of various calls to detour functions
            TemplateArgs.Add("ReportDebugStringDetourResults", "1");
            // function informs detours to ignore this if too small.
            TemplateArgs.Add(DetourIgnoreIfToSmallKey, "1");
            
            
        }
        
        protected override string GetBuild()
        {
            // is name of the routine this GetBuild() is emitted
            string RoutineName;
            // specifies the name of the function to 
            string TargetFunctionToDetour;
            string TargetReplacement;
            
            string TypeDefName;
            string TypeDefType;

            bool PrecallToSmall;
            bool ReportDebugStringCall;
            bool ReportDetourLinkFail;
            bool isFatalIfCant;

            /// when ClassMode is AttachMode, this is "DetourAttach" otherwise it is "DetourDetach"
            string DetourAttachCall;
            string DetourActionVerb;

            bool WillDynamicallyImport = false;
            string DynmaicDllName = string.Empty;
            if (Mode == ClassMode.AttachMode)
            {
                DetourAttachCall = DetourAttach;
                DetourActionVerb = "Attach To";
            }
            else
            {
                DetourAttachCall = DetourDetach;
                DetourActionVerb = "Restore";
            }
            #region GetBuild() Extract Templates to local vars


            try
            {
                string val = TemplateArgs[DetourAttachFunction.ReplacementFunctionKey];
                if (string.IsNullOrEmpty(val) == false)
                {
                    TargetReplacement = val;
                }
                else
                {
                    throw new InvalidOperationException(string.Format( CultureInfo.InvariantCulture, MessageStrings.DetourAttachRequired_NonNullTemplate, DetourAttachFunction.ReplacementFunctionKey));
                }
            }
            catch (KeyNotFoundException e)
            {
                
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, MessageStrings.DetourAttachRequired_KeyMissing, ReplacementFunctionKey), e);
            }
            try
            {
                string val = TemplateArgs[ReportDebugStringDetourResultsKey];
                if (val != null)
                {
                    if (val.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ReportDetourLinkFail = true;
                    }
                    else
                    {
                        ReportDetourLinkFail = false;
                    }
                }
                else
                {
                    ReportDetourLinkFail = false;
                }
            }
            catch (KeyNotFoundException)
            {
                ReportDetourLinkFail = false;
            }


            try
            {
                string val = TemplateArgs[DetourIgnoreIfToSmallKey];
                if (val != null)
                {
                    if (val.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        PrecallToSmall = true;
                    }
                    else
                    {
                        PrecallToSmall = false;
                    }
                }
                else
                {
                    PrecallToSmall = false;
                }
            }
            catch (KeyNotFoundException)
            {
                PrecallToSmall = false;
            }



            try
            {
                string val = TemplateArgs[DetourAttachFunction.ReportDebugStringEntryKey];
                if (val != null)
                {
                    if (val.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ReportDebugStringCall = true;
                    }
                    else
                    {
                        ReportDebugStringCall = false;
                    }
                }
                else
                {
                    ReportDebugStringCall = false;
                }
            }
            catch (KeyNotFoundException)
            {
                ReportDebugStringCall = false;
            }



            try
            {
                string val = TemplateArgs[DetourAttachFunction.AttachRequiredKey];
                if (val != null)
                {
                    if (val.Equals("1", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isFatalIfCant = true;
                    }
                    else
                    {
                        isFatalIfCant = false;
                    }
                }
                else
                {
                    isFatalIfCant = false;
                }
            }
            catch (KeyNotFoundException)
            {
                isFatalIfCant = true;
            }


            try
            {
                TargetFunctionToDetour = TemplateArgs[OriginalFunctionKey];
            }
            catch (KeyNotFoundException e)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, MessageStrings.DetourAttachRequired_KeyMissing, OriginalFunctionKey), e);
            }

            if (string.IsNullOrEmpty(TargetFunctionToDetour))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, MessageStrings.DetourAttachRequired_NonNullTemplate, OriginalFunctionKey));
            }


            try
            {
                RoutineName = TemplateArgs[DetourAttachFunction.RoutineNameKey];
            }
            catch (KeyNotFoundException)
            {
                RoutineName = "DetourSingleTarget_" + TargetFunctionToDetour;
            }


            try
            {
                string Val = TemplateArgs[DetourAttachDetach.TypeDefVarType];
                if (string.IsNullOrEmpty(Val))
                {
                    TypeDefType = TargetFunctionToDetour + "_PtrType";
                }
                else
                {
                    TypeDefType = Val;
                }
            }
            catch (KeyNotFoundException)
            {
                TypeDefType = TargetFunctionToDetour + "_PtrType";
            }


            try
            {
                string Val = TemplateArgs[DetourAttachDetach.TypeDefVarName];
                if (string.IsNullOrEmpty(Val))
                {
                    TypeDefName = TargetFunctionToDetour + "_Ptr";
                }
                else
                {
                    TypeDefName = Val;
                }
            }
            catch (KeyNotFoundException)
            {
                TypeDefName = TargetFunctionToDetour + "_Ptr";
            }


            try
            {
                string Val = TemplateArgs[DetourAttachDetach.DetourOriginalSource];
                if (string.IsNullOrEmpty(Val))
                {
                    WillDynamicallyImport = false;
                }
                else
                {

                    WillDynamicallyImport = false;
                    if (string.Equals( Val, "directassignment", StringComparison.InvariantCultureIgnoreCase) )
                    {
                        WillDynamicallyImport = false;
                    }
                    if (string.Equals( Val, "DynamicLoad", StringComparison.InvariantCultureIgnoreCase))
                    {
                        WillDynamicallyImport = true;
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                WillDynamicallyImport = false;
            }


            #endregion

            if (!Enabled)
            {
                return string.Empty;
            }

            using (var Buffer = new MemoryStream())
            {
                List<string> InsertArgs = new List<string>();
                
                CodeGen.WriteComment(Buffer, "Routine to " + DetourActionVerb + " function " + TargetFunctionToDetour);
                
                CodeGen.EmitDeclareFunction(Buffer, "BOOL", CodeGen.EmitDeclareFunctionSpecs.WINAPI, RoutineName, null, null);
                CodeGen.WriteNewLine(Buffer);
                CodeGen.WriteLeftBracket(Buffer, true);
                CodeGen.EmitDeclareVariable(Buffer, "LONG", DetourResultValue, "0");
                CodeGen.EmitDeclareWideStringStream(Buffer, DiverCppWideStreamVar, false);

                if (ReportDebugStringCall)
                {
                        CodeGen.EmitCallOutputDebugString(Buffer, "Starting to " + DetourActionVerb + " "+ RoutineName + "\\r\\n", true);
                }

                if (WillDynamicallyImport)
                {
                    var DllNameAsArg = new List<string>() { string.Format(CultureInfo.InvariantCulture, "L\"{0}\"", DynmaicDllName) };
                    CodeGen.WriteComment(Buffer, "Code was generated to Load target DLL, link to it and " + DetourActionVerb + " " + RoutineName);
                    CodeGen.WriteLeftBracket(Buffer);
                        CodeGen.EmitDeclareVariable(Buffer, "HMODULE", "DllHandle");
                    CodeGen.EmitCallFunction(Buffer, "GetModuleHandleW", null, DllNameAsArg , "DllHandle");
                    
                    CodeGen.WriteIf(Buffer, "DllHandle == NULL");
                    CodeGen.WriteLeftBracket(Buffer);
                        CodeGen.EmitCallFunction(Buffer, "LoadLibraryW", null, DllNameAsArg, "DllHandle");
                        CodeGen.WriteComment(Buffer, "Warning: This handle is left intentionally open.");


                    CodeGen.EmitAssignVariable(Buffer, TypeDefName, string.Format(CultureInfo.InvariantCulture, "GetProcAddress(DllHandle,\"{0}\"", TargetFunctionToDetour));
                    CodeGen.WriteRightBracket(Buffer);
                    CodeGen.WriteRightBracket(Buffer);
                    
                }
                else
                {
                    CodeGen.WriteComment(Buffer, "Code was generated to statically  use the original " + TargetFunctionToDetour + " and let the C/C++ linker do the rest");
                    CodeGen.EmitAssignVariable(Buffer, TypeDefName, TargetFunctionToDetour);
                }

                if (PrecallToSmall)
                {
                    CodeGen.EmitCallFunction(Buffer, DetourSetIgnoreTooSmall, null, new List<string>() { "FALSE" });
                }

                CodeGen.EmitCallFunction(Buffer, DetourTransactionBegin, null, null, DetourResultValue);
                CodeGen.WriteIf(Buffer, DetourResultValue + " != NO_ERROR");
                {
                    CodeGen.WriteLeftBracket(Buffer, true);

                    if (ReportDetourLinkFail)
                    {
                        InsertArgs.Add("L\"Call to " + DetourTransactionBegin + "() for " + DetourActionVerb +" function " + TargetFunctionToDetour + " failed with error code \"");
                        InsertArgs.Add(DetourResultValue);
                        InsertArgs.Add("endl");
                        CodeGen.EmitInsertStream(Buffer, DiverCppWideStreamVar, InsertArgs, false);
                        InsertArgs.Clear();

                        CodeGen.EmitCallOutputDebugString(Buffer, DiverCppWideStreamVar + WideStringStreamToWcharPtr, false);
                    }

                    CodeGen.EmitReturnX(Buffer, "FALSE");
                    CodeGen.WriteRightBracket(Buffer, true);
                }

                CodeGen.EmitAssignVariable(Buffer, DetourResultValue, string.Format(CultureInfo.InvariantCulture, "{0}({1}())", DetourUpdateThread, GetCurrentThread));

                CodeGen.WriteIf(Buffer, DetourResultValue +" != NO_ERROR");
                {
                    CodeGen.WriteLeftBracket(Buffer, true);

                    if (ReportDetourLinkFail)
                    {
                        InsertArgs.Add("L\"Call to " + DetourUpdateThread + "() for " + DetourActionVerb + " function " + TargetFunctionToDetour + " failed with error code \"");
                        InsertArgs.Add(DetourResultValue);
                        InsertArgs.Add("endl");
                        CodeGen.EmitInsertStream(Buffer, DiverCppWideStreamVar, InsertArgs, false);
                        InsertArgs.Clear();

                        CodeGen.EmitCallOutputDebugString(Buffer, DiverCppWideStreamVar + WideStringStreamToWcharPtr, false);
                    }


                    CodeGen.EmitCallFunction(Buffer, DetourTransactionAbort, null, null);

                    CodeGen.EmitReturnX(Buffer, "FALSE");
                    CodeGen.WriteRightBracket(Buffer, true);
                }

                // unsure if &(PVOID&) or just (PVOID*)
                //CodeGen.EmitAssignVariable(Buffer, DetourResultValue, string.Format(CultureInfo.InvariantCulture, DetourAttachCall + "((PVOID*){0},  {1})", TargetFunctionToDetour, TargetReplacement));
                CodeGen.EmitAssignVariable(Buffer, DetourResultValue, string.Format(CultureInfo.InvariantCulture, DetourAttachCall + "(&(PVOID&){0},  {1})", TypeDefName, TargetReplacement));
                CodeGen.WriteIf(Buffer, DetourResultValue + " != NO_ERROR");
                {
                    CodeGen.WriteLeftBracket(Buffer, true);

                    if (ReportDetourLinkFail)
                    {
                        
                        InsertArgs.Add("L\"Call to " + DetourAttach + "() for " +DetourActionVerb + " function " + TargetFunctionToDetour + " failed with error code \"");
                        InsertArgs.Add(DetourResultValue);
                        InsertArgs.Add("endl");
                        CodeGen.EmitInsertStream(Buffer, DiverCppWideStreamVar, InsertArgs, false);
                        InsertArgs.Clear();

                        CodeGen.EmitCallOutputDebugString(Buffer, DiverCppWideStreamVar + WideStringStreamToWcharPtr, false);
                    }

                    CodeGen.EmitCallFunction(Buffer, DetourTransactionAbort, null, null);

                    CodeGen.EmitReturnX(Buffer, "FALSE");
                    CodeGen.WriteRightBracket(Buffer, true);
                }


                CodeGen.EmitCallFunction(Buffer, DetourTransactionCommit, null, null, DetourResultValue);


                CodeGen.WriteIf(Buffer, DetourResultValue +" != NO_ERROR");
                {
                    CodeGen.WriteLeftBracket(Buffer, true);

                    if (ReportDetourLinkFail)
                    {
                        InsertArgs.Add("L\"Call to " + DetourTransactionCommit + "() for " + DetourActionVerb + " function " + TargetFunctionToDetour + " failed with error code \"");
                        InsertArgs.Add(DetourResultValue);
                        InsertArgs.Add("endl");
                        CodeGen.EmitInsertStream(Buffer, DiverCppWideStreamVar, InsertArgs, false);
                        InsertArgs.Clear();

                        CodeGen.EmitCallOutputDebugString(Buffer, DiverCppWideStreamVar + WideStringStreamToWcharPtr, false);
                    }

                    CodeGen.EmitCallFunction(Buffer, DetourTransactionAbort, null, null);

                    CodeGen.EmitReturnX(Buffer, "FALSE");
                    CodeGen.WriteRightBracket(Buffer, true);
                }

                CodeGen.EmitReturnX(Buffer, "TRUE");
                CodeGen.WriteRightBracket(Buffer, true);

              
                Buffer.Position = 0;
                byte[] ret = new byte[Buffer.Length];
                Buffer.Read(ret, 0, (int)Buffer.Length);
                return CodeGen.TargetEncoding.GetString(ret);


            }
        }
    
    }
    /// <summary>
    /// implements a routien that dllmain will call to force the debugeed app to keep the dll loaded in memory
    /// </summary>
    public class PinRoutine: FunctionPiece
    {
        /// <summary>
        /// Have this set to something not "0" to have the pin routine emit a 2nd argument that will receive results for GetLastError() if not null
        /// </summary>
        public const string WantLastErrorArgKey = "Want2ndLastError";
        public PinRoutine()
        {
            //TemplateArgs = new DiverXmlDictionary();
            TemplateArgs.Add(RoutineNameKey, "PinSelfInMemory");
            TemplateArgs.Add(WantLastErrorArgKey, "0");
        }


        protected override string GetBuild()
        {
            string RoutineName;
            bool WantArg2Ptr;
            #region Get Template Data
                try
                {
                    string val = TemplateArgs[WantLastErrorArgKey];
                    if ( (val != null) )
                    {
                        if (val.Equals("0", StringComparison.OrdinalIgnoreCase))
                        {
                            WantArg2Ptr = false;
                        }
                        else
                        {
                            WantArg2Ptr = true;
                        }
                    }
                    else
                    {
                        WantArg2Ptr = false;
                    }
                }
                catch (KeyNotFoundException)
                {
                    WantArg2Ptr = false;
                }

                try
                {
                    RoutineName = TemplateArgs[RoutineNameKey];
                }
                catch (KeyNotFoundException)    
                {
                    RoutineName = "PinSelfInMemory";
                }
                var OldIndent = CodeGen.IndentLevel;
                CodeGen.IndentLevel = 0;
            using (MemoryStream ret = new MemoryStream())
            {
                List<string> ArgTypes = new List<string>();
                List<string> ArgNames = new List<string>();
                ArgTypes.Add("HINSTANCE");

                if (WantArg2Ptr)
                {
                    ArgTypes.Add("DWORD*"); 
                }
                ArgNames.Add("DllSelfAddress");
                if (WantArg2Ptr)
                {
                    ArgNames.Add("LastErrorHelp");
                }

                CodeGen.WriteComment(ret, "Once " + RoutineName + " is called, this gets a handle to the dll this code is in.");
                CodeGen.EmitDeclareVariable(ret, "HMODULE", "SelfId", "0");
                    CodeGen.EmitDeclareFunction(ret, "BOOL", CodeGen.EmitDeclareFunctionSpecs.WINAPI, RoutineName, ArgTypes, ArgNames);
                CodeGen.WriteNewLine(ret);

                CodeGen.WriteLeftBracket(ret, true);
                CodeGen.EmitDeclareVariable(ret, "BOOL", "Result");
                CodeGen.EmitDeclareWideStringStream(ret, "DebugStringBuffer");
                CodeGen.EmitCallFunction(ret, "SetLastError", null, new List<string>() { "0" });

                CodeGen.EmitCallFunction(ret, "GetModuleHandleExW", null, new List<string>() { "(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS |  GET_MODULE_HANDLE_EX_FLAG_PIN)", "((LPCWSTR) " + ArgNames[0] + ")", "&SelfId" }, "Result");

                CodeGen.WriteIf(ret, "Result == TRUE");
                {
                    CodeGen.WriteLeftBracket(ret, true);
                    List<string> BufferArgs = new List<string>();
                    BufferArgs.Add("\"GetModuleHandleExW() Reports success with pinning Diver Dll in memory with LastErrorCode of \"");
                    BufferArgs.Add("GetLastError()");
                    BufferArgs.Add("endl");
                    CodeGen.EmitInsertStream(ret, "DebugStringBuffer", BufferArgs, false);
                    CodeGen.EmitCallOutputDebugString(ret, "DebugStringBuffer" + ".str().c_str()");
                    CodeGen.WriteRightBracket(ret, true);
                }
                CodeGen.WriteLiteral(ret, "else");
                {
                    CodeGen.WriteLeftBracket(ret, true);
                    List<string> BufferArgs = new List<string>();
                    BufferArgs.Add("\"GetModuleHandleExW() Reports failure with pinning Diver Dll in memory with LastErrorCode of \"");
                    BufferArgs.Add("GetLastError()");
                    BufferArgs.Add("endl");
                    CodeGen.EmitInsertStream(ret, "DebugStringBuffer", BufferArgs, false);
                    CodeGen.EmitCallOutputDebugString(ret, "DebugStringBuffer" + ".str().c_str()");
                    CodeGen.WriteRightBracket(ret, true);
                }


                if (WantArg2Ptr)
                {
                    CodeGen.WriteIf(ret, "LastErrorHelp != 0");
                    CodeGen.WriteLeftBracket(ret, true);
                     CodeGen.WriteLiteral(ret, "*LastErrorHelp = GetLastError()");
                    CodeGen.WriteRightBracket(ret, true);
                }

                CodeGen.EmitReturnX(ret, "Result");

                CodeGen.WriteRightBracket(ret, true);
                    byte[] ret_data = new byte[ret.Length];
                    ret.Position = 0;
                    ret.Read(ret_data, 0, (int)ret.Length);

                CodeGen.IndentLevel = OldIndent;
                return CodeGen.TargetEncoding.GetString(ret_data);   
                    
                }
              
            #endregion
            /*
            {
                try
                {
                    RoutineName = TemplateArgs[RoutineNameKey];
                }
                catch (KeyNotFoundException)
                {
                    RoutineName = "PinSelfInMemory";
                }
                return string.Format(CultureInfo.InvariantCulture, Template, "\"", RoutineName);
            }*/
        }

    }

    public class LoadLibraryWrapper: FunctionPiece
    {
        string NameOfLoadLibraryA_Wrapper;
        string NameOfLoadLibraryW_Wrapper;
        string NameOfLoadLibraryExA_Wrapper;
        string NameOfLoadLibraryExW_Wrapper;
        string NameOfLoadLibraryCommon_Wrapper;
        string UnicodeVarName_Value;



        /// </summary>
        public List<NativeFunctionClass> TargetFunctions = new List<NativeFunctionClass>();

        #region Template Args

        /// <summary>
        /// this key will be the name of the function that the other detoured LoadLibrary routines will point too. It will eventually be calling LoadLibraryExW
        /// </summary>
        public const string LoadLibraryCommonKey = "LoadLibraryCommonKey";
        /// <summary>
        /// this key will be the title of the LoadLibraryA DetourFunction
        /// </summary>
        public const string LoadLibraryAKey = "LoadLibraryAKey";
        /// <summary>
        /// this key becomes the name of the LoadLibraryW DetourFunction
        /// </summary>
        public const string LoadLibraryWKey = "LoadLibraryWKey";

        /// <summary>
        /// this key becomes the name of the LoadLibraryEx DetourFunction
        /// </summary>
        public const string LoadLibraryExAKey = "LoadLibraryExAKey";

        /// <summary>
        /// this key becomes the name of the  LoadLibraryExW DetourFunction
        /// </summary>
        public const string LoadLibraryExWKey = "LoadLibraryExWKey";

        /// <summary>
        /// this key becomes the name of the unicode string that we create if needed (LoadLibraryA, LoadLibraryExA)
        /// </summary>
        public const string UnicodePtrNameKey = "UnicodePtrNameKey";
        #endregion
        public LoadLibraryWrapper()
        {
            TemplateArgs.Add(UnicodePtrNameKey, "UnicodeString");
            TemplateArgs.Add(LoadLibraryWKey, "LoadLibraryW_Detour");
            TemplateArgs.Add(LoadLibraryExWKey, "LoadLibraryExW_Detour");
            TemplateArgs.Add(LoadLibraryExAKey, "LoadLibraryExA_Detour");
            TemplateArgs.Add(LoadLibraryAKey, "LoadLibrayA_Detour");
        }
        
        /// <summary>
        /// as the template args are spread thru a couple of routines, this extracts them and sets private values the code can use
        /// </summary>
        private void SetArguments()
        {

            string val;

            try
            {
                val = TemplateArgs[UnicodePtrNameKey];
                if (string.IsNullOrEmpty(val))
                {
                    val = "UnicodeString";
                }
            }
            catch (KeyNotFoundException)
            {
                val = "UnicodeString";
            }

            UnicodeVarName_Value = val;


            try
            {
                val = TemplateArgs[LoadLibraryWKey];
                if (string.IsNullOrEmpty(val))
                {
                    val = "LoadLibraryW_Detour";
                }
            }
            catch (KeyNotFoundException)
            {
                val = "LoadLibraryW_Detour";
            }
            NameOfLoadLibraryW_Wrapper = val;

            try
            {
                val = TemplateArgs[LoadLibraryAKey];
                if (string.IsNullOrEmpty(val))
                {
                    val = "LoadLibraryA_Detour";
                }
            }
            catch (KeyNotFoundException)
            {
                val = "LoadLibraryA_Detour";
            }
            NameOfLoadLibraryA_Wrapper = val;

            try
            {
                val = TemplateArgs[LoadLibraryExWKey];
                if (string.IsNullOrEmpty(val))
                {
                    val = "LoadLibraryExW_Detour";
                }
            }
            catch (KeyNotFoundException)
            {
                val = "LoadLibraryExW_Detour";
            }
            NameOfLoadLibraryExW_Wrapper = val;

            try
            {
                val = TemplateArgs[LoadLibraryExAKey];
                if (string.IsNullOrEmpty(val))
                {
                    val = "LoadLibraryExA_Detour";
                }
            }
            catch (KeyNotFoundException)
            {
                val = "LoadLibraryExA_Detour";
            }
            NameOfLoadLibraryExA_Wrapper = val;

            try
            {
                val = TemplateArgs[LoadLibraryCommonKey];
                if (string.IsNullOrEmpty(val))
                {
                    val = "LoadLibraryCommon_Detour";
                }
            }
            catch (KeyNotFoundException)
            {
                val = "LoadLibraryCommon_Detour";
            }
            NameOfLoadLibraryCommon_Wrapper = val;


        }

        /// <summary>
        /// Emit code to allocate a block of memory, covert an ansi string to its unicode and paste it in the new block
        /// </summary>
        /// <param name="Output">emit to this using <see cref="CodeGen.TargetEncoding"/></param>
        /// <param name="UnicodeStringName">becomes the name of the unicode string variable</param>
        /// <param name="AnsiStringName">becomes the name of the ansi string source</param>
        /// <param name="DeclareUnicode">if set we declare the unicode string variable</param>
        private void EmitANSIUnicodeBlerb(Stream Output, string UnicodeStringName, string AnsiStringName, bool DeclareUnicode)
        {
            CodeGen.WriteComment(Output, "This Body of Code in the brackets below is shared between the detoured LoadLibraryA, LoadLibraryExA");
            CodeGen.WriteComment(Output, "It converts a non null ansi string to unicode and assigns it to a variable named UnicodeEqual");
            if (DeclareUnicode)
            {
                CodeGen.EmitDeclareVariable(Output, "LPCWSTR", UnicodeStringName, "NULL");
            }
            CodeGen.WriteLeftBracket(Output, true);
                
                
                CodeGen.WriteIf(Output, AnsiStringName + " != 0");
                CodeGen.WriteLeftBracket(Output, true);
                CodeGen.EmitDeclareVariable(Output, "size_t", "CharCount", string.Format(CultureInfo.InvariantCulture, "strlen({0})", AnsiStringName));

                    CodeGen.EmitCallMalloc(Output, "((CharCount+1) * sizeof(wchar_t))", UnicodeStringName, false);
                    CodeGen.WriteIf(Output, string.Format(CultureInfo.InvariantCulture, "({0} != 0)", UnicodeStringName));
                    CodeGen.WriteLeftBracket(Output, true);
                        CodeGen.EmitCallZeroMemory(Output, UnicodeStringName, "((CharCount+1) * sizeof(wchar_t))", CodeGen.ZeroMemoryArg.ExactlyAsPassed);
                    CodeGen.WriteRightBracket(Output, true);

                    CodeGen.EmitDeclareVariable(Output, "int", "ConvertResult");
                    CodeGen.EmitCallFunction(Output, "MultiByteToWideChar", null, new List<string> { "C_ACP", "0", AnsiStringName, "CharCount", UnicodeStringName,  "CharCount"  }, "ConvertResult");
                    
            CodeGen.WriteRightBracket(Output, true);
                CodeGen.WriteElse(Output);
                CodeGen.WriteLeftBracket(Output, true);
                    CodeGen.EmitAssignVariable(Output, UnicodeStringName, AnsiStringName);
                CodeGen.WriteRightBracket(Output, true);
            CodeGen.WriteRightBracket(Output, true);
        }
        private void EmitLoadLibraryCommon(Stream Output)
        {
            NativeFunctionClass DetourLoadLibraryExW = new NativeFunctionClass();
            DetourLoadLibraryExW.DetourFunctionName = NameOfLoadLibraryCommon_Wrapper;
            DetourLoadLibraryExW.CallingConvention = CodeGen.EmitDeclareFunctionSpecs.WINAPI;
            DetourLoadLibraryExW.ReturnType = new NativeFunctionArg("HMODULE", UnmanagedType.SysInt);

            DetourLoadLibraryExW.Arguments.Add(new NativeFunctionArg("LPCWSTR", "lpLibFileName", UnmanagedType.LPWStr));
            DetourLoadLibraryExW.Arguments.Add(new NativeFunctionArg("HANDLE", "hFile", UnmanagedType.U4));
            DetourLoadLibraryExW.Arguments.Add(new NativeFunctionArg("DWORD", "dwFlags", UnmanagedType.U4));
            DetourLoadLibraryExW.FunctionName = "LoadLibraryExW_Ptr";
            DetourLoadLibraryExW.EmitDetourFunction(Output, "LoadLibraryExW_Ptr");
        }
        private void EmitLoadLibraryA(Stream Output)
        {
            List<string> ArgTypes;
            List<string> ArgNames;
            List<string> FuncCall;

            ArgTypes = new List<string>();
            ArgNames = new List<string>();
            FuncCall = new List<string>();

            ArgTypes.Add("LPCSTR");
            ArgNames.Add("lpLibFileName");

            FuncCall.Add("UnicodePtr");
            FuncCall.Add("NULL");
            FuncCall.Add("0");
            FuncCall.Add("L\"LoadLibraryA\"");

            CodeGen.EmitDeclareFunction(Output, "HMODULE", CodeGen.EmitDeclareFunctionSpecs.WINAPI, NameOfLoadLibraryA_Wrapper, ArgTypes, ArgNames);
            CodeGen.WriteNewLine(Output);
            CodeGen.WriteLeftBracket(Output, true);
                EmitANSIUnicodeBlerb(Output, UnicodeVarName_Value, ArgNames[0], true);
                CodeGen.EmitDeclareVariable(Output, "HMODULE", "Result", "0");
                CodeGen.EmitCallFunction(Output, NameOfLoadLibraryCommon_Wrapper, null, FuncCall, "Result");
                CodeGen.EmitCallFree(Output, UnicodeVarName_Value, true);
                CodeGen.EmitReturnX(Output, "Result");
            CodeGen.WriteRightBracket(Output, true);
            
        }

        private void EmitLoadLibraryW(Stream Target)
        {

        }

        private void EmitLoadLibraryExA(Stream Target)
        {

        }

        private void EmitLoadLibraryExW(Stream Target)
        {

        }

        protected override string GetBuild()
        {
            using (MemoryStream Target = new MemoryStream())
            {
                SetArguments();
                EmitLoadLibraryA(Target);
                EmitLoadLibraryW(Target);
                EmitLoadLibraryExA(Target);
                EmitLoadLibraryExW(Target);
                EmitLoadLibraryCommon(Target);

                Target.Flush();
                Target.Position = 0;
                byte[] Bytes = new byte[Target.Length];
                Target.Read(Bytes, 0, (int) Target.Length);
                return CodeGen.TargetEncoding.GetString(Bytes);
            }
        }
    }
    /// <summary>
    /// Overwrite for LoadLibraryA during 
    /// </summary>
    public class LoadLibraryA: FunctionPiece
    {
        public LoadLibraryA()
        {
            BackingTemplateArgs.Add(RoutineNameKey, "LoadLibraryA_OnDemandLink");
        }

        protected override string GetBuild()
        {

            using (MemoryStream Output = new MemoryStream())
            {
                List<string> ArgType, ArgName;
                ArgType = new List<string>();   
                ArgType.Add("LPCSTR");
                ArgName = new List<string>();
                ArgName.Add("lpLibFileName");

                string unicodename = ArgName[0] + "Unicode";
                CodeGen.EmitDeclareFunction(Output, "HMODULE", CodeGen.EmitDeclareFunctionSpecs.WINAPI, BackingTemplateArgs[RoutineNameKey], ArgType, ArgName);
                CodeGen.WriteNewLine(Output);
                CodeGen.WriteLeftBracket(Output);

                CodeGen.WriteComment(Output, "Wrapper for ondemand detour mode. This converts the string to unicode to non-null and calls a common detour loadlibrary that will do the work");
                CodeGen.EmitDeclareVariable(Output, "HMODULE", "ret");
                CodeGen.EmitDeclareVariable(Output, "wchar_t*", unicodename);
                CodeGen.WriteIf(Output, ArgName[0] + " != 0");
                CodeGen.WriteLeftBracket(Output, true);
                 
                {
                    CodeGen.EmitDeclareVariable(Output, "SIZE_T", "StringLenChars", "(strlen(lpLibFileName) + 1)");
                    CodeGen.EmitAssignVariable(Output, unicodename, "(wchar_t*)malloc(stringLenChars * sizeof(wchar_t))");
                    CodeGen.WriteIf(Output, unicodename + " == 0");
                    {
                        CodeGen.WriteLeftBracket(Output,true);
                        CodeGen.EmitCallFunction(Output, "SetLastError", null, new List<string>() { "ERROR_OUTOFMEMORY" });
                        CodeGen.EmitReturnX(Output, "NULL");
                        CodeGen.WriteRightBracket(Output, true);
                    }
                    List<string> ConvertCallArgs = new List<string>();
                    ConvertCallArgs.Add("CP_ACP");
                    ConvertCallArgs.Add("MB_COMPOSITE");
                    ConvertCallArgs.Add(ArgName[0]);
                    ConvertCallArgs.Add("StringLenChars");
                }
                CodeGen.WriteRightBracket(Output, true);
                CodeGen.WriteElse(Output);
                CodeGen.WriteLeftBracket(Output, true);
                    CodeGen.EmitAssignVariable(Output, unicodename, "NULL");
                CodeGen.WriteRightBracket(Output, true);

                    ArgType.Add("wchar_t*");
                    ArgName.Clear();
                    ArgName.Add(unicodename);
                    ArgName.Add("L\"LoadLibraryA\"");

                CodeGen.EmitCallFunction(Output, "LoadLibraryDetourCommon", ArgType, ArgName, "ret");
                    CodeGen.WriteLiteral(Output, "free(" + unicodename + ");");
                    CodeGen.EmitReturnX(Output, "ret");
                CodeGen.WriteRightBracket(Output);
                byte[] Ret = new byte[Output.Length];
                Output.Position = 0;
                Output.Read(Ret, 0, Ret.Length);
                return CodeGen.TargetEncoding.GetString(Ret);
            }
       
        }
    }

    public class LoadLibraryExA : FunctionPiece
    {
        protected override string GetBuild()
        {
            throw new NotImplementedException();
        }
    }

    public class LoadLibraryDiverW: FunctionPiece
    {
        protected override string GetBuild()
        {
            throw new NotImplementedException();
        }
    }

    public class LoadLibraryDiverExW : FunctionPiece
    {
        protected override string GetBuild()
        {
            throw new NotImplementedException();
        }
    }

    public class LoadLibraryDiverExWCommon: FunctionPiece
    {
        protected override string GetBuild()
        {
            throw new NotImplementedException();
        }
    }

    public class LoadLibraryDiverWCommon : FunctionPiece
    {
        protected override string GetBuild()
        {
            throw new NotImplementedException();
        }
    }




    /// <summary>
    /// This class makes the dllmain routine
    /// <list type=">bullet">
    /// DetourHelperCodeFlag.   If "1"  then we include code to return sucess if DetourIsHelperProcess() returns TRUE   
    /// </list>
    ///  <list type=">bullet">
    /// PinCode.             If "1" then we generate code to get a module handle of this dll with NativeMethod GetModuleHandleExW using GET_MODULE_HANDLE_EX_FLAG_PIN
    /// </list>
    ///  <list type=">bullet">
    ///  RoutineName.         This Becomes the name of the routine when the code this class contaisn is emitted.
    /// </list>
    /// </summary>
    public class DllMainRoutine: FunctionPiece
    {
        private const string DetoursIsHelperProcessStr = "DetourIsHelperProcess";

        /// <summary>
        /// Contains DllMain's Argument Types in string form
        /// </summary>
        private readonly static List<string>  FuncArgsTypes = new List<string>() { "HINSTANCE", "DWORD", "LPVOID" };
        /// <summary>
        /// contains DllMain's Argument Names in string form
        /// </summary>
        private readonly static List<string> FuncArgNames = new List<string>() { "hinstDLL", "fdwReason", "lpReserved" };

        // helper to index an array
        private const int hInstDll = 0;
        // helper to index an array
        private const int fdWReason = 1;
        // helper to index an array
        private const int lpReseved = 2;

        /// <summary>
        /// This is the Function Template that'll generate the PinCode if the the <see cref="PinCodeKey"/> Template is set to "1"
        /// </summary>
        public PinRoutine PinTemplate { get;  } = new PinRoutine();
        /// <summary>
        /// if set to "1" this add a call to DetourIsHelperProcess() andreturn TRUE if that returns true
        /// </summary>
        public static readonly string DetourHelperCodeFlagKey = "DetourHelperCodeFlag";

        /// <summary>
        /// if set to "1" we add code to prevent the dll from being unleaded (via GetModuleHandleExW() <see cref="PinRoutine"/> to implmentation
        /// </summary>
        public static readonly string PinCodeKey = "PinCode";

        /// <summary>
        /// if '1' we just pin outself into the memory with the pin and repsond the threadattach / detach calls to update code as needed
        /// </summary>
        public static readonly string EnableThreadedAttachesKey = "WantThreadDetourAttachCalls";

        /// <summary>
        /// Code to dump into the DllMain PROCESS_ATTACH
        /// </summary>
        public string ProcessAttachCode { get; set; } = string.Empty;
        /// <summary>
        /// Code to dump into DllMain PROCESS_DETACH
        /// </summary>
        public string ProcessDetachCode { get; set; } = string.Empty;
        /// <summary>
        /// Code to dump into DllMain Threadattach
        /// </summary>
        public string ThreadAttachCode { get; set; } = string.Empty;
        /// <summary>
        /// Code to dump as DllMain ThreadDetach
        /// </summary>
        public string ThreadDetachCode { get; set; } = string.Empty;

        public DllMainRoutine()
        {
            TemplateArgs.Add(DetourHelperCodeFlagKey, "1");
            TemplateArgs.Add(PinCodeKey, "1");
            TemplateArgs.Add(RoutineNameKey, "DllMain");
            TemplateArgs.Add(EnableThreadedAttachesKey, "1");
        }

        protected override string GetBuild()
        {
            // include the DetourIsHelperProcess() call
            bool WantHelperCall;
            // include the code to pin self in memory
            bool WantPin;
            // title of routine, aka DllMain

            string RoutineName;

            // Want to attach and detatch on a thread by thread basis
            bool PlaceAttachInThreadRoutine;

            #region Get Template differents
            try
            {
                PlaceAttachInThreadRoutine = false;
                string val = TemplateArgs[EnableThreadedAttachesKey];
                if (val != null)
                {
                    if (val.Equals("1", StringComparison.OrdinalIgnoreCase))
                    {
                        PlaceAttachInThreadRoutine = true;
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                PlaceAttachInThreadRoutine = false;
            }


            try
            {
                WantHelperCall = false;
                string val = TemplateArgs[DetourHelperCodeFlagKey];
                if (val != null)
                {
                    if (val.Equals("1", StringComparison.OrdinalIgnoreCase))
                    {
                        WantHelperCall = true;
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                WantHelperCall = false;
            }


            try
            {
                string val = TemplateArgs[RoutineNameKey];
                RoutineName = val;
            }
            catch (KeyNotFoundException)
            {
                RoutineName = "DllMain";
            }
            try
            {
                WantPin = false;
                string val = TemplateArgs[PinCodeKey];
                if (val != null)
                {
                    if (val.Equals("1", StringComparison.OrdinalIgnoreCase))
                    {
                        WantPin = true;
                    }
                }
            }
            catch (KeyNotFoundException)
            {
                WantPin = false;
            }
            #endregion

            using (var TmpBuff = new MemoryStream())
            {
                if (PlaceAttachInThreadRoutine)
                {
                    CodeGen.WriteComment(TmpBuff, "Code generated with '" + EnableThreadedAttachesKey + " enabled (set to 1).");
                    CodeGen.WriteComment(TmpBuff, "This causes the DetourAttach() transactions to be made in DLL_THREAD_ATTACH instead of DLL_PROCESS_ATTACH");
                }
                else
                {
                    CodeGen.WriteComment(TmpBuff, "Code generated with '" + EnableThreadedAttachesKey + " disabled (set to 0).");
                    CodeGen.WriteComment(TmpBuff, "This causes the DetourAttach() transactions to be made in DLL_PROCESS_ATTACH instead of DLL_THREAD_ATTACH");
                }

                if (WantHelperCall)
                {
                    CodeGen.WriteComment(TmpBuff, "Code generated with '" + DetourHelperCodeFlagKey + " enabled (set to 1).");
                    CodeGen.WriteComment(TmpBuff, "This means a call to DetourIsHelperProcess will be make and " + RoutineName + "will return TRUE without futher processing if DetourIsHelperProcess() routines TRUE");
                }
                else
                {
                    CodeGen.WriteComment(TmpBuff, "Code generated with '" + DetourHelperCodeFlagKey + " disabled (set to 0).");
                    CodeGen.WriteComment(TmpBuff, "This means no code to check if DetourIsHelperProcess() is constructed");
                }

                if (WantPin)
                {
                    CodeGen.WriteComment(TmpBuff, "Code generated with '" + PinCodeKey + " enabled (set to 1).");
                    CodeGen.WriteComment(TmpBuff, RoutineName + " will include code with  a call to GetModuleHandleExW() with GET_MODULE_HANDLE_EX_FLAG_PIN");
                    CodeGen.WriteComment(TmpBuff, "This can assist in preventing access violations should Diver be prematurely unmapped without restoring hooks");
                }
                else
                {
                    CodeGen.WriteComment(TmpBuff, "Code generated with '" + DetourHelperCodeFlagKey + " disabled (set to 0).");
                    CodeGen.WriteComment(TmpBuff, RoutineName + " will include NOT include code that callsGetModuleHandleExW() with GET_MODULE_HANDLE_EX_FLAG_PIN");
                    CodeGen.WriteComment(TmpBuff, "WARNING. Not enabled this flag risks access violations if Diver becomes prematurely unloaded without restoring hooks");
                }


                if (WantPin)
                {
                    CodeGen.WriteLiteralNoIndent(TmpBuff, PinTemplate.GetFinalBuild(), false);
                }
                CodeGen.WriteNewLine(TmpBuff);

                // emit function title
                CodeGen.EmitDeclareFunction(TmpBuff, "BOOL", CodeGen.EmitDeclareFunctionSpecs.WINAPI, RoutineName, DllMainRoutine.FuncArgsTypes, DllMainRoutine.FuncArgNames);
                CodeGen.WriteNewLine(TmpBuff);
                CodeGen.WriteLeftBracket(TmpBuff, true);
                {
                    CodeGen.EmitDeclareVariable(TmpBuff, "BOOL", "Result", "FALSE");
                    if (WantHelperCall)
                    {
                        CodeGen.EmitCallFunction(TmpBuff, DetoursIsHelperProcessStr, null, null, "Result");
                        CodeGen.WriteIf(TmpBuff, "Result == TRUE");
                        CodeGen.WriteLeftBracket(TmpBuff, true);
                        {
                            CodeGen.EmitReturnX(TmpBuff, "Result");
                            CodeGen.WriteRightBracket(TmpBuff, true);
                        }
                    }
                    CodeGen.WriteNewLine(TmpBuff);


                    if (WantPin)
                    {
                        CodeGen.EmitCallFunction(TmpBuff, PinTemplate[RoutineNameKey], null, new List<string>() { FuncArgNames[hInstDll] });
                    }

                    CodeGen.EmitCallFunction(TmpBuff, "DetourRestoreAfterWith", null, null, string.Empty);

                    CodeGen.WriteNewLine(TmpBuff);

                    CodeGen.WriteSwitch(TmpBuff, FuncArgNames[fdWReason]);
                    {
                        CodeGen.WriteLeftBracket(TmpBuff, true);
                            CodeGen.WriteCase(TmpBuff, "DLL_PROCESS_ATTACH");
                            {
                                CodeGen.WriteLeftBracket(TmpBuff, true);
                                //if (PlaceAttachInThreadRoutine == false)
                                {
                                    CodeGen.WriteLiteralNoIndent(TmpBuff, ProcessAttachCode);
                                }
                                CodeGen.WriteBreak(TmpBuff);
                                CodeGen.WriteRightBracket(TmpBuff, true);
                            }
                
                            CodeGen.WriteCase(TmpBuff, "DLL_PROCESS_DETACH");
                            {
                                CodeGen.WriteLeftBracket(TmpBuff, true);
                                //if (PlaceAttachInThreadRoutine == false)
                                {
                                    CodeGen.WriteLiteralNoIndent(TmpBuff, ProcessDetachCode);
                                }
                            CodeGen.WriteBreak(TmpBuff);
                                CodeGen.WriteRightBracket(TmpBuff, true);
                            }

                            CodeGen.WriteCase(TmpBuff, "DLL_THREAD_ATTACH");
                            {
                                CodeGen.WriteLeftBracket(TmpBuff, true);
                                //if (PlaceAttachInThreadRoutine == true)
                                {
                                    CodeGen.WriteLiteralNoIndent(TmpBuff, ThreadAttachCode);
                                }
                            CodeGen.WriteBreak(TmpBuff);
                                CodeGen.WriteRightBracket(TmpBuff, true);
                            }
            
                            CodeGen.WriteCase(TmpBuff, "DLL_THREAD_DETACH");
                            {
                                CodeGen.WriteLeftBracket(TmpBuff, true);
                                //if (PlaceAttachInThreadRoutine == true)
                                {
                                    CodeGen.WriteLiteralNoIndent(TmpBuff, ThreadDetachCode);
                                }
                            CodeGen.WriteBreak(TmpBuff);
                                CodeGen.WriteRightBracket(TmpBuff, true);
                            }


                        CodeGen.WriteRightBracket(TmpBuff, true);
                    }

                    CodeGen.EmitReturnX(TmpBuff, "Result");
                    CodeGen.WriteRightBracket(TmpBuff, true);
                    CodeGen.WriteNewLine(TmpBuff);

                }
       




                // convert to c# string and return
                byte[] Ret = new byte[TmpBuff.Length];
                TmpBuff.Position = 0;
                TmpBuff.Read(Ret, 0, Ret.Length);
                return CodeGen.TargetEncoding.GetString(Ret);
            }                       
 

        }
      
    }
}
