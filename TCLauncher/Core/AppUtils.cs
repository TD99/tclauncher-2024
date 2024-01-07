using System;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using static TCLauncher.Core.IoUtils.Tcl;
using System.Threading.Tasks;
using System.Windows.Media;
using TCLauncher.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Management.Instrumentation;
using CmlLib.Core;

namespace TCLauncher.Core
{
    /// <summary>
    /// A utility class for handling application updates and installations.
    /// </summary>
    public static class AppUtils
    {
        /// <summary>
        /// Checks for a new version of the application.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a boolean value indicating whether a new version is available.
        /// </returns>
        public static async Task<bool> CheckForNewVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var compilationDate = File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
            var updateApiUrl = $"https://tcraft.link/tclauncher/api/plugins/version-checker/?version={version}&date={compilationDate:yyyy-MM-dd}";

            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(updateApiUrl);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync();

            var obj = JObject.Parse(content);
            var isNew = (bool)obj["new"];

            return isNew;
        }

        /// <summary>
        /// Retrieves the name of the newest version of the application.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a string representing the name of the newest version.
        /// </returns>
        public static async Task<string> GetNewestVersionName()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var compilationDate = File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
            var updateApiUrl = $"https://tcraft.link/tclauncher/api/plugins/version-checker/?version={version}&date={compilationDate:yyyy-MM-dd}";

            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(updateApiUrl);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();

            var obj = JObject.Parse(content);
            var newVersion = (string)obj["version"];

            return newVersion;
        }

        /// <summary>
        /// Retrieves the URL of the MSI installer for the newest version of the application.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a string representing the URL of the MSI installer.
        /// </returns>
        public static async Task<string> GetMsiUrl()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var compilationDate = File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
            var updateApiUrl = $"https://tcraft.link/tclauncher/api/plugins/version-checker/?version={version}&date={compilationDate:yyyy-MM-dd}";

            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(updateApiUrl);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();

            var obj = JObject.Parse(content);
            var msi = (string)obj["msi"];

            return msi;
        }

        /// <summary>
        /// Retrieves the current version of the application.
        /// </summary>
        /// <returns>
        /// A string representing the current version of the application.
        /// </returns>
        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return version;
        }


        /// <summary>
        /// Checks for updates and prompts the user to install if a new version is available.
        /// </summary>
        /// <param name="userInitiated">Indicates whether the update check was initiated by the user.</param>
        public static async void HandleUpdates(bool userInitiated = false)
        {
            try
            {
                var isNew = await CheckForNewVersion();
                var newVersion = await GetNewestVersionName();
                var version = GetCurrentVersion();
                var msi = await GetMsiUrl();

                if (isNew)
                {
                    var result = MessageBox.Show($"Eine neuere Version ({newVersion}) von TCLauncher ist verfügbar. Jetzt installieren?", "TCLauncher", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        InstallUpdates(msi);
                    }
                }
                else if (userInitiated)
                {
                    MessageBox.Show($"Die neuste Version ({version}) ist bereits installiert.", "TCLauncher");
                }
            }
            catch
            {
                // TODO: Don't ignore
            }
        }


        /// <summary>
        /// Installs updates from a specified URL and shuts down the current application.
        /// </summary>
        /// <param name="msiurl">The URL of the MSI installer for the update.</param>
        public static void InstallUpdates(string msiurl)
        {
            Process.Start("msiexec", $"/i {msiurl}");
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Gets all settings from Properties.Settings.Default and returns them as a dictionary.
        /// </summary>
        /// <returns>A dictionary containing the settings keys and their corresponding values.</returns>
        public static Dictionary<string, object> GetAllSettings()
        {
            var settings = new Dictionary<string, object>();

            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                string key = currentProperty.Name;
                var value = Properties.Settings.Default[key];
                settings.Add(key, value);
            }

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

        /// <summary>
        /// Asynchronously retrieves a DebugObject containing various application and system information.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a DebugObject with the current state of the application and system.
        /// </returns>
        public static async Task<DebugObject> GetDebugObject()
        {
            return new DebugObject
            {
                Launcher = App.Launcher,
                PathRegistry = new[]
                {
                    RootPath,
                    CachePath,
                    DefaultPath,
                    UdataPath,
                    InstancesPath
                },
                Version = GetCurrentVersion(),
                NewestVersion = await GetNewestVersionName(),
                IsUpgradeable = await CheckForNewVersion(),
                Args = App.AppArgs,
                FriendlyName = App.FRIENDLY_NAME,
                UriScheme = App.URI_SCHEME,
                UriArgs = App.UriArgs,
                IsSilent = App.is_silent,
                KillOld = App.kill_old,
                IsInternetAvailable = InternetUtils.ReachPage("https://www.google.com/"),
                IsTcraftReacheable = InternetUtils.ReachPage("https://tcraft.link/tclauncher/api"),
                TotalAdapterMemoryInGb = SystemInfoUtils.GetTotalAdapterMemoryInGb(),
                TotalPhysicalMemoryInGb = SystemInfoUtils.GetTotalPhysicalMemoryInGb(),
                DefaultConnectionLimit = System.Net.ServicePointManager.DefaultConnectionLimit,
                LoadedPlugins = new []
                {
                    "AppletLoader",
                    "SimpleEdit",
                    "server-tool",
                    "version-checker"
                },
                Settings = GetAllSettings()
            };
        }
    }
}
