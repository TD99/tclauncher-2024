using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.MVVM.View;
using TCLauncher.MVVM.ViewModel;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainViewModel vm;
        private readonly bool is_silent;
        private bool _allowClose;
        private bool _closeAfterOperation;

        public MainWindow(bool silent = false)
        {
            InitializeComponent();
            vm = (MainViewModel)this.DataContext;
            is_silent = silent;

            //ResetBgMedia();
            loadingGrid.Visibility = Visibility.Visible;
            mainBorder.Visibility = Visibility.Collapsed;

            _ = CheckForUpdatesSilently();

            HandleFirstTime();
            ReloadNavPolicies();

            if (Settings.Default.UsePixelFontEverywhere)
            {
                FontFamily = (FontFamily)FindResource("PixelifySans");
            }

            Closing += MainWindow_OnClosing;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_allowClose || !AppServices.Operations.IsBusy) return;
            e.Cancel = true;
            if (AppServices.Overlays.Host.IsOpen) return;
            _ = AppServices.Overlays.ShowSheetAsync(
                Languages.ResourceManager.GetString("operation_in_progress") ?? "Operation in progress",
                new OperationCloseSheet(cancel =>
                {
                    _closeAfterOperation = true;
                    if (cancel) AppServices.Operations.RequestCancellation();
                    AppServices.Operations.PropertyChanged += Operations_OnPropertyChanged;
                }, () => Environment.Exit(0)), false);
        }

        private void Operations_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_closeAfterOperation || e.PropertyName != nameof(IOperationCoordinator.IsBusy) ||
                AppServices.Operations.IsBusy) return;
            AppServices.Operations.PropertyChanged -= Operations_OnPropertyChanged;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _allowClose = true;
                Close();
            }));
        }

        private static async Task CheckForUpdatesSilently()
        {
            var result = await AppServices.Updates.CheckAsync(
                Assembly.GetExecutingAssembly().GetName().Version,
                CancellationToken.None);
            if (result.IsSuccess && result.Value.IsUpdateAvailable)
                AppServices.Log.Info("update.available", result.Value.Manifest.Version);
        }

        // TODO: CHECK IF FIRST TIME
        private void HandleFirstTime()
        {
            if (!Settings.Default.FirstTime && !IoUtils.TclDirectory.IsEmpty(IoUtils.Tcl.InstancesPath)) return;
            Settings.Default.FirstTime = false;
            //newToolTip.PlacementTarget = serverBtn;
            //newToolTip.IsOpen = true;
        }

        public void ReloadNavPolicies()
        {
            // used to reload the navigation policies like previews
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!is_silent)
            {
                if (App.IsCoreLoaded)
                {
                    loadingGrid.Visibility = Visibility.Collapsed;
                }

                loadingAnim();
            }
            else
            {
                loadingGrid.Visibility = Visibility.Collapsed;
                mainBorder.Visibility = Visibility.Visible;
                mainBorder.Opacity = 100;
            }
        }

        public void loadingAnim()
        {
            DoubleAnimation pageAnim = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard pageStoryboard = new Storyboard();
            pageStoryboard.Children.Add(pageAnim);

            Storyboard.SetTarget(pageAnim, mainBorder);
            Storyboard.SetTargetProperty(pageAnim, new PropertyPath(OpacityProperty));

            pageStoryboard.Completed += (s2, e2) =>
            {
                pageAnim = null;
                pageStoryboard = null;

                loadingGrid.Visibility = Visibility.Collapsed;
                MainFrame.Children.Remove(loadingGrid);

                mainBorder.Visibility = Visibility.Visible;
                mainBorder.Opacity = 100;
            };

            pageStoryboard.Begin();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void minimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized) return;
            WindowState = WindowState.Minimized;
        }


        private void TopDrag_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        public void navigateToHome()
        {
            //homeBtn.IsChecked = true;
            vm.HomeViewCommand.Execute(null);
        }

        public void NavigateToHome(Guid profileId)
        {
            vm.SelectHomeProfile(profileId);
        }

        public void navigateToServer()
        {
            //serverBtn.IsChecked = true;
            vm.ServerListViewCommand.Execute(null);
        }

        public void navigateToLogin()
        {
            vm.AccountListViewCommand.Execute(null);
        }

        //public void navigateToSettings()
        //{
        //    settingsBtn.IsChecked = true;
        //    vm.SettingsViewCommand.Execute(null);
        //}

        public void navigateToStatus()
        {
            //statusBtn.IsChecked = true;
            vm.StatusViewCommand.Execute(null);
        }

        private void Logo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return; // (っ °Д °;)っ
            var currentAngle = !(logo.RenderTransform is RotateTransform transform) ? 0 : transform.Angle;

            var rotateTransform = new RotateTransform(currentAngle, logo.ActualWidth / 2, logo.ActualHeight / 2);
            logo.RenderTransform = rotateTransform;

            var angle = e.ChangedButton == MouseButton.Right ? -360 : 360;

            var animation = new DoubleAnimation(currentAngle, currentAngle + angle, TimeSpan.FromMilliseconds(350));
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

        private void AccountManagerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            vm.AccountListViewCommand.Execute(null);
        }

        public void SetDisplayAccount(string username, bool isOffline = false)
        {
            if (username != null)
            {
                AccountManagerBtnName.Text = isOffline ? username + " • Offline" : username;
                OfflineAccountGlyph.Visibility = isOffline ? Visibility.Visible : Visibility.Collapsed;
                AccountFallbackPicture.Visibility = isOffline ? Visibility.Collapsed : Visibility.Visible;
                AccountManagerBtnPicture.Source = isOffline
                    ? null
                    : new BitmapImage(new Uri($"https://mc-heads.net/avatar/{username}", UriKind.Absolute));
            }
            else
            {
                AccountManagerBtnName.Text = Languages.not_logged_button_text;
                OfflineAccountGlyph.Visibility = Visibility.Collapsed;
                AccountFallbackPicture.Visibility = Visibility.Visible;
                AccountManagerBtnPicture.Source =
                    new BitmapImage(new Uri("pack://application:,,,/Assets/Images/anonymous.png"));
            }
        }

        private void MainWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Topmost = true;
                Topmost = false;
            }
        }

        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            var animation = new ColorAnimation
            {
                From = ((SolidColorBrush)Background).Color,
                To = Color.FromRgb(102, 111, 123),
                Duration = new Duration(TimeSpan.FromMilliseconds(100))
            };
            Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            var animation = new ColorAnimation
            {
                From = ((SolidColorBrush)Background).Color,
                To = Color.FromRgb(71, 77, 85),
                Duration = new Duration(TimeSpan.FromMilliseconds(100))
            };
            Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        private void AccountListButton_OnClick(object sender, RoutedEventArgs e)
        {
            vm.AccountListViewCommand.Execute(null);
        }
    }
}