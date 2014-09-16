using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PatNet.Lib
{
    public class TextWriterTraceListenerWithTimestamp : TextWriterTraceListener
    {
        public override void WriteLine(string message)
        {
            base.Write(DateTime.Now.ToString("<MM/dd/yy hh:mm:ss.fff>"));
            base.Write(": ");
            base.WriteLine(message);
        }
        public override void WriteLine(string message, string category)
        {
            base.Write(DateTime.Now.ToString("<MM/dd/yy hh:mm:ss.fff>"));            
            base.Write(category);
            base.Write(": ");
            base.WriteLine(message);
        }

        public TextWriterTraceListenerWithTimestamp(string fileName) : base(fileName.ToLower().EndsWith(".txt") ? fileName : fileName + ".txt")
        {
            Trace.AutoFlush = true;           
        }
        protected override void Dispose(bool disposing)
        {
            base.Flush();            
            base.Dispose(disposing);
        }

    }


    public static class TraceLogLevels
    {
        public const string DEBUG = "[Debug]";
        public const string WARN = "[Warn]";
        public const string ERROR = "[Error]";
        public const string INFO = "[Info]";
    }

   
}
