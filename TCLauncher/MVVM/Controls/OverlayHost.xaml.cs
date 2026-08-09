using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using TCLauncher.Core;
using TCLauncher.Core.Services;

namespace TCLauncher.MVVM.Controls
{
    public partial class OverlayHost : UserControl
    {
        public OverlayHost()
        {
            InitializeComponent();
            Loaded += (sender, args) =>
            {
                Root.DataContext = AppServices.Overlays.Host;
                ActivityTray.DataContext = new OperationTrayViewModel(AppServices.Operations);
            };
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void Backdrop_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AppServices.Overlays.DismissFromOutside();
            e.Handled = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || !AppServices.Overlays.Host.IsOpen) return;
            AppServices.Overlays.DismissFromOutside();
            e.Handled = true;
        }
    }

    internal sealed class OperationTrayViewModel : INotifyPropertyChanged
    {
        public IOperationCoordinator Coordinator { get; }
        public ICommand CancelOperationCommand { get; }
        public bool IsBusy => Coordinator.IsBusy;
        public ActiveOperation Active => Coordinator.Active;
        public event PropertyChangedEventHandler PropertyChanged;

        public OperationTrayViewModel(IOperationCoordinator coordinator)
        {
            Coordinator = coordinator;
            CancelOperationCommand = new RelayCommand(_ => Coordinator.RequestCancellation());
            Coordinator.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(IOperationCoordinator.IsBusy) || args.PropertyName == nameof(IOperationCoordinator.Active))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            };
        }
    }
}
