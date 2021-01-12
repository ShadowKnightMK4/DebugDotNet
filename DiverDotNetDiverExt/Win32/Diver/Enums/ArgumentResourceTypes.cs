using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiverDotNetDiverExt.Win32.Diver.Enums
{
   public enum ResourceType
    {
        /// <summary>
        /// The Handle is a Process Handle.
        /// </summary>
        Process = 2,
        /// <summary>
        /// The Handle is a file handle
        /// </summary>
        File = 3,
        /// <summary>
        /// The handle is a Pipe
        /// </summary>
        Pipe = 4,
        /// <summary>
        /// The handle is a registry key.
        /// </summary>
        RegistryKey = 8

    }
}
