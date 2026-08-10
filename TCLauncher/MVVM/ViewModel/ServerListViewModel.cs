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
        private string _minecraftVersionFilter = "All versions";
        private string _availabilityFilter = "All games";

        public ObservableCollection<Instance> ServerList
        {
            get => _serverList;
            set
            {
                _serverList = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasResults));
            }
        }

        public bool HasResults => ServerList.Count > 0;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string LoadingText
        {
            get => _loadingText;
            set
            {
                _loadingText = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string LoaderFilter
        {
            get => _loaderFilter;
            set
            {
                _loaderFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public IReadOnlyList<string> LoaderFilters { get; } = new[] { "All", "Vanilla", "Fabric", "Forge", "NeoForge" };

        public ObservableCollection<string> MinecraftVersionFilters { get; } =
            new ObservableCollection<string> { "All versions" };

        public IReadOnlyList<string> AvailabilityFilters { get; } = new[] { "All games", "Installed", "Available" };

        public string MinecraftVersionFilter
        {
            get => _minecraftVersionFilter;
            set
            {
                _minecraftVersionFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public string AvailabilityFilter
        {
            get => _availabilityFilter;
            set
            {
                _availabilityFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public ICommand RefreshCommand { get; }

        public ServerListViewModel(bool loadAutomatically = true)
        {
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            if (loadAutomatically) RefreshCommand.Execute(null);
        }

        internal async Task LoadAsync(CancellationToken cancellationToken)
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
                            ThumbnailURL = File.Exists(installed.ThumbnailURL)
                                ? installed.ThumbnailURL
                                : remoteInstance.ThumbnailURL
                        };
                        _allInstances.Add(merged);
                        localById.Remove(remoteInstance.Guid);
                    }
                    else _allInstances.Add(remoteInstance);
                }

                _allInstances.AddRange(localById.Values);

                var versions = _allInstances.Select(item => item.McVersion)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(value => value).ToList();

                void UpdateVersions()
                {
                    MinecraftVersionFilters.Clear();
                    MinecraftVersionFilters.Add("All versions");
                    foreach (var version in versions) MinecraftVersionFilters.Add(version);
                    if (!MinecraftVersionFilters.Contains(MinecraftVersionFilter))
                        MinecraftVersionFilter = "All versions";
                }

                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                    Application.Current.Dispatcher.Invoke((Action)UpdateVersions);
                else UpdateVersions();

                if (!result.IsSuccess) StatusText = result.Message;
                else if (result.Value.IsOffline) StatusText = "Offline • showing cached games";
                else if (result.Value.IsStale) StatusText = "Cached catalog • may be out of date";
                else StatusText = "Updated " + DateTime.Now.ToString("t");

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
                    (item.Version ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (item.Type ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.GetEffectiveLoader().Type.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (item.GetEffectiveLoader().Version ?? string.Empty).IndexOf(query,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (item.WorkingDirDesc?.Any(pair =>
                         pair.Key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         pair.Value.Any(value => value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)) ??
                     false));
            }

            if (!string.IsNullOrWhiteSpace(LoaderFilter) && LoaderFilter != "All")
                filtered = filtered.Where(item => item.GetEffectiveLoader().Type.ToString() == LoaderFilter);
            if (!string.IsNullOrWhiteSpace(MinecraftVersionFilter) && MinecraftVersionFilter != "All versions")
                filtered = filtered.Where(item =>
                    string.Equals(item.McVersion, MinecraftVersionFilter, StringComparison.OrdinalIgnoreCase));
            if (AvailabilityFilter == "Installed") filtered = filtered.Where(item => item.Is_Installed);
            else if (AvailabilityFilter == "Available") filtered = filtered.Where(item => !item.Is_Installed);

            var materialized = filtered.OrderByDescending(item => item.Is_Installed).ThenBy(item => item.DisplayName)
                .ToList();
            void Update() => ServerList = new ObservableCollection<Instance>(materialized);
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.Invoke((Action)Update);
            else Update();
        }

        internal void AddOrReplace(Instance instance)
        {
            _allInstances.RemoveAll(item => item.Guid == instance.Guid);
            _allInstances.Add(instance);
            ApplyFilters();
        }

        internal void SetInstancesForTesting(IEnumerable<Instance> instances)
        {
            _allInstances.Clear();
            _allInstances.AddRange(instances);
            ApplyFilters();
        }
    }
}