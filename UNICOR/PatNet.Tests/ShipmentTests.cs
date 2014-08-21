using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PatNet.Lib;

namespace PatNet.Tests
{
    [TestClass]
    public class ShipmentTests
    {
        readonly string testPath = System.Environment.CurrentDirectory + @"\4585\";
        [TestMethod]
        [ExpectedException(typeof(ShipmentDirectoryNotFoundException))]
        public void MissingDirectoryTest() 
        {
            DeleteTestDir();
            Shipment s = new Shipment(testPath, "4585");            
            //s.LoadLSTManifestFiles(testPath);            
        }

        [TestMethod]
        [ExpectedException(typeof(ShipmentException))]
        public void TooManyLstTest()
        {
            DeleteTestDir();
            CreateTestDir();

            File.CreateText(testPath + @"ShipA.LST").Close();
            File.CreateText(testPath + @"ShipT.LST").Close();
            File.CreateText(testPath + @"ShipG.LST").Close();
            Shipment s = new Shipment(testPath, "4585");
            //s.LoadLSTManifestFiles(testPath);
        }

        [TestMethod]
        public void TooManyLstTestInverse()
        {
            DeleteTestDir();
            CreateTestDir();
            File.CreateText(testPath + @"ShipA.LST").Close();
            File.CreateText(testPath + @"ShipT.LST").Close();
            var s = new Shipment(testPath, "4585");
            //s.LoadLSTManifestFiles(testPath);
        }

        [TestMethod]       
        public void MissingShipALoad()
        {
            DeleteTestDir();
            CreateTestDir();

            File.CreateText(testPath + @"ShipT.LST").Close();                       
            var s = new Shipment(testPath, "4585");
            //s.LoadLSTManifestFiles(testPath);
        }
        [TestMethod]
        [ExpectedException(typeof(ShipmentFileMissingException))]
        public void MissingShipTLoad()
        {
            DeleteTestDir();
            CreateTestDir();

            File.CreateText(testPath + @"ShipA.LST").Close();
            var s = new Shipment(testPath, "4585");
            //s.LoadLSTManifestFiles(testPath);
        }

        private void CreateTestDir()
        {            
            if (!Directory.Exists(testPath))
                Directory.CreateDirectory(testPath);
        }

        private void DeleteTestDir()
        {           
            if (Directory.Exists(testPath))
                Directory.Delete(testPath, true);
        }
    }
}
