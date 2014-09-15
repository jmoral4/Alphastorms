using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Claims;
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
                DirectoryInfo d = new DirectoryInfo(path);
                
                ValidateShipmentDirectory(path);
                var lstFiles = d.GetFiles("*.lst");
                if (lstFiles.Count() > 2)
                    throw new ShipmentException("More than 2 .LST files were present!");
                if( !lstFiles.Any())
                    throw new ShipmentFileMissingException("Missing ShipT manifest! (ShipTXXXXXX.lst file)");

                foreach (var f in lstFiles)
                {
                    if (f.Name.ToUpper().StartsWith("SHIPA"))
                    {                      
                        _amendmentManifest= new AmendmentManifestFile(f.Name, path);
                        isValid = _amendmentManifest.Validate() && isValid;
                        if (_amendmentManifest.Amendments.Count == 0)
                            throw new ShipmentException("Manifest file (ShipA) did not contain any data!");
                    }

                    if (f.Name.ToUpper().StartsWith("SHIPT"))
                    {
                        _shipmentManifest = new ShipmentManifestFile(f.Name, path);
                        isValid = _shipmentManifest.Validate() && isValid;
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

        /*
         * 
         * <?xml version="1.0"?>
             <shipment id="897123">
                <patent id="12s101">
                <patentFileCount>2</patentFileCount>
                <pages>24<pages>
                <charCount>98123<charCount>
                <claimCount>5</claimCount>
                <isComplex>false<isComplex>
                <hasAmd>true<hasAmd>                
                </patent>
             </shipment>
         * 
         */

        public class Patent
        {
            public string PatentNumber { get; set; }
        
            public string AmendmentFile { get; set; }
            public string Bodyfile { get; set; }
            public int BodyFileCount { get; set; }
            public string Abstract { get; set; }
            public int PageCount { get; set; }
            public int ClaimCount { get; set; }
            public bool IsComplex { get; set; }

            public bool HasAmendment
            {
                get { return !String.IsNullOrEmpty(AmendmentFile); }
            }

            public Patent ()
            {
                
            }
        

        }

        public void Process()
        {
            System.Diagnostics.Stopwatch st = new Stopwatch();
            st.Start();
            Stopwatch procWatch = new Stopwatch();
            procWatch.Start();
            _shipmentState = ShipmentStates.PROCESSING;

            //generate a patent list
            foreach (var p in _shipmentManifest.ShipmentFiles.Values)
            {
                
                
                Patent patent = new Patent();
                patent.PatentNumber = p.Name;
                //create an output
                string outputTemp = @"C:\temp\output\" + p.Name;
                if (!Directory.Exists(outputTemp))
                    Directory.CreateDirectory(outputTemp);

                var amendment = _amendmentManifest.Amendments.Values.Where(x => x.Name == p.Name).FirstOrDefault();
                var path = @"C:\DEV\UNICOR\Information for Jonathan\4853\";
                if (amendment != null)
                {
                    File.Copy(path + amendment.FileName, outputTemp + @"\" + p.Name + "_amd.doc");

                }

                //extract abstract

               
                using (StreamReader sr = new StreamReader(path + p.FileName))
                {

                    List<string> buffer = new List<string>();
                    string line;
                    int totalPageCount = 0;
                    int bodyCount = 0;
                    int bodySectionPageCount = 0;
                    int claimCount = 0;
                    List<string> abs = new List<string>();
                    int runningCharCount = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        buffer.Add(line);
                        if (line.Contains("+ea"))
                        {//write buffer as abstract
                            //12485859  Claim: 19 Page: 25                            
                            abs.AddRange(buffer);
                            //File.WriteAllLines(outputTemp + "\\" + p.Name + "_abstract.doc", buffer);
                            buffer.Clear();
                        }

                        if (line.Contains("+cm"))
                        {
                            var cmNum =
                                line.Split(new string[] {"."}, StringSplitOptions.RemoveEmptyEntries)[0].Split(
                                    new char[] {' '})[1];
                            int claims = 0;
                            Int32.TryParse(cmNum, out claims);
                            if (claimCount < claims)
                                claimCount = claims;
                        }

                        if (line.Contains("+pg"))
                        {
                            totalPageCount++;
                            bodySectionPageCount++;
                        }

                        if (bodySectionPageCount > 100)
                        {
                            bodyCount++;
                            bodySectionPageCount = 0;
                            File.WriteAllLines(outputTemp + "\\" + p.Name + "_body_" + bodyCount + ".doc", buffer);
                            buffer.Clear();
                        }

                        //calculate character count excluding special characters
                        var cleanWords = line.Split(new char[] {' '}).Where(x => !x.StartsWith("+")).ToList();
                        foreach (var word in cleanWords)
                        {
                            runningCharCount += word.Length;
                        }

                    }
                    //write abstract
                    if (abs.Count > 0)
                    {
                        abs.Insert(0, string.Format("{0} Claim: {1} Page: {2}", p.Name, claimCount, totalPageCount));
                        File.WriteAllLines(outputTemp + "\\" + p.Name + "_abstract.doc", abs);
                    }

                    if (buffer.Count > 0)
                        {
                            File.WriteAllLines(outputTemp + "\\" + p.Name + "_body_" + bodyCount + ".doc", buffer);
                        }

                    buffer.Clear();

                    //write the xml
                    /*
                    <?xml version="1.0"?>
             <shipment id="897123">
                <patent id="12s101">
                <patentFileCount>2</patentFileCount>
                <pages>24<pages>
                <charCount>98123<charCount>
                <claimCount>5</claimCount>
                <isComplex>false<isComplex>
                <hasAmd>true<hasAmd>                
                </patent>
             </shipment>
             */

                    List<string> xmlOut = new List<string>();

                    xmlOut.Add("<?xml version=\"1.0\"?>");
                    xmlOut.Add(string.Format("<shipment id=\"{0}\">", this.ShipmentNumber));
                    xmlOut.Add(string.Format("\t<patent id=\"{0}\">", p.Name));
                    var fCount = (abs.Count > 0 ? 1 : 0) + bodySectionPageCount + 2; //abs+image file
                    xmlOut.Add(string.Format("\t<patentFileCount>{0}</patentFileCount>", fCount));
                    xmlOut.Add(string.Format("\t<pages>{0}</pages>", totalPageCount));
                    xmlOut.Add(string.Format("\t<charCount>{0}</charCount>", runningCharCount));
                    xmlOut.Add(string.Format("\t<claimCount>{0}</claimCount>", claimCount));
                    xmlOut.Add(string.Format("\t<isComplex>{0}</isComplex>", false));
                    xmlOut.Add(string.Format("\t<hasAmd>{0}</hasAmd>", abs.Count > 0));
                    xmlOut.Add("\t</patent>");
                    xmlOut.Add("</shipment>");
                    File.WriteAllLines(outputTemp + "\\" + p.Name + "_patent.xml", xmlOut);

                    //lastly, add associated images

                    // first we are creating application of word.
                    
                    Microsoft.Office.Interop.Word.Application WordApp = new Microsoft.Office.Interop.Word.Application();
                    // now creating new document.
                    WordApp.Documents.Add();
                    // see word file behind your program
                    WordApp.Visible = false;
                    // get the reference of active document
                    Microsoft.Office.Interop.Word.Document doc = WordApp.ActiveDocument;
                    var imagePath = path + "\\" + p.Name;
                    //get image directory associated..
                    // iterating process for adding all images which is selected by filedialog
                    foreach (string filename in Directory.GetFiles(imagePath))
                    {
                        // now add the picture in active document reference
                        doc.InlineShapes.AddPicture(filename, Type.Missing, Type.Missing, Type.Missing);
                    }
                   
                    // file is saved.
                    doc.SaveAs( outputTemp + "\\" + p.Name + "_img.doc", Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                    // application is now quit.
                    WordApp.Quit(Type.Missing, Type.Missing, Type.Missing);
                }

                Trace.WriteLine("Processed Patent " + p.Name + " in " + procWatch.ElapsedMilliseconds + "ms!");
                procWatch.Restart();

            }

           Trace.WriteLine("Completed processing " + _shipmentManifest.ShipmentFiles.Count  + " patents in " + st.ElapsedMilliseconds + "ms", TraceLogLevels.INFO);
            st.Stop();
            _shipmentState = ShipmentStates.COMPLETED;
        }

        public void ProcessAmendmentFiles()
        {
            //create a shipment

        }
    }

 
}
