
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace PatNet.Lib
{     

    public class FolderWatcher
    {
        readonly int _folderWatchTimeout;
        private readonly FileSystemWatcher _fsw;
        private DateTime _lastTimeStamp;
        private bool _processing = true;

        public FolderWatcher(string path, int folderWatchTimeout)
        {
            _folderWatchTimeout = folderWatchTimeout;
            _fsw = new FileSystemWatcher {Path = path};
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Watch()
        {
            // objectives:
            // duplicate shipment check
            // wait for .LST file
            // when .LST file is spotted, verify all files present
            // while there are files that are locked (being copied) or new, continue waiting on .LST file
            // detect files are copied. renew timer each time we find a file locked or an onchanged event triggers

            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            _fsw.NotifyFilter =NotifyFilters.CreationTime
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;            

            // Add event handlers.
            _fsw.Changed += new FileSystemEventHandler(OnChanged);
            _fsw.Created += new FileSystemEventHandler(OnChanged);
            _fsw.Deleted += new FileSystemEventHandler(OnChanged);
            _fsw.IncludeSubdirectories = true;
            // Begin watching.
            _fsw.EnableRaisingEvents = true;

            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Q)
                {
                    break;
                }

                if (_processing)
                {
                    //how much time has elapsed
                    var diff = DateTime.Now.Subtract(_lastTimeStamp);
                    Console.WriteLine("Total Milliseconds from last alert:" + diff.TotalMilliseconds);
                    if (diff.TotalMilliseconds > _folderWatchTimeout)
                    {
                        Console.WriteLine("Timeout ended. Processing Over!");
                        _processing = false;
                    }

                }

            }
            
        }

        // Define the event handlers. 
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            _processing = true;
            _lastTimeStamp = DateTime.Now;
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

    }
}
