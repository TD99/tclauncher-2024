using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;

namespace T_Craft_Game_Launcher.Core
{
    /// <summary>
    /// A utility class for handling application updates and installations.
    /// </summary>
    public static class AppUtils
    {
        /// <summary>
        /// Checks for updates and prompts the user to install if a new version is available.
        /// </summary>
        /// <param name="userInitiated">Indicates whether the update check was initiated by the user.</param>
        public static async void HandleUpdates(bool userInitiated = false)
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var compilationDate = File.GetCreationTime(Assembly.GetExecutingAssembly().Location);
                // TODO: Replace with variable URL
                var updateApiUrl = $"https://tcraft.link/tclauncher/api/plugins/version-checker/?version={version}&date={compilationDate:yyyy-MM-dd}";

                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(updateApiUrl);
                if (!response.IsSuccessStatusCode) return;
                
                var content = await response.Content.ReadAsStringAsync();

                var obj = JObject.Parse(content);
                var isNew = (bool)obj["new"];
                var newVersion = (string)obj["version"];
                var msi = (string)obj["msi"];

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
    }
}
