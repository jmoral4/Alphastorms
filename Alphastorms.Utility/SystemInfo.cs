using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RogueSquadLib.BaseServices
{
    public sealed class OSInfo
    {
        private static readonly Lazy<OSInfo>
            lazy =
            new Lazy<OSInfo>
                (() => new OSInfo());

        public static OSInfo Instance { get { return lazy.Value; } }
        
        public int CPUCores { get; set; } 
        public string OSVersion { get; set; }                  
        public bool IsHiDefSupported { get; set; }
        public string OSDescription { get; set; }
        public string OSArchitecture { get; set; }
        public string ProcessArchitecture { get; set; }
        public string Framework { get; set; }
        public string FrameworkDescription { get; set; }
        public string OSName { get; set; }

        public GraphicsAdapter DefaultGraphicsCard => GraphicsAdapter.DefaultAdapter;
        public ReadOnlyCollection<GraphicsAdapter> GraphicsCards { get; set; }

        public long MemoryInUse => GC.GetTotalMemory(false);
        public long ApplicationMemory => Environment.WorkingSet;

        private OSInfo()
        {
            GraphicsCards = GraphicsAdapter.Adapters;
            IsHiDefSupported = DefaultGraphicsCard.IsProfileSupported(GraphicsProfile.HiDef);
            OSName = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                     System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                     System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" : "Unknown";
            OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;          
            OSVersion = Environment.OSVersion.VersionString;
            OSArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
            ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
            FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Framework = Assembly
                    .GetEntryAssembly()?
                    .GetCustomAttribute<TargetFrameworkAttribute>()?
                    .FrameworkName;
            CPUCores =  Environment.ProcessorCount;
        }

        public void DebugListAllStats() {

            Debug.WriteLine($"-----------------------------------------------------");
            Debug.WriteLine($"GRAPHICS: >>>>>>>>>>>>>>>>>>>>>>");
            Debug.WriteLine($"Supports HD Graphics:{IsHiDefSupported}");
            Debug.WriteLine($"DefaultGPU: {DefaultGraphicsCard.Description} Res:{DefaultGraphicsCard.CurrentDisplayMode.Width}x{DefaultGraphicsCard.CurrentDisplayMode.Height} Ra:{DefaultGraphicsCard.CurrentDisplayMode.AspectRatio} Wide:{DefaultGraphicsCard.IsWideScreen}");            
            Debug.WriteLine("OS: >>>>>>>>>>>>>>>>>>>>>");
            Debug.WriteLine($"Name:{OSName} Version:{OSVersion} Desc:{OSDescription}");
            Debug.WriteLine($"ProcessorArchitecture:{ProcessArchitecture} CPUCores:{CPUCores}");
            Debug.WriteLine($"AvailableApplicationMemory:{ApplicationMemory / 1024 / 1024}MB MemoryInUse:{MemoryInUse / 1024 / 1024}");
            Debug.WriteLine(".NET FRAMEWORK: >>>>>>>>>>>>>>>>>>>>>>");
            Debug.WriteLine($"Framework:{Framework} Desc:{FrameworkDescription}");
            Debug.WriteLine("DISK: >>>>>>>>>>>>>>>>>>>>>>>");
            Debug.WriteLine($"Free Disk Space: {GetFreeDiskSpace() / 1024 / 1024 / 1044}GB");

            Debug.WriteLine("SUPPORTED RESOLUTIONS: >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            foreach (var displayMode in DefaultGraphicsCard.SupportedDisplayModes)
            {
                Debug.WriteLine($"Ratio:{displayMode.AspectRatio} Res:{displayMode.Width}x{displayMode.Height} {displayMode.Format}");
            }

        }

        public long GetFreeDiskSpace(string driveName = "" )
        {
            if (String.IsNullOrEmpty(driveName))
                driveName = System.AppContext.BaseDirectory;

            return new DriveInfo(driveName).AvailableFreeSpace;
        }

        public DriveInfo[] GetAllDrives() {
            return DriveInfo.GetDrives();
        }
    }

    public static class VersionInfo
    {
        public static string GetVersionString()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            DateTime buildDate = new DateTime(2000, 1, 1)
                                    .AddDays(version.Build).AddSeconds(version.Revision * 2);
            return $"{version} ({buildDate})";
        }
    }

}
