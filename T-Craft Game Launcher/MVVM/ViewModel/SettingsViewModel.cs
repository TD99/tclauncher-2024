using System.ComponentModel;
using System.Windows.Input;
using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
{
    class SettingsViewModel : ObservableObject, INotifyPropertyChanged
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
            DownloadMirror = Properties.Settings.Default.DownloadMirror;
            SaveCommand = new RelayCommand(
                p => {
                    Save();
                });
        }

        private void Save()
        {
            Properties.Settings.Default.DownloadMirror = DownloadMirror;
            Properties.Settings.Default.Save();
        }
    }
}
