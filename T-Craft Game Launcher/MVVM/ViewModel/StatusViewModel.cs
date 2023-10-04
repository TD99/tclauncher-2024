using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
            double ramGb = SystemInfoUtils.GetTotalPhysicalMemoryInGb();
            Dictionary<string, double> gpusGb = SystemInfoUtils.GetTotalAdapterMemoryInGb();

            RamInfo = "RAM: " + ramGb + " GB";
            var gpuInfo = new StringBuilder("GPU: ");
            foreach (var entry in gpusGb)
                gpuInfo.Append($"{entry.Key}: {entry.Value} GB / ");

            //TODO: Add Global Mem requirements
            IsRamReqirementMet = ramGb >= 2;

            GpuInfo = gpuInfo.ToString();

            IsGpuReqirementMet = gpusGb.Any(gpu => gpu.Value > 0.5);

            var tclSize = Math.Round(IoUtils.AppData.GetSize(), 2);
            var freeSize = Math.Round(IoUtils.FileSystem.GetFreeStorageInGb(), 2);
            var othersSize = Math.Round(IoUtils.FileSystem.GetTotalStorageInGb() - tclSize - freeSize, 2);

            StorageAnalyzerData = new List<StackedBarItem>
            {
                new StackedBarItem
                {
                    Name = "TCL",
                    Value = tclSize,
                    Unit = "GB",
                    Color = Colors.LightBlue
                },
                new StackedBarItem
                {
                    Name = "Sonstige",
                    Value = othersSize,
                    Unit = "GB",
                    Color = Colors.LightGoldenrodYellow
                },
                new StackedBarItem
                {
                    Name = "Frei",
                    Value = freeSize,
                    Unit = "GB",
                    Color = Colors.LightGray
                }
            };
        }

    }
}