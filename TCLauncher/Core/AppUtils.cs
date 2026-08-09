using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using CmlLib.Core;
using TCLauncher.Properties;
using static TCLauncher.Core.IoUtils.Tcl;

namespace TCLauncher.Core
{
    public static class AppUtils
    {
        public static DateTime GetCompilationDate()
        {
            return File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static Dictionary<string, object> GetAllSettings()
        {
            var settings = new Dictionary<string, object>();
            foreach (SettingsProperty property in Settings.Default.Properties)
                settings[property.Name] = Settings.Default[property.Name];
            return settings;
        }

        public static MinecraftPath GetMinecraftPathShared(Guid instanceGuid)
        {
            var path = GetMinecraftPathIsolated(instanceGuid);
            path.Versions = Path.Combine(SharedPath, "versions");
            path.Library = Path.Combine(SharedPath, "libraries");
            return path;
        }

        public static MinecraftPath GetMinecraftPathIsolated(Guid instanceGuid)
        {
            return new MinecraftPath(GetInstanceDataPath(instanceGuid));
        }
    }
}
