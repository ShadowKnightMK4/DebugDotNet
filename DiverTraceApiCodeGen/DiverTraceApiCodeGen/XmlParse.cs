using DiverTraceApiCodeGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DiverApiCodeGen
{
    /*
     * Parser for XML
     * 
     * <DRIVERCODEGEN_CONFIG>       <- contains config settings
     *           
     * </DRIVERCODEGEN_CONFIG>
     * <RoutineList>
     *      <Routine>
     *          <Name>
     * 
     * 
     * 
     */
    /// <summary>
    /// Parse an XML file that contains config settings to generate code
    /// <list type="bullet"></list>
    /// </summary>
    public sealed class DiverXmlReader: IDisposable
    {
        /// <summary>
        /// Underyline file
        /// </summary>
        FileStream Fn;
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// read the specifs at the target xml file and retern a DetoursCodeGen capabile of meeting them if pssible
        /// </summary>
        /// <param name="TargetFile"></param>
        /// <returns></returns>
        public DetoursCodeGen  ReadXmlSpecs(string TargetFile)
        {
            DetoursCodeGen ret = new DetoursCodeGen();

            using (XmlReader ReadMe = XmlReader.Create(TargetFile, new XmlReaderSettings()))
            {
                while (ReadMe.EOF != true)
                {
                    ReadMe.ReadStartElement("DIVERCODEFILE");
                    
                }
            }

            return null;
            
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            if (Fn != null)
            {
                Fn.Dispose();
                Fn = null;
            }
            IsDisposed = true;
        }
    }
}
