using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using DebugEventDotNet;
using DebugDotNet.Win32.Structs;

namespace DebugDotNet
{

  
    /// <summary>
    /// Implements a Simple Debug Event watcher with <see cref="DebugEventWorkerThread"/>
    /// </summary>
    public class DebugWorldView :IDisposable
    {
        /// <summary>
        /// Handle to the Process we currently are debugging
        /// </summary>
        Process TrueHandle;
        /// <summary>
        /// used to resolve a bas address for a single dll name.
        /// </summary>
        Dictionary<IntPtr, string> ModuleBaseReference;
        /// <summary>
        /// The magic happens with this 
        /// </summary>
        DebugEventWorkerThread WorkerThread;

        /// <summary>
        /// Get if the object is disposed
        /// </summary>
        public bool IsDisposed { get; private set; }


        /// <summary>
        /// Finalize
        /// </summary>
        ~DebugWorldView()
        {
            Dispose(false);
        }
        /// <summary>
        /// Dispose implemntation
        /// </summary>
        /// <param name="Managed"></param>
        protected virtual void Dispose(bool Managed)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(DebugWorldView));
            }
            if (Managed)
            {
                ModuleBaseReference.Clear();
                ModuleBaseReference = null;
            }
            WorkerThread?.Dispose();
            IsDisposed = true;
        }

        /// <summary>
        /// Dispose of Resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Debug this precess with the specified creation settings
        /// </summary>
        /// <param name="Process">process to debug</param>
        /// <param name="Settings">The setting to use</param>
        public DebugWorldView(string Process, DebugEventWorkerThread.CreationSetting Settings)
        {
            TrueHandle = new Process();
            TrueHandle.StartInfo = new ProcessStartInfo(Process);
            TrueHandle.StartInfo.UseShellExecute = false;

            WorkerThread = new DebugEventWorkerThread(TrueHandle, Settings);
        }

        /// <summary>
        /// start debugging the passed process based on the settings
        /// </summary>
        public void Start()
        {
            WorkerThread.Start();
        }







    }
    
}
