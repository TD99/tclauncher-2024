using System.Collections.Generic;
using System;

namespace T_Craft_Game_Launcher.MVVM.Model
{
    public class Instance
    {
        public enum InstanceType
        {
            Forge,
            Vanilla
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Guid Guid { get; set; }
        public string Version { get; set; }
        public bool Upgradeable { get; set; }
        public string ThumbnailURL { get; set; }
        public InstanceType Type { get; set; }
        public List<Server> Servers { get; set; }
        public List<string> RessourcePacks { get; set; }
        public List<string> Mods { get; set; }
        public List<Config> Config { get; set; }

        public Instance()
        {
            this.ThumbnailURL = "/Images/nothumb.png";
        }
    }
}
