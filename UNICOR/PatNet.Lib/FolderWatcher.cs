
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Office.Interop.Word;
using Timer = System.Timers.Timer;
using System.Diagnostics;

namespace PatNet.Lib
{

    public class FolderWatcher
    {
        public enum WatcherStates { WAITING, COPYING }

        public WatcherStates WatcherState;
        private bool _gotAShipment;
        private bool _receiving;
        private const int MAX_KEEP_ALIVE = 30000; // 2 minutes or 120000 milliseconds
        private readonly FileSystemWatcher _fsw;
        private readonly FileSystemWatcher _dsw;
        private int _keepAlive = MAX_KEEP_ALIVE;
        private Timer _processingTimer ;
        private int interval = 1000;

        public FolderWatcher(string path)
        {
            // monitor filesystem for changes
            _fsw = new FileSystemWatcher { Path = path };
            _fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName; 
            _fsw.Created += new FileSystemEventHandler(OnChanged);
            _fsw.IncludeSubdirectories = true;
            _dsw = new FileSystemWatcher {Path = path};
            _dsw.NotifyFilter = NotifyFilters.DirectoryName;
            _dsw.IncludeSubdirectories = false;
            _dsw.Created += new FileSystemEventHandler(OnChanged);
            _receiving = false;
              _processingTimer = new Timer(interval);
            _processingTimer.Elapsed += CheckIfDone;
            WatcherState  = WatcherStates.WAITING;
        }

        public void Start()
        {
           // Thread.Sleep(1000); //allow main thread a moment to continue before potentially trigger signals
            _fsw.EnableRaisingEvents = true;
            _dsw.EnableRaisingEvents = true;
            _processingTimer.Enabled = true;
        }
        public void Stop()
        {
            _fsw.EnableRaisingEvents = false;
            _dsw.EnableRaisingEvents = true;
            _processingTimer.Enabled = false;
        }

        public bool IsReceiving { get { return _receiving; } }
        public string LastShipmentNumber = "";

        private void CheckIfDone(object source, ElapsedEventArgs args)
        {
            _keepAlive -= interval;
            if (_keepAlive <= 0)
            {
                //haven't gotten an update in 2 minutes, assume copying is completed
                _receiving = false;
                Trace.WriteLine("Nothing Heard! all done.", TraceLogLevels.INFO);
                WatcherState = WatcherStates.WAITING;                
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            WatcherState = WatcherStates.COPYING;        
            if (source == _dsw)
            {
              
                //directory written
                LastShipmentNumber = e.Name;
                _receiving = true; 
                Trace.WriteLine("Rec'd patent folder " + LastShipmentNumber, TraceLogLevels.INFO);
            }
            else          
            {                
                _receiving = true;
                _keepAlive = MAX_KEEP_ALIVE; //refresh
                Trace.WriteLine("Rec'd data _keepAlive " + e.Name, TraceLogLevels.INFO);
            }

           
          
        }

    }



    public class FolderWatcher2
    {
        private const int MAX_KEEP_ALIVE = 300000; // 5 minutes or 300000 milliseconds
        private readonly FileSystemWatcher _fsw;
        private DateTime _lastTimeStamp;
        private bool _processing = true;
        private Timer _processingTimer ;
        private int _keepAlive = MAX_KEEP_ALIVE; 
        private const int _interval = 1000;


        private enum ProcessingStates
        {
            WAITING, COPYING, PROCESSING
        }

        private ProcessingStates ProcessingState;


        public FolderWatcher2(string path)
        {
            ProcessingState = ProcessingStates.WAITING;

            //create a timer which will eventually launch based on the results of the file system watcher
            _processingTimer = new Timer(_interval);
            _processingTimer.Elapsed += Watch;
            //_processingTimer.Enabled = true;

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
            //_fsw.EnableRaisingEvents = true; 

        }

        public void Start()
        {
            _fsw.EnableRaisingEvents = true;
            _processingTimer.Enabled = true;
        }

        public void Stop()
        {
            _fsw.EnableRaisingEvents = false;
            _processingTimer.Enabled = false;
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void Watch(object source, ElapsedEventArgs args)
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

      
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                //we can only move to copying from waiting
                if (ProcessingState == ProcessingStates.WAITING)
                {
                    ProcessingState = ProcessingStates.COPYING;
                    _keepAlive = MAX_KEEP_ALIVE; //effectively push back the count
                }
                else if( ProcessingState == ProcessingStates.COPYING)
                {
                    //already processing, push back the state
                    _keepAlive = MAX_KEEP_ALIVE;                    
                }
            }

            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("Object: " + e.FullPath + " " + e.ChangeType);                        
        }

    }
}
