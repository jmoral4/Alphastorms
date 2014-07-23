using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatNet.Lib;

namespace PatNet.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //NOTE:
            //  1. number is derived from shipment folder detected
            //  2. once folder is processed, enitre folder should be copied to /backup 
            Console.WriteLine("App started. Init FolderWatcher.");
            var today = DateTime.Now.ToString("MMddyy");
            TextWriterTraceListenerWithTimestamp timeStampListener =
                   new TextWriterTraceListenerWithTimestamp(today);
            ConsoleTraceListener ct = new ConsoleTraceListener();
            Trace.Listeners.Add(timeStampListener);
            Trace.Listeners.Add(ct);            
            Trace.WriteLine("App started. Init FolderWatcher.", TraceLogLevels.INFO);
            Trace.WriteLineIf(true, "test of true", TraceLogLevels.WARN);
            var number = "4853";
            var path = @"E:\UNICOR\Information for Jonathan\export\4853";

            Trace.WriteLine("Recv'd Patent number " + number, TraceLogLevels.INFO);
            Trace.WriteLine("Processing directory: " + path, TraceLogLevels.INFO);

            //FolderWatcher fw = new FolderWatcher(@"C:\Temp\input", 500);
            //fw.Watch();
            Shipment shipment = new Shipment(path, number);
            shipment.Validate(path);
            if (shipment.IsValid)
            {
                shipment.Process();
            }

            Console.WriteLine("Press any key to Exit");
            Console.ReadKey();

        }
    }
}
