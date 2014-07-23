using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatNet.Lib
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class ShipmentException : Exception
    {
        public ShipmentException(string msg) : base(msg)
        {
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class ShipmentFileMissingException : FileNotFoundException
    {
        public ShipmentFileMissingException(string msg) : base(msg)
        {
        }        
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
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
