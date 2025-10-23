﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;
using TCLauncher.Core;
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
                foreach (string file in Directory.GetFiles(IoUtils.Tcl.InstancesPath, "config.json", SearchOption.AllDirectories))
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        string json = reader.ReadToEnd();
                        var instance = JsonConvert.DeserializeObject<Instance>(json);

                        if (instance.Is_Installed)
                        {
                            InstalledInstance installed = new InstalledInstance(instance);
                            LocalList.Add(installed);
                            Console.WriteLine($@"***Loaded {instance.Name}");
                        }
                    }
                }
            }
            catch
            {
                if (!Settings.Default.FirstTime)
                    MessageBox.Show(Languages.local_instances_load_error_message);
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
    }
}
