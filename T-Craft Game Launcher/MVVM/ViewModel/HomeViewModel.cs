using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Model;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
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
        }

        private void LoadLocalInstances()
        {
            try
            {
                string instancesFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances");
                foreach (string file in Directory.GetFiles(instancesFolder, "config.json", SearchOption.AllDirectories))
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        string json = reader.ReadToEnd();
                        var instance = JsonConvert.DeserializeObject<Instance>(json);

                        if (instance.Is_Installed)
                        {
                            InstalledInstance installed = new InstalledInstance(instance.Name, instance.DisplayName, instance.Guid, instance.AppletURL);
                            LocalList.Add(installed);
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Die lokalen Instanzen konnten nicht geladen werden!");
            }
        }

        private void LoadLastSelected()
        {
            Guid guidLastPlayed = Properties.Settings.Default.LastSelected;
            if (guidLastPlayed == new Guid("00000000-0000-0000-0000-000000000000")) return;

            InstalledInstance instance = LocalList.FirstOrDefault(x => x.Guid == guidLastPlayed);
            if (instance == null) return;

            LastSelected = instance;
        }
    }
}
