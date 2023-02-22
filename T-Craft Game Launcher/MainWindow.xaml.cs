using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Model;
using T_Craft_Game_Launcher.MVVM.ViewModel;

namespace T_Craft_Game_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double lastTop;
        private bool is_minimAnim = false;
        private DateTime lastWinAct = DateTime.MinValue;
        private string remote_url = Properties.Settings.Default.DownloadMirror;
        private MainViewModel vm;

        public MainWindow()
        {
            InitializeComponent();
            vm = (MainViewModel)this.DataContext;
            INetCheck();

            genericConfig();
        }

        private async void genericConfig()
        {
            string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
            string instanceFolder = Path.Combine(tclFolder, "Instances");
            string runtimeFolder = Path.Combine(tclFolder, "Runtime");
            string udataFolder = Path.Combine(tclFolder, "UData");
            try
            {
                if (!Directory.Exists(tclFolder)) Directory.CreateDirectory(tclFolder);
                if (!Directory.Exists(instanceFolder)) Directory.CreateDirectory(instanceFolder);
                if (!Directory.Exists(udataFolder)) Directory.CreateDirectory(udataFolder);
                if (!Directory.Exists(runtimeFolder))
                {
                    Directory.CreateDirectory(runtimeFolder);

                    string runtimeURL = @"https://launcher.mojang.com/download/Minecraft.exe";
                    string runtimeFile = Path.Combine(runtimeFolder, "Minecraft.exe");

                    using (var client = new WebClient())
                    {
                        string fileSize = await GetFileSizeAsync(runtimeURL);

                        ActionWindow action = new ActionWindow($"Installieren des Pakets 'ch.tcraft.runtime'\nGrösse: {fileSize}");
                        action.Show();
                        action.Closed += (sender, e) =>
                        {
                            client.CancelAsync();
                        };

                        try
                        {
                            await client.DownloadFileTaskAsync(new Uri(runtimeURL), runtimeFile);
                        }
                        catch
                        {
                            MessageBox.Show("Download abgebrochen!");
                            action.Close();
                            return;
                        }

                        action.Close();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Ein Initialisierungsfehler ist aufgetreten.");
                return;
            }
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
            loadingAnim();
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
            TimeSpan elapsed = DateTime.Now - lastWinAct;
            if (is_minimAnim || elapsed.TotalSeconds < 3) return;

            is_minimAnim = true;
            lastTop = this.Top;

            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

            for (double i = this.Top; i < screenHeight; i += 50)
            {
                this.Top = i;
                await Task.Delay(1);
            }

            this.Top = screenHeight;

            this.WindowState = WindowState.Minimized;

            lastWinAct = DateTime.Now;
        }

        private void maximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - lastWinAct;
            if (elapsed.TotalSeconds < 3) return;

            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;

            lastWinAct = DateTime.Now;
        }

        private void topDrag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void INetCheck()
        {
            if (INetTools.requestPage(remote_url))
            {
                connectionIndicator.Fill = Brushes.Green;
                connectionStatus.Text = "Verbunden";
            }
            else if (INetTools.requestPage("google.com"))
            {
                connectionIndicator.Fill = Brushes.Yellow;
                connectionStatus.Text = "Getrennt";
            }
            else
            {
                connectionIndicator.Fill = Brushes.Red;
                connectionStatus.Text = "Kein Internet";
            }
        }

        private async void Window_StateChanged(object sender, System.EventArgs e)
        {
            INetCheck();
            if (this.WindowState == WindowState.Normal && is_minimAnim)
            {
                for (double i = this.Top; i > lastTop; i -= 50)
                {
                    this.Top = i;
                    await Task.Delay(1);
                }

                this.Top = lastTop;
                is_minimAnim = false;
            }
            mainBorder.CornerRadius = new CornerRadius(20);
            mainBorder.BorderThickness = new Thickness(2);
            if (this.WindowState == WindowState.Maximized)
            {
                mainBorder.CornerRadius = new CornerRadius(0);
                mainBorder.BorderThickness = new Thickness(0);
            }
        }

        private async void connectionBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            connectionIndicator.Fill = Brushes.Gray;
            connectionStatus.Text = "Bitte warten";
            await Task.Delay(1000);
            INetCheck();
        }

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

        public void navigateToConfigEditor()
        {
            vm.ConfigEditorViewCommand.Execute(null);
        }
    }
}
