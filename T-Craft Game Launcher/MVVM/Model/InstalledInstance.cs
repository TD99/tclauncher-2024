using System;

namespace T_Craft_Game_Launcher.MVVM.Model
{
    class InstalledInstance
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Guid Guid { get; set; }
        public string AppletURL { get; set; }

        public InstalledInstance(string name, string displayName, Guid guid, string appletURL)
        {
            Name = name;
            DisplayName = displayName;
            Guid = guid;
            AppletURL = appletURL;
        }
    }
}
