
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
        public enum WatcherStates { INIT,WAITING, COPYING,  COMPLETED }

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
            WatcherState  = WatcherStates.INIT;
        }

        public void Start()
        {
           // Thread.Sleep(1000); //allow main thread a moment to continue before potentially trigger signals
            _fsw.EnableRaisingEvents = true;
            _dsw.EnableRaisingEvents = true;
            _processingTimer.Enabled = true;
            WatcherState = WatcherStates.WAITING;
        }
        public void Stop()
        {
            _fsw.EnableRaisingEvents = false;
            _dsw.EnableRaisingEvents = true;
            _processingTimer.Enabled = false;
            WatcherState = WatcherStates.INIT;
        }

        public bool IsReceiving { get { return _receiving; } }
        public string LastShipmentNumber = "";

        private void CheckIfDone(object source, ElapsedEventArgs args)
        {
            
            if (WatcherState == WatcherStates.COPYING)
            {
                _keepAlive -= interval;
                if (_keepAlive <= 0)
                {
                    
                    //haven't gotten an update in 2 minutes, assume copying is completed
                    _receiving = false;
                    Trace.WriteLine("Nothing Heard! all done.", TraceLogLevels.INFO);
                    WatcherState = WatcherStates.COMPLETED;
                }
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


}
