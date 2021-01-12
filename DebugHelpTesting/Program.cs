using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebugDotNet.Win32.Symbols;

namespace DebugHelpTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            DebugHelpLibrary test = new DebugHelpLibrary();
            var VersionData = DebugHelpLibrary.GetDebugVersion();
            if (VersionData.HasValue)
            {
                Console.WriteLine(VersionData.ToString());
            }
        }
    }
}
