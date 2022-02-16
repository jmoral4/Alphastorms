using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace RogueSquadLib.Util
{
   public class JsonTypeLoader<T> where T: class
    {
        public string FileName { get; set; }
        DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(T));

        public T LoadType()
        {
            if (!File.Exists(FileName)) return default(T);

            using (FileStream stream = new FileStream(FileName, FileMode.Open))
            {
                return (T)_serializer.ReadObject(stream);
            }
        }
        public void SaveType(T type)
        {
            using (FileStream stream = new FileStream(FileName, FileMode.Create))
            {
                _serializer.WriteObject(stream, type);
            }
        }
    }  
}
