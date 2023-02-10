using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand PlayViewCommand { get; set; }
        public RelayCommand ServerListViewCommand { get; set; }
        public RelayCommand InstanceViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }
        public RelayCommand ConfigEditorViewCommand { get; set; }

        public HomeViewModel HomeVM { get; set; }
        public PlayViewModel PlayVM { get; set; }
        public ServerListViewModel ServerListVM { get; set; }
        public InstanceViewModel InstanceVM { get; set; }
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
            PlayVM = new PlayViewModel();
            ServerListVM = new ServerListViewModel();
            InstanceVM = new InstanceViewModel();
            SettingsVM = new SettingsViewModel();
            ConfigEditorVM = new ConfigEditorViewModel();

            CurrentView = HomeVM;

            HomeViewCommand = new RelayCommand(o =>
            {
                CurrentView = HomeVM;
            });
            PlayViewCommand = new RelayCommand(o =>
            {
                CurrentView = PlayVM;
            });
            ServerListViewCommand = new RelayCommand(o =>
            {
                CurrentView = ServerListVM;
            });
            InstanceViewCommand = new RelayCommand(o =>
            {
                CurrentView = InstanceVM;
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
