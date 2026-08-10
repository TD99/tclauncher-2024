using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BusyIndicator;
using CmlLib.Core;
using fNbt;
using Newtonsoft.Json;
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

            UserNameTextBlock.Text = App.Session != null ? (", " + App.Session.Username) : "";

            Loaded += (sender, e) => { RefreshApplets(); };
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
                AppServices.Overlays.ShowToast("Choose a profile", Languages.select_instance_message,
                    ToastTone.Warning);
                ResetPlayButton();
                return;
            }

            if (App.Session == null || !App.Session.CheckIsValid())
            {
                if (App.LoginHandler.AccountManager.GetAccounts().Count != 1)
                {
                    await AppServices.Overlays.ShowSheetAsync("Choose an account",
                        "Select a Microsoft account or create an explicit offline profile before playing.");
                    App.MainWin.navigateToLogin();
                    ResetPlayButton();
                    return;
                }
                else
                {
                    try
                    {
                        App.SetMicrosoftSession(await App.LoginHandler.AuthenticateSilently());
                    }
                    catch (Exception ex)
                    {
                        AppServices.Overlays.ShowToast("Sign-in failed", ex.Message, ToastTone.Error);
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

                var selectedServer = ServerSelect.SelectedItem as Server;

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

                var launchResult = await AppServices.Operations.RunAsync(
                    "Starting " + instance.DisplayName,
                    true,
                    (progress, cancellationToken) => AppServices.Launches.StartAsync(
                        instance,
                        App.Session,
                        selectedServer,
                        App.Launcher,
                        App.MinecraftPath,
                        progress,
                        cancellationToken));

                if (!launchResult.IsSuccess)
                    throw launchResult.Exception ?? new InvalidOperationException(launchResult.Message);

                var process = launchResult.Value.Process;

                playBtn.Content = Languages.running_game_message;

                process.Exited += (sender1, e1) => { Dispatcher.Invoke(ResetPlayButton); };
                launchStarted = true;
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
                _ = AppServices.Overlays.ShowSheetAsync("Minecraft could not start", new LaunchErrorSheet(ex.Message,
                    () => PlayBtn_Click(playBtn, new RoutedEventArgs())), false);
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

        private async void AppletItem_OnClick(object sender, RoutedEventArgs e)
        {
            var applet = (sender as Button)?.Tag as Applet;
            if (applet == null || !applet.is_action) return;
            if (!Uri.TryCreate(applet.ActionURL, UriKind.Absolute, out var target) ||
                target.Scheme != Uri.UriSchemeHttps)
            {
                AppServices.Overlays.ShowToast("Link blocked", "Only secure links can be opened.", ToastTone.Warning);
                return;
            }

            if (!await AppServices.Overlays.ConfirmAsync("Open external link",
                    "Open " + target.Host + " in your browser?", "Open browser", "Cancel")) return;
            Process.Start(target.ToString());
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
            var merged = new List<Applet>();
            var result = await AppServices.Catalog.LoadAsync(CancellationToken.None);
            if (!result.IsSuccess)
            {
                CatalogStatus.Text = "Offline";
            }
            else
            {
                var load = result.Value;
                if (load.IsOffline) CatalogStatus.Text = Languages.ResourceManager.GetString("catalog_offline");
                else if (load.IsStale) CatalogStatus.Text = "Cached catalog may be out of date";
                var cards = load.Catalog.Content.Select(card => new Applet(card.Weight, null, card.ImageUrl, card.Title,
                    card.Summary, card.ActionUrl, true, "T-Craft"));
                var featured = load.Catalog.Items.Where(item => item.Featured).Take(2).Select(item =>
                    new Applet(100, item.Slug, item.ThumbnailUrl, item.Title, item.Summary,
                        "https://tcraft.link/tclauncher/", true, "T-Craft"));
                merged.AddRange(cards.Concat(featured));
            }

            if (profileSelect.SelectedItem is InstalledInstance selected &&
                Uri.TryCreate(selected.AppletURL, UriKind.Absolute, out var appletUri) &&
                appletUri.Scheme == Uri.UriSchemeHttps)
            {
                try
                {
                    var json = await LauncherHttpClient.Instance.GetStringAsync(appletUri);
                    var legacy = JsonConvert.DeserializeObject<List<Applet>>(json) ?? new List<Applet>();
                    foreach (var applet in legacy)
                    {
                        applet.Origin = "Profile";
                        merged.Add(applet);
                    }
                }
                catch (Exception exception)
                {
                    AppServices.Log.Warning("home.profile_content_unavailable", exception.Message);
                }
            }

            Applets = new ObservableCollection<Applet>(merged.Where(card => !string.IsNullOrWhiteSpace(card.Title))
                .GroupBy(card => (card.Origin ?? "") + "|" + card.Title).Select(group => group.First())
                .OrderByDescending(card => card.Weight).Take(6));
            mainApplets.ItemsSource = Applets;
        }

        private void ServerSelect_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isServerListLoading) return;

            if (!(profileSelect.SelectedItem is InstalledInstance selectedInstance)) return;

            selectedInstance.LastServer = (ServerSelect.SelectedItem as Server)?.Address;
            IoUtils.Tcl.SaveInstalledInstanceConfig(selectedInstance);
        }

        private void DiscoverButton_OnClick(object sender, RoutedEventArgs e) => App.MainWin.navigateToServer();

        private void RecentProfile_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is InstalledInstance profile)) return;
            profileSelect.SelectedItem =
                (profileSelect.ItemsSource as IEnumerable<InstalledInstance>)?.FirstOrDefault(item =>
                    item.Guid == profile.Guid);
        }
    }
}