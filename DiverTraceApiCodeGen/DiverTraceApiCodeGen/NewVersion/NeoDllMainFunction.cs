using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiverTraceApiCodeGen.NewVersion
{

    public enum NeoDiverDebugStringChannel
    {
        /// <summary>
        /// Uses OutputDebugStringW()
        /// </summary>
        AlwaysDebugString = 0,
        /// <summary>
        /// Usually Useful info
        /// </summary>
        Information = 1,
        /// <summary>
        /// Errors in doing something
        /// </summary>
        Errors = 2,
        /// <summary>
        /// Critical errors that result in failure of the DLL
        /// </summary>
        Fatal = 3

    }
    /// <summary>
    /// A Class that handles Generated the DllMain Function. This is effectually the root of the NeoDiver Code Generator
    /// </summary>
    public class NeoDllMainFunction: NeoNativeFunction
    {


        private List<string> ProjectIncludesBacking = new List<string>();
        /// <summary>
        /// Contains a list of things generated to be included at the top, such as "myprojectspecificfile.h"
        /// </summary>
        public List<string> ProjectIncludes
        {
            get
            {
                if (ProjectIncludesBacking == null)
                {
                    ProjectIncludesBacking = new List<string>();
                }
                return ProjectIncludesBacking;
            }
        }


        private List<string> StandardInludesBacking = new List<string>();
        /// <summary>
        /// Contains a list of things to include that are part of the standard includes such as "windows.h"
        /// </summary>
        public List<string> StandardInclude
        {
            get
            {
                if (StandardInludesBacking == null)
                {
                    StandardInludesBacking = new List<string>();
                }
                return StandardInludesBacking;
            }
        }



        private List<string> NameSpace_CPPBacking = new List<string>();
        /// <summary>
        /// contains a list of CPP name spaces to emit as using. Is emitted as "using namespace X" where X is each entry in this once.
        /// </summary>
        public List<string> NameSpaceList
        {
            get
            {
                if (NameSpace_CPPBacking == null)
                {
                    NameSpace_CPPBacking = new List<string>();
                }
                return NameSpace_CPPBacking;
            }
        }


        /// <summary>
        /// Emit the contained include statements and name space statements
        /// </summary>
        /// <returns>Emits code that includes the standard includes, then project includes, then using name space statements in that order</returns>
        public string GeneratePrefixStuff()
        {
            using (MemoryStream Output = new MemoryStream())
            {
                StandardInclude.ForEach(p => { CodeGen.EmitStandardInclude(Output, p); });
                ProjectIncludes.ForEach(p => { CodeGen.EmitProjectInclude(Output, p); });
                NameSpaceList.ForEach(p => { CodeGen.WriteUsingNameSpace(Output, p); });
                return CodeGen.MemoryStreamToString(Output);
            }
        }
        /// <summary>
        /// This instructs <see cref="NeoDllMainFunction"/> in how to generate the routine. You may use Default as a shortcut for the static Default value
        /// </summary>
        [Flags]
        public enum DetourSettings
        { 
            // Default Mode
            Default = 0,
            // message is sent to debugger showing the routine name that was not detoured
            ReportDetourErrors = 1,
            // message sent to debugger showing the routine name that was detoured
            ReportDetourSuccess = 2,

            /// <summary>
            /// DllMain() will include routines to call the Attach Code
            /// </summary>
            WantAttach = 4,
            /// <summary>
            /// DllMain() will include routines to call detach code
            /// </summary>
            WantDeattach = 8,
            /// <summary>
            /// The Generated code will be placed in the DLL_PROCESS_ATTACH entry
            /// </summary>
            WantProcessLevel = 16,
            /// <summary>
            /// The generated code will be in DLL_THREAD_ATTACH Entry
            /// </summary>
            WantThreadLevel = 32,

            /// <summary>
            /// Not currently used
            /// </summary>
            StaticLink = 64,
            /// <summary>
            /// Not currently used
            /// </summary>
            DynamicLink = 128,

            /// <summary>
            /// If set the DLL in pinned in memory and detach code is NOT generated as the DLL will never be unloaded the normal way by Windows API. To Require this feature set <see cref="RequirePin"/>
            /// </summary>
            PinDllInMemory = 256,

            /// <summary>
            /// if set with <see cref="PinDllInMemory"/>, this generates code to tell Windows the DLL did not load properly if it can't be pinned. Ignored otherwise
            /// </summary>
            RequirePin = 512,

            /// <summary>
            /// Triggers a cause to report with OutputDebugString() or the diver version of it should the pin fail, requires <see cref="PinDllInMemory"/> set
            /// </summary>
            ReportPinFailure = 1024,

            /// <summary>
            /// Reports pin success with OutputDebugString() or the diver version if the pin works. Requires <see cref="PinDllInMemor"y/> set
            /// </summary>
            ReportPinOK = 2048,

            PerfectDetour = 4096,
        }

        /// <summary>
        /// The default settings if leaving the <see cref="NeoDllMainFunction.CurrentDetourMode"/> set to <see cref="DetourSettings.Default"/>
        /// </summary>
        public static DetourSettings DefaultSettings
        {
            get
            {
                return DetourSettings.PerfectDetour | DetourSettings.PinDllInMemory | DetourSettings.ReportDetourErrors | DetourSettings.ReportPinFailure | DetourSettings.ReportPinOK | DetourSettings.WantAttach | DetourSettings.WantProcessLevel | DetourSettings.RequirePin;
            }
        }


        public DetourSettings CurrentDetourMode { get; set; } = DetourSettings.Default;

        #region inherits from NeoNativeFunction that are not currently used / are meaningless

        /// <summary>
        /// Inherited from <see cref="NeoNativeFunction"/>, this is not used and will always be false
        /// </summary>
        public override bool AttachFuncCallDetourIgnoreToSmall
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// inherited from <see cref="NeoNativeFunction"/>, this currently is not used but may be changed
        /// </summary>
        public override int AttachFuncDebugStringChannel
        {
            get; set;
        }

        /// <summary>
        /// inherited from <see cref="NeoNativeFunction"/>, this is not used  and will always be false
        /// </summary>
        public override bool AttachFuncOnlyLogErrors
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///  inherited from <see cref="NeoNativeFunction"/>, currently meaningless
        /// </summary>
        public override string AttachFunctionName { get => base.AttachFunctionName; set => base.AttachFunctionName = value; }

        /// <summary>
        ///  inherited from <see cref="NeoNativeFunction"/>, currently meaningless. Will always be false
        /// </summary>
        public override bool JustCallIt
        {
            get
            {
                return false;
            }
            set
            {
                
            }
        }

        /// <summary>
        /// not used
        /// </summary>
        public override bool ReplaceFuncWantDiver { get => base.ReplaceFuncWantDiver; set => base.ReplaceFuncWantDiver = value; }


        /// <summary>
        /// not used
        /// </summary>
        public override string DynamicLinkDllName { get => base.DynamicLinkDllName; set => base.DynamicLinkDllName = value; }


        /// <summary>
        ///  not used
        /// </summary>
        public override string OriginalFunctionName { get => base.OriginalFunctionName; set => base.OriginalFunctionName = value; }


        /// <summary>
        /// not used
        /// </summary>
        public override string OriginalFunctionNamePtr { get => base.OriginalFunctionNamePtr; set => base.OriginalFunctionNamePtr = value; }


        /// <summary>
        /// not used
        /// </summary>
        public override string OriginalFunctionNamePtrType { get => base.OriginalFunctionNamePtrType; set => base.OriginalFunctionNamePtrType = value; }


        /// <summary>
        ///  not used
        /// </summary>
        public override bool OutputDebugArguments { get => base.OutputDebugArguments; set => base.OutputDebugArguments = value; }

        /// <summary>
        /// not used 
        /// </summary>
        public override bool OutputDebugName { get => base.OutputDebugName; set => base.OutputDebugName = value; }
        #endregion


        /// <summary>
        /// DllMain has a specific set of arguments that cannot be changed. You can get a copy of them.
        /// </summary>
        public override List<NeoNativeFunctionArg> Arguments
        { 
            get
            {
                List<NeoNativeFunctionArg> ret = new List<NeoNativeFunctionArg>();
                ret.AddRange(base.Arguments);
                return ret;
            }
        }


        /// <summary>
        ///  Instance a Specialized <see cref="NeoNativeFunction"/> that will generate the Diver's DllMain function.
        /// </summary>
        public NeoDllMainFunction()
        {
            base.Arguments.Add(new NeoNativeFunctionArg("HINSTANCE", "hinstDLL", NeoNativeTypeData.U4));
            base.Arguments.Add(new NeoNativeFunctionArg("DWORD", "fdwReason", NeoNativeTypeData.U4));
            base.Arguments.Add(new NeoNativeFunctionArg("LPVOID", "lpReserved", NeoNativeTypeData.U4));
            base.ReturnValue = new NeoNativeFunctionArg("BOOL", NeoNativeTypeData.Bool);
            RoutineName = "DllMain";

        }

        /// <summary>
        /// Allows access to the list of routines this will be detouring.
        /// </summary>

        public List<NeoNativeFunction> DetourThese
        { 
            get
            {
                if (DetourTheseBacking == null)
                {
                    DetourTheseBacking = new List<NeoNativeFunction>();
                }
                return DetourTheseBacking;
            }
            
        }


        #region partial access to the private variable of DetourTheseBacking
        /// <summary>
        /// add a list of routines to the list we use
        /// </summary>
        /// <param name="Routines">non null list of routines</param>
        public void AddList(List<NeoNativeFunction> Routines)
        {
            if (Routines == null)
            {
                throw new ArgumentNullException(nameof(Routines));
            }

            Routines.ForEach(p => { DetourTheseBacking.Add(p); });
        }

        /// <summary>
        /// Wipes the list of routines we are to be detouring
        /// </summary>
        public void ClearRoutines()
        {
            DetourTheseBacking.Clear();
        }

        /// <summary>
        /// add a routine to list of routines to detour / generate
        /// </summary>
        /// <param name="Fn">Non Null, Will contain the class containing the data for the routine to make</param>
        public void AddRoutineToDetour(NeoNativeFunction Fn)
        {
            if (Fn == null)
            {
                throw new ArgumentNullException(nameof(Fn));
            }
            DetourTheseBacking.Add(Fn);
        }
        /// <summary>
        /// add a routine to the list of routines to detour / generate
        /// </summary>
        /// <param name="Fn">Non Null, Will contain the class containing the data for the routine to make</param>
        public void RemoveRoutineToDetour(NeoNativeFunction Fn)
        {
            if (Fn == null)
            {
                throw new ArgumentNullException(nameof(Fn));
            }
            DetourTheseBacking.Remove(Fn);
        }

        /// <summary>
        /// private backing value for the public <see cref="DetourThese"/> property
        /// </summary>
        private List<NeoNativeFunction> DetourTheseBacking = new List<NeoNativeFunction>();
        #endregion



        
        public string GenerateReplacementFunctions()
        {
            using (MemoryStream Output = new MemoryStream())
            {
                int RoutineNumber = 1;
                CodeGen.WriteComment(Output, "The list of Replacement  function data is listed below this comment");
                CodeGen.WriteBeginCommentBlock(Output);
                DetourThese.ForEach(p => {
                    CodeGen.WriteLiteral(Output, string.Format(CultureInfo.InvariantCulture, "#{0},  {1}\r\n", RoutineNumber++, p.OriginalFunctionName));
                }
                );
                CodeGen.WriteEndCommentBlock(Output);

                CodeGen.WriteNewLine(Output);
                DetourThese.ForEach(
                    p => {
                        CodeGen.WriteLiteralNoIndent(Output, p.GenerateDetourFunction(), false);
                        CodeGen.WriteNewLine(Output);
                        CodeGen.WriteNewLine(Output);
                    }
                    );
                return CodeGen.MemoryStreamToString(Output);
            }
        }

        

        /// <summary>
        /// Generate the AttachFunction Data for each routine contained without <see cref="DetourThese"/> in one call and return the string containing the data
        /// </summary>
        /// <returns>a string containing the typedef info, function pointer info, and the AttachFunction body for each routine</returns>
        public string GenerateAttachFunctions()
        {
            using (MemoryStream Output = new MemoryStream())
            {
                int RoutineNumber = 1;
                CodeGen.WriteComment(Output, "The list of Attach function data is listed below this comment");
                CodeGen.WriteBeginCommentBlock(Output);
                DetourThese.ForEach(p => {
                    CodeGen.WriteLiteral(Output, string.Format(CultureInfo.InvariantCulture, "#{0},  {1}\r\n", RoutineNumber++, p.OriginalFunctionName));
                }
                );
                CodeGen.WriteEndCommentBlock(Output);
                CodeGen.WriteNewLine(Output);
                DetourThese.ForEach(
                    p =>{
                        CodeGen.WriteLiteralNoIndent(Output, p.GenerateAttachFunction(), false);
                        CodeGen.WriteNewLine(Output);
                        CodeGen.WriteNewLine(Output);
                    }
                    );
                return CodeGen.MemoryStreamToString(Output);
            }
        }

        private void AddDynamicLinkModeItems()
        {
            DetourThese.Add(new NeoLoadLibraryA());
            DetourThese.Add(new NeoLoadLibraryW());
            DetourThese.Add(new NeoLoadLibraryExW());
            DetourThese.Add(new NeoLoadLibraryExA());
        }
        /// <summary>
        /// make the Diver Output.cpp source file in one call after settings everything up
        /// </summary>
        /// <returns>results of <see cref="GenerateReplacementFunctions"/>, <see cref="GenerateAttachFunctions"/>, <see cref="GenerateFunction"/> in that order</returns>
        public string GenerateDiverSource()
        {
            bool ContainsOnDemand = false;
            DetourTheseBacking.ForEach(p =>
           {
              if (p.LinkMode == FunctionType.OnDemandLink)
               {
                   // at least one is.
                   ContainsOnDemand = true;
            
               }
           });
            
            if (ContainsOnDemand)
            {
                AddDynamicLinkModeItems();
            }
            using (MemoryStream Output = new MemoryStream())
            {
                CodeGen.WriteLiteralNoIndent(Output, GeneratePrefixStuff());
                CodeGen.WriteLiteralNoIndent(Output, GenerateReplacementFunctions());
                CodeGen.WriteLiteralNoIndent(Output, GenerateAttachFunctions());
                CodeGen.WriteLiteralNoIndent(Output, GenerateFunction());

                return CodeGen.MemoryStreamToString(Output);
            }
        }


        /// <summary>
        /// For <see cref="NeoDllMainFunction"/>, this is the same as calling <see cref="GenerateAttachFunctions"/>
        /// </summary>
        /// <returns></returns>
        public override string GenerateAttachFunction()
        {
            return GenerateAttachFunctions();
        }
        /// <summary>
        /// Generate the DllMain() function with the settings contained in <see cref="CurrentDetourMode"/>
        /// </summary>
        /// <returns></returns>
        public override string GenerateFunction()
        {
            DetourSettings TargetSettings;
            List<string> CallForAttach = new List<string>() { "TRUE" };
            List<string> CallForDetach = new List<string>() { "FALSE" };
            if (CurrentDetourMode == DetourSettings.Default)
            {
                TargetSettings = DefaultSettings;
            }
            else
            {
                TargetSettings = CurrentDetourMode;
            }

            using (MemoryStream Output = new MemoryStream())
            {

                CodeGen.EmitDeclareFunction(Output, ReturnValue.ArgType, CodeGen.EmitDeclareFunctionSpecs.WINAPI, RoutineName, ExtractArgumentTypes(base.Arguments), ExtractArgumentNames(base.Arguments));
                CodeGen.WriteNewLine(Output);
                CodeGen.WriteLeftBracket(Output, true);
                if (TargetSettings.HasFlag( DetourSettings.PinDllInMemory) )
                {
                    CodeGen.WriteComment(Output, "Code generated to Pin DLL. The static prefixes should allow the data to persist between calls.");
                    CodeGen.EmitDeclareVariable(Output, "static HMODULE", "PinHandle", "0");
                    CodeGen.EmitDeclareVariable(Output, "static BOOL", "PinCallResult", "FALSE");
                    CodeGen.WriteIf(Output, "PinCallResult == FALSE");
                    CodeGen.WriteLeftBracket(Output, true);
                     CodeGen.EmitCallFunction(Output, "GetModuleHandleExW", null, new List<string>() {"GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS || GET_MODULE_HANDLE_EX_FLAG_PIN ", "(LPCWSTR)"+ base.Arguments[0].ArgName, "PinHandle" }, "&PinCallResult");

                        if (TargetSettings.HasFlag(  DetourSettings.RequirePin) )
                        {
                            CodeGen.WriteIf(Output, "PinCallResult == FALSE");
                            CodeGen.WriteLeftBracket(Output, true);
                                if (TargetSettings.HasFlag(DetourSettings.ReportPinFailure))
                                {
                                    EmitCallDiverOutputDebugMessage("L\"Failed to PIN the Diver DLL in place.\"", true, 0, true);
                                }
                                CodeGen.EmitReturnX(Output, "FALSE");
                                
                            CodeGen.WriteRightBracket(Output, true);
                            CodeGen.WriteElse(Output);
                            CodeGen.WriteLeftBracket(Output, true);
                                if (TargetSettings.HasFlag(DetourSettings.ReportPinOK))
                                {
                                    EmitCallDiverOutputDebugMessage("L\"Diver DLL Pinned in place OK\"", true, 0, true);
                                }
                            CodeGen.WriteRightBracket(Output, true);
                    }

                    //CodeGen.WriteIf(Output, "PinCallResult == FALSE");
                    CodeGen.WriteRightBracket(Output, true);
                }
                else
                {
                    CodeGen.WriteComment(Output, "Code was not generated to Pin DLL. Warning: Premature unloaded may cause access violations exceptions");
                }

                CodeGen.EmitDeclareVariable(Output, "BOOL", "HelpCheck");

                CodeGen.WriteNewLine(Output);
                CodeGen.WriteComment(Output, "DLL is in a helper process. Per Detours Documentation the DLL should leave things along and let Detours Do its work.");
                CodeGen.EmitCallFunction(Output, "DetourIsHelperProcess", null, null, "HelpCheck");

                CodeGen.WriteIf(Output, "HelpCheck == true");
                CodeGen.WriteLeftBracket(Output, true);
              
                CodeGen.EmitReturnX(Output, "TRUE");
                CodeGen.WriteRightBracket(Output, true);


                


                CodeGen.WriteSwitch(Output, base.Arguments[1].ArgName);
                CodeGen.WriteLeftBracket(Output, true);
                    
                    CodeGen.WriteCase(Output, "DLL_PROCESS_ATTACH");
                    CodeGen.WriteLeftBracket(Output, true);
                        if (TargetSettings.HasFlag(DetourSettings.WantAttach) && ( (TargetSettings.HasFlag(DetourSettings.WantProcessLevel) == true) || (TargetSettings.HasFlag(DetourSettings.WantThreadLevel) == false) ) )
                        {
                        CodeGen.EmitDeclareVariable(Output, "bool", "result", "false");
                        CodeGen.EmitDeclareWideStringStream(Output, "Message", true);
                        DetourThese.ForEach(
                        p => {
                            CodeGen.WriteComment(Output, string.Format(CultureInfo.InvariantCulture, "Entry to attach to the routine named '{0}'", p.OriginalFunctionName));
                            CodeGen.EmitCallFunction(Output, p.AttachFunctionName, null, CallForAttach , "result");

                            if (TargetSettings.HasFlag( DetourSettings.ReportDetourErrors))
                            {
                                CodeGen.WriteComment(Output, "Code set to report when an error happens when detouring");
                                CodeGen.WriteIf(Output, "result == FALSE");
                                CodeGen.WriteLeftBracket(Output, true);
                                CodeGen.EmitInsertStream(Output, "Message", new List<string>() { "L\"The attach function for the routine \"", "L\"" + p.OriginalFunctionName + "\"", "L\" failed with the error code of \"", " GetLastError() ", "endl"  }, false);
                                EmitCallDiverOutputDebugMessage("Message" + CodeGen.GetStreamStringToStringPiece(), true, (int)NeoDiverDebugStringChannel.Errors, false);
                                CodeGen.EmitClearWideStreamStream(Output, "Message");
                                CodeGen.WriteRightBracket(Output, true);
                            }
                            if (TargetSettings.HasFlag(DetourSettings.ReportDetourSuccess))
                            {
                                CodeGen.WriteComment(Output, "Code set to report when success happens when detouring");
                                CodeGen.WriteIf(Output, "result == TRUE");
                                CodeGen.WriteLeftBracket(Output, true);
                                CodeGen.EmitInsertStream(Output, "Message", new List<string>() { "L\"The attach function for the routine \"", "L\"" + p.OriginalFunctionName + "\"", "L\" worked OK\""}, false);
                                EmitCallDiverOutputDebugMessage("Message" + CodeGen.GetStreamStringToStringPiece(), true, (int)NeoDiverDebugStringChannel.Errors, false);
                                CodeGen.EmitClearWideStreamStream(Output, "Message");
                                CodeGen.WriteRightBracket(Output, true);
                            }
                            if (TargetSettings.HasFlag(DetourSettings.PerfectDetour) )
                            {
                                CodeGen.WriteComment(Output, "Code generated with PerfectDetour set. This routine returns FALSE if any detour did not work. For DLLMain in DLL_PROCESS_ATTACH, that indicates failure to Load OK");
                                CodeGen.WriteIf(Output, "result == FALSE");
                                CodeGen.WriteLeftBracket(Output, true);
                                    CodeGen.EmitReturnX(Output, "FALSE");
                                CodeGen.WriteRightBracket(Output, true);
                            }

                            CodeGen.WriteComment(Output, string.Format(CultureInfo.InvariantCulture, "End Entry to attach to the routine named '{0}'", p.OriginalFunctionName));
                        }
                        );

                        }
                        CodeGen.WriteBreak(Output);
                    CodeGen.WriteRightBracket(Output, true);

                    CodeGen.WriteCase(Output, "DLL_PROCESS_DETACH");
                    CodeGen.WriteLeftBracket(Output, true);
                if (TargetSettings.HasFlag(DetourSettings.WantAttach) && ((TargetSettings.HasFlag(DetourSettings.WantProcessLevel) == true) || (TargetSettings.HasFlag(DetourSettings.WantThreadLevel) == false)))
                {
                    CodeGen.EmitDeclareVariable(Output, "bool", "result", "false");
                    CodeGen.EmitDeclareWideStringStream(Output, "Message", true);
                    DetourThese.ForEach(
                    p => {
                        CodeGen.WriteComment(Output, string.Format(CultureInfo.InvariantCulture, "Entry to detach to the routine named '{0}'", p.OriginalFunctionName));
                        CodeGen.EmitCallFunction(Output, p.AttachFunctionName, null, CallForDetach, "result");

                        if (TargetSettings.HasFlag(DetourSettings.ReportDetourErrors))
                        {
                            CodeGen.WriteComment(Output, "Code set to report when an error happens when detouring");
                            CodeGen.WriteIf(Output, "result == FALSE");
                            CodeGen.WriteLeftBracket(Output, true);
                            CodeGen.EmitInsertStream(Output, "Message", new List<string>() { "L\"The detach function for the routine \"", "L\"" + p.OriginalFunctionName + "\"", "L\" failed with the error code of \"", " GetLastError() ", "endl" }, false);
                            EmitCallDiverOutputDebugMessage("Message" + CodeGen.GetStreamStringToStringPiece(), true, (int)NeoDiverDebugStringChannel.Errors, false);
                            CodeGen.EmitClearWideStreamStream(Output, "Message");
                            CodeGen.WriteRightBracket(Output, true);
                        }
                        if (TargetSettings.HasFlag(DetourSettings.ReportDetourSuccess))
                        {
                            CodeGen.WriteComment(Output, "Code set to report when success happens when detouring");
                            CodeGen.WriteIf(Output, "result == TRUE");
                            CodeGen.WriteLeftBracket(Output, true);
                            CodeGen.EmitInsertStream(Output, "Message", new List<string>() { "L\"The detach function for the routine \"", "L\"" + p.OriginalFunctionName + "\"", "L\" worked OK\"" }, false);
                            EmitCallDiverOutputDebugMessage("Message" + CodeGen.GetStreamStringToStringPiece(), true, (int)NeoDiverDebugStringChannel.Errors, false);
                            CodeGen.EmitClearWideStreamStream(Output, "Message");
                            CodeGen.WriteRightBracket(Output, true);
                        }
                        if (TargetSettings.HasFlag(DetourSettings.PerfectDetour))
                        {
                            CodeGen.WriteComment(Output, "Code generated with PerfectDetour set. This routine returns FALSE if any detour did not work. For DLLMain in DLL_PROCESS_ATTACH, that indicates failure to Load OK");
                            CodeGen.WriteComment(Output, "This return value is ignored though when in DLL_PROCESS_DETACH");
                            CodeGen.WriteIf(Output, "result == FALSE");
                            CodeGen.WriteLeftBracket(Output, true);
                            CodeGen.EmitReturnX(Output, "FALSE");
                            CodeGen.WriteRightBracket(Output, true);
                        }

                        CodeGen.WriteComment(Output, string.Format(CultureInfo.InvariantCulture, "End Entry to attach to the routine named '{0}'", p.OriginalFunctionName));
                        CodeGen.WriteNewLine(Output);
                    }
                    );

                }
                CodeGen.WriteBreak(Output);
                    CodeGen.WriteRightBracket(Output, true);


                    CodeGen.WriteCase(Output, "DLL_THREAD_ATTACH");
                    CodeGen.WriteLeftBracket(Output, true);
                        CodeGen.WriteBreak(Output);
                    CodeGen.WriteRightBracket(Output, true);


                    CodeGen.WriteCase(Output, "DLL_THREAD_DETACH");
                    CodeGen.WriteLeftBracket(Output, true);
                        CodeGen.WriteBreak(Output);
                    CodeGen.WriteRightBracket(Output, true);

                // switch
                CodeGen.WriteRightBracket(Output, true);

                CodeGen.EmitReturnX(Output, "TRUE");
                // routine end
                CodeGen.WriteRightBracket(Output, true);
                
                return CodeGen.MemoryStreamToString(Output);
            }
        }

        /// <summary>
        /// returns the name of the Routine that will be generated. Will always be "DllMain"
        /// </summary>
        public override string RoutineName { get => base.RoutineName;       }
    }
}
