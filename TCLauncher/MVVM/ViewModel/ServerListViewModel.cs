using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.ViewModel
{
    internal sealed class ServerListViewModel : ObservableObject
    {
        private readonly List<Instance> _allInstances = new List<Instance>();
        private ObservableCollection<Instance> _serverList = new ObservableCollection<Instance>();
        private bool _isLoading;
        private string _loadingText;
        private string _statusText;
        private string _searchText;
        private string _loaderFilter = "All";
        private bool _installedOnly;
        private double _itemWidth = 285;
        private double _itemHeight = 165;

        public ObservableCollection<Instance> ServerList
        {
            get => _serverList;
            set { _serverList = value; OnPropertyChanged(); }
        }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
        public string LoadingText { get => _loadingText; set { _loadingText = value; OnPropertyChanged(); } }
        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); ApplyFilters(); } }
        public string LoaderFilter { get => _loaderFilter; set { _loaderFilter = value; OnPropertyChanged(); ApplyFilters(); } }
        public bool InstalledOnly { get => _installedOnly; set { _installedOnly = value; OnPropertyChanged(); ApplyFilters(); } }
        public IReadOnlyList<string> LoaderFilters { get; } = new[] { "All", "Vanilla", "Fabric", "Forge", "NeoForge" };
        public ICommand RefreshCommand { get; }

        public double ItemWidth { get => _itemWidth; set { _itemWidth = value; OnPropertyChanged(); } }
        public double ItemHeight { get => _itemHeight; set { _itemHeight = value; OnPropertyChanged(); } }
        public double ItemMinWidth { get; } = 250;
        public double ItemMaxWidth { get; } = 550;
        public double ItemMinHeight { get; } = 150;
        public double ItemMaxHeight { get; } = 330;

        public ServerListViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            RefreshCommand.Execute(null);
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            IsLoading = true;
            LoadingText = Languages.data_fetching_text;
            StatusText = null;
            try
            {
                var result = await AppServices.Catalog.LoadAsync(cancellationToken);
                var remote = result.IsSuccess
                    ? result.Value.Catalog.Items.Select(item => item.ToInstance()).ToList()
                    : new List<Instance>();
                var local = LoadLocalInstances();
                var localById = local.ToDictionary(item => item.Guid);

                _allInstances.Clear();
                foreach (var remoteInstance in remote)
                {
                    InstalledInstance installed;
                    if (localById.TryGetValue(remoteInstance.Guid, out installed))
                    {
                        var merged = new InstalledInstance(remoteInstance)
                        {
                            LastServer = installed.LastServer,
                            Is_LocalSource = installed.Is_LocalSource,
                            ThumbnailURL = File.Exists(installed.ThumbnailURL) ? installed.ThumbnailURL : remoteInstance.ThumbnailURL
                        };
                        _allInstances.Add(merged);
                        localById.Remove(remoteInstance.Guid);
                    }
                    else _allInstances.Add(remoteInstance);
                }
                _allInstances.AddRange(localById.Values);

                if (!result.IsSuccess) StatusText = result.Message;
                else if (result.Value.IsOffline) StatusText = Languages.ResourceManager.GetString("catalog_offline");
                else if (result.Value.IsStale) StatusText = "Cached catalog may be out of date";

                ApplyFilters();
            }
            catch (OperationCanceledException)
            {
                StatusText = "Refresh cancelled";
            }
            catch (Exception exception)
            {
                AppServices.Log.Error("discovery.load_failed", exception);
                StatusText = Languages.installed_instances_load_error_message;
                _allInstances.Clear();
                _allInstances.AddRange(LoadLocalInstances());
                ApplyFilters();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<InstalledInstance> LoadLocalInstances()
        {
            var result = new List<InstalledInstance>();
            Directory.CreateDirectory(IoUtils.Tcl.InstancesPath);
            foreach (var directory in Directory.GetDirectories(IoUtils.Tcl.InstancesPath))
            {
                var configPath = Path.Combine(directory, "config.json");
                if (!File.Exists(configPath)) continue;
                InstalledInstance installed;
                try
                {
                    installed = JsonConvert.DeserializeObject<InstalledInstance>(File.ReadAllText(configPath));
                    installed?.NormalizeLegacyConfiguration();
                }
                catch (Exception exception)
                {
                    AppServices.Log.Warning("discovery.local_skipped", exception.Message);
                    continue;
                }
                if (installed == null || AppServices.InstanceConfigs.Validate(installed).Count > 0) continue;
                installed.InstallationDir = directory;
                installed.DataDir = Path.Combine(directory, "data");
                installed.ConfigFile = configPath;
                installed.Is_Installed = true;
                result.Add(installed);
            }
            return result;
        }

        private void ApplyFilters()
        {
            IEnumerable<Instance> filtered = _allInstances;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.Trim();
                filtered = filtered.Where(item =>
                    (item.DisplayName ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (item.Name ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (item.McVersion ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (item.Type ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!string.IsNullOrWhiteSpace(LoaderFilter) && LoaderFilter != "All")
                filtered = filtered.Where(item => item.GetEffectiveLoader().Type.ToString() == LoaderFilter);
            if (InstalledOnly) filtered = filtered.Where(item => item.Is_Installed);

            var materialized = filtered.OrderByDescending(item => item.Is_Installed).ThenBy(item => item.DisplayName).ToList();
            void Update() => ServerList = new ObservableCollection<Instance>(materialized);
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess()) Application.Current.Dispatcher.Invoke((Action)Update);
            else Update();
        }
    }
}
