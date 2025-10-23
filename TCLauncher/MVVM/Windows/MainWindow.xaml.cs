using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TCLauncher.Core;
using TCLauncher.MVVM.ViewModel;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel vm;
        private readonly bool is_silent;

        public MainWindow(bool silent = false)
        {
            InitializeComponent();
            vm = (MainViewModel)this.DataContext;
            is_silent = silent;

            //ResetBgMedia();
            loadingGrid.Visibility = Visibility.Visible;
            mainBorder.Visibility = Visibility.Collapsed;

            AppUtils.HandleUpdates();

            HandleFirstTime();
            ReloadNavPolicies();

            if (Settings.Default.UsePixelFontEverywhere)
            {
                FontFamily = (FontFamily)FindResource("PixelifySans");
            }
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
            } else
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



        //private void UpdateNetSpeeds()
        //{
        //    if (InternetUtils.ReachPage(remote_url))
        //    {
        //        connectionIndicator.Fill = Brushes.Green;
        //        long ms = InternetUtils.PingPage("https://www.google.com");
        //        connectionStatus.Text = (ms < 0) ? "Verbunden" : $"Verbunden ({ms} ms)";
        //    }
        //    else if (InternetUtils.ReachPage("google.com"))
        //    {
        //        connectionIndicator.Fill = Brushes.Yellow;
        //        connectionStatus.Text = "Getrennt";
        //    }
        //    else
        //    {
        //        connectionIndicator.Fill = Brushes.Red;
        //        connectionStatus.Text = "Kein Internet";
        //    }
        //}

        //private void Window_StateChanged(object sender, System.EventArgs e)
        //{
        //    switch (this.WindowState)
        //    {
        //        case WindowState.Normal:
        //            mainBorder.CornerRadius = new CornerRadius(20);
        //            mainBorder.BorderThickness = new Thickness(2);
        //            break;
        //        case WindowState.Maximized:
        //            FadeIn(0.2);
        //            mainBorder.CornerRadius = new CornerRadius(0);
        //            mainBorder.BorderThickness = new Thickness(0);
        //            break;
        //        case WindowState.Minimized:
        //            break;
        //        default:
        //            break;
        //    }
        //}

        //private async void connectionBorder_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    connectionIndicator.Fill = Brushes.Gray;
        //    connectionStatus.Text = "Bitte warten";
        //    await Task.Delay(1000);
        //    UpdateNetSpeeds();
        //}

        public void navigateToHome()
        {
            //homeBtn.IsChecked = true;
            vm.HomeViewCommand.Execute(null);
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

        public void SetDisplayAccount(string username)
        {
            if (username != null)
            {
                AccountManagerBtnName.Text = username;
                AccountManagerBtnPicture.Source = new BitmapImage(new Uri($"https://mc-heads.net/avatar/{username}", UriKind.Absolute));
            }
            else
            {
                AccountManagerBtnName.Text = Languages.not_logged_button_text;
                AccountManagerBtnPicture.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/steve.png"));
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

        //private void BgMediaElement_OnMediaEnded(object sender, RoutedEventArgs e)
        //{
        //    ResetBgMedia();
        //}

        //private void ResetBgMedia()
        //{
        //    BgMediaElement.Position = TimeSpan.Zero;
        //    BgMediaElement.Play();
        //}

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
