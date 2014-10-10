using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Policy;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using JetBrains.Annotations;
using Microsoft.SharePoint.Client;
using PatNet.Lib;
using System.Configuration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using System.Windows.Media.Imaging;
//using Novacode;
using System.Drawing;
using File = System.IO.File;


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

       

        public void Process(string path, string outputPath)
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
                string outputTemp = outputPath + @"\" + patent.PatentNumber;
                if (!Directory.Exists(outputTemp))
                    Directory.CreateDirectory(outputTemp);

                var amendment = _amendmentManifest.Amendments.Values.Where(x => x.Name == patent.PatentNumber).FirstOrDefault();
                
                if (amendment != null)
                {
                    //File.Copy(path +  @"\" + amendment.FileName, outputTemp + @"\" + patent.PatentNumber + "_amd.doc");
                    CopyFileToWordDocument(path + @"\" + amendment.FileName, outputTemp + @"\" + patent.PatentNumber + "_amd.docx");
                }
               
                using (StreamReader sr = new StreamReader(path + @"\" + p.FileName))
                {

                    List<string> buffer = new List<string>();
                    string line;
                    int totalPageCount = _shipmentManifest.ShipmentFiles[p.FileName].PageCount;
                    int bodyCount = 0;
                    int bodySectionPageCount = 0;
                    int claimCount =  _shipmentManifest.ShipmentFiles[p.FileName].HeaderClaims;

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
                        
                        //check if we hit a complexity flag
                        foreach (string s in complexityStrings)
                        {
                            if ( line.ToUpper().Contains(s.ToUpper()))
                                patent.IsComplex = true;
                        }
                        //calc page count
                        if (line.Contains("+pg"))
                        {
                            bodySectionPageCount++; //for breaking up into sections
                        }

                        //segment on pagecount > 100
                        if (bodySectionPageCount > 100)
                        {
                            bodyCount++;
                            bodySectionPageCount = 0;
                            string s = buffer.Aggregate(string.Empty, (current, b) => current + b + Environment.NewLine);
                            //File.WriteAllLines(outputTemp + "\\" + patent.PatentNumber + "_body_" + bodyCount + ".docx", buffer);
                            WriteDocument(outputTemp + "\\" + patent.PatentNumber + "_body_" + bodyCount + ".docx",
                                s);
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
                        string s = abs.Aggregate(string.Empty, (current, b) => current + b + Environment.NewLine);
                        WriteDocument(outputTemp + "\\" + patent.PatentNumber + "_abstract.docx",
                            s);
                        //File.WriteAllLines(outputTemp + "\\" + patent.PatentNumber + "_abstract.docx", abs);
                    }

                    //write any remaining data to file
                    if (buffer.Count > 0)
                    {
                        string s = buffer.Aggregate(string.Empty, (current, b) => current + b + Environment.NewLine);
                        WriteDocument(outputTemp + "\\" + patent.PatentNumber + "_body_" + bodyCount + ".docx", 
                            s );

                       // File.WriteAllLines(outputTemp + "\\" + patent.PatentNumber + "_body_" + bodyCount + ".docx", buffer);
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
                    SaveImagesUsingOOXML(path, patent.PatentNumber, outputTemp);
                }

                Trace.WriteLine("Processed Patent " + patent.PatentNumber + " in " + procWatch.ElapsedMilliseconds + "ms!");
                totalProcessTime += procWatch.ElapsedMilliseconds;
                procWatch.Restart();

            }

           Trace.WriteLine("Completed processing " + _shipmentManifest.ShipmentFiles.Count  + " patents in " + totalProcessTime + "ms", TraceLogLevels.INFO);

            _shipmentState = ShipmentStates.COMPLETED;
        }

       
        

        public void Send(string sharepointMappedLocation, string localFolder)
        {
            Trace.WriteLine("Connecting to mapped location", TraceLogLevels.DEBUG);
            DirectoryInfo di = new DirectoryInfo(localFolder);
            DirectoryInfo remoteFolder = new DirectoryInfo(sharepointMappedLocation);
            Trace.WriteLine("Mapped folders - getting directories", TraceLogLevels.DEBUG);
            var parent = di.Name;
            var children = di.GetDirectories();

            remoteFolder =  remoteFolder.CreateSubdirectory(parent);
            Trace.WriteLine("Created parent folder", TraceLogLevels.DEBUG);
            //create new children
            foreach (var child in children)
            {
               var temp = remoteFolder.CreateSubdirectory(child.Name);
               Trace.WriteLine("created child folder " + child.Name, TraceLogLevels.DEBUG);
                var files = child.GetFiles();
                //copy files from folder to remote directory
                Trace.WriteLine("Copying files", TraceLogLevels.DEBUG);
                foreach (var file in files)
                {
                    file.CopyTo(temp.FullName + "/" + file.Name);
                }
                Trace.WriteLine("all files n child copied", TraceLogLevels.DEBUG);
            }
            Trace.WriteLine("All folders copied", TraceLogLevels.DEBUG);
      
        }

        public void Send(string sharepointServerPath,  string listName, string folderPath, string username = "", string password = "")
        {
            using (var clientContext = new ClientContext(sharepointServerPath))
            {
                if (username == string.Empty && password == string.Empty)
                {
                    Trace.WriteLine("SharePoint: No credentials provided, using default user credentials", TraceLogLevels.INFO);
                    clientContext.Credentials = CredentialCache.DefaultCredentials;
                }
                else
                {
                    Trace.WriteLine("SharePoint: Using App.config credentials. ", TraceLogLevels.INFO);
                    System.Net.NetworkCredential n = new NetworkCredential(username, password);
                    clientContext.Credentials = n;
                }
                DirectoryInfo di = new DirectoryInfo(folderPath);
                var parent = di.Name;
                var children = di.GetDirectories();

                Trace.WriteLine("Creating Parent Folder for list " + listName, TraceLogLevels.ERROR);
                //create parent folder
//                CreateFolder(clientContext.Web, listName, parent);
                var library = clientContext.Web.Lists.GetByTitle(listName);
                var p = library.RootFolder.Folders.Add(parent);
                clientContext.Web.Context.Load(p);
                clientContext.Web.Context.ExecuteQuery();
                Trace.WriteLine("Created Parent Folder successfully within " + listName, TraceLogLevels.ERROR);




                //create child folders
                foreach (var child in children)
                {
                    var r = p.Folders.Add(child.Name);
                    clientContext.Web.Context.Load(r);
                    clientContext.Web.Context.ExecuteQuery();
                   // var folder = CreateFolder(clientContext.Web, listName, parent + "/" + child.Name);
                    Trace.WriteLine("Created child folder " + child.Name, TraceLogLevels.ERROR);
                }
                 

                //copy files into folders
                foreach (var childDirectory in children)
                {
                    //get files
                    foreach (var file in childDirectory.GetFiles())
                    {
                        using (var fs = new FileStream(file.FullName, FileMode.Open))
                        {
                            try
                            {
                                var list = clientContext.Web.Lists.GetByTitle(listName);
                                clientContext.Load(list.RootFolder);
                                clientContext.ExecuteQuery();
                                var fileUrl = String.Format("{0}/{1}/{2}/{3}", list.RootFolder.ServerRelativeUrl, parent,
                                    childDirectory.Name, file.Name);
                                Trace.WriteLine("Uploading file to SharePoint: " + fileUrl);
                                Microsoft.SharePoint.Client.File.SaveBinaryDirect(clientContext, fileUrl, fs, true);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine("Failed copying file to SharePoint", TraceLogLevels.ERROR);
                                Trace.WriteLine(ex.Message, TraceLogLevels.ERROR);
                                if (ex.InnerException != null)
                                {
                                    Trace.WriteLine(ex.InnerException.Message, TraceLogLevels.ERROR);
                                }
                            
                                Trace.WriteLine(ex.StackTrace, TraceLogLevels.ERROR);
                                
                            }

                        }
                    }
                }
            }

         
        }

 

        public static Folder CreateFolder(Web web, string listTitle, string fullFolderUrl)
        {
            if (string.IsNullOrEmpty(fullFolderUrl))
                throw new ArgumentNullException("fullFolderUrl");
            var list = web.Lists.GetByTitle(listTitle);
            return CreateFolderInternal(web, list.RootFolder, fullFolderUrl);
        }


        private static Folder CreateFolderInternal(Web web, Folder parentFolder, string fullFolderUrl)
        {
            var folderUrls = fullFolderUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string folderUrl = folderUrls[0];
            var curFolder = parentFolder.Folders.Add(folderUrl);

            try
            {
              
                web.Context.Load(curFolder);
                web.Context.ExecuteQuery();

                if (folderUrls.Length > 1)
                {
                    var subFolderUrl = string.Join("/", folderUrls, 1, folderUrls.Length - 1);
                    return CreateFolderInternal(web, curFolder, subFolderUrl);
                }
                
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed creating folder: " + fullFolderUrl, TraceLogLevels.ERROR );
                Trace.WriteLine(ex.Message, TraceLogLevels.ERROR);
                if (ex.InnerException != null)
                {
                    Trace.WriteLine(ex.InnerException.Message, TraceLogLevels.ERROR);
                }
                Trace.WriteLine(ex.StackTrace, TraceLogLevels.ERROR);
            }
            return curFolder;
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
                //Console.WriteLine(result);
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
                return "Error reading App settings";
            }
        }

        public void WriteDocument(string outputPath, string content)
        {
            using (var document = WordprocessingDocument.Create(
                outputPath, WordprocessingDocumentType.Document))
            {
                document.AddMainDocumentPart();
                var run = new Run();
                parseTextForOpenXML(run,content );
                document.MainDocumentPart.Document = new Document(
                    new Body(new Paragraph( run )));
                document.Close();
                
            }

        }

        private void parseTextForOpenXML(Run run, string textualData)
        {
            string[] newLineArray = {Environment.NewLine};
            string[] textArray = textualData.Split(newLineArray, StringSplitOptions.None);

            bool first = true;

            foreach (string line in textArray)
            {
                if (! first)
                {
                    run.Append(new Break());
                }

                first = false;

                Text txt = new Text();
                txt.Text = line;
                run.Append(txt);
            }
        }

        public void CopyFileToWordDocument(string filename, string outputPath)
        {
            var amdContents = File.ReadAllText(filename);

            using (var document = WordprocessingDocument.Create(
                outputPath, WordprocessingDocumentType.Document))
            {
                document.AddMainDocumentPart();
                document.MainDocumentPart.Document = new Document(
                    new Body(new Paragraph(new Run(new Text(amdContents)))));
                document.Close();

            }
        }

        public void SaveImagesUsingOOXML(string inputPath, string patentNumber, string outputPath)
        {
            var imagePath = inputPath + "\\" + patentNumber;
            var outputDocument = outputPath + "\\" + patentNumber + "_img.docx";
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputDocument, WordprocessingDocumentType.Document))
                {
                    // Add a main document part. 
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                    // Create the document structure and add some text.
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());
                    
                 wordDocument.Close();
                }

            

            foreach (string filename in Directory.GetFiles(imagePath))
            {
                InsertAPicture(outputDocument, filename);
            }
        }



        public static void InsertAPicture(string document, string fileName)
        {
            try
            {
                using (WordprocessingDocument wordprocessingDocument =
                    WordprocessingDocument.Open(document, true))
                {
                    MainDocumentPart mainPart = wordprocessingDocument.MainDocumentPart;



                    ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);

                    using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        imagePart.FeedData(stream);
                        stream.Close();
                    }

                    AddImageToBody(wordprocessingDocument, mainPart.GetIdOfPart(imagePart), fileName);

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error writing images docX!", TraceLogLevels.ERROR);
                Trace.WriteLine(ex.Message, TraceLogLevels.ERROR);
                Trace.WriteLine(ex.StackTrace, TraceLogLevels.ERROR);
            }
        }


        private static void AddImageToBody(WordprocessingDocument wordDoc, string relationshipId, string fileName)
        {

            var img = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));
            var widthPx = img.PixelWidth;
            var heightPx = img.PixelHeight;
            var horzRezDpi = img.DpiX;
            var vertRezDpi = img.DpiY;
            const int emusPerInch = 914400;
            const int emusPerCm = 360000;
            var maxWidthCm = 16.51;
            var widthEmus = (long)(widthPx / horzRezDpi * emusPerInch);
            var heightEmus = (long)(heightPx / vertRezDpi * emusPerInch);
            var maxWidthEmus = (long)(maxWidthCm * emusPerCm);
            if (widthEmus > maxWidthEmus)
            {
                var ratio = (heightEmus * 1.0m) / widthEmus;
                widthEmus = maxWidthEmus;
                heightEmus = (long)(widthEmus * ratio);
            }

            // Define the reference of the image.
            var element =
                 new Drawing(
                     new DW.Inline(
                         new DW.Extent() { Cx = widthEmus, Cy = heightEmus },
                         new DW.EffectExtent()
                         {
                             LeftEdge = 0L,
                             TopEdge = 0L,
                             RightEdge = 0L,
                             BottomEdge = 0L
                         },
                         new DW.DocProperties()
                         {
                             Id = (UInt32Value)1U,
                             Name = "Picture 1"
                         },
                         new DW.NonVisualGraphicFrameDrawingProperties(
                             new A.GraphicFrameLocks() { NoChangeAspect = true }),
                         new A.Graphic(
                             new A.GraphicData(
                                 new PIC.Picture(
                                     new PIC.NonVisualPictureProperties(
                                         new PIC.NonVisualDrawingProperties()
                                         {
                                             Id = (UInt32Value)0U,
                                             Name = "New Bitmap Image.jpg"
                                         },
                                         new PIC.NonVisualPictureDrawingProperties()),
                                     new PIC.BlipFill(
                                         new A.Blip(
                                             new A.BlipExtensionList(
                                                 new A.BlipExtension()
                                                 {
                                                     Uri =
                                                       "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                                 })
                                         )
                                         {
                                             Embed = relationshipId,
                                             CompressionState = A.BlipCompressionValues.Print
                                         },
                                         new A.Stretch(
                                             new A.FillRectangle())),
                                     new PIC.ShapeProperties(
                                         new A.Transform2D(
                                             new A.Offset() { X = 0L, Y = 0L },
                                             new A.Extents() { Cx = widthEmus, Cy = heightEmus }),
                                         new A.PresetGeometry(
                                             new A.AdjustValueList()
                                         ) { Preset = A.ShapeTypeValues.Rectangle }))
                             ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                     )
                     {
                         DistanceFromTop = (UInt32Value)0U,
                         DistanceFromBottom = (UInt32Value)0U,
                         DistanceFromLeft = (UInt32Value)0U,
                         DistanceFromRight = (UInt32Value)0U,
                         EditId = "50D07946"
                     });

            // Append the reference to body, the element should be in a Run.
            wordDoc.MainDocumentPart.Document.Body.AppendChild(new Paragraph(new Run(element)));
        }



     
    }
}
