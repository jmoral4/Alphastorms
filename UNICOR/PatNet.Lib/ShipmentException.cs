using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatNet.Lib
{
    public class ShipmentException : Exception
    {
        public ShipmentException(string msg) : base(msg)
        {
        }
    }

    public class ShipmentFileMissingException : FileNotFoundException
    {
        public ShipmentFileMissingException(string msg) : base(msg)
        {
        }        
    }

    public class ShipmentDirectoryNotFoundException : DirectoryNotFoundException
    {
        public ShipmentDirectoryNotFoundException(string msg) : base(msg)
        {
        }

        public ShipmentDirectoryNotFoundException()
        {
        }
    }
}
