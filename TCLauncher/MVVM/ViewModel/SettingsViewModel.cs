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
            get
            {
                return downloadMirror;
            }
            set
            {
                downloadMirror = value;
                OnPropertyChanged(nameof(DownloadMirror));
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
