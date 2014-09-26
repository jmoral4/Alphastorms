using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using PatNet.Lib;

namespace PatNet.ConsoleApp
{
    class Program
    {
        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            
        }

        public enum ApplicationStates { RUNNING, SHUTDOWN }

        public static ApplicationStates AppState;

        static void Main(string[] args)
        {
            //FileService fsvc = new FileService(workingDir);
            //fsvc.InitializeEnvironment();

            // INPUT, OUTPUT, BACKUP, TEMP
            //setup environment      
            string workingDir = ReadSetting("WorkingDirectory");
            CreateDirectory(workingDir);
            Directory.SetCurrentDirectory(workingDir);            
            CreateDirectory("Input");
            CreateDirectory("Output");
            CreateDirectory("Backup");
            CreateDirectory("_temp");
            AppState = ApplicationStates.RUNNING;



            //NOTE:
            //  1. number is derived from shipment folder detected
            //  2. once folder is processed, enitre folder should be copied to /backup 
            Console.WriteLine("App started. Init FolderWatcher.");
            var today = DateTime.Now.ToString("MMddyy");
            TextWriterTraceListenerWithTimestamp timeStampListener =
                   new TextWriterTraceListenerWithTimestamp( today);
            ConsoleTraceListener ct = new ConsoleTraceListener();
            Trace.Listeners.Add(timeStampListener);
            Trace.Listeners.Add(ct);
            Trace.WriteLine("App started. Init FolderWatcher.", TraceLogLevels.INFO);
                 
            Trace.WriteLine("configured working directory " + workingDir, TraceLogLevels.INFO);
            
            Trace.WriteLineIf(true, "test of true", TraceLogLevels.WARN);

            FolderWatcher fw = new FolderWatcher(workingDir + "Input");

            while (AppState == ApplicationStates.RUNNING)
            {
                if (fw.WatcherState == FolderWatcher.WatcherStates.INIT)
                {
                    fw.Start();
                    fw.WatcherState = FolderWatcher.WatcherStates.WAITING;
                    Trace.WriteLine("Started Watching for updates. Current State: " + fw.WatcherState.ToString(), TraceLogLevels.INFO);
                }

                if (fw.WatcherState == FolderWatcher.WatcherStates.COMPLETED)
                {
                    Trace.WriteLine("FolderWatcher reported copying was completed! Beginning processing.", TraceLogLevels.INFO);
                    fw.Stop();                    
                    var number = fw.LastShipmentNumber;
                    var path = workingDir + @"Input\" + number;
                    var outputPath = workingDir + @"Output\" + number;
                    Shipment shipment = new Shipment(path, number);
                    Trace.WriteLine("Validating Shipment", TraceLogLevels.INFO);
                    if (shipment.Validate(path))
                    {
                        Trace.WriteLine("Shipment was valid! Processing!", TraceLogLevels.INFO);
                        shipment.Process(path, outputPath);
                    }
                    else
                    {
                        Trace.WriteLine("Shipment was invalid!", TraceLogLevels.ERROR);
                        
                    }
                    
                    fw.WatcherState = FolderWatcher.WatcherStates.INIT;
                }
            }

            //fw.Start();

            //while (fw.WatcherState != FolderWatcher.WatcherStates.COMPLETED)
            //{


            //}


            //while (fw.WatcherState == FolderWatcher.WatcherStates.WAITING)
            //{
            //}


            ////got something

            //while (fw.WatcherState == FolderWatcher.WatcherStates.COPYING)
            //{
            //}

            ////done copying

            ////get shipment
            //var shipmentNum = fw.LastShipmentNumber;

          




            //var number = "4853b";
            //var path = ReadSetting("RootPath");
            ////var path = @"E:\UNICOR\Information for Jonathan\export\4853";
            ////var path = @"C:\DEV\UNICOR\Information for Jonathan\4853";

            //Trace.WriteLine("Recv'd Shipment number " + number, TraceLogLevels.INFO);
            //Trace.WriteLine("Processing directory: " + path, TraceLogLevels.INFO);

            ////FolderWatcher fw = new FolderWatcher(@"C:\Temp\input", 500);
            ////fw.Watch();
            //Shipment shipment = new Shipment(path, number);            
            //if (shipment.Validate(path))
            //{
            //    shipment.Process();
            //}

            Console.WriteLine("Press any key to Exit");
            Console.ReadKey();

        }

        static string ReadSetting(string key)
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

    }
}
