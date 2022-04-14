using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alphastorms.Shared.BaseServices;
using System.Net;

namespace Alphastorms.Downloader.Win
{
    internal class DownloaderConfig
    {
        /*
         ServerIp=127.0.0.1
ServerPort=2790
DefaultImage=""
DefaultWelcome="Alphastorms Downloader"
DefaultMessage="Connecting..."
Version="0.1.4.13.22"
         */

        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string DefaultImage { get; set; }
        public string WelcomeMessage { get; set; }
        public string DefaultMessage { get; set; }
        public string DefaultStateMessage { get; set; }
        public string Version { get; set; }

        public static DownloaderConfig FromIni(string iniFile)
        {
            DownloaderConfig config = new DownloaderConfig();
            IniReader reader = new IniReader(iniFile);
            config.IpAddress = reader.GetValue("ServerIp");
            config.Port = Int32.Parse(reader.GetValue("ServerPort"));
            config.DefaultImage = reader.GetValue("DefaultImage");
            config.WelcomeMessage = reader.GetValue("DefaultWelcome");
            config.DefaultMessage = reader.GetValue("DefaultMessage");
            config.DefaultStateMessage = reader.GetValue("DefaultStateMessage");
            
            return config;

        }
    }
}
