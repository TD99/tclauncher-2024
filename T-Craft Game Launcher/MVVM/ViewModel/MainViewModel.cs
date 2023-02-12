using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand ServerListViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }
        public RelayCommand ConfigEditorViewCommand { get; set; }

        public HomeViewModel HomeVM { get; set; }
        public ServerListViewModel ServerListVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }
        public ConfigEditorViewModel ConfigEditorVM { get; set; }

        private object _currentView;

        public object CurrentView
        {
            get
            {
                return _currentView;
            }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            HomeVM = new HomeViewModel();
            ServerListVM = new ServerListViewModel();
            SettingsVM = new SettingsViewModel();
            ConfigEditorVM = new ConfigEditorViewModel();

            CurrentView = HomeVM;

            HomeViewCommand = new RelayCommand(o =>
            {
                CurrentView = HomeVM;
            });
            ServerListViewCommand = new RelayCommand(o =>
            {
                CurrentView = ServerListVM;
            });
            SettingsViewCommand = new RelayCommand(o =>
            {
                CurrentView = SettingsVM;
            });
            ConfigEditorViewCommand = new RelayCommand(o =>
            {
                CurrentView = ConfigEditorVM;
            });
        }
    }
}
