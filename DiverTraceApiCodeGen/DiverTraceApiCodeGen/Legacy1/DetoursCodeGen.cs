using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
// this file is intended for the detours part of code gen.

namespace DiverTraceApiCodeGen
{
    /// <summary>
    /// dictionary used by DiverCodeGen to Serialize Various Things
    /// </summary>
    [Serializable]
    public class DiverXmlDictionary :  Dictionary<string, string>, IXmlSerializable
    {

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            string key, val;
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            key = val = null;
            while (!reader.EOF)
            {
                reader.MoveToContent();
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == nameof(DiverXmlDictionary))
                    {
                        reader.Read();
                        reader.MoveToElement();
                       if (reader.LocalName == "DiverDictionary")
                        {
                            bool quit = false;
                            while (reader.Read() && !quit)
                            {
                                reader.MoveToElement();
                                switch (reader.NodeType)
                                {
                                    case XmlNodeType.EndElement:
                                        quit = true;
                                        break;
                                    case XmlNodeType.Element:
                                        key = reader.NamespaceURI;
                                        val = reader.Value;
                                        
                                        break;
                                    case XmlNodeType.Text:
                                        val = reader.Value;
                                        if (key != null)
                                            this.Add(key, val);
                                        break;
                                    default:
                                        // nothing
                                        break;
                                }
                            }
                        }
                        
                        
                        
                    }
                }
                
            }
            
        }

        public void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            writer.WriteStartElement("DiverDictionary");
                foreach (string key in Keys)
                {
                    writer.WriteElementString("key", key, this[key]);
                    
                }
            writer.WriteEndElement();
        }
    }
    public class DetoursCodeGen
    {
        public DllMainRoutine DllMainCodeGen { get; set; }
        public DetoursCodeGen()
        {
            DefaultSettings();
            DllMainCodeGen = new DllMainRoutine();
        }
        
        public DetoursCodeGen(bool DefaultsOK)
        {
            if (DefaultsOK)
            {
                DefaultSettings();
                DllMainCodeGen = new DllMainRoutine();
            }
        }

        /// <summary>
        /// This list contains *all* the functions that will be detoured 
        /// </summary>
        public List<NativeFunctionClass> FunctionList { get; } = new List<NativeFunctionClass>();
        /// <summary>
        /// list of all include files that are included after standard
        /// </summary>
        public List<string> ProjectIncludes { get;  } = new List<string>();
        /// <summary>
        /// list of include files 
        /// </summary>
        public List<string> StandardIncludes { get;  } = new List<string>();
        /// <summary>
        /// contains all name spaces used
        /// </summary>
        public List<string> UsingNameSpaces { get;  } = new List<string>();

        /// <summary>
        /// #pragma lib lists
        /// </summary>
        public List<string> PragmaLibs { get; } = new List<string>();



        // if any attach call returns false, the DLL returns fail to load
        public bool NeedPerfectAttach { get; set; } = true;

        

        /// <summary>
        /// Same as using <see cref="DllMainRoutine.EnableThreadedAttachesKey"/> with the Template collection"/>
        /// </summary>
        public bool WantPerThreadDetourAttach
        {
            get
            {
                string check;
                try
                {
                    check = DllMainCodeGen[DllMainRoutine.EnableThreadedAttachesKey];
                }
                catch (KeyNotFoundException)
                {
                    DllMainCodeGen.TemplateArgs.Add(DllMainRoutine.EnableThreadedAttachesKey, "0");
                    return false;
                }
                if (check == null)
                {
                    return false;
                }
                if (check.Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;

            }
            set
            {
                string dif;
                if (value)
                {
                    dif = "1";
                }
                else
                {
                    dif = "0";
                }

                if (DllMainCodeGen == null)
                {
                    DllMainCodeGen = new DllMainRoutine();
                }
                {
                    try
                    {
                        DllMainCodeGen.TemplateArgs[DllMainRoutine.EnableThreadedAttachesKey] = dif;
                    }
                    catch (KeyNotFoundException)
                    {
                        DllMainCodeGen.TemplateArgs.Add(DllMainRoutine.EnableThreadedAttachesKey, "1");
                    }
                }
            }
        }

   
        /// <summary>
        /// Emit the code contained within this class
        /// </summary>
        public void EmitCodeLecacy(Stream Output)
        {
            // holds a collection of functions that will need LoadLibraryXXX overwritten to auto detour
            /*
             *      LoadLibraryExW and LoadLibraryW will contain the detouring code
             *      LoadLibraryA and LoadLibraryExA will just convert the passed string to unicode and call its compatriets
             */
            List<NativeFunctionClass> OnDemandDetour = new List<NativeFunctionClass>();
            /*
             *  holds a collection of functions that staticly import the detoured routine. 
             *  They need to special privs
             */
            List<NativeFunctionClass> StaticDetour = new List<NativeFunctionClass>();
            /*
             * holds a list of Functions that will have load library called at the start.
             * This list is hard coded to accept kernel32.dll and ntdll.dll as valid dll names.
             * Specifiying another means It gets placed on the OnDemandDetourBucket
             */ 
            List<NativeFunctionClass> DllMainLink = new List<NativeFunctionClass>();
            List<string> AttachFuncCache = new List<string>();
            // emit the include and pragma stuff.
            EmitIncludeFileList();

            // first pass is checking if there's any routines that have loadlibrary mode set
            foreach (NativeFunctionClass Fn in FunctionList)
            {
                var Dummy_ = Fn.GenerateAttachFunction();
                switch (Dummy_[DetourAttachFunction.DetourOriginalSource])
                {
                    case DetourAttachFunction.DetourOriginalSourceLoadLibraryOnStart:
                        throw new NotImplementedException(DetourAttachFunction.DetourOriginalSourceLoadLibraryOnStart);
                        {
                            if (string.Equals((Dummy_[DetourAttachFunction.DetourSourceDllName]), "kernel32.dll", StringComparison.OrdinalIgnoreCase) == false)
                            {
                                if (string.Equals((Dummy_[DetourAttachFunction.DetourSourceDllName]), "ntdll.dll", StringComparison.OrdinalIgnoreCase) == false)
                                {
                                    // we fallback to on demand
                                    OnDemandDetour.Add(Fn);
                                }
                                else
                                {
                                    DllMainLink.Add(Fn);
                                }
                            }
                            else
                            {
                                DllMainLink.Add(Fn);
                            }
                            break;
                        }
                    case DetourAttachFunction.DetourOriginalSourceLoadLibraryOnDemand:
                        {
                            throw new NotImplementedException(DetourAttachFunction.DetourOriginalSourceLoadLibraryOnDemand);
                            OnDemandDetour.Add(Fn);
                            break;
                        }
                    case DetourAttachFunction.DetourOriginalSourceDirectAssigment:
                        {
                            StaticDetour.Add(Fn);
                            break;
                        }
                }

            }

            if (OnDemandDetour.Count > 0)
            {
                
                NativeFunctionClass LoadLibraryA = new NativeFunctionClass();
                NativeFunctionClass LoadLibraryW = new NativeFunctionClass();
                NativeFunctionClass LoadLibraryExA = new NativeFunctionClass();
                NativeFunctionClass LoadLibraryExW = new NativeFunctionClass();

                StaticDetour.Add(LoadLibraryW);
                StaticDetour.Add(LoadLibraryExA);
                StaticDetour.Add(LoadLibraryExW);
                StaticDetour.Add(LoadLibraryA);

                LoadLibraryA.Arguments.Add(new NativeFunctionArg("LPCSTR", "lpLibName", System.Runtime.InteropServices.UnmanagedType.LPStr));
                LoadLibraryA.ReturnType = new NativeFunctionArg("HMODULE", System.Runtime.InteropServices.UnmanagedType.SysInt);
                LoadLibraryA.HowToGetSourceFunction = NativeFunctionClass.LinkMode.Static;
                LoadLibraryA.FunctionName = "LoadLibraryA";
                LoadLibraryA.PreferredTypeDefName = "LoadLibraryA_DynamicLink";


                LoadLibraryW.Arguments.Add(new NativeFunctionArg("LPWCSTR", "lpLibName", System.Runtime.InteropServices.UnmanagedType.LPWStr));
                LoadLibraryW.ReturnType = new NativeFunctionArg("HMODULE", System.Runtime.InteropServices.UnmanagedType.SysInt);
                LoadLibraryW.HowToGetSourceFunction = NativeFunctionClass.LinkMode.Static;
                LoadLibraryW.FunctionName = "LoadLibraryW";
                LoadLibraryW.PreferredTypeDefName = "LoadLibraryW_DynamicLink";

                LoadLibraryExA.Arguments.Add(new NativeFunctionArg("LPCSTR", "lpLibName", System.Runtime.InteropServices.UnmanagedType.LPStr));
                LoadLibraryExA.Arguments.Add(new NativeFunctionArg("HANDLE", "hFile", System.Runtime.InteropServices.UnmanagedType.U4));
                LoadLibraryExA.Arguments.Add(new NativeFunctionArg("DWORD", "dwFlags", System.Runtime.InteropServices.UnmanagedType.U4));
                LoadLibraryExA.ReturnType = new NativeFunctionArg("HMODULE", System.Runtime.InteropServices.UnmanagedType.SysInt);
                LoadLibraryExA.HowToGetSourceFunction = NativeFunctionClass.LinkMode.Static;
                LoadLibraryExA.FunctionName = "LoadLibraryExA";
                LoadLibraryExA.PreferredTypeDefName = "LoadLibraryExA_DynamicLink";

                LoadLibraryExW.Arguments.Add(new NativeFunctionArg("LPWCSTR", "lpLibName", System.Runtime.InteropServices.UnmanagedType.LPStr));
                LoadLibraryExW.Arguments.Add(new NativeFunctionArg("HANDLE", "hFile", System.Runtime.InteropServices.UnmanagedType.U4));
                LoadLibraryExW.Arguments.Add(new NativeFunctionArg("DWORD", "dwFlags", System.Runtime.InteropServices.UnmanagedType.U4));
                LoadLibraryExW.ReturnType = new NativeFunctionArg("HMODULE", System.Runtime.InteropServices.UnmanagedType.SysInt);
                LoadLibraryExW.HowToGetSourceFunction = NativeFunctionClass.LinkMode.Static;
                LoadLibraryExW.FunctionName = "LoadLibraryExW";
                LoadLibraryExW.PreferredTypeDefName = "LoadLibraryExW_DynamicLink";

                LoadLibraryW.FuncStyle = NativeFunctionClass.EmitStyles.IncludeDiverCode | NativeFunctionClass.EmitStyles.DebugStringArgs | NativeFunctionClass.EmitStyles.DebugStringNameOnly | NativeFunctionClass.EmitStyles.LoadLibraryCheckInternal;
                LoadLibraryExA.FuncStyle = LoadLibraryExW.FuncStyle = LoadLibraryA.FuncStyle = LoadLibraryW.FuncStyle;



            }

            foreach (NativeFunctionClass Fn in StaticDetour)
            {
                var Attach = Fn.GenerateAttachFunction();
                string Typedef_VarName;
                string Typedef_VarType;
                CodeGen.WriteComment(Output, "The Below typedef and variable receive a pointer to the original function of " + Fn.FunctionName);
                if (string.IsNullOrEmpty(Fn.PreferredTypeDefName))
                {
                    Typedef_VarName = Fn.FunctionName + "_Ptr";
                    Typedef_VarType = Fn.FunctionName + "_PtrType";
                }
                else
                {
                    Typedef_VarName = Fn.PreferredTypeDefName;
                    Typedef_VarType = Fn.PreferredTypeDefName + "_PtrType";
                    if (Typedef_VarType == Typedef_VarName)
                    {
                        Typedef_VarType += "1";
                    }
                }

                Attach[DetourAttachDetach.TypeDefVarName] = Typedef_VarName;
                Attach[DetourAttachDetach.TypeDefVarType] = Typedef_VarType;


                Fn.EmitTypedefFunction(Output, Typedef_VarType);
                CodeGen.EmitDeclareVariable(Output, Typedef_VarType, Typedef_VarName);
                Fn.EmitDetourFunction(Output, Attach[DetourAttachFunction.PointerContainerNameKey]);

                AttachFuncCache.Add(Attach.TemplateArgs[FunctionPiece.RoutineNameKey]);

                CodeGen.WriteLiteralNoIndent(Output, Attach.GetFinalBuild());
            }

            foreach (NativeFunctionClass Fn in DllMainLink)
            {

            }

            //
            {
                
                DynamicDetourName DetourThis = new DynamicDetourName();

                DetourThis.OnDemandLinks = OnDemandDetour;
                CodeGen.WriteLiteralNoIndent(Output, DetourThis.GetFinalBuild());
            }
            /*
                foreach (NativeFunctionClass Fn in FunctionList)
            {
                var Attach = Fn.GenerateAttachFunction();
                string Typedef_VarName;
                string Typedef_VarType;
                CodeGen.WriteComment(Output, "The Below typedef and variable receive a pointer to the original function of " + Fn.FunctionName);
                if (string.IsNullOrEmpty(Fn.PreferredTypeDefName))
                {
                    Typedef_VarName = Fn.FunctionName + "_Ptr";
                    Typedef_VarType = Fn.FunctionName + "_PtrType";
                }
                else
                {
                    Typedef_VarName = Fn.PreferredTypeDefName;
                    Typedef_VarType = Fn.PreferredTypeDefName + "_PtrType";
                    if (Typedef_VarType == Typedef_VarName)
                    {
                        Typedef_VarType += "1";
                    }
                }

                Attach[DetourAttachDetach.TypeDefVarName] = Typedef_VarName;
                Attach[DetourAttachDetach.TypeDefVarType] = Typedef_VarType;
                

                Fn.EmitTypedefFunction(Output, Typedef_VarType);
                CodeGen.EmitDeclareVariable(Output, Typedef_VarType, Typedef_VarName);
                Fn.EmitDetourFunction(Output, Attach[DetourAttachFunction.PointerContainerNameKey]);

                AttachFuncCache.Add(Attach.TemplateArgs[FunctionPiece.RoutineNameKey]);

                CodeGen.WriteLiteralNoIndent(Output, Attach.GetFinalBuild());
            }*/

            using (var TmpBuff = new MemoryStream())
            {

                foreach (string Fn in AttachFuncCache)
                {
                    // NOTE: result is already declared in the dllmain code gen. 
                    // this code here is sandwiched in a case block in that statement
                    CodeGen.EmitCallFunction(TmpBuff, Fn, null, null, "Result");
                    CodeGen.WriteIf(TmpBuff, "Result == FALSE");
                    {
                        CodeGen.WriteLeftBracket(TmpBuff, true);
                        if (NeedPerfectAttach)
                        {
                            CodeGen.EmitReturnX(TmpBuff, "Result");
                        }
                        CodeGen.WriteRightBracket(TmpBuff, true);
                    }

                }

                TmpBuff.Position = 0;
                byte[] Data = new byte[TmpBuff.Length];
                TmpBuff.Read(Data, 0, (int)TmpBuff.Length);
                string finalbuff = CodeGen.TargetEncoding.GetString(Data);
                if (WantPerThreadDetourAttach)
                {
                    DllMainCodeGen.ThreadAttachCode = finalbuff;
                    DllMainCodeGen.ProcessAttachCode = "Result = TRUE;";
                }
                else
                {
                    DllMainCodeGen.ProcessAttachCode = finalbuff;
                }
            }

            CodeGen.WriteLiteral(Output, DllMainCodeGen.GetFinalBuild());
            
            

          /// <summary>
          /// Writes a series of include statements and pragam statements to the output buffer. Assigns
          /// </summary>
            void EmitIncludeFileList()
            {

                foreach (string HardLib in PragmaLibs)
                {
                    CodeGen.WritePragmaLib(Output, HardLib);
                }

                foreach (string StandardInc in StandardIncludes)
                {
                    CodeGen.EmitStandardInclude(Output, StandardInc);
                }

                foreach (string ProjectInc in ProjectIncludes)
                {
                    CodeGen.EmitProjectInclude(Output, ProjectInc);
                }

                CodeGen.WriteNewLine(Output);

                foreach (string Name in UsingNameSpaces)
                {
                    CodeGen.WriteUsingNameSpace(Output, Name);
                }
            }
        }
        private void DefaultSettings()
        {
            UsingNameSpaces.Clear();
            UsingNameSpaces.Add("std");

            StandardIncludes.Clear();
            StandardIncludes.Add("windows.h");
            StandardIncludes.Add("iostream");

            ProjectIncludes.Clear();
            ProjectIncludes.Add("detours.h");
            
            PragmaLibs.Clear();
            PragmaLibs.Add("detours.lib");

            DllMainCodeGen = new DllMainRoutine();
            DllMainCodeGen.Enabled = true;







        }
    }
}
