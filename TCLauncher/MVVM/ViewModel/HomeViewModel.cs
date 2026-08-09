using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.ViewModel
{
    class HomeViewModel : ObservableObject
    {
        private ObservableCollection<InstalledInstance> _localList;

        public ObservableCollection<InstalledInstance> LocalList
        {
            get => _localList;
            set
            {
                _localList = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<InstalledInstance> RecentProfiles { get; } = new ObservableCollection<InstalledInstance>();

        private InstalledInstance _lastSelected;
        public InstalledInstance LastSelected
        {
            get => _lastSelected;
            set
            {
                _lastSelected = value;
                OnPropertyChanged();
            }
        }

        public HomeViewModel()
        {
            LocalList = new ObservableCollection<InstalledInstance>();
            LoadLocalInstances();
            LoadLastSelected();
            LoadRecentProfiles();
        }

        private void LoadLocalInstances()
        {
            try
            {
                foreach (string file in Directory.GetFiles(IoUtils.Tcl.InstancesPath, "config.json", SearchOption.AllDirectories))
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        string json = reader.ReadToEnd();
                        var instance = JsonConvert.DeserializeObject<Instance>(json);
                        instance?.NormalizeLegacyConfiguration();

                        if (instance?.Is_Installed == true)
                        {
                            InstalledInstance installed = new InstalledInstance(instance);
                            LocalList.Add(installed);
                            Console.WriteLine($@"***Loaded {instance.Name}");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                if (!Settings.Default.FirstTime) AppServices.Log.Warning("home.instances_load_failed", exception.Message);
            }
        }

        private void LoadLastSelected()
        {
            Guid guidLastPlayed = Settings.Default.LastSelected;
            InstalledInstance instance = LocalList.FirstOrDefault(x => x.Guid == guidLastPlayed);
            if (instance == null)
            {
                if (LocalList.Any())
                {
                    LastSelected = LocalList[0];
                }
                return;
            }
            LastSelected = instance;
        }

        private void LoadRecentProfiles()
        {
            var orderedIds = AppServices.Activity.List().Select(item => item.ProfileId).Distinct().Take(4).ToList();
            foreach (var id in orderedIds)
            {
                var profile = LocalList.FirstOrDefault(item => item.Guid == id);
                if (profile != null) RecentProfiles.Add(profile);
            }
            foreach (var profile in LocalList.Where(item => RecentProfiles.All(recent => recent.Guid != item.Guid)).Take(4 - RecentProfiles.Count))
                RecentProfiles.Add(profile);
        }

        internal void SelectProfile(Guid profileId)
        {
            var profile = LocalList.FirstOrDefault(item => item.Guid == profileId);
            if (profile != null) LastSelected = profile;
        }
    }
}
