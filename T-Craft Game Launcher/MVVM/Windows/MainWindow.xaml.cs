using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CmlLib.Core.Auth;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.ViewModel;

namespace T_Craft_Game_Launcher.MVVM.Windows
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

            AppUtils.HandleUpdates();

            handleFirstTime();
        }

        // TODO: CHECK IF FIRST TIME
        private void handleFirstTime()
        {
            newToolTip.PlacementTarget = serverBtn;
            newToolTip.IsOpen = true;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!is_silent)
            {
                loadingAnim();
            } else
            {
                loadingImg.Visibility = Visibility.Collapsed;
            }
        }

        private void loadingAnim()
        {
            mainBorder.Visibility = Visibility.Collapsed;
            mainBorder.Opacity = 0;
            DoubleAnimation inAnim = new DoubleAnimation
            {
                From = 0,
                To = 200,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation pageAnim = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard inStoryboard = new Storyboard();
            inStoryboard.Children.Add(inAnim);

            Storyboard.SetTarget(inAnim, loadingImg);
            Storyboard.SetTargetProperty(inAnim, new PropertyPath(FrameworkElement.WidthProperty));

            Storyboard.SetTarget(inAnim, loadingImg);
            Storyboard.SetTargetProperty(inAnim, new PropertyPath(FrameworkElement.HeightProperty));

            inStoryboard.Completed += (s, e) =>
            {
                Storyboard pageStoryboard = new Storyboard();
                pageStoryboard.Children.Add(pageAnim);

                Storyboard.SetTarget(pageAnim, mainBorder);
                Storyboard.SetTargetProperty(pageAnim, new PropertyPath(FrameworkElement.OpacityProperty));

                mainBorder.Visibility = Visibility.Visible;

                pageStoryboard.Completed += (s2, e2) =>
                {
                    inAnim = null;
                    inStoryboard = null;
                    pageAnim = null;
                    pageStoryboard = null;
                    loadingImg.Visibility = Visibility.Collapsed;

                    mainBorder.Visibility = Visibility.Visible;
                    mainBorder.Opacity = 100;
                };

                pageStoryboard.Begin();
            };

            inStoryboard.Begin();
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void minimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized) return;

            minimizeBtn.IsEnabled = false;
            
            FadeOut(0.15);

            await Task.Delay(150);
            
            WindowState = WindowState.Minimized;

            FadeIn(0);

            minimizeBtn.IsEnabled = true;
        }

        private async void maximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                FadeOut(0.2);
                await Task.Delay(300);
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

        private void Window_StateChanged(object sender, System.EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Normal:
                    mainBorder.CornerRadius = new CornerRadius(20);
                    mainBorder.BorderThickness = new Thickness(2);
                    break;
                case WindowState.Maximized:
                    FadeIn(0.2);
                    mainBorder.CornerRadius = new CornerRadius(0);
                    mainBorder.BorderThickness = new Thickness(0);
                    break;
                case WindowState.Minimized:
                    break;
                default:
                    break;
            }
        }

        //private async void connectionBorder_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    connectionIndicator.Fill = Brushes.Gray;
        //    connectionStatus.Text = "Bitte warten";
        //    await Task.Delay(1000);
        //    UpdateNetSpeeds();
        //}

        public void navigateToHome()
        {
            homeBtn.IsChecked = true;
            vm.HomeViewCommand.Execute(null);
        }

        public void navigateToServer()
        {
            serverBtn.IsChecked = true;
            vm.ServerListViewCommand.Execute(null);
        }

        public void navigateToSettings()
        {
            settingsBtn.IsChecked = true;
            vm.SettingsViewCommand.Execute(null);
        }

        public void navigateToStatus()
        {
            statusBtn.IsChecked = true;
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
            switch (e.Key)
            {
                case Key.F11:
                    this.WindowState = (this.WindowState == WindowState.Maximized)
                        ? WindowState.Normal
                        : WindowState.Maximized;
                    break;
            }
        }

        private async void AccountManagerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var accountWindow = new AccountWindow(App.Session?.UUID)
            {
                Owner = this
            };
            accountWindow.Show();

            var session = await accountWindow.LoginTask.Task;
            if (session == null) return;
            SetDisplayAccount(session?.Username);
            App.Session = session;
        }

        public void SetDisplayAccount(string username)
        {
            AccountManagerBtn.Content = username;
        }
    }
}
