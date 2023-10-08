using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Model;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
{
    public sealed class StatusViewModel : INotifyPropertyChanged
    {
        private string _ramInfo;
        private bool _isRamReqirementMet;
        private string _gpuInfo;
        private bool _isGpuReqirementMet;
        private List<StackedBarItem> _storageAnalyzerData;

        private readonly string _cacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL\\Cache\\stats.txt");

        public string RamInfo
        {
            get => _ramInfo;
            private set
            {
                _ramInfo = value;
                OnPropertyChanged();
            }
        }

        public bool IsRamReqirementMet
        {
            get => _isRamReqirementMet;
            set
            {
                _isRamReqirementMet = value;
                OnPropertyChanged();
            }
        }

        public string GpuInfo
        {
            get => _gpuInfo;
            private set
            {
                _gpuInfo = value;
                OnPropertyChanged();
            }
        }

        public bool IsGpuReqirementMet
        {
            get => _isGpuReqirementMet;
            set
            {
                _isGpuReqirementMet = value;
                OnPropertyChanged();
            }
        }

        public List<StackedBarItem> StorageAnalyzerData
        {
            get => _storageAnalyzerData;
            set
            {
                _storageAnalyzerData = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public StatusViewModel()
        {
            LoadDataAsync();
        }

        public void RefreshData() => LoadDataAsync(true);

        private async void LoadDataAsync(bool force = false)
        {
            await Task.Run(() =>
            {
                // Load cache if available
                if (File.Exists(_cacheFilePath) && !force)
                {
                    var cachedData = File.ReadAllLines(_cacheFilePath);
                    var cacheTime = DateTime.Parse(cachedData[0]);
                    if ((DateTime.Now - cacheTime).TotalMinutes <= 30)
                    {
                        RamInfo = cachedData[1];
                        IsRamReqirementMet = bool.Parse(cachedData[2]);
                        GpuInfo = cachedData[3];
                        IsGpuReqirementMet = bool.Parse(cachedData[4]);
                        StorageAnalyzerData = JsonConvert.DeserializeObject<List<StackedBarItem>>(cachedData[5]);
                        return;
                    }
                }

                // Calculate data
                var ramGb = SystemInfoUtils.GetTotalPhysicalMemoryInGb();
                var gpusGb = SystemInfoUtils.GetTotalAdapterMemoryInGb();

                RamInfo = "RAM: " + ramGb + " GB";
                var gpuInfo = new StringBuilder("GPU: ");
                foreach (var entry in gpusGb)
                    gpuInfo.Append($"{entry.Key}: {entry.Value} GB / ");

                //TODO: Add Global Mem requirements
                IsRamReqirementMet = ramGb >= 2;

                GpuInfo = gpuInfo.ToString();

                IsGpuReqirementMet = gpusGb.Any(gpu => gpu.Value > 0.5);

                var tclSize = Math.Round(IoUtils.Tcl.GetSize(), 2);
                var freeSize = Math.Round(IoUtils.FileSystem.GetFreeStorageInGb(), 2);
                var othersSize = Math.Round(IoUtils.FileSystem.GetTotalStorageInGb() - tclSize - freeSize, 2);

                StorageAnalyzerData = new List<StackedBarItem>
                {
                    new StackedBarItem
                    {
                        Name = "TCL",
                        Value = tclSize,
                        Unit = "GB",
                        Color = Color.FromRgb(71, 83, 103)
                    },
                    new StackedBarItem
                    {
                        Name = "Sonstige",
                        Value = othersSize,
                        Unit = "GB",
                        Color = Color.FromRgb(157, 166, 180)
                    },
                    new StackedBarItem
                    {
                        Name = "Frei",
                        Value = freeSize,
                        Unit = "GB",
                        Color = Color.FromRgb(180, 180, 180)
                    }
                };

                // Save cache
                var dataToCache = new List<string>
                {
                    DateTime.Now.ToString(),
                    RamInfo,
                    IsRamReqirementMet.ToString(),
                    GpuInfo,
                    IsGpuReqirementMet.ToString(),
                    JsonConvert.SerializeObject(StorageAnalyzerData)
                };
                File.WriteAllLines(_cacheFilePath, dataToCache);
            });
        }
    }
}