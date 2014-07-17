using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PatNet.Lib
{
    public class ShipmentFile
    {
        public string Name { get; set; }
        public int PageCount { get; set; }
        public int HeaderClaims { get; set; }

    }

    public class Shipment
    {
        //example line from SHPTXXXX.lst
        //  //1. ShipT4853\13201171.001; Pgs = 32; Header Claims = 5  
        private readonly List<ShipmentFile> _fileManifest;
        private readonly List<string> _admendments; 

        public int FileCount { get { return _fileManifest.Count; } }

        public Shipment()
        {
            _fileManifest = new List<ShipmentFile>();
            _admendments = new List<string>();
        }

        //log warning if file doesn't end in .lst somehow
        public void LoadShipmentAFile(string filename)
        {
            Console.WriteLine("Processing Shipment A File - {0}", filename);
            Debug.Assert(filename.ToUpper().EndsWith("LST"), "Expected LoadShipmentTFile to end in .LST extension!");
            if( _admendments.Count > 0)
                _admendments.Clear();
            TextReader tr = new StreamReader(filename);
            string line;

            while ((line = tr.ReadLine()) != null)
            {
                if (line.ToUpper().Contains(".AMD") )
                {
                    var amd = line.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    _admendments.Add(amd);
                  
                }
            }

            tr.Close();           

            Console.WriteLine("Processed {0} amendments!", _admendments.Count);
        }

        public void LoadShipmentTFile(string filename)
        {
            Console.WriteLine("Processing Shipment T File - {0}", filename);
            Debug.Assert(filename.ToUpper().EndsWith("LST"), "Expected LoadShipmentTFile to end in .LST extension!");
            if( _fileManifest.Count > 0 )
                _fileManifest.Clear();

            TextReader tr = new StreamReader(filename);
            string line;
          
            while ((line = tr.ReadLine()) != null)
            {
                if (line.Contains("Pgs") && line.Contains("Header Claims"))
                {
                    var sections = line.Split(';');
                    var fileStats = new ShipmentFile();
                    
                    fileStats.Name = sections[0].Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries)[1].Replace(".001", "");
                    fileStats.PageCount = Int32.Parse(sections[1].Split('=')[1].Trim());
                    fileStats.HeaderClaims = Int32.Parse(sections[2].Split('=')[1].Trim());
                    _fileManifest.Add(fileStats);
                }
            }

            tr.Close();

            //debug out
            foreach (var f in _fileManifest)
            {
                Console.WriteLine("{0} - {1} - {2}", f.Name, f.PageCount, f.HeaderClaims);
            }

            Console.WriteLine("Processed {0} files!", _fileManifest.Count);
        }

        public void Validate()
        {
           //  number is from the folder path, stored in number as well
            //scan for .lst files
            DirectoryInfo di = new DirectoryInfo(Path);

            var lstFiles = di.GetFiles("*.lst");
            foreach (var file in lstFiles)
            {
                if( file.Name.StartsWith("ShipT"))
                    LoadShipmentTFile(file.FullName);
                if(file.Name.StartsWith("ShipA"))
                    LoadShipmentAFile(file.FullName);
            }

            //perform validation
            var allFiles = di.GetFiles();
            var missingFiles = new List<string>();
            foreach (var f in _fileManifest)
            {
                var matchCount = allFiles.Count(x => x.Name == f.Name + ".dta");
                if( matchCount == 0)
                    missingFiles.Add(f.Name + ".dta");
            }


            foreach (var a in _admendments)
            {
                var matchCount = allFiles.Count(x => x.Name == a);
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
                Console.WriteLine("Missing: {0}" , s);
            }
        }

        public string Path { get; set; }

        public string Number { get; set; }
    }

 
}
