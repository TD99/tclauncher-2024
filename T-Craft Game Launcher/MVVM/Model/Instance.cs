using System.Collections.Generic;
using System;

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
        public Dictionary<string, List<string>> WorkingDirDesc { get; set; }
        public bool Is_Installed { get; set; }
        public string AppletURL { get; set; }
        public Instance()
        {
            ThumbnailURL = "/Images/nothumb.png";
            Is_Installed = false;
        }

        public Instance(string name, string displayName, Guid guid, string version, bool upgradeable, string thumbnailURL, string type, string mcVersion, string workingDirZipURL, Dictionary<string, List<string>> workingDirDesc, bool is_Installed, string appletURL)
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
            WorkingDirDesc = workingDirDesc;
            Is_Installed = is_Installed;
            AppletURL = appletURL;
        }
    }
}
