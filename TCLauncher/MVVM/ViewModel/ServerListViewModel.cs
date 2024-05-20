using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.Properties;

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

        private string _loadingText;

        public string LoadingText
        {
            get => _loadingText;
            set
            {
                _loadingText = value;
                OnPropertyChanged();
            }
        }

        public ServerListViewModel()
        {
            Task.Run(async () =>
            {
                IsLoading = true;
                LoadingText = Languages.preparing_text;
                await Task.Delay(400);
                await LoadServers();
                LoadingText = Languages.finishing_text;
                IsLoading = false;
            });
        }

        private async Task LoadServers()
        {
            var cacheFilePath = Path.Combine(IoUtils.Tcl.CachePath, "ServerListCache.json");
            var tempServerList = File.Exists(cacheFilePath) ? LoadFromCache(cacheFilePath) : null;
            tempServerList = await FetchServerList(tempServerList, cacheFilePath);
            tempServerList = LoadLocalInstances(tempServerList);
            if (tempServerList != null) LoadInstances(tempServerList);
        }

        private ObservableCollection<Instance> LoadFromCache(string cacheFilePath)
        {
            LoadingText = Languages.cache_loading_text;
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
            LoadingText = Languages.data_fetching_text;
            ObservableCollection<Instance> internetData = null;
            try
            {
                var response = await _httpClient.GetAsync(Properties.Settings.Default.DownloadMirror);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    internetData = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content);
                }
                else
                {
                    return tempServerList;
                }
            }
            catch
            {
                return tempServerList;
            }

            var isCacheUpToDate = tempServerList?.Count == internetData?.Count;
            if (isCacheUpToDate)
            {
                foreach (var i in internetData)
                {
                    var guid = i.Guid;
                    var cacheInstance = tempServerList.FirstOrDefault(x => x.Guid == guid);
                    if (cacheInstance != null)
                    {
                        if (cacheInstance.IsSameAs(i)) continue;
                    }

                    isCacheUpToDate = false;
                    break;
                }
            }

            if (isCacheUpToDate) return tempServerList;

            {
                tempServerList = internetData;
                if (tempServerList != null)
                {
                    var thumbsDir = Path.Combine(IoUtils.Tcl.CachePath, "thumbs");
                    try
                    {
                        if (Directory.Exists(thumbsDir))
                            Directory.Delete(thumbsDir, true);

                        Directory.CreateDirectory(thumbsDir);
                    }
                    catch
                    {
                        // ignored
                    }

                    foreach (var i in tempServerList)
                    {
                        var path = Path.Combine(thumbsDir, i.Guid + Path.GetExtension(i.ThumbnailURL));
                        var oldThumbnailUrl = i.ThumbnailURL;
                        try
                        {
                            var thumbResponse = await _httpClient.GetAsync(i.ThumbnailURL);
                            if (thumbResponse.IsSuccessStatusCode)
                            {
                                var thumbContent = await thumbResponse.Content.ReadAsByteArrayAsync();
                                File.WriteAllBytes(path, thumbContent);
                                i.ThumbnailURL = path;
                            }
                        }
                        catch
                        {
                            i.ThumbnailURL = oldThumbnailUrl;
                        }
                    }
                    try
                    {
                        File.WriteAllText(cacheFilePath, JsonConvert.SerializeObject(tempServerList));
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(string.Format(Languages.cache_write_error_message, e.Message), Languages.error);
                    }
                }
            }

            return tempServerList;
        }

        private ObservableCollection<Instance> LoadLocalInstances(ObservableCollection<Instance> tempServerList)
        {
            LoadingText = Languages.local_instances_loading_text;
            try
            {
                var instancesFolder = IoUtils.Tcl.InstancesPath;

                // Durchlaufen Sie jede Instanz (config.json-Datei im Instanzordner im Instanzenordner)
                foreach (var directory in Directory.GetDirectories(instancesFolder))
                {
                    var configPath = Path.Combine(directory, "config.json");
                    if (!File.Exists(configPath)) continue;
                    var config = JsonConvert.DeserializeObject<Instance>(File.ReadAllText(configPath));

                    // Überprüfen Sie, ob die Instanz in tempServerList vorhanden ist, indem Sie instance.IsSameAsDecent() überprüfen
                    if (tempServerList.Any(instance => instance.IsSameAsDecent(config))) continue;

                    config = new InstalledInstance(config)
                    {
                        Is_LocalSource = true
                    };

                    // Wenn nicht, fügen Sie die Instanz zu tempServerList hinzu
                    tempServerList.Add(config);
                }
            }
            catch
            {
                // ignored
            }

            // Rückgabe von tempServerList
            return tempServerList;
        }

        private void LoadInstances(ObservableCollection<Instance> tempServerList)
        {
            LoadingText = Languages.instances_loading_text;
            try
            {
                var finalServerList = new ObservableCollection<Instance>();
                foreach (var instance in tempServerList)
                {
                    if (instance.Is_Installed)
                    {
                        finalServerList.Add(instance);
                        continue;
                    }

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
                MessageBox.Show(Languages.installed_instances_load_error_message);
            }
        }
    }
}
