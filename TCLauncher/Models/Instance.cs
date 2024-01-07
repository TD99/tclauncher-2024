using System.Collections.Generic;
using System;
using System.Linq;

namespace TCLauncher.Models
{
    public class Instance
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Guid Guid { get; set; }
        public string Version { get; set; }
        public bool Upgradeable { get; set; }
        public string ThumbnailURL { get; set; } = "/Images/nothumb.png";
        public string Type { get; set; }
        public string McVersion { get; set; }
        public string WorkingDirZipURL { get; set; }
        public List<Patch> Patches { get; set; }
        public bool UsePatch { get; set; }
        public Dictionary<string, List<string>> WorkingDirDesc { get; set; }
        public bool Is_Installed { get; set; }
        public string AppletURL { get; set; }
        public Dictionary<string, object> Requirements { get; set; }
        public List<Server> Servers { get; set; }

        public int? MinimumRamMb { get; set; }
        public int? MaximumRamMb { get; set; }
        public string[] JVMArguments { get; set; }
        

        public Instance()
        {
        }

        public Instance(string name, string displayName, Guid guid, string version, bool upgradeable, string thumbnailUrl, string type, string mcVersion, string workingDirZipUrl, List<Patch> patches, bool usePatch, Dictionary<string, List<string>> workingDirDesc, string appletUrl, Dictionary<string, object> requirements, List<Server> servers, int? minimumRamMb, int? maximumRamMb, string[] jvmArguments)
        {
            Name = name;
            DisplayName = displayName;
            Guid = guid;
            Version = version;
            Upgradeable = upgradeable;
            ThumbnailURL = thumbnailUrl;
            Type = type;
            McVersion = mcVersion;
            WorkingDirZipURL = workingDirZipUrl;
            Patches = patches;
            UsePatch = usePatch;
            WorkingDirDesc = workingDirDesc;
            AppletURL = appletUrl;
            Requirements = requirements;
            Servers = servers;
            MinimumRamMb = minimumRamMb;
            MaximumRamMb = maximumRamMb;
            JVMArguments = jvmArguments;
        }

        public Patch GetCurrentPatch()
        {
            return Patches?.OrderByDescending(p => p.ID).FirstOrDefault();
        }
    }
}
