using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PatNet.Lib
{
    public class AmendmentManifestFile
    {
        private readonly string _filename;
        private readonly string _workingDir;
        private readonly Dictionary<string, AmendmentFile> _amendments;
        public Dictionary<string, AmendmentFile> Amendments
        {
            get { return _amendments; }
        }
        public AmendmentManifestFile(string filename, string workingDir)
        {
            _filename = filename;
            _workingDir = workingDir;
            _amendments = new Dictionary<string, AmendmentFile>();
        }



        public bool Validate()
        {            
            //WARN: on missing files
            //ERROR: on too-many file (you're not on the manifest!)
            Console.WriteLine("Processing Shipment A File - {0}",_filename);
            var retVal = true;
            
            using (TextReader tr = File.OpenText(_workingDir + @"\" + _filename))
            {
                string line;

                while ((line = tr.ReadLine()) != null)
                {
                    if (line.ToUpper().Contains(".AMD"))
                    {
                        var amd = line.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1];
                        Amendments.Add( amd, new AmendmentFile(){ FileName = amd, Name = amd.Replace(".amd", "") , IsPresent = false});
                    }
                }
            }

            DirectoryInfo d = new DirectoryInfo(_workingDir);
            var allFiles = d.GetFiles("*.amd");

            foreach (var f in allFiles)
            {
                if (Amendments.ContainsKey(f.Name))
                {
                    Amendments[f.Name].IsPresent = true;
                }
                else
                {
                    Trace.WriteLine("File " + f + " was not in the ShipA Manifest and shouldn't be here!", TraceLogLevels.ERROR);
                    retVal = false;
                }
            }

            foreach (var a in Amendments.Values)
            {
                if( !a.IsPresent) 
                    Trace.WriteLine("File " + a.FileName + " was missing form the Shipment directory!", TraceLogLevels.WARN);
            }

            Trace.WriteLine("Processed " + Amendments.Count + " amendments!");
            return retVal;
        }
    }
}