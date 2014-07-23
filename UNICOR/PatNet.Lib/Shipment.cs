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
        public bool IsValidated { get; set; }    
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
        public bool IsValidated { get; set; }

    
    }

    public class AmendmentManifestFile
    {
        private string _lstFile;
        private string _workingDir;
        private Dictionary<string, AmendmentFile> _textAmendments;

        public AmendmentManifestFile(string lstFile, string workingDir)
        {
            _lstFile = lstFile;
            _workingDir = workingDir;
        }

        public bool Validate()
        {            
            //WARN: on missing files
            //ERROR: on too-many file (you're not on the manifest!)
            Console.WriteLine("Processing Shipment A File - {0}",_lstFile);

            var amendments = new Dictionary<string, AmendmentFile>();
            var retVal = true;
            
            using (TextReader tr = File.OpenText(_lstFile))
            {
                string line;

                while ((line = tr.ReadLine()) != null)
                {
                    if (line.ToUpper().Contains(".AMD"))
                    {
                        var amd = line.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1];
                        amendments.Add( amd, new AmendmentFile(){ FileName = amd, Name = amd.Replace(".amd", "") });
                    }
                }
            }

            var allFiles = Directory.GetFiles(_workingDir);

            foreach (var f in allFiles)
            {
                if (amendments.ContainsKey(f))
                {
                    amendments[f].IsValidated = true;
                }
                else
                {
                    Trace.WriteLine("File " + f + " was not accounted for in ShipA Manifest!", TraceLogLevels.ERROR);
                    retVal = false;
                }
            }

            foreach (var a in amendments.Values)
            {
                if( !a.IsValidated) 
                    Trace.WriteLine("File " + a.FileName + " was missing form the Shipment directory!", TraceLogLevels.WARN);
            }

            _textAmendments = amendments;
            Trace.WriteLine("Processed " + amendments.Count + " amendments!");
            return retVal;
        }
    }

    public class ShipmentManifestFile
    {
        private string _lstFile;
        private string _workingDir;
        private Dictionary<string, ShipmentFile> _textOcrFiles;

        public ShipmentManifestFile(string lstFile, string workingDir)
        {
            _lstFile = lstFile;
            _workingDir = workingDir;
        }

        public bool Validate()
        {
            Contract.Requires(!String.IsNullOrEmpty(_lstFile));
            var retVal = true;
            Trace.WriteLine("Processing Shipment Manifest [" + _lstFile + "]", TraceLogLevels.INFO);
            var shipmentFiles = new List<ShipmentFile>();
            using (TextReader tr = new StreamReader(_lstFile))
            {
                //EXAMPLE: 1. ShipT4853\13201171.001; Pgs = 32; Header Claims = 5  
                string line;
                while ((line = tr.ReadLine()) != null)
                {
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
                            shipmentFiles.Add(fileStats);
                        }
                        else
                        {
                            Trace.WriteLine("Patent File line was formatted incorrectly!", TraceLogLevels.WARN);
                        }
                    }
                }
            }

            var allFiles = Directory.GetFiles(_workingDir);

            foreach (var f in shipmentFiles)
            {
                var matchCount = allFiles.Count(x => x == f.FileName);
                if (matchCount == 0)
                    missingFiles.Add(f);
            }

            //check if images are present according to count
            foreach (var f in shipmentFiles)
            {
                var c = Directory.GetFiles(_workingDir + @"\" + f.Name).Count();
                if (c != f.PageCount)
                    Console.WriteLine("Missing Image files for patent {0}. Expected {1} but had {2}!", f.Name, f.PageCount, c);
            }

            return retVal;
        }
    }


    public class Shipment
    {
        //example line from SHPTXXXX.lst
        //  1. ShipT4853\13201171.001; Pgs = 32; Header Claims = 5  
        //  1. ShipA4853\13201171.amd
        private List<ShipmentFile> _fileManifest;
        private readonly List<ShipmentFile> _admendments;
        private ShipmentStates _shipmentState;
        public ShipmentStates ShipmentState { get { return _shipmentState; } }

        public readonly string Path;
        public readonly string ShipmentNumber;
        private bool _isValid;
        public bool IsValid { get { return _isValid; } }

        public int FileCount { get { return _fileManifest.Count; } }
        public int AmendmentCount { get { return _admendments.Count; } }

        public Shipment([NotNull] string path, [NotNull] string shipmentNumber)
        {
            Path = path;
            ShipmentNumber = shipmentNumber;
            _fileManifest = new List<ShipmentFile>();
            _admendments = new List<ShipmentFile>();           
        }

        //log warning if file doesn't end in .lst somehow
      

        public bool Validate(string path)
        {
            _shipmentState = ShipmentStates.VALIDATING;
            try
            {
                //create manifest file
                var lstFiles = Directory.GetFiles("*.lst");
                AmendmentManifestFile amf = null;
                foreach (var f in lstFiles)
                {
                    if (f.ToUpper().StartsWith("SHIPA"))
                    {
                        amf = new AmendmentManifestFile(f, path);
                        amf.Validate();
                    }

                    //if( f.ToUpper().StartsWith("SHIPT"))
                       

                }               
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().ToString() +  ex.Message, TraceLogLevels.ERROR);                 
                // if we failed to load lst files then we should fail the validation
                _shipmentState = ShipmentStates.FAILED;
                return false;
            }
            Console.WriteLine("Loaded and moving on");
            
            //perform validation
            DirectoryInfo di = new DirectoryInfo(path);           
            var allFiles = di.GetFiles();
            var missingFiles = new List<ShipmentFile>();
            foreach (var f in _fileManifest)
            {
                var matchCount = allFiles.Count(x => x.Name == f.FileName);
                if( matchCount == 0)
                    missingFiles.Add(f);
            }


            foreach (var a in _admendments)
            {
                var matchCount = allFiles.Count(x => x.Name == a.FileName);
                if (matchCount == 0)
                    missingFiles.Add(a );
            }

            //check if images are present according to count
            foreach (var f in _fileManifest)
            {
                var c = Directory.GetFiles(Path + @"\" + f.Name).Count();
                if( c != f.PageCount )
                    Console.WriteLine("Missing Image files for patent {0}. Expected {1} but had {2}!", f.Name, f.PageCount, c);

            }

            foreach (var s in missingFiles)
            {
                Console.WriteLine("Missing file: {0}" , s.FileName);
            }


            return _isValid = missingFiles.Count == 0 ? true : false;

        }

        
        public void LoadLSTFiles(string patentDirectory)
        {
            Contract.Requires(!String.IsNullOrEmpty(patentDirectory));            
            
            if (!Directory.Exists(patentDirectory))
                throw new ShipmentDirectoryNotFoundException("Shipment directory did not exist at: " + patentDirectory);

            var di = new DirectoryInfo(patentDirectory);
            var lstFiles = di.GetFiles("*.lst");
            if (lstFiles.Count() > 2)
                throw new ShipmentException("More than 2 .LST files were present!");

            var dataFile = lstFiles.FirstOrDefault(x => x.Name.StartsWith("ShipT"));
            if (dataFile == null)
                throw new ShipmentFileMissingException("Missing Manifest (shipTXXXXX.lst) file!");
           
            var patentFiles = LoadShipTFiles(dataFile.FullName);
            _fileManifest = patentFiles;
            if (_fileManifest.Count == 0)
                throw new ShipmentException("ShipT Manifest did not contain any data!");

            var amendmentFile = lstFiles.FirstOrDefault(x => x.Name.StartsWith("ShipA"));
            if (amendmentFile != null)
            {
                var amendments = LoadShipAFiles(amendmentFile.FullName);
                if( amendments.Count==0)
                    throw new ShipmentException("Manifest file (ShipA) did not contain any data!");

            }
            else
            {
                Trace.WriteLine("No amendment files were found.", TraceLogLevels.WARN);
            }
        }

        //DEPRECATED
        private List<ShipmentFile> LoadShipAFiles( string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException("filename");
            Console.WriteLine("Processing Shipment A File - {0}", filename);

            var amendments = new List<ShipmentFile>();
            if (_admendments.Count > 0)
                _admendments.Clear();

            using (TextReader tr = new StreamReader(filename))
            {
                string line;

                while ((line = tr.ReadLine()) != null)
                {
                    if (line.ToUpper().Contains(".AMD"))
                    {
                        var amd = line.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1];
                        amendments.Add(new ShipmentFile() { FileName = amd, Name = amd.Replace(".amd", "") });                        
                    }
                }
            }

            Trace.WriteLine("Processed " + amendments.Count + " amendments!");
            
            return amendments;
        }

        private List<ShipmentFile> LoadShipTFiles(string filename)
        {
            Contract.Requires(!String.IsNullOrEmpty(filename));
            Trace.WriteLine("Processing Shipment Manifest [" + filename + "]", TraceLogLevels.INFO);
            var shipmentFiles = new List<ShipmentFile>();
            using (TextReader tr = new StreamReader(filename))
            {
                //EXAMPLE: 1. ShipT4853\13201171.001; Pgs = 32; Header Claims = 5  
                string line;
                while ((line = tr.ReadLine()) != null)
                {
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
                            shipmentFiles.Add(fileStats);
                        }
                        else
                        {
                            Trace.WriteLine("Patent File line was formatted incorrectly!", TraceLogLevels.WARN);
                        }
                    }
                }
            }

            Trace.WriteLine("Processed " + shipmentFiles.Count + " files!", TraceLogLevels.INFO);
            return shipmentFiles;
        }        

        public void Process()
        {
           // throw new NotImplementedException();
        }
    }

 
}
