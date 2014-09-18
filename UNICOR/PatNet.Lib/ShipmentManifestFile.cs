using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PatNet.Lib
{
    public class ShipmentManifestFile
    {
        private readonly string _filename;
        private readonly string _workingDir;
        private readonly Dictionary<string, ShipmentFile> _shipmentFiles;
        public Dictionary<string, ShipmentFile> ShipmentFiles { get { return _shipmentFiles; } }

        public ShipmentManifestFile(string filename, string workingDir)
        {
            _filename = filename;
            _workingDir = workingDir;
            _shipmentFiles = new Dictionary<string, ShipmentFile>();
        }

        public bool Validate()
        {
            var retVal = true;
            Trace.WriteLine("Processing Shipment Manifest [" + _filename + "]", TraceLogLevels.INFO);
            using (TextReader tr = new StreamReader(_workingDir + @"\" + _filename))
            {
                //EXAMPLE: 1. ShipT4853\13201171.001; Pgs = 32; Header Claims = 5  
                string line;
                int linecount = 0;
                while ((line = tr.ReadLine()) != null)
                {
                    linecount++;
                    if (line.Contains("Pgs") && line.Contains("Header Claims"))
                    {
                        var sections = line.Split(';');
                        if (sections.Count() > 2)
                        {
                            var fileStats = new ShipmentFile
                            {
                                Name =
                                    sections[0].Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1]
                                        .Replace(
                                            ".001", ""),
                                PageCount = Int32.Parse(sections[1].Split('=')[1].Trim()),
                                HeaderClaims = Int32.Parse(sections[2].Split('=')[1].Trim())
                            };
                            fileStats.FileName = fileStats.Name + ".dta";
                            _shipmentFiles.Add(fileStats.FileName, fileStats);
                        }
                        else
                        {
                            Trace.WriteLine("Patent File line was formatted incorrectly! Line " + linecount, TraceLogLevels.ERROR);
                        }
                    }
                }
            }

            DirectoryInfo d = new DirectoryInfo(_workingDir);
            var allFiles = d.GetFiles("*.dta");
            foreach (var f in allFiles)
            {
                if (_shipmentFiles.ContainsKey(f.Name))
                {
                    _shipmentFiles[f.Name].IsPresent = true;
                }
                else
                {
                    Trace.WriteLine("File " + f + " was not in the ShipT Manifest and shouldn't be here!", TraceLogLevels.ERROR);
                    retVal = false;
                }
            }

            foreach (var a in _shipmentFiles.Values)
            {
                if (!a.IsPresent)
                    Trace.WriteLine("File " + a.FileName + " was missing form the Shipment directory!", TraceLogLevels.WARN);
            }
           

            //check if images are present according to count
            foreach (var f in _shipmentFiles)
            {
                var shipment = f.Value;
                if (f.Value.IsPresent)
                {
                    var c = Directory.GetFiles(_workingDir + @"\" + shipment.Name).Count();
                    if (c != shipment.PageCount)
                        Trace.WriteLine(
                            String.Format("Missing Image files for patent {0}. Expected {1} but had {2}!", shipment.Name,
                                shipment.PageCount, c),
                            TraceLogLevels.ERROR
                            );
                }              
            }
            Trace.WriteLine("Processed " + ShipmentFiles.Count + " patents!");
            return retVal;
        }
    }
}