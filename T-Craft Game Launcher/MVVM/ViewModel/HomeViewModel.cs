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
        private InstalledInstance _lastPlayed;
        public InstalledInstance LastPlayed
        {
            get => _lastPlayed;
            set
            {
                _lastPlayed = value;
                OnPropertyChanged();
            }
        }

        public HomeViewModel()
        {
            LocalList = new ObservableCollection<InstalledInstance>();
            LoadLocalInstances();
            LoadLastPlayed();
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
                            InstalledInstance installed = new InstalledInstance(instance.Name, instance.DisplayName, instance.Guid);
                            LocalList.Add(installed);
                        }
                    }

                }
            }
            catch
            {
                MessageBox.Show("Ein Fehler ist aufgetreten!");
            }
        }

        private void LoadLastPlayed()
        {
            Guid guidLastPlayed = Properties.Settings.Default.LastPlayed;
            if (guidLastPlayed == new Guid("00000000-0000-0000-0000-000000000000")) return;

            InstalledInstance instance = LocalList.FirstOrDefault(x => x.Guid == guidLastPlayed);
            if (instance == null) return;

            //Weiterfahren
        }
    }
}
