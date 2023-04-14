using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;

namespace T_Craft_Game_Launcher.Core
{
    public static class AppTools
    {
        public async static void HandleUpdates(bool userInitiated = false)
        {
            try
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                DateTime compilationDate = File.GetCreationTime(Assembly.GetExecutingAssembly().Location);

                string updateAPIURL = $"https://tcraft.link/tclauncher/api/plugins/version-checker/?version={version}&date={compilationDate.ToString("yyyy-MM-dd")}";

                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(updateAPIURL);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();

                    JObject obj = JObject.Parse(content);
                    bool isNew = (bool)obj["new"];
                    string newVersion = (string)obj["version"];
                    string msi = (string)obj["msi"];

                    if (isNew)
                    {
                        MessageBoxResult result = MessageBox.Show($"Eine neuere Version ({newVersion}) von TCLauncher ist verfügbar. Jetzt installieren?", "TCLauncher", MessageBoxButton.YesNo);
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
            } catch { }
        }

        public static void InstallUpdates(string MSIURL)
        {
            Process.Start("msiexec", $"/i {MSIURL}");
            Application.Current.Shutdown();
        }
    }
}
