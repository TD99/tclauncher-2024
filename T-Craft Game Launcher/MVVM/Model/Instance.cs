using System.Collections.Generic;
using System;
using System.Linq;

namespace T_Craft_Game_Launcher.MVVM.Model
{
    public class Instance
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Guid Guid { get; set; }
        public string Version { get; set; }
        public bool Upgradeable { get; set; }
        public string ThumbnailURL { get; set; }
        public string Type { get; set; }
        public string McVersion { get; set; }
        public string WorkingDirZipURL { get; set; }
        public List<Patch> Patches { get; set; }
        public bool UsePatch { get; set; }
        public Dictionary<string, List<string>> WorkingDirDesc { get; set; }
        public bool Is_Installed { get; set; }
        public string AppletURL { get; set; }
        public Dictionary<string, object> Requirements { get; set; }

        public Instance()
        {
            ThumbnailURL = "/Images/nothumb.png";
            Is_Installed = false;
        }

        public Instance(string name, string displayName, Guid guid, string version, bool upgradeable, string thumbnailURL, string type, string mcVersion, string workingDirZipURL, List<Patch> patches, bool usePatch, Dictionary<string, List<string>> workingDirDesc, bool is_Installed, string appletURL, Dictionary<string, object> requirements)
        {
            Name = name;
            DisplayName = displayName;
            Guid = guid;
            Version = version;
            Upgradeable = upgradeable;
            ThumbnailURL = thumbnailURL;
            Type = type;
            McVersion = mcVersion;
            WorkingDirZipURL = workingDirZipURL;
            Patches = patches;
            UsePatch = usePatch;
            WorkingDirDesc = workingDirDesc;
            Is_Installed = is_Installed;
            AppletURL = appletURL;
            Requirements = requirements;
        }

        public Patch GetCurrentPatch()
        {
            if (Patches == null) return null;

            return Patches.OrderByDescending(p => p.ID).FirstOrDefault();
        }
    }
}
