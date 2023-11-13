using System;
using System.Collections.Generic;
using CmlLib.Core;

namespace T_Craft_Game_Launcher.Models
{
    public class DebugObject
    {
        public CMLauncher Launcher { get; set; }
        public string[] PathRegistry { get; set; }
        public string Version { get; set; }
        public string NewestVersion { get; set; }
        public bool? IsUpgradeable { get; set; }
        public string Args { get; set; }
        public string FriendlyName { get; set; }
        public string UriScheme { get; set; }
        public Uri UriArgs { get; set; }
        public bool? IsSilent { get; set; }
        public bool? KillOld { get; set; }
        public bool? IsInternetAvailable { get; set; }
        public bool? IsTcraftReacheable { get; set; }
        public Dictionary<string, double> TotalAdapterMemoryInGb { get; set; }
        public double? TotalPhysicalMemoryInGb { get; set; }
        public string[] LoadedFonts { get; set; }
        public string[] LoadedPlugins { get; set; }

        public DebugObject(CMLauncher launcher, string[] pathRegistry, string version, string newestVersion, bool? isUpgradeable, string args, string friendlyName, string uriScheme, Uri uriArgs, bool? isSilent, bool? killOld, bool? isInternetAvailable, bool? isTcraftReacheable, Dictionary<string, double> totalAdapterMemoryInGb, double? totalPhysicalMemoryInGb, string[] loadedFonts, string[] loadedPlugins)
        {
            Launcher = launcher;
            PathRegistry = pathRegistry;
            Version = version;
            NewestVersion = newestVersion;
            IsUpgradeable = isUpgradeable;
            Args = args;
            FriendlyName = friendlyName;
            UriScheme = uriScheme;
            UriArgs = uriArgs;
            IsSilent = isSilent;
            KillOld = killOld;
            IsInternetAvailable = isInternetAvailable;
            IsTcraftReacheable = isTcraftReacheable;
            TotalAdapterMemoryInGb = totalAdapterMemoryInGb;
            TotalPhysicalMemoryInGb = totalPhysicalMemoryInGb;
            LoadedFonts = loadedFonts;
            LoadedPlugins = loadedPlugins;
        }

        public DebugObject()
        {
        }
    }
}
