using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CmlLib.Core;
using CmlLib.Utils;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.Models;
using T_Craft_Game_Launcher.MVVM.Windows;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public ObservableCollection<Applet> Applets { get; set; }
        private byte StartupBehaviourLevel = Properties.Settings.Default.StartBehaviour;

        public HomeView()
        {
            InitializeComponent();
            checkInstanceListEmpty();
        }

        private async void loadWV()
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        }

        // TODO: IMPROVE CODE QUALITY
        private void checkInstanceListEmpty()
        {
            // TODO: CHECK FOR N° OF INSTANCES INSTEAD
            if (Properties.Settings.Default.FirstTime)
            {
                profileSelect.Visibility = Visibility.Collapsed;
                profileNoneText.Visibility = Visibility.Visible;
            }
        }

        private void discoverEvent(object sender, MouseButtonEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).navigateToServer();
                }
            }
        }

        private async void playBtn_Click(object sender, RoutedEventArgs e)
        {
            var tclInstancesFolder = IoUtils.Tcl.InstancesPath;
            if (!(profileSelect.SelectedItem is InstalledInstance instance))
            {
                MessageBox.Show("Bitte wähle eine Instanz aus!");
                return;
            }

            if (App.Session == null || !App.Session.CheckIsValid())
            {
                MessageBox.Show("Bitte melde dich an!");
                return;
            }

            var instanceFolder = Path.Combine(tclInstancesFolder, instance.Guid.ToString(), "data");

            try
            {
                App.Launcher = new CMLauncher(new MinecraftPath(instanceFolder));
                // TODO: Variable versions
                var process = await App.Launcher.CreateProcessAsync("1.20.1", new MLaunchOption
                {
                    Session = App.Session
                });

                var processUtil = new ProcessUtil(process);
                processUtil.OutputReceived += (sender1, e1) => { Console.WriteLine(e1); };
                processUtil.StartWithEvents();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //try
            //{
            //    string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
            //    string instanceFolder = Path.Combine(tclFolder, "Instances");
            //    string runtimeFolder = Path.Combine(tclFolder, "Runtime");
            //    string udataFolder = Path.Combine(tclFolder, "UData");

            //    string selectedInstanceFolder = Path.Combine(instanceFolder, selectedInstance.Guid.ToString());
            //    string instanceDataFolder = Path.Combine(selectedInstanceFolder, "data");

            //    if (!Directory.Exists(selectedInstanceFolder))
            //    {
            //        MessageBox.Show($"Die Instanz '{selectedInstance.Name}' konnte nicht gefunden werden!");
            //        return;
            //    }

            //    string udataLoginFile = Path.Combine(udataFolder, "launcher_accounts.json");
            //    string targetLoginFile = Path.Combine(instanceDataFolder, "launcher_accounts.json");

            //    if (File.Exists(udataLoginFile))
            //    {
            //        File.Copy(udataLoginFile, targetLoginFile, true);
            //    }

            //    string msaCredentialsFile = Path.Combine(udataFolder, "launcher_msa_credentials.bin");
            //    string targetMsaCredentialsFile = Path.Combine(instanceDataFolder, "launcher_msa_credentials.bin");

            //    if (File.Exists(msaCredentialsFile))
            //    {
            //        File.Copy(msaCredentialsFile, targetMsaCredentialsFile, true);
            //    }

            //    string exeFile = Path.Combine(runtimeFolder, "Minecraft.exe");
            //    if (!File.Exists(exeFile))
            //    {
            //        MessageBox.Show("Der MC Launcher existiert nicht!");
            //        return;
            //    }

            //    Process launcher = new Process();
            //    launcher.StartInfo.FileName = exeFile;
            //    launcher.StartInfo.Arguments = $"--workDir=\"{instanceDataFolder}\"";
            //    launcher.EnableRaisingEvents = true;

            //    launcher.Exited += (sender1, e1) =>
            //    {
            //        try
            //        {
            //            if (File.Exists(targetLoginFile) && new FileInfo(targetLoginFile).Length > 0)
            //            {
            //                File.Copy(targetLoginFile, udataLoginFile, true);
            //            }
            //            if (File.Exists(targetMsaCredentialsFile) && new FileInfo(targetMsaCredentialsFile).Length > 0)
            //            {
            //                File.Copy(targetMsaCredentialsFile, msaCredentialsFile, true);
            //            }
            //        }
            //        catch { }
            //    };

            //    var action = new ActionWindow("Start vorbereiten...");
            //    action.Owner = Application.Current.MainWindow;
            //    action.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //    action.Show();

            //    launcher.Start();

            //    Task.Delay(1000).ContinueWith(t =>
            //    {
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            action.Hide();
            //        });
            //    });

            //    switch (StartupBehaviourLevel)
            //    {
            //        case 0:
            //            break;
            //        case 1:
            //            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            //            break;
            //        case 2:
            //            Application.Current.Shutdown();
            //            break;
            //    }
            //}
            //catch
            //{
            //    MessageBox.Show("Ein Startfehler ist aufgetreten!");
            //}
        }

        private void SetAppletViewState(bool val = true)
        {
            if (val)
            {
                homeOverview.Visibility = Visibility.Collapsed;
                mainApplets.Visibility = Visibility.Collapsed;
                appletView.Visibility = Visibility.Visible;
            }
            else
            {
                homeOverview.Visibility = Visibility.Visible;
                mainApplets.Visibility = Visibility.Visible;
                appletView.Visibility = Visibility.Collapsed;

                webView.Source = new Uri("https://tcraft.link/tclauncher/api/plugins/applet-loader/");
            }
        }

        private async void AppletItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            Applet applet = (Applet)border.DataContext;

            if (applet.ActionURL is null) return;
            
            SetAppletViewState(true);

            await Task.Delay(2000);

            webView.Source = new System.Uri(applet.ActionURL);
        }

        private void profileSelect_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (profileSelect.SelectedItem is InstalledInstance selectedInstance)
            {
                Properties.Settings.Default.LastSelected = selectedInstance.Guid;
                Properties.Settings.Default.Save();

                if (selectedInstance.Servers != null && selectedInstance.Servers.Any())
                {
                    servInfo.Visibility = Visibility.Visible;
                    serverSelect.SelectedIndex = 0;
                }
                else
                {
                    servInfo.Visibility = Visibility.Collapsed;
                }
            }
            RefreshApplets();
        }

        private async void RefreshApplets()
        {
            mainApplets.ItemsSource = new ObservableCollection<Applet>
            {
                new Applet(1, null, "https://tcraft.link/tclauncher/api/assets/loader.gif", null, null, null),
                new Applet(2, null, "https://tcraft.link/tclauncher/api/assets/loader.gif", null, null, null)
            };

            try
            {
                InstalledInstance selectedInstance = profileSelect.SelectedItem as InstalledInstance;
                if (selectedInstance is null) throw new Exception();
                string appletsURL = selectedInstance.AppletURL;
                if (String.IsNullOrEmpty(appletsURL)) throw new Exception();
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(appletsURL);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Applets = new ObservableCollection<Applet>(JsonConvert.DeserializeObject<ObservableCollection<Applet>>(content).OrderByDescending(a => a.Weight));
                }
            }
            catch {
                Applets = null;
            }

            mainApplets.ItemsSource = Applets;
        }

        private void webViewBackButton_Click(object sender, RoutedEventArgs e)
        {
            SetAppletViewState(false);
        }

        private void serverSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serverSelect.SelectedItem == null) return;
            Server server = (Server)serverSelect.SelectedItem;
            currentServerImg.Source = new BitmapImage(new Uri(@"https://tcraft.link/tclauncher/api/plugins/server-tool/GetAccent.php?literal&url=" + server.IP));
        }
    }
}
