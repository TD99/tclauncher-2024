using System;
using System.Collections.Generic;

namespace T_Craft_Game_Launcher.MVVM.Model
{
    class InstalledInstance
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Guid Guid { get; set; }
        public string AppletURL { get; set; }
        public List<Server> Servers { get; set; }

        public InstalledInstance(string name, string displayName, Guid guid, string appletURL, List<Server> servers)
        {
            Name = name;
            DisplayName = displayName;
            Guid = guid;
            AppletURL = appletURL;
            Servers = servers;
        }
    }
}
