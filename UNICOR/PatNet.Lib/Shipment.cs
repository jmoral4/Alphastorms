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
using System.Configuration;

namespace PatNet.Lib
{

    public enum ShipmentStates
    {
        UNKNOWN, VALIDATING, PROCESSING, COMPLETED, FAILED
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
                        if (_amendmentManifest.Amendments.Count == 0) //change to warning?
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
                isValid=false;
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

        public void Process()
        {
            long totalProcessTime = 0;
            Stopwatch procWatch = new Stopwatch();
            procWatch.Start();
            _shipmentState = ShipmentStates.PROCESSING;
            string[] complexityStrings = GetComplexityValues();
            //load complexity values

            //generate a patent list
            foreach (var p in _shipmentManifest.ShipmentFiles.Values)
            {                
                
                Patent patent = new Patent();
                patent.PatentNumber = p.Name;
                patent.IsComplex = false;
                //create an output -- TODO ..add ending \ if missing to TempSetting
                string outputTemp = ReadSetting("TempOutputPath") + patent.PatentNumber;
                if (!Directory.Exists(outputTemp))
                    Directory.CreateDirectory(outputTemp);

                var amendment = _amendmentManifest.Amendments.Values.Where(x => x.Name == patent.PatentNumber).FirstOrDefault();
                var path = @"C:\DEV\UNICOR\Information for Jonathan\4853\";
                if (amendment != null)
                {
                    File.Copy(path + amendment.FileName, outputTemp + @"\" + patent.PatentNumber + "_amd.doc");

                }
               
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

                        //write abstract to a separate buffer
                        if (line.Contains("+ea"))
                        {
                            //12485859  Claim: 19 Page: 25                            
                            abs.AddRange(buffer);
                            buffer.Clear();
                        }

                        //calculate claimcount
                        if (line.Contains("+cm"))
                        {
                            //claim is excluded if it's listed as canceled
                            if (!line.ToLower().Contains("(canceled)"))
                            {
                                var cmNum =
                                    line.Split(new string[] {"."}, StringSplitOptions.RemoveEmptyEntries)[0].Split(
                                        new char[] {' '})[1];
                                int claims = 0;
                                Int32.TryParse(cmNum, out claims);
                                if (claims > 0)
                                    claimCount++;
                                //if (claimCount < claims)
                                //    claimCount = claims;
                            }                           
                        }

                        //check if we hit a complexity flag
                        foreach (string s in complexityStrings)
                        {
                            if ( line.Contains(s))
                                patent.IsComplex = true;

                        }
                        //calc page count
                        if (line.Contains("+pg"))
                        {
                            totalPageCount++;
                            bodySectionPageCount++;
                        }

                        //segment on pagecount > 100
                        if (bodySectionPageCount > 100)
                        {
                            bodyCount++;
                            bodySectionPageCount = 0;
                            File.WriteAllLines(outputTemp + "\\" + patent.PatentNumber + "_body_" + bodyCount + ".doc", buffer);
                            buffer.Clear();
                        }

                        //calculate character count excluding special characters
                        //...direct character count
                        runningCharCount += line.Replace("\r", "").Replace("\n","").Length;

                        //var cleanWords = line.Split(new char[] {' '}).Where(x => !x.StartsWith("+")).ToList();
                        //foreach (var word in cleanWords)
                        //{
                        //    runningCharCount += word.Length;
                        //}

                    }

                    //write abstract
                    if (abs.Count > 0)
                    {
                        abs.Insert(0, string.Format("{0} Claim: {1} Page: {2}", patent.PatentNumber, claimCount, totalPageCount));
                        File.WriteAllLines(outputTemp + "\\" + patent.PatentNumber + "_abstract.doc", abs);
                    }

                    //write any remaining data to file
                    if (buffer.Count > 0)
                    {
                        File.WriteAllLines(outputTemp + "\\" + patent.PatentNumber + "_body_" + bodyCount + ".doc", buffer);
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
                    xmlOut.Add(string.Format("\t<patent id=\"{0}\">", patent.PatentNumber));
                    var fCount = (abs.Count > 0 ? 1 : 0) + bodySectionPageCount + 2; //abs+image file+xml
                    xmlOut.Add(string.Format("\t<patentFileCount>{0}</patentFileCount>", fCount));
                    xmlOut.Add(string.Format("\t<pages>{0}</pages>", totalPageCount));
                    xmlOut.Add(string.Format("\t<charCount>{0}</charCount>", runningCharCount));
                    xmlOut.Add(string.Format("\t<claimCount>{0}</claimCount>", claimCount));
                    xmlOut.Add(string.Format("\t<isComplex>{0}</isComplex>", patent.IsComplex));
                    xmlOut.Add(string.Format("\t<hasAmd>{0}</hasAmd>", abs.Count > 0));
                    xmlOut.Add("\t</patent>");
                    xmlOut.Add("</shipment>");
                    File.WriteAllLines(outputTemp + "\\" + patent.PatentNumber + "_patent.xml", xmlOut);

                    //lastly, add associated images
                    SaveImagesToDoc(path, patent.PatentNumber, outputTemp);
                }

                Trace.WriteLine("Processed Patent " + patent.PatentNumber + " in " + procWatch.ElapsedMilliseconds + "ms!");
                totalProcessTime += procWatch.ElapsedMilliseconds;
                procWatch.Restart();

            }

           Trace.WriteLine("Completed processing " + _shipmentManifest.ShipmentFiles.Count  + " patents in " + totalProcessTime + "ms", TraceLogLevels.INFO);

            _shipmentState = ShipmentStates.COMPLETED;
        }

        private string[] GetComplexityValues()
        {
            return ReadSetting("ComplexityValues").Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);            
        }
        private string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                Console.WriteLine(result);
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return "Error reading App settings";
            }
        }

        private void SaveImagesToDoc(string path, string patentNumber, string outputDirectory)
        {
            Microsoft.Office.Interop.Word.Application WordApp = new Microsoft.Office.Interop.Word.Application();
            // now creating new document.
            WordApp.Documents.Add();
            // see word file behind your program
            WordApp.Visible = false;
            // get the reference of active document
            Microsoft.Office.Interop.Word.Document doc = WordApp.ActiveDocument;
            var imagePath = path + "\\" + patentNumber;
            //get image directory associated..
            // iterating process for adding all images which is selected by filedialog
            foreach (string filename in Directory.GetFiles(imagePath))
            {
                // now add the picture in active document reference
                doc.InlineShapes.AddPicture(filename, Type.Missing, Type.Missing, Type.Missing);
            }

            // file is saved.
            doc.SaveAs(outputDirectory + "\\" + patentNumber + "_img.doc", Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                Type.Missing, Type.Missing, Type.Missing);
            // application is now quit.
            WordApp.Quit(Type.Missing, Type.Missing, Type.Missing);
        }

        public void ProcessAmendmentFiles()
        {
            //create a shipment

        }
    }
}
