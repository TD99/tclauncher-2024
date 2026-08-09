using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System;
using System.Windows.Media.Animation;
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
                AppServices.Overlays.Host.PropertyChanged += Host_OnPropertyChanged;
            };
            Unloaded += (sender, args) => AppServices.Overlays.Host.PropertyChanged -= Host_OnPropertyChanged;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void Host_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(OverlayHostViewModel.IsOpen) || !AppServices.Overlays.Host.IsOpen) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Surface.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                if (!SystemParameters.ClientAreaAnimation) return;
                Surface.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
                SurfaceTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty,
                    new DoubleAnimation(AppServices.Overlays.Host.IsDrawer ? 28 : 0, 0, TimeSpan.FromMilliseconds(190))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }), System.Windows.Threading.DispatcherPriority.Input);
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
