﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DebugDotNet {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class StringMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal StringMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DebugDotNet.StringMessages", typeof(StringMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Can&apos;t Start the Process. It&apos;s already running..
        /// </summary>
        internal static string DebugEventWorkerThreadAlreadyRunning {
            get {
                return ResourceManager.GetString("DebugEventWorkerThreadAlreadyRunning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The internal list of debug targets is blank?.
        /// </summary>
        internal static string DebugEventWorkerThreadEmptyInternalList {
            get {
                return ResourceManager.GetString("DebugEventWorkerThreadEmptyInternalList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Can&apos;t Set Single Thread Mode After calling start for this instance of DebugEventWorkerThread.
        /// </summary>
        internal static string DebugEventWorkerThreadSetSingleThreadModeAfteStart {
            get {
                return ResourceManager.GetString("DebugEventWorkerThreadSetSingleThreadModeAfteStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Target Process already exited .
        /// </summary>
        internal static string DebugHelpLibraryProcessExited {
            get {
                return ResourceManager.GetString("DebugHelpLibraryProcessExited", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DebugProcess class does not support specifying a username.
        /// </summary>
        internal static string DebugProcessNoUserNameAllowed {
            get {
                return ResourceManager.GetString("DebugProcessNoUserNameAllowed", resourceCulture);
            }
        }
    }
}
