using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BusyIndicator;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using fNbt;
using Microsoft.Web.WebView2.Core;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView
    {
        private ObservableCollection<Applet> Applets { get; set; }
        private readonly byte _startupBehaviourLevel = Settings.Default.StartBehaviour;
        private bool _isServerListLoading;

        public HomeView()
        {
            InitializeComponent();

            UserNameTextBlock.Text = App.Session != null ? (" " + App.Session.Username) : "";

            Loaded += (sender, e) =>
            {
                RefreshApplets();
            };
        }

        private async Task LoadWv()
        {
            // TODO: Add language changement support

            if (webView.CoreWebView2 != null) return;
            await webView.EnsureCoreWebView2Async();

            var core = webView.CoreWebView2;
            var settings = core.Settings;
            var profile = core.Profile;

            settings.AreBrowserAcceleratorKeysEnabled = false;
            //settings.AreDefaultContextMenusEnabled = false;
            settings.AreDevToolsEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsGeneralAutofillEnabled = false;
            settings.UserAgent += " TCLauncher/" + AppUtils.GetCurrentVersion();
            settings.IsSwipeNavigationEnabled = false;
            profile.IsGeneralAutofillEnabled = false;
            profile.IsPasswordAutosaveEnabled = false;
            profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Balanced;

            webView.CoreWebView2.ContextMenuRequested += delegate (object sender,
                CoreWebView2ContextMenuRequestedEventArgs args)
            {
                var menuList = args.MenuItems;

                var itemNamesToRemove = new[] { "saveAs", "print", "webCapture", "share", "moreTools" };
                var itemsToRemove = menuList.Where(coreWebView2ContextMenuItem => itemNamesToRemove.Contains(coreWebView2ContextMenuItem.Name)).ToList();

                foreach (var coreWebView2ContextMenuItem in itemsToRemove)
                {
                    menuList.Remove(coreWebView2ContextMenuItem);
                }

                var newItem =
                    webView.CoreWebView2.Environment.CreateContextMenuItem(
                        Languages.webview_context_open_in_web_browser, null, CoreWebView2ContextMenuItemKind.Command);
                newItem.CustomItemSelected += delegate
                {
                    var pageUri = args.ContextMenuTarget.PageUri;
                    Task.Run(() => InternetUtils.OpenUrlInWebBrowser(pageUri));
                };
                // TODO: Add icon with newItem.Icon = 
                menuList.Insert(menuList.Count, newItem);
            };
        }

        private void DiscoverEvent(object sender, MouseButtonEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).navigateToServer();
                }
            }
        }

        private async void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            var launchStarted = false;
            playBtn.IsEnabled = false;
            playBtn.Content = new Indicator()
            {
                IndicatorType = IndicatorType.ThreeDots,
                Margin = new Thickness(10)
            };

            var tclInstancesFolder = IoUtils.Tcl.InstancesPath;
            if (!(profileSelect.SelectedItem is InstalledInstance instance))
            {
                MessageBox.Show(Languages.select_instance_message);
                ResetPlayButton();
                return;
            }

            if (App.Session == null || !App.Session.CheckIsValid())
            {
                if (App.LoginHandler.AccountManager.GetAccounts().Count != 1)
                {
                    var result = MessageBox.Show(Languages.login_prompt_message, Languages.login, MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        App.MainWin.navigateToLogin();
                        ResetPlayButton();
                        return;
                    }
                    MessageBox.Show("Choose a Microsoft account or create an explicit offline profile on the Accounts page.", Languages.login);
                    App.MainWin.navigateToLogin();
                    ResetPlayButton();
                    return;
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
                        ResetPlayButton();
                        return;
                    }
                }

            }

            var instanceFolder = Path.Combine(tclInstancesFolder, instance.Guid.ToString(), "data");

            try
            {
                ServicePointManager.DefaultConnectionLimit = 256;

                var path = new MinecraftPath();
                if (instance.UseIsolation != true)
                {
                    switch (Settings.Default.SandboxLevel)
                    {
                        case 0:
                            path = AppUtils.GetMinecraftPathShared(instance.Guid);
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

                App.Launcher = new MinecraftLauncher(App.MinecraftPath.BasePath);

                string startVersion;
                using (var loaderCancellation = new CancellationTokenSource())
                {
                    var loaderWindow = new OperationWindow { Owner = App.MainWin };
                    loaderWindow.CancelRequested += (loaderSender, loaderArgs) => loaderCancellation.Cancel();
                    loaderWindow.Show();
                    try
                    {
                        startVersion = await AppServices.ModLoaders.EnsureInstalledAsync(
                            instance,
                            App.Launcher,
                            new Progress<OperationProgress>(loaderWindow.Update),
                            loaderCancellation.Token);
                    }
                    finally
                    {
                        loaderWindow.Close();
                    }
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
                    ExtraJvmArguments = instance.JVMArguments?.Select(argument => new MArgument(argument)),

                    ServerIp = mcServerAddress.IP,
                    ServerPort = mcServerAddress.Port ?? 25565,

                    VersionType = "\u00a7b@TCLauncher\u00a7r",
                    //GameLauncherName = "tcl",
                    //GameLauncherVersion = AppUtils.GetCurrentVersion(),

                    //DockName = "Minecraft on TCL"
                };

                var actionWindow = new ActionWindow(Languages.loading_game_message);

                App.Launcher.FileProgressChanged += (sender1, e1) =>
                {
                    // TODO: Check for start event
                    var progress = e1.ProgressedTasks;
                    var total = e1.TotalTasks;
                    var percent = total == 0 ? 0 : progress * 100d / total;
                    
                    actionWindow.percent = (int)Math.Round(percent);
                    actionWindow.text = e1.Name;

                    if (percent == 100)
                    {
                        actionWindow.Close();
                    }
                };

                App.Launcher.ByteProgressChanged += (sender1, e1) =>
                {
                    // This is only called when downloading, not when launching
                    // TODO: Add percent logic
                };

                actionWindow.Show();

                // Server List
                try
                {
                    if (instance.Servers != null && instance.Servers.Count > 0)
                    {
                        var serversFilePath = Path.Combine(instanceFolder, "servers.dat");
                        var serversNbtFile = new NbtFile();

                        bool validFile =
                            File.Exists(serversFilePath) &&
                            new FileInfo(serversFilePath).Length > 0;

                        if (validFile)
                        {
                            try
                            {
                                serversNbtFile.LoadFromFile(serversFilePath);
                            }
                            catch
                            {
                                // File exists but is corrupted or invalid
                                validFile = false;
                            }
                        }

                        if (!validFile)
                        {
                            // Create fresh valid NBT
                            var root = new NbtCompound("");
                            var serversList = new NbtList("servers", NbtTagType.Compound);
                            root.Add(serversList);
                            serversNbtFile.RootTag = root;
                        }

                        var servers = serversNbtFile.RootTag.Get<NbtList>("servers");

                        foreach (var instanceServer in instance.Servers)
                        {
                            bool exists = false;

                            foreach (NbtCompound server in servers)
                            {
                                var ip = server.Get<NbtString>("ip")?.Value;
                                if (ip == instanceServer.Address)
                                {
                                    exists = true;
                                    break;
                                }
                            }

                            if (exists)
                                continue;

                            if (instanceServer.Name == null || instanceServer.Address == null)
                                continue;

                            var newServer = new NbtCompound
                            {
                                new NbtString("name", instanceServer.Name),
                                new NbtString("ip", instanceServer.Address),
                                new NbtShort("x-tcl-suggested", 1)
                            };

                            servers.Add(newServer);
                        }

                        serversNbtFile.SaveToFile(serversFilePath, NbtCompression.None);
                    }
                }
                catch
                {
                    // Ignored for now
                }

                // TODO: Variable versions
                var process = await App.Launcher.CreateProcessAsync(startVersion, App.LaunchOption);

                playBtn.Content = Languages.running_game_message;

                process.EnableRaisingEvents = true;
                process.Exited += (sender1, e1) =>
                {
                    // TODO: Add closed logic

                    AppServices.Log.Info("game.exited", $"profile={instance.Guid}; exitCode={process.ExitCode}");
                    Dispatcher.Invoke(ResetPlayButton);
                };
                process.Start();
                launchStarted = true;
                AppServices.Log.Info("game.started", $"profile={instance.Guid}; processId={process.Id}; version={startVersion}");
                switch (_startupBehaviourLevel)
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
                AppServices.Log.Error("game.launch_failed", ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (!launchStarted) ResetPlayButton();
            }
        }

        private void ResetPlayButton()
        {
            playBtn.Content = Languages.play_button_text;
            playBtn.IsEnabled = true;
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
                webView.Source = null;
            }
        }

        private async void AppletItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Border border)) return;
            if (!(border.DataContext is Applet applet)) return;

            if (!applet.is_action) return;
            if (applet.OpenExternal)
            {
                Uri external;
                if (Uri.TryCreate(applet.ActionURL, UriKind.Absolute, out external) && external.Scheme == Uri.UriSchemeHttps)
                {
                    if (MessageBox.Show($"Open {external.Host} in your browser?", "TCLauncher", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                        Process.Start(applet.ActionURL);
                    return;
                }

                var result = MessageBox.Show(Languages.sandbox_security_message, Languages.tclauncher_security, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel) return;
            }

            await LoadWv();
            SetAppletViewState();
            try
            {
                webView.Source = new Uri(applet.ActionURL);
            }
            catch
            {
                try
                {
                    webView.Source = new Uri(
                        "data:text/plain;base64,RGllIFJlc3NvdXJjZSBrb25udGUgbmljaHQgZ2VsYWRlbiB3ZXJkZW4uIE1vZWdsaWNoZSBHcnVlbmRlIHNpbmQ6Ci0gSW50ZXJuZXRwcm9ibGVtZQotIE5pY2h0IGV4aXN0aWVyZW5kZSBSZXNzb3VyY2UKLSBVbmd1ZWx0aWdlcyBSZXNzb3VyY2VuZm9ybWF0Ci0gQmxvY2tpZXJ1bmcgZHVyY2ggVENMYXVuY2hlci1TaWNoZXJoZWl0");
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void ProfileSelect_SelectionChanged(object sender, RoutedEventArgs e)
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
                    serverList.Insert(0, new Server(Languages.no_server_message, null, null));

                    ServerSelect.ItemsSource = serverList;

                    ServerSelect.SelectedItem =
                        serverList.FirstOrDefault(s => s.Address == selectedInstance.LastServer) ?? serverList[0];

                    _isServerListLoading = false;
                }
                catch
                {
                    _isServerListLoading = true;

                    var serverList = new List<Server>();
                    serverList.Insert(0, new Server(Languages.no_server_message, null, null));

                    ServerSelect.ItemsSource = serverList;

                    ServerSelect.SelectedItem = serverList[0];

                    _isServerListLoading = false;
                }
            }
            RefreshApplets();
        }

        private async void RefreshApplets()
        {
            CatalogStatus.Text = string.Empty;
            var result = await AppServices.Catalog.LoadAsync(CancellationToken.None);
            if (!result.IsSuccess)
            {
                CatalogStatus.Text = result.Message;
                Applets = new ObservableCollection<Applet>();
            }
            else
            {
                var load = result.Value;
                if (load.IsOffline) CatalogStatus.Text = Languages.ResourceManager.GetString("catalog_offline");
                else if (load.IsStale) CatalogStatus.Text = "Cached catalog may be out of date";
                var cards = load.Catalog.Content.Select(card => new Applet(card.Weight, null, card.ImageUrl, card.Title, card.Summary, card.ActionUrl, true));
                var featured = load.Catalog.Items.Where(item => item.Featured).Take(2).Select(item =>
                    new Applet(100, item.Slug, item.ThumbnailUrl, item.Title, item.Summary, "https://tcraft.link/tclauncher/", true));
                Applets = new ObservableCollection<Applet>(cards.Concat(featured).OrderByDescending(card => card.Weight).Take(4));
            }

            mainApplets.ItemsSource = Applets;
        }

        private void WebViewBackButton_Click(object sender, RoutedEventArgs e)
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

        private async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (webView?.CoreWebView2 == null) return;
            try
            {
                await webView.EnsureCoreWebView2Async();
                webView.Stop();
                webView.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        private void WebView_OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Uri target;
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out target) || target.Scheme != Uri.UriSchemeHttps ||
                !(target.Host.Equals("tcraft.link", StringComparison.OrdinalIgnoreCase) || target.Host.EndsWith(".tcraft.link", StringComparison.OrdinalIgnoreCase)))
            {
                e.Cancel = true;
                if (target != null && target.Scheme == Uri.UriSchemeHttps &&
                    MessageBox.Show($"Open {target.Host} in your browser?", "TCLauncher", MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                    Process.Start(target.ToString());
            }
        }
    }
}
