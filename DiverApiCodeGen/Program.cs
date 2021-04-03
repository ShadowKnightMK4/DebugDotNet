using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiverTraceApiCodeGen.NewVersion;
using System.Xml.Serialization;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace DiverConsoleAppCompiler
{
    static class Program
    {
        
        enum Flags
        {
            XmlStubGenerator = 1
        }


        static Flags CodeCodeMode;

        static int XmlStubGenLen = 8;

        static void Usage()
        {

        }


        static void HandleArguments(string[] args)
        {
            if ( (args.Length <= 0) || (args == null))
            {
                throw new InvalidDataException();
            }
            else
            {
                for (int step = 0; step < args.Length;step++)
                {
                    if ( (args[step].StartsWith("/"))) 
                    {
                        switch (args[step].ToUpperInvariant())
                        {
                            case "/SOURCE:":
                            case "/GENXMLSTUB":
                                CodeCodeMode |= Flags.XmlStubGenerator;
                                break;
                            case "/XMLSTUBLEN":
                                if (step +1 >= args.Length)
                                {
                                    throw new InvalidOperationException();
                                }
                                else
                                {
                                    XmlStubGenLen = int.Parse(args[step + 1]);
                                }
                                break;

                        }
                    }
                }
            }
        }
        static int Main(string[] args)
        {
            try
            {
                HandleArguments(args);
            }
            catch (InvalidDataException)
            {
                Usage();
                return -99999;
            }


            return 0;
        }


    }
}
