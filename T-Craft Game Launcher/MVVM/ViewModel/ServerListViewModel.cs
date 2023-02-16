using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Model;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
{
    class ServerListViewModel : ObservableObject
    {
        private ObservableCollection<Instance> _serverList;
        private readonly HttpClient _httpClient = new HttpClient();

        public ObservableCollection<Instance> ServerList
        {
            get => _serverList;
            set
            {
                _serverList = value;
                OnPropertyChanged();
            }
        }

        public ServerListViewModel()
        {
            LoadServers();
        }

        private async void LoadServers()
        {
            try
            {
                var cacheFilePath = Path.Combine(Path.GetTempPath(), "ServerListCache.json");

                if (File.Exists(cacheFilePath))
                {
                    var cacheContent = File.ReadAllText(cacheFilePath);
                    ServerList = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(cacheContent);
                }

                var response = await _httpClient.GetAsync(Properties.Settings.Default.DownloadMirror);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (ServerList == null || content != JsonConvert.SerializeObject(ServerList))
                    {
                        ServerList = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content);
                        File.WriteAllText(cacheFilePath, content);
                    }
                }

                CheckInstalled();
            }
            catch
            {
                MessageBox.Show("Ein Fehler beim Laden der verfügbaren Profile ist aufgetreten.", "Fehler");
            }
        }

        private void CheckInstalled()
        {
            try
            {
                ObservableCollection<Instance> tempServerList = ServerList;
                foreach (var instance in tempServerList)
                {
                    string instanceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", instance.Guid.ToString());
                    string installFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", instance.Guid.ToString(), "data");
                    string configFile = Path.Combine(instanceFolder, "config.json");

                    if (!Directory.Exists(instanceFolder) || !Directory.Exists(installFolder) || !File.Exists(configFile))
                    {
                        instance.Is_Installed = false;
                    }
                    else
                    {
                        instance.Is_Installed = true;
                    }
                }
                ServerList = tempServerList;
            }
            catch
            {
                MessageBox.Show("Ein Fehler ist aufgetreten.");
            }
        }
    }
}
