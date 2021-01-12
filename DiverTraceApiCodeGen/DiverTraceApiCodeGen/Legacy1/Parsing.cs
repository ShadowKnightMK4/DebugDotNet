using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiverTraceApiCodeGen
{
    /// <diver>
    /// 
    /// <Routine>
    ///     <TargetName="IsDebuggerPresent"></TargetName>
    ///     <ImportType="Static|GetProcAddress|OnDemand"> </ImportType>
    ///     <Library>Kernel32.dll</Library>
    ///     <FunctioGeneration>
    ///         <DebugString>
    ///             <OnFuncEnter></OnFuncEnter>             include a write to OutputDebugStringW() with function name
    ///             <Arguments></Arguments>                 include a write to OutputDebugStrngW() with function arguments
    ///             <IncludeDetourLinkageMessages>                  the detourattach routine includes debugoutput
    ///             </IncludeDetourLinkageMessages>
    ///         </DebugString> 
    ///         <Detour>
    ///         <style="RequiredToWork|Optional"> </style>          Required means DLL will not loaf function does not link
    ///                                                             Option means its fine
    ///         <DisableToSmall="true|false"></DisableToSmall>      -- the detour function also contains call to disable fail to small code
    ///         <ReportLinkFail="DebugString|DiverCall></ReportLinkFail>                   -- a call to outputdebugstring is done on code failure to link
    ///                                                                                     -- if set to DiverCall the routine calls RaiseDiverDetourFail()
    ///                                                                                      -- should IncludeDiver not be set, this value is treated as DebugString
    ///         </Detour>
    ///         <IncludeDiver>"true"</IncludeDiver>     <!--This tells the code to include diver protocol-->
    ///     </FunctioGeneration>
    ///     </ImportType>
    /// </Routine>
    /// 
    /// </diver>
    ///
    /// ImportType 
    /// Static                  =>  Direct Assignment in the source with a typedef ptr. This requires the DLL to be compiled with staticlib that imports routine
    /// GetProcAddress          => Code Requires specific library specified. Detouring is done in SeperasteThread
    /// OnDemand                => Code Overwrites LoadLibraryXXX stuff and detours routine upon attempt to Load that specific library.
    /// 
    ///

    /// <summary>
    /// Parse an input file to get output
    /// </summary>
    class Parsing
    {
    }
}
