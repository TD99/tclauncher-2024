using TCLauncher.Core;

namespace TCLauncher.MVVM.ViewModel
{
    internal class MainViewModel : ObservableObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand ServerListViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }
        public RelayCommand StatusViewCommand { get; set; }
        public RelayCommand AccountListViewCommand { get; set; }
        public RelayCommand AccountOptionsViewCommand { get; set; }

        private HomeViewModel HomeViewModel { get; set; }
        private ServerListViewModel ServerListViewModel { get; set; }
        private SettingsViewModel SettingsViewModel { get; set; }
        private StatusViewModel StatusViewModel { get; set; }
        private AccountListViewModel AccountListViewModel { get; set; }
        private AccountOptionsViewModel AccountOptionsViewModel { get; set; }


        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            private set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            HomeViewModel = new HomeViewModel();
            ServerListViewModel = new ServerListViewModel();
            SettingsViewModel = new SettingsViewModel();
            StatusViewModel = new StatusViewModel();
            AccountListViewModel = new AccountListViewModel();
            AccountOptionsViewModel = new AccountOptionsViewModel();

            CurrentView = HomeViewModel;

            HomeViewCommand = new RelayCommand(o =>
            {
                CurrentView = HomeViewModel;
            });
            ServerListViewCommand = new RelayCommand(o =>
            {
                CurrentView = ServerListViewModel;
            });
            SettingsViewCommand = new RelayCommand(o =>
            {
                CurrentView = SettingsViewModel;
            });
            StatusViewCommand = new RelayCommand(o =>
            {
                CurrentView = StatusViewModel;
            });
            AccountListViewCommand = new RelayCommand(o =>
            {
                CurrentView = AccountListViewModel;
            });
            AccountOptionsViewCommand = new RelayCommand(o =>
            {
                CurrentView = AccountOptionsViewModel;
            });
        }
    }
}
