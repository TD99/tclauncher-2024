using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.Sessions;
using CmlLib.Core.ProcessBuilder;
using Microsoft.Win32;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;
using TCLauncher.Setup;
using static System.String;
using static TCLauncher.Core.MessageBoxUtils;

namespace TCLauncher
{
    public partial class App
    {
        public const string URI_SCHEME = "tcl";
        public const string FRIENDLY_NAME = "TCLauncher";
        private const string PIPE_NAME = "TCLauncher.WindowsEdition.v1";
        private static Mutex mutex;
        private SingleInstanceService _singleInstance;
        
        public static bool is_silent;
        public static bool kill_old;

        private static MSession _session;

        public static string AppArgs;
        public static Uri UriArgs;

        public static bool IsCoreLoaded = false;

        private static bool LoadUI = true;

        public static MSession Session
        {
            get => _session;
            set
            {
                _session = value;
                Settings.Default.LastAccountUUID = value?.UUID ?? "";
                Settings.Default.Save();
            }
        }

        public static JELoginHandler LoginHandler;

        public static MinecraftPath MinecraftPath { get; set; }
        public static MinecraftLauncher Launcher { get; set; }
        public static MainWindow MainWin { get; set; }
        public static InstallerWelcomeWindow InstallerWin { get; set; }

        public App()
        {
            AppServices.Initialize(IoUtils.Tcl.RootPath);
            SetLanguage(Settings.Default.Language);
            Startup += App_Startup;
            DispatcherUnhandledException += (sender, args) =>
            {
                var operationId = Guid.NewGuid().ToString("N");
                AppServices.Log.Error("application.dispatcher_unhandled", args.Exception, operationId);
                MessageBox.Show($"TCLauncher encountered an unexpected error. Reference: {operationId}", "TCLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                AppServices.Log.Error("application.domain_unhandled", args.ExceptionObject as Exception ?? new Exception("Unknown fatal error"));
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                AppServices.Log.Error("application.task_unobserved", args.Exception);
                args.SetObserved();
            };
        }

        public static void SetMicrosoftSession(MSession session)
        {
            Session = session;
            AppServices.OfflineProfiles.Select(null);
            if (session == null) AppServices.AccountSelection.Clear();
            else AppServices.AccountSelection.SetMicrosoft(session.UUID, session.Username);
            MainWin?.SetDisplayAccount(session?.Username, false);
        }

        public static void SetOfflineSession(OfflineProfile profile)
        {
            if (profile == null)
            {
                Session = null;
                AppServices.OfflineProfiles.Select(null);
                AppServices.AccountSelection.Clear();
                MainWin?.SetDisplayAccount(null, false);
                return;
            }
            AppServices.OfflineProfiles.Select(profile.Id);
            Session = MSession.CreateOfflineSession(profile.Username);
            AppServices.AccountSelection.SetOffline(profile.Id, profile.Username);
            MainWin?.SetDisplayAccount(profile.Username, true);
        }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            UriArgs = Get_AppURI(e.Args);
            AppArgs = Join(" ", e.Args);

            bool createdNew;
            mutex = new Mutex(true, PIPE_NAME, out createdNew);
            if (!createdNew && Settings.Default.MultiInstances != 2)
            {
                await SingleInstanceService.SendAsync(PIPE_NAME, e.Args, 3000);
                Shutdown();
                return;
            }

            if (UriArgs == null)
            {
                await ProcessAppArgs(e.Args);
            }
            else
            {
                ProcessAppURI(UriArgs);
            }

            RegisterURIScheme();

            RegisterDefaultEnvironment();

            try
            {
                IoUtils.Tcl.CreateDirectries();
            }
            catch (Exception exception)
            {
                var result = MessageBox.Show(Languages.error_creating_folder_structure + exception.Message, Languages.initialization_error, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel) Environment.Exit(1);
            }

            Launcher = new MinecraftLauncher(IoUtils.Tcl.DefaultPath);

            LoginHandler = new JELoginHandlerBuilder()
                .WithAccountManager(Path.Combine(IoUtils.Tcl.UdataPath, "tcl_accounts.json"))
                .Build();

            if (LoadUI) ShowUI();
            TryAutoLogin();

            if (createdNew)
            {
                _singleInstance = new SingleInstanceService(PIPE_NAME, arguments => Dispatcher.BeginInvoke(new Action(() => HandleHandoff(arguments))));
                _singleInstance.Start();
            }

            IsCoreLoaded = true;
        }

        public static void SetLanguage(string language, bool isHotReload = false)
        {
            var newCulture = new CultureInfo(language);
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;
            CultureInfo.DefaultThreadCurrentCulture = newCulture;
            CultureInfo.DefaultThreadCurrentUICulture = newCulture;

            if (isHotReload)
            {
                HotReload();
            }
        }

        public static void HotReload()
        {
            RegisterDefaultEnvironment();
            var oldWin = MainWin;
            MainWin = new MainWindow
            {
                Top = oldWin.Top,
                Left = oldWin.Left,
                Width = oldWin.Width,
                Height = oldWin.Height,
                WindowState = oldWin.WindowState,
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            Current.MainWindow = MainWin;
            MainWin.Show();
            oldWin.Close();
        }

        public static void HotReloadInstaller()
        {
            RegisterDefaultEnvironment();
            var oldWin = InstallerWin;
            InstallerWin = new InstallerWelcomeWindow(InstallerWin.CurrentStep)
            {
                Top = oldWin.Top,
                Left = oldWin.Left,
                Width = oldWin.Width,
                Height = oldWin.Height,
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            InstallerWin.Show();
            oldWin.Close();
        }

        private async void TryAutoLogin()
        {
            try
            {
                var selected = AppServices.AccountSelection.Get();
                if (selected?.Kind == AccountSelectionKind.Offline && Guid.TryParse(selected.StableId, out var offlineId))
                {
                    var offline = AppServices.OfflineProfiles.List().FirstOrDefault(profile => profile.Id == offlineId);
                    if (offline != null) SetOfflineSession(offline);
                    else SetOfflineSession(null);
                    return;
                }

                if (selected?.Kind != AccountSelectionKind.Microsoft) return;
                var accounts = LoginHandler.AccountManager.GetAccounts();
                foreach (var account in accounts)
                {
                    if (!(account is JEGameAccount jeGameAccount)) continue;
                    if (!string.Equals(jeGameAccount?.Profile?.UUID, selected.StableId, StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        var session = await LoginHandler.Authenticate(jeGameAccount);
                        SetMicrosoftSession(session);
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                }
            } catch (Exception e)
            {
                MessageBox.Show(Languages.error_automatic_sign_in + e.Message);
            }
        }

        private Uri Get_AppURI(string[] args)
        {
            if (args.Length > 0)
            {
                if (Uri.TryCreate(args[0], UriKind.Absolute, out var uri) &&
                    String.Equals(uri.Scheme, URI_SCHEME, StringComparison.OrdinalIgnoreCase))
                {
                    return uri;
                }
            }

            return null;
        }

        private async Task ProcessAppArgs(string[] arguments)
        {
            for (var i = 0; i != arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "--uninstallCheck":
                        try
                        {
                            var targetDir = Path.GetFullPath(arguments[i + 1] ?? throw new ArgumentNullException());
                            var instancesDir = Path.GetFullPath(IoUtils.Tcl.InstancesPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                            if (!targetDir.StartsWith(instancesDir, StringComparison.OrdinalIgnoreCase) || targetDir.Equals(instancesDir, StringComparison.OrdinalIgnoreCase))
                                throw new DirectoryNotFoundException(Languages.target_dir_not_instances_dir);
                            if (Directory.Exists(targetDir)) Directory.Delete(targetDir);
                        }
                        catch (Exception err)
                        {
                            ShowToVoid(Languages.error_cleanup + err.Message);
                        }
                        break;
                    case "--installSuccess":
                        is_silent = true;
                        try
                        {
                            ShowToVoid(string.Format(Languages.package_installed_named, arguments[i + 1]));
                        }
                        catch
                        {
                            ShowToVoid(Languages.package_installed);
                        }
                        break;
                    case "--updateSuccess":
                        is_silent = true;
                        try
                        {
                            ShowToVoid(string.Format(Languages.package_config_updated_named, arguments[i + 1]));
                        }
                        catch
                        {
                            ShowToVoid(Languages.config_updated);
                        }
                        break;
                    case "--uninstallSuccess":
                        is_silent = true;
                        try
                        {
                            ShowToVoid(string.Format(Languages.package_uninstalled_named, arguments[i + 1]));
                        }
                        catch
                        {
                            ShowToVoid(Languages.package_uninstalled);
                        }
                        break;
                    case "--installPackage":
                        try
                        {
                            var filePath = arguments[i + 1];
                            if (!File.Exists(filePath)) throw new FileNotFoundException();
                            var fileName = Path.GetFileName(filePath);
                            var dialog = new CustomButtonDialog(DialogButtons.YesNo, string.Format(Languages.prompt_install_package, fileName));
                            dialog.ShowDialog();

                            var result = await dialog.Result;
                            if (result != DialogButton.Yes) break;
                            if (Path.GetExtension(filePath) != ".tcl") throw new FileFormatException();

                            try
                            {
                                var preview = AppServices.Packages.PreviewImport(filePath);
                                if (!preview.IsSuccess) throw new InvalidDataException(preview.Message);
                                var resolution = ImportConflictResolution.Cancel;
                                if (preview.Value.HasConflict)
                                {
                                    var conflict = MessageBox.Show("A profile with this ID already exists.\n\nYes: replace it\nNo: import as a copy\nCancel: stop",
                                        Languages.package_import, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                                    if (conflict == MessageBoxResult.Yes) resolution = ImportConflictResolution.Replace;
                                    else if (conflict == MessageBoxResult.No) resolution = ImportConflictResolution.ImportAsCopy;
                                    else break;
                                }
                                var import = AppServices.Packages.Import(filePath, resolution);
                                if (!import.IsSuccess) throw new InvalidDataException(import.Message);
                            }
                            catch (Exception exception)
                            {
                                ShowToVoid(string.Format(Languages.package_install_failed_named, fileName, exception));
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowToVoid(Format(Languages.package_load_failed, exception));
                        }
                        break;
                    case "--silent":
                        is_silent = true;
                        break;
                    case "--installer-part-welcome":
                        InstallerWin = new InstallerWelcomeWindow();
                        InstallerWin.Show();
                        LoadUI = false;
                        break;
                }
            }
        }

        private void ProcessAppURI(Uri uri)
        {
            try
            {
                string URIStr = uri.OriginalString.Substring(uri.OriginalString.IndexOf(":") + 1);
                string[] pairs = URIStr.Split('&');

                Dictionary<string, string> URIArgs = pairs
                    .Select(pair => pair.Split('='))
                    .ToDictionary(keyValue => Uri.UnescapeDataString(keyValue[0]), keyValue => Uri.UnescapeDataString(keyValue[1]));

                AppServices.Log.Info("application.uri_received", string.Join(",", URIArgs.Keys));
            }
            catch (Exception exception)
            {
                AppServices.Log.Warning("application.uri_rejected", exception.Message);
            }
        }

        private void RegisterURIScheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + URI_SCHEME))
                {
                    string applicationLocation = typeof(App).Assembly.Location;

                    key.SetValue("", "URL:" + FRIENDLY_NAME);
                    key.SetValue("URL Protocol", "");

                    using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon.SetValue("", applicationLocation + ",0");
                    }

                    using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format(Languages.error_registering_uri_schemes, e.Message));
            }
        }

        private static void RegisterDefaultEnvironment()
        {
            var cultureInfo = CultureInfo.CurrentUICulture;
            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", $"--lang={cultureInfo.Name}");
        }

        private void ShowUI()
        {
            MainWin = new MainWindow(is_silent);
            MainWin.Show();
        }

        private async void HandleHandoff(string[] arguments)
        {
            try
            {
                if (MainWin != null)
                {
                    if (MainWin.WindowState == WindowState.Minimized) MainWin.WindowState = WindowState.Normal;
                    MainWin.Show();
                    MainWin.Activate();
                    MainWin.Topmost = true;
                    MainWin.Topmost = false;
                }
                var uri = Get_AppURI(arguments);
                if (uri != null) ProcessAppURI(uri);
                else await ProcessAppArgs(arguments);
            }
            catch (Exception exception)
            {
                AppServices.Log.Error("single_instance.handoff_failed", exception);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _singleInstance?.Dispose();
            mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
