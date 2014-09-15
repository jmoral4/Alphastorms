
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace PatNet.Lib
{     

    public class FolderWatcher
    {
        private const int MAX_KEEP_ALIVE = 300000;
        private readonly FileSystemWatcher _fsw;
        private DateTime _lastTimeStamp;
        private bool _processing = true;
        private Timer _processingTimer ;
        private int _keepAlive = MAX_KEEP_ALIVE; // 300000 or 5 minutes in milliseconds
        private const int _interval = 5000;


        private enum ProcessingStates
        {
            WAITING, COPYING, PROCESSING
        }

        private ProcessingStates ProcessingState;


        public FolderWatcher(string path)
        {
            ProcessingState = ProcessingStates.WAITING;            

            // monitor filesystem for changes
            _fsw = new FileSystemWatcher { Path = path };
            _fsw.NotifyFilter = NotifyFilters.CreationTime
              | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Add event handlers.
            _fsw.Changed += new FileSystemEventHandler(OnChanged);
            _fsw.Created += new FileSystemEventHandler(OnChanged);
            // _fsw.Deleted += new FileSystemEventHandler(OnChanged);
            _fsw.IncludeSubdirectories = true;
            // Begin watching.
            _fsw.EnableRaisingEvents = true; 

            //create a timer which will eventually launch based on the results of the file system watcher
            _processingTimer = new Timer(_interval);
            _processingTimer.Elapsed +=Watch;
            _processingTimer.Enabled = true;
            
            
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Watch(object source, ElapsedEventArgs args)
        {
            // objectives:
            // duplicate shipment check
            // wait for .LST file
            // when .LST file is spotted, verify all files present
            // while there are files that are locked (being copied) or new, continue waiting on .LST file
            // detect files are copied. renew timer each time we find a file locked or an onchanged event triggers

            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */


            //check to see if we've heard from the copying in a bit or if we have all the .LST files to continue

            // initial implementation is a state machine moving from waiting to copying to processing to waiting

            if (ProcessingState == ProcessingStates.COPYING)
            {
                _keepAlive -= _interval;
                if (_keepAlive <= 0)                
                {
                    //assume we're done copying, haven't heard from anything in 5 minutes
                    ProcessingState = ProcessingStates.PROCESSING;
                }

            }


            if (ProcessingState == ProcessingStates.PROCESSING)
            {
                //wait for completion... do nothing

            }



            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }

                if (_processing)
                {
                    //how much time has elapsed
                    var diff = DateTime.Now.Subtract(_lastTimeStamp);
                    Console.WriteLine("Total Milliseconds from last alert:" + diff.TotalMilliseconds);
                    //if (diff.TotalMilliseconds > _folderWatchTimeout)
                    //{
                    //    Console.WriteLine("Timeout ended. Processing Over!");
                    //    _processing = false;
                    //}

                }

            }
            
        }

        // Define the event handlers. 
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                //notify that we started receiving something
                if (ProcessingState != ProcessingStates.PROCESSING)
                {
                    ProcessingState = ProcessingStates.COPYING;
                    _keepAlive = MAX_KEEP_ALIVE; //effectively push back the count
                }
                else
                {
                    //we're already processing! Log this as a potential error
                    //TODO: Log
                    
                }
            }

            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("Object: " + e.FullPath + " " + e.ChangeType);                        
        }

    }
}
