using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphastorms.Downloader.Win
{
    internal class DownloaderApp
    {
        public DownloaderApp(DownloaderConfig config)
        { 
        
        }

        // gather basic pc stats  (cpu cores, gpu type)
        public void GatherLocalStats()
        { }

        // download the latest server message
        public void DownloadLatestMessage()
        { }

        // download execution plan which defines which actions we have to take
        public void DownloadExecutionPlan()
        { }

        // performed multiple times to validate stages of the execution plan
        public void Validate()
        { }

        // final authentication where the final signatures are uploaded and verified, a key is delivered to allow entry to the game
        public void PerformFinalAuthentication()
        { }
    }
}
