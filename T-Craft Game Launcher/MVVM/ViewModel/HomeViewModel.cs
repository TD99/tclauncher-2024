using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.Models;

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
                            InstalledInstance installed = new InstalledInstance(instance.Name, instance.DisplayName, instance.Guid, instance.AppletURL, instance.Servers);
                            LocalList.Add(installed);
                        }
                    }
                }
            }
            catch
            {
                if (!Properties.Settings.Default.FirstTime) MessageBox.Show("Die lokalen Instanzen konnten nicht geladen werden!");
            }

            //if ((LocalList.Count < 1 && !Properties.Settings.Default.FirstTime) ||
            //    (LocalList.Count > 0 && Properties.Settings.Default.FirstTime))
            //{
            //    Properties.Settings.Default.FirstTime = !Properties.Settings.Default.FirstTime;
            //    Properties.Settings.Default.Save();

            //    MessageBox.Show("Eine Aktualisierung wurde bereitgestellt. Der Launcher wird neu gestartet.");
            //    string appPath = Process.GetCurrentProcess().MainModule.FileName;
            //    Process.Start(appPath, $"--silent");
            //    Application.Current.Shutdown();
            //}
        }

        private void LoadLastSelected()
        {
            Guid guidLastPlayed = Properties.Settings.Default.LastSelected;
            InstalledInstance instance = LocalList.FirstOrDefault(x => x.Guid == guidLastPlayed);

            if (instance == null)
            {
                if (LocalList.Count() >= 1)
                {
                    LastSelected = LocalList[0];
                }
                return;
            }

            LastSelected = instance;
        }
    }
}
