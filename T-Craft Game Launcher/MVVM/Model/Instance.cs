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

        public Instance()
        {
            this.ThumbnailURL = "/Images/nothumb.png";
        }
        public Instance(bool isMessage)
        {
            if (isMessage)
            {
                
            } else
            {
                this.ThumbnailURL = "/Images/nothumb.png";
            }
        }
    }
}
