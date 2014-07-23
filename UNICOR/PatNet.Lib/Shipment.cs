using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using JetBrains.Annotations;

namespace PatNet.Lib
{
    public class ShipmentFile
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public int PageCount { get; set; } //optional - not in shipA
        public int HeaderClaims { get; set; } // optional - not in shipA

        //other uses .. for tracking during processing
        // WasPresent, Successful, etc
    }

    public class Shipment
    {
        //example line from SHPTXXXX.lst
        //  1. ShipT4853\13201171.001; Pgs = 32; Header Claims = 5  
        //  1. ShipA4853\13201171.amd
        private readonly List<ShipmentFile> _fileManifest;
        private readonly List<ShipmentFile> _admendments;

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
        private int LoadAmendmentFile([NotNull] string filename)
        {
            if (filename == null) throw new ArgumentNullException("filename");
            Console.WriteLine("Processing Shipment A File - {0}", filename);
            Debug.Assert(filename.ToUpper().EndsWith("LST"), "Expected LoadShipmentTFile to end in .LST extension!");

            if( _admendments.Count > 0)
                _admendments.Clear();

            using (TextReader tr = new StreamReader(filename))
            {
                string line;

                while ((line = tr.ReadLine()) != null)
                {
                    if (line.ToUpper().Contains(".AMD") )
                    {
                        var amd = line.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1];
                        _admendments.Add(new ShipmentFile(){FileName = amd, Name = amd.Replace(".amd","")});                    
                    }
                }
            }

            Console.WriteLine("Processed {0} amendments!", _admendments.Count);
            return _admendments.Count;
        }

        private List<ShipmentFile> LoadShipmentManifest([NotNull] string filename)
        {
            Contract.Requires(String.IsNullOrEmpty(filename));
            if (filename == null) throw new ArgumentNullException("filename");
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
                        var fileStats = new ShipmentFile
                        {
                            Name =
                                sections[0].Split(new string[] {@"\"}, StringSplitOptions.RemoveEmptyEntries)[1].Replace(
                                    ".001", ""),
                            PageCount = Int32.Parse(sections[1].Split('=')[1].Trim()),
                            HeaderClaims = Int32.Parse(sections[2].Split('=')[1].Trim())
                        };
                        fileStats.FileName = fileStats.Name + ".dta";
                        shipmentFiles.Add(fileStats);                        
                    }
                }
            }

            Trace.WriteLine("Processed " + shipmentFiles.Count + " files!", TraceLogLevels.INFO);
            return shipmentFiles;
        }        

        public bool Validate(string path)
        {
            try
            {
                LoadLSTManifestFiles(path);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message, TraceLogLevels.ERROR);                
            }

            
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

        
        public void LoadLSTManifestFiles(string patentDirectory)
        {
            if (!Directory.Exists(patentDirectory))
                throw new ShipmentDirectoryNotFoundException("Shipment directory did not exist at: " + patentDirectory);

            var di = new DirectoryInfo(patentDirectory);
            var lstFiles = di.GetFiles("*.lst");
            if (lstFiles.Count() > 2)
                throw new ShipmentException("More than 2 .LST files were present!");

            var dataFile = lstFiles.FirstOrDefault(x => x.Name.StartsWith("ShipT"));
            if (dataFile == null)
                throw new ShipmentFileMissingException("Missing Manifest (shipTXXXXX.lst) file!");

            var patentFiles = LoadShipmentManifest(dataFile.FullName);
            _fileManifest = patentFiles; 
            if (manifestCount == 0)
                throw new ShipmentException("ShipT Manifest did not contain any data!");

            var amendmentFile = lstFiles.FirstOrDefault(x => x.Name.StartsWith("ShipA"));
            if (amendmentFile != null)
            {
                var amendments = LoadAmendmentFile(amendmentFile.FullName);

            }
            else
            {
                Trace.WriteLine("No amendment files were found.", TraceLogLevels.WARN);
            }
        }

        public void Process()
        {
           // throw new NotImplementedException();
        }
    }

 
}
