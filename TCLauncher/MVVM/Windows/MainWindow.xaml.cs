using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CmlLib.Core.Auth;
using TCLauncher.Core;
using TCLauncher.MVVM.ViewModel;

namespace TCLauncher.MVVM.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string remote_url = Properties.Settings.Default.DownloadMirror;
        private MainViewModel vm;
        private bool is_silent = false;

        public MainWindow(bool silent = false)
        {
            InitializeComponent();
            vm = (MainViewModel)this.DataContext;
            is_silent = silent;

            ResetBgMedia();
            loadingGrid.Visibility = Visibility.Visible;
            mainBorder.Visibility = Visibility.Collapsed;

            AppUtils.HandleUpdates();

            HandleFirstTime();
            ReloadNavPolicies();
        }

        // TODO: CHECK IF FIRST TIME
        private void HandleFirstTime()
        {
            if (!Properties.Settings.Default.FirstTime && !IoUtils.TclDirectory.IsEmpty(IoUtils.Tcl.InstancesPath)) return;
            Properties.Settings.Default.FirstTime = false;
            //newToolTip.PlacementTarget = serverBtn;
            //newToolTip.IsOpen = true;
        }

        public void ReloadNavPolicies()
        {
            socBtn.Visibility = Properties.Settings.Default.UseSocial ? Visibility.Visible : Visibility.Collapsed;
        }

        public async Task<string> GetFileSizeAsync(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        long fileSize = response.Content.Headers.ContentLength ?? 0;
                        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                        int order = 0;
                        while (fileSize >= 1024 && order < sizes.Length - 1)
                        {
                            fileSize = fileSize / 1024;
                            order++;
                        }
                        return string.Format("{0:0.##} {1}", fileSize, sizes[order]);
                    }
                }
            }
            catch { }

            return "Unbekannt";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!is_silent)
            {
                loadingAnim();
            } else
            {
                loadingGrid.Visibility = Visibility.Collapsed;
            }
        }

        public void loadingAnim()
        {
            DoubleAnimation pageAnim = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = new Duration(TimeSpan.FromSeconds(4)),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
            };
            
            Storyboard pageStoryboard = new Storyboard();
            pageStoryboard.Children.Add(pageAnim);

            Storyboard.SetTarget(pageAnim, mainBorder);
            Storyboard.SetTargetProperty(pageAnim, new PropertyPath(FrameworkElement.OpacityProperty));

            pageStoryboard.Completed += (s2, e2) =>
            {
                pageAnim = null;
                pageStoryboard = null;
                loadingGrid.Visibility = Visibility.Collapsed;

                mainBorder.Visibility = Visibility.Visible;
                mainBorder.Opacity = 100;
            };

            pageStoryboard.Begin();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void minimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized) return;
            WindowState = WindowState.Minimized;
        }

        private async void maximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void TopDrag_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.WindowState = (this.WindowState == WindowState.Maximized)
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                this.DragMove();
            }
        }

        private void TopDrag_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
            }
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

        private void FadeIn(double secs)
        {
            var fadeInAnimation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromSeconds(secs));
            BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }

        private void FadeOut(double secs)
        {
            var fadeOutAnimation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(secs));
            BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }

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
            vm.AccountViewCommand.Execute(null);
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
            if (e.ClickCount == 2) // (っ °Д °;)っ
            {
                var transform = logo.RenderTransform as RotateTransform;
                double currentAngle = transform == null ? 0 : transform.Angle;

                var rotateTransform = new RotateTransform(currentAngle, logo.ActualWidth / 2, logo.ActualHeight / 2);
                logo.RenderTransform = rotateTransform;

                var angle = e.ChangedButton == MouseButton.Right ? -360 : 360;

                var animation = new DoubleAnimation(currentAngle, currentAngle + angle, TimeSpan.FromMilliseconds(350));
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }

        private void loadingImg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // (っ °Д °;)っ
            {
                this.FontFamily = new FontFamily("Comic Sans MS");
                AppName.Text = "ComicLauncher";
            }
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
                this.WindowState = (this.WindowState == WindowState.Maximized)
                    ? WindowState.Normal
                    : WindowState.Maximized;
        }

        private void AccountManagerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            vm.AccountViewCommand.Execute(null);
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
                AccountManagerBtnName.Text = "Nicht eingeloggt";
                AccountManagerBtnPicture.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/steve.png"));
            }
        }

        private async void MainWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Topmost = true;
                Topmost = false;
            }
        }

        private void BgMediaElement_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            ResetBgMedia();
        }

        private void ResetBgMedia()
        {
            BgMediaElement.Position = TimeSpan.Zero;
            BgMediaElement.Play();
        }
    }
}
