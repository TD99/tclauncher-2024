using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Utils;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using CmlLib.Core.Installer.FabricMC;
using TCLauncher.MVVM.ViewModel;
using TCLauncher.Properties;
using CmlLib.Core.Installer;
using CmlLib.Core.Downloader;
using CmlLib.Core.Version;

namespace TCLauncher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView
    {
        public ObservableCollection<Applet> Applets { get; set; }
        private byte StartupBehaviourLevel = Properties.Settings.Default.StartBehaviour;
        private HomeViewModel _vm;
        private bool _isServerListLoading = false;

        public HomeView()
        {
            InitializeComponent();
            _vm = (HomeViewModel)DataContext;
        }

        private async void loadWV()
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
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
                if (App.LoginHandler.AccountManager.GetAccounts().Count != 1)
                {
                    var result =
                        MessageBox.Show(
                            "Möchtest du dich mit einem Benutzerkonto anmelden? Klicke auf 'Nein' für ein Offline-Konto.",
                            "Login", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        App.MainWin.navigateToLogin();
                        return;
                    }

                    var dialog = new CustomInputDialog("Bitte gib den gewünschten Offline-Benutzernamen ein.")
                    {
                        Owner = App.MainWin
                    };

                    dialog.Show();

                    if (!await dialog.Result) return;

                    App.Session = MSession.CreateOfflineSession(dialog.ResponseText);
                    App.MainWin.SetDisplayAccount(dialog.ResponseText + " (Offline)");
                }
                else
                {
                    try
                    {
                        App.Session = await App.LoginHandler.AuthenticateSilently();
                        App.MainWin.SetDisplayAccount(App.Session?.Username);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }

            }

            var instanceFolder = Path.Combine(tclInstancesFolder, instance.Guid.ToString(), "data");

            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 256;

                var path = new MinecraftPath();
                var isIsolated = true;
                if (instance.UseIsolation != true)
                {
                    switch (Settings.Default.SandboxLevel)
                    {
                        case 0:
                            path = AppUtils.GetMinecraftPathShared(instance.Guid);
                            isIsolated = false;
                            break;
                        case 1:
                            path = AppUtils.GetMinecraftPathIsolated(instance.Guid);
                            break;
                    }
                }
                else
                {
                    path = AppUtils.GetMinecraftPathIsolated(instance.Guid);
                }

                App.MinecraftPath = path;

                App.Launcher = new CMLauncher(App.MinecraftPath);

                if (instance.UseFabric == true && !string.IsNullOrEmpty(instance.McVersion))
                {
                    var fabricLoader = new FabricVersionLoader();
                    var fabricVersions = await fabricLoader.GetVersionMetadatasAsync();

                    var fabric = fabricVersions.GetVersionMetadata(instance.McVersion);
                    await fabric.SaveAsync(App.MinecraftPath);

                    // update version list
                    await App.Launcher.GetAllVersionsAsync();
                }

                if (instance.UseForge == true && !string.IsNullOrEmpty(instance.McVersion))
                {
                    var names = new List<string>
                    {
                        instance.McVersion
                    };

                    await InstanceAssetsUtils.GetAssets(names, isIsolated);
                }

                var serverAddressString = ((Server) ServerSelect.SelectedItem).Address;
                var mcServerAddress = InternetUtils.GetMcServerAddress(serverAddressString);

                App.LaunchOption = new MLaunchOption
                {
                    StartVersion = null, // Fix
                    Session = App.Session,

                    Path = App.MinecraftPath,
                    MinimumRamMb = instance.MinimumRamMb ?? 0,
                    MaximumRamMb = instance.MaximumRamMb ?? 1024,
                    JVMArguments = instance.JVMArguments,

                    ServerIp = mcServerAddress.IP,
                    ServerPort = mcServerAddress.Port ?? 25565,

                    VersionType = "\u00a7b@TCLauncher",
                    //GameLauncherName = "tcl",
                    //GameLauncherVersion = AppUtils.GetCurrentVersion(),

                    //DockName = "Minecraft on TCL"
                };

                var actionWindow = new ActionWindow("Lade Spiel...");

                App.Launcher.FileChanged += (e1) =>
                {
                    // TODO: Check for start event
                    var progress = e1.ProgressedFileCount;
                    var total = e1.TotalFileCount;
                    var percent = progress / total * 100;

                    actionWindow.percent = percent;
                    actionWindow.text = $"[{e1.FileKind}] {e1.FileName}";
                };

                App.Launcher.ProgressChanged += (sender1, e1) =>
                {
                    // TODO: Add percent logic
                };

                actionWindow.Show();

                // TODO: Variable versions
                var process = await App.Launcher.CreateProcessAsync(instance.McVersion, App.LaunchOption);

                var processUtil = new ProcessUtil(process);
                processUtil.Exited += (sender1, e1) =>
                {
                    // TODO: Add closed logic
                };
                processUtil.StartWithEvents();
                switch (StartupBehaviourLevel)
                {
                    case 0:
                        break;
                    case 1:
                        App.MainWin.WindowState = WindowState.Minimized;
                        break;
                    case 2:
                        Application.Current.Shutdown();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

            if (!applet.is_action) return;
            if (applet.OpenExternal)
            {
                if (InternetUtils.HasProtocol(applet.ActionURL))
                {
                    Process.Start(applet.ActionURL);
                    return;
                }

                var result = MessageBox.Show($"Die Aktion ist möglicherweise gefährlich! Soll sie in der TCLauncher-Sandbox ausgeführt werden?\n\nACTION='{applet.ActionURL}'", "TCLauncher Sicherheit", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel) return;
            }

            SetAppletViewState();
            await Task.Delay(2000);
            try
            {
                webView.Source = new Uri(applet.ActionURL);
            }
            catch
            {
                webView.Source = new Uri("data:text/plain;base64,RGllIFJlc3NvdXJjZSBrb25udGUgbmljaHQgZ2VsYWRlbiB3ZXJkZW4uIE1vZWdsaWNoZSBHcnVlbmRlIHNpbmQ6Ci0gSW50ZXJuZXRwcm9ibGVtZQotIE5pY2h0IGV4aXN0aWVyZW5kZSBSZXNzb3VyY2UKLSBVbmd1ZWx0aWdlcyBSZXNzb3VyY2VuZm9ybWF0Ci0gQmxvY2tpZXJ1bmcgZHVyY2ggVENMYXVuY2hlci1TaWNoZXJoZWl0");
            }
        }

        private void profileSelect_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (profileSelect.SelectedItem is InstalledInstance selectedInstance)
            {
                Settings.Default.LastSelected = selectedInstance.Guid;
                Settings.Default.Save();

                // Set server list
                try
                {
                    _isServerListLoading = true;

                    var serverList = new List<Server>(selectedInstance.Servers);
                    serverList.Insert(0, new Server("No server", null, null));

                    ServerSelect.ItemsSource = serverList;

                    ServerSelect.SelectedItem =
                        serverList.FirstOrDefault(s => s.Address == selectedInstance.LastServer) ?? serverList[0];

                    _isServerListLoading = false;
                }
                catch
                {
                    _isServerListLoading = true;

                    var serverList = new List<Server>();
                    serverList.Insert(0, new Server("No server", null, null));

                    ServerSelect.ItemsSource = serverList;

                    ServerSelect.SelectedItem = serverList[0];

                    _isServerListLoading = false;
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

        private void ServerSelect_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isServerListLoading) return;

            if (!(profileSelect.SelectedItem is InstalledInstance selectedInstance)) return;
            
            selectedInstance.LastServer = ((Server)ServerSelect.SelectedItem).Address;
            IoUtils.Tcl.SaveInstalledInstanceConfig(selectedInstance);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            webView.Stop();
            webView.Dispose();
        }
    }
}
