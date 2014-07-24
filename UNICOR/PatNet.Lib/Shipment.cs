using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using JetBrains.Annotations;
using PatNet.Lib;

namespace PatNet.Lib
{
    public class ShipmentFile
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public int PageCount { get; set; } //optional - not in shipA
        public int HeaderClaims { get; set; } // optional - not in shipA
        public bool IsPresent { get; set; }
        public bool HasImage { get; set; }
        //other uses .. for tracking during processing
        // WasPresent, Successful, etc
    }

    public enum ShipmentStates
    {
        UNKNOWN, VALIDATING, PROCESSING, COMPLETED, FAILED
    }

    public class AmendmentFile
    {
        public string FileName { get; set; }
        public string Name { get; set; }
        public bool IsPresent { get; set; }
    
    }

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
            
            using (TextReader tr = File.OpenText(_filename))
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

            var allFiles = Directory.GetFiles(_workingDir);

            foreach (var f in allFiles)
            {
                if (Amendments.ContainsKey(f))
                {
                    Amendments[f].IsPresent = true;
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
            using (TextReader tr = new StreamReader(_filename))
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

            var allFiles = Directory.GetFiles(_workingDir);
            foreach (var f in allFiles)
            {
                if (_shipmentFiles.ContainsKey(f))
                {
                    _shipmentFiles[f].IsPresent = true;
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

            return retVal;
        }
    }


    public class Shipment
    {
        //example line from SHPTXXXX.lst
        //  1. ShipT4853\13201171.001; Pgs = 32; Header Claims = 5  
        //  1. ShipA4853\13201171.amd
        private ShipmentManifestFile _shipmentManifest;
        private AmendmentManifestFile _amendmentManifest;

        private ShipmentStates _shipmentState;
        public ShipmentStates ShipmentState { get { return _shipmentState; } }

        public readonly string Path;
        public readonly string ShipmentNumber;


        public Shipment([NotNull] string path, [NotNull] string shipmentNumber)
        {
            Path = path;
            ShipmentNumber = shipmentNumber;         
        }

        //log warning if file doesn't end in .lst somehow
      

        public bool Validate(string path)
        {
            _shipmentState = ShipmentStates.VALIDATING;
            var isValid = true;
            try
            {
                ValidateShipmentDirectory(path);
                var lstFiles = Directory.GetFiles(path,"*.lst");
                if (lstFiles.Count() > 2)
                    throw new ShipmentException("More than 2 .LST files were present!");
                if( !lstFiles.Any())
                    throw new ShipmentFileMissingException("Missing ShipT manifest! (ShipTXXXXXX.lst file)");

                foreach (var f in lstFiles)
                {
                    if (f.ToUpper().StartsWith("SHIPA"))
                    {                      
                        _amendmentManifest= new AmendmentManifestFile(f, path);
                        isValid = _amendmentManifest.Validate() && isValid;
                        if (_amendmentManifest.Amendments.Count == 0)
                            throw new ShipmentException("Manifest file (ShipA) did not contain any data!");
                    }

                    if (f.ToUpper().StartsWith("SHIPT"))
                    {
                        _shipmentManifest = new ShipmentManifestFile(f, path);                         
                        isValid = _amendmentManifest.Validate() && isValid;
                        if (_shipmentManifest.ShipmentFiles.Count == 0)
                            throw new ShipmentException("ShipT Manifest did not contain any data!");
                    }
                }        
       
                if( _amendmentManifest == null )
                    Trace.WriteLine("No amendment files were found.", TraceLogLevels.WARN);
                if (_shipmentManifest == null)
                    throw new ShipmentFileMissingException("Missing Manifest (shipTXXXXX.lst) file!");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().ToString() +  ex.Message, TraceLogLevels.ERROR);                 
                // if we failed to load lst files then we should fail the validation
                _shipmentState = ShipmentStates.FAILED;
                return false;
            }
            return isValid;

        }


        private void ValidateShipmentDirectory(string patentDirectory)
        {
            if( String.IsNullOrEmpty(patentDirectory))
                throw new ShipmentDirectoryNotFoundException("Shipment directory did not exist at: NULL" );
            if (!Directory.Exists(patentDirectory))
                throw new ShipmentDirectoryNotFoundException("Shipment directory did not exist at: " + patentDirectory);                   
        }
     

        public void Process()
        {
            _shipmentState = ShipmentStates.PROCESSING;
            // throw new NotImplementedException();
        }
    }

 
}
