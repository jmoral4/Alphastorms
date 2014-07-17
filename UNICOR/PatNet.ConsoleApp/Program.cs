using System;
using System.Collections.Generic;
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
            Console.WriteLine("App started. Init FolderWatcher.");
            //FolderWatcher fw = new FolderWatcher(@"C:\Temp\input", 500);
            //fw.Watch();
            Shipment shipment = new Shipment();
            shipment.Path = @"E:\UNICOR\Information for Jonathan\export\4853";
            shipment.Number = "4853";
            shipment.Validate();
          

            Console.WriteLine("Press any key to Exit");
            Console.ReadKey();

        }
    }
}
