using System;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using static TCLauncher.Core.IoUtils.Tcl;
using System.Threading.Tasks;
using TCLauncher.Models;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Compression;
using CmlLib.Core;
using Newtonsoft.Json;
using static TCLauncher.Core.MessageBoxUtils;

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

        public static void CreateTemplateInstance()
        {
            var randGuid = Guid.NewGuid();

            var instanceFolder = Path.Combine(InstancesPath, randGuid.ToString());
            Directory.CreateDirectory(instanceFolder);
            var installedInstance = new InstalledInstance
            {
                Guid = randGuid,
                Name = "template-" + randGuid,
                DisplayName = "Template " + randGuid.ToString().Substring(0, 4),
                Version = "1.0.0",
                Type = "Template",
                McVersion = "<Insert McVersion>",
                ForgeVersion = "",
                UseFabric = false,
                UseForge = false,
                UseIsolation = false,
                MinimumRamMb = 0,
                MaximumRamMb = 8024
            };
            var jsonOut = JsonConvert.SerializeObject(installedInstance);
            File.WriteAllText(Path.Combine(instanceFolder, "config.json"), jsonOut);

            Directory.CreateDirectory(Path.Combine(instanceFolder, "data"));

            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule != null)
            {
                string appPath = processModule.FileName;
                Process.Start(appPath, $"--installSuccess {installedInstance.DisplayName}");
            }

            Application.Current.Shutdown();
        }

        public static bool ImportInstance(string zipPath)
        {
            var cachePath = CachePath;
            var zipName = Path.GetFileNameWithoutExtension(zipPath);
            var importerDir = Path.Combine(cachePath, "importer_" + zipName);

            if (Directory.Exists(importerDir)) Directory.Delete(importerDir, true);

            ZipFile.ExtractToDirectory(zipPath, importerDir);

            var configPath = Path.Combine(importerDir, "config.json");
            var config = JsonConvert.DeserializeObject<Instance>(File.ReadAllText(configPath));

            var guid = config.Guid;

            var instanceFolder = Path.Combine(InstancesPath, guid.ToString());
            if (Directory.Exists(instanceFolder))
            {
                var result = MessageBox.Show("Diese Instanz ist bereits installiert. Überschreiben?", null, MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Directory.Delete(instanceFolder, true);
                }
                else
                {
                    return false;
                }
            }

            Directory.CreateDirectory(instanceFolder);

            var payloadPath = Path.Combine(importerDir, "payload.zip");
            ZipFile.ExtractToDirectory(payloadPath, Path.Combine(instanceFolder, "data"));

            var thumbExt = Path.GetExtension(config.ThumbnailURL);
            var thumbFile = Path.Combine(importerDir, "thumb" + thumbExt);

            if (File.Exists(thumbFile))
            {
                File.Copy(thumbFile, Path.Combine(instanceFolder, "thumb" + thumbExt));
                config.ThumbnailURL = Path.Combine(instanceFolder, "thumb" + thumbExt);
            }

            var installedInstance = new InstalledInstance(config);
            var jsonOut = JsonConvert.SerializeObject(installedInstance);
            File.WriteAllText(Path.Combine(instanceFolder, "config.json"), jsonOut);

            Directory.Delete(importerDir, true);

            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule != null)
            {
                string appPath = processModule.FileName;
                Process.Start(appPath, $"--installSuccess {config.DisplayName}");
            }

            Application.Current.Shutdown();

            return true;
        }

        // TODO: needs rework, better code quality
        public static void LoadInstanceImporter()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".tcl",
                Filter = "TCL Package (*.tcl)|*.tcl"
            };

            var result = dlg.ShowDialog();

            if (result != true) return;

            var zipPath = dlg.FileName;
            
            ImportInstance(zipPath);
        }

        // TODO: needs rework, better code quality
        public static async Task<Instance> LoadInstanceBuilder()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { DefaultExt = ".zip", Filter = "Zip Files (*.zip)|*.zip" };
            var result = dlg.ShowDialog();
            if (result != true) return null;

            var cachePath = CachePath;
            var zipPath = dlg.FileName;
            var zipName = Path.GetFileNameWithoutExtension(zipPath);
            var creatorDir = Path.Combine(cachePath, "creator_" + zipName);
            if (Directory.Exists(creatorDir)) Directory.Delete(creatorDir, true);

            Directory.CreateDirectory(creatorDir);

            var extractPath = Path.Combine(creatorDir, "payload");
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            string thumbLoc = null;
            var result2 = MessageBox.Show("Möchtest du ein Thumbnail für das Paket setzen?", "Paket erstellen", MessageBoxButton.YesNo);
            if (result2 == MessageBoxResult.Yes)
            {
                var dlg2 = new Microsoft.Win32.OpenFileDialog { DefaultExt = ".png", Filter = "Image Files (*.png, *.jpg)|*.png;*.jpg" };
                var result3 = dlg2.ShowDialog();
                if (result3 == true)
                {
                    var thumbnailPath = Path.Combine(creatorDir, "thumb" + Path.GetExtension(dlg2.FileName));
                    File.Copy(dlg2.FileName, thumbnailPath);
                    thumbLoc = "[baseURL]/thumb" + Path.GetExtension(dlg2.FileName);
                }
            }

            var i = new Instance
            {
                Guid = Guid.NewGuid(),
                Name = await AskForString("Bitte geben Sie den Namen ein"),
                DisplayName = await AskForString("Bitte geben Sie den Anzeigenamen ein"),
                Version = await AskForString("Bitte geben Sie die Version ein"),
                Upgradeable = await AskForBool("Ist es aktualisierbar? (true/false)") ?? false,
                Type = await AskForString("Bitte geben Sie den Typ ein"),
                McVersion = await AskForString("Bitte geben Sie die McVersion ein"),
                ForgeVersion = await AskForString("Bitte geben Sie die ForgeVersion ein"),
                UseFabric = await AskForBool("Verwenden Sie Fabric? (true/false)", true),
                UseForge = await AskForBool("Verwenden Sie Forge? (true/false)", true),
                UsePatch = await AskForBool("Verwenden Sie Patch? (true/false)", true) ?? false,
                UseIsolation = await AskForBool("Verwenden Sie Isolation? (true/false)", true),
                WorkingDirDesc = await AskForJson<Dictionary<string, List<string>>>("Bitte geben Sie den Beschreibungsbaum als rohen JSON-Text ein", true),
                Requirements = await AskForJson<Dictionary<string, object>>("Bitte geben Sie die Anforderungen als rohen JSON-Text ein", true),
                Servers = await AskForJson<List<Server>>("Bitte geben Sie die Server als rohen JSON-Text ein", true),
                MinimumRamMb = await AskForInt("Bitte geben Sie die minimale RAM-Menge in MB ein", true),
                MaximumRamMb = await AskForInt("Bitte geben Sie die maximale RAM-Menge in MB ein", true),
                JVMArguments = await AskForJson<string[]>("Bitte geben Sie die JVMArguments als rohen JSON-Text ein", true),
                ThumbnailURL = thumbLoc,
                WorkingDirZipURL = "[baseURL]/payload.zip",
            };

            var json = JsonConvert.SerializeObject(i);
            var jsonPath = Path.Combine(creatorDir, "config.json");
            File.WriteAllText(jsonPath, json);

            var result4 = MessageBox.Show("Möchtest du das Paket jetzt erstellen?", "Paket erstellen", MessageBoxButton.YesNo);
            if (result4 != MessageBoxResult.Yes)
            {
                Process.Start(creatorDir);
                return null;
            }

            ZipFile.CreateFromDirectory(extractPath, Path.Combine(creatorDir, "payload.zip"));
            Directory.Delete(extractPath, true);
            ZipFile.CreateFromDirectory(creatorDir, Path.Combine(cachePath, i.Name + ".zip"));
            Directory.Delete(creatorDir, true);

            var dlg3 = new Microsoft.Win32.SaveFileDialog { FileName = i.Name, DefaultExt = ".tcl", Filter = "TCL Package (*.tcl)|*.tcl" };
            var result5 = dlg3.ShowDialog();
            if (result5 != true) return null;

            File.Copy(Path.Combine(cachePath, i.Name + ".zip"), dlg3.FileName);
            File.Delete(Path.Combine(cachePath, i.Name + ".zip"));

            MessageBox.Show("Das Paket wurde erfolgreich erstellt.", "Paket erstellen");

            return i;
        }


    }
}
