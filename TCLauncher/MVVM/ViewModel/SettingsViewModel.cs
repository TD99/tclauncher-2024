using System.Windows.Input;
using TCLauncher.Core;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.ViewModel
{
    class SettingsViewModel : ObservableObject
    {
        private string downloadMirror;

        public string DownloadMirror
        {
            get => downloadMirror;
            set
            {
                downloadMirror = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; set; }

        public SettingsViewModel()
        {
            DownloadMirror = Settings.Default.DownloadMirror;
            SaveCommand = new RelayCommand(
                p => {
                    Save();
                });
        }

        private void Save()
        {
            Settings.Default.DownloadMirror = DownloadMirror;
            Settings.Default.Save();
        }
    }
}
