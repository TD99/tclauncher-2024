using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using TCLauncher.Core;
using TCLauncher.Models;

namespace TCLauncher.MVVM.ViewModel
{
    class ServerListViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient = new HttpClient();

        private ObservableCollection<Instance> _serverList;
        public ObservableCollection<Instance> ServerList
        {
            get => _serverList;
            set
            {
                _serverList = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ServerListViewModel()
        {
            Task.Run(async () =>
            {
                IsLoading = true;
                await Task.Delay(200);
                await LoadServers();
                IsLoading = false;
            });
        }

        private async Task LoadServers()
        {
            var cacheFilePath = Path.Combine(IoUtils.Tcl.CachePath, "ServerListCache.json");
            ObservableCollection<Instance> tempServerList = File.Exists(cacheFilePath) ? LoadFromCache(cacheFilePath) : null;
            tempServerList = await FetchServerList(tempServerList, cacheFilePath);
            if (tempServerList != null) LoadInstances(tempServerList);
        }

        private ObservableCollection<Instance> LoadFromCache(string cacheFilePath)
        {
            try
            {
                var cacheContent = File.ReadAllText(cacheFilePath);
                return JsonConvert.DeserializeObject<ObservableCollection<Instance>>(cacheContent);
            }
            catch
            {
                return null;
            }
        }

        private async Task<ObservableCollection<Instance>> FetchServerList(ObservableCollection<Instance> tempServerList, string cacheFilePath)
        {
            try
            {
                var response = await _httpClient.GetAsync(Properties.Settings.Default.DownloadMirror);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (tempServerList == null || content != JsonConvert.SerializeObject(tempServerList))
                    {
                        tempServerList = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content);
                        File.WriteAllText(cacheFilePath, content);
                    }
                }
            }
            catch
            {
                if (tempServerList == null)
                {
                    MessageBox.Show("Die Profilliste kann nicht geladen werden!", "Fehler");
                }
            }
            return tempServerList;
        }

        private void LoadInstances(ObservableCollection<Instance> tempServerList)
        {
            try
            {
                var finalServerList = new ObservableCollection<Instance>();
                foreach (var instance in tempServerList)
                {
                    string instanceFolder = IoUtils.Tcl.GetInstancePath(instance.Guid);
                    string installFolder = IoUtils.Tcl.GetInstanceDataPath(instance.Guid);
                    string configFile = IoUtils.Tcl.GetInstanceConfigPath(instance.Guid);

                    if (Directory.Exists(instanceFolder) && Directory.Exists(installFolder) && File.Exists(configFile))
                    {
                        var installedinstance = new InstalledInstance(instance);
                        finalServerList.Add(installedinstance);
                    }
                    else
                    {
                        finalServerList.Add(instance);
                    }
                }
                ServerList = finalServerList;
            }
            catch
            {
                MessageBox.Show("Ein Fehler beim Laden der installierten Instanzen ist aufgetreten.");
            }
        }


        //private async void LoadServers()
        //{
        //    bool error = false;
        //    var cacheFilePath = Path.Combine(IoUtils.Tcl.CachePath, "ServerListCache.json");

        //    try
        //    {
        //        if (File.Exists(cacheFilePath))
        //        {
        //            var cacheContent = File.ReadAllText(cacheFilePath);
        //            ServerList = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(cacheContent);
        //        }
        //    }
        //    catch
        //    {
        //        error = true;
        //    }

        //    try {
        //        var response = await _httpClient.GetAsync(Properties.Settings.Default.DownloadMirror);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();

        //            if (ServerList == null || content != JsonConvert.SerializeObject(ServerList))
        //            {
        //                ServerList = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content);
        //                File.WriteAllText(cacheFilePath, content);
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        if (error)
        //        {
        //            MessageBox.Show("Die Profilliste kann nicht geladen werden!", "Fehler");
        //        }
        //    }

        //    CheckInstalled();
        //}

        //private void CheckInstalled()
        //{
        //    try
        //    {
        //        var tempServerList = new ObservableCollection<Instance>();
        //        foreach (var instance in ServerList)
        //        {
        //            string instanceFolder = IoUtils.Tcl.GetInstancePath(instance.Guid);
        //            string installFolder = IoUtils.Tcl.GetInstanceDataPath(instance.Guid);
        //            string configFile = IoUtils.Tcl.GetInstanceConfigPath(instance.Guid);

        //            if (Directory.Exists(instanceFolder) && Directory.Exists(installFolder) && File.Exists(configFile))
        //            {
        //                var installedinstance = new InstalledInstance(instance);
        //                tempServerList.Add(installedinstance);
        //            }
        //            else
        //            {
        //                tempServerList.Add(instance);
        //            }
        //        }
        //        ServerList = tempServerList;
        //    }
        //    catch
        //    {
        //        MessageBox.Show("Ein Fehler beim Laden der installierten Instanzen ist aufgetreten.");
        //    }
        //}
    }
}
