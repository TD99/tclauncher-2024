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
using Microsoft.Win32;
using TCLauncher.Core;
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
        private static Mutex mutex;
        
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
        public static CMLauncher Launcher { get; set; }
        public static MLaunchOption LaunchOption { get; set; }
        public static MainWindow MainWin { get; set; }
        public static InstallerWelcomeWindow InstallerWin { get; set; }

        public App()
        {
            SetLanguage(Settings.Default.Language);
            Startup += App_Startup;
        }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            UriArgs = Get_AppURI(e.Args);
            AppArgs = Join(" ", e.Args);

            if (UriArgs == null)
            {
                await ProcessAppArgs(e);
            }
            else
            {
                ProcessAppURI(UriArgs);
            }

            bool createdNew;
            mutex = new Mutex(true, FRIENDLY_NAME, out createdNew);
            if (!createdNew)
            {
                var multiInstances = Settings.Default.MultiInstances;
                switch (multiInstances)
                {
                    case 0:
                        kill_old = true;
                        is_silent = true;
                        break;
                    case 1:
                        Environment.Exit(0);
                        break;
                }
            }

            RegisterURIScheme();

            RegisterDefaultEnvironment();

            // check if forge ad
            try
            {
                var adUrl = "https://adfoc.us/serve/sitelinks/?id=271228&url=https://tcraft.link/tclauncher/api/plugins/start-tcl?forgeAdValidationKey=";

                var forgeAdFile = Path.Combine(IoUtils.Tcl.UdataPath, "forge.adtcl");
                if (File.Exists(forgeAdFile))
                {
                    var guid = Guid.Parse(File.ReadAllText(forgeAdFile));
                    if (guid != Guid.Empty)
                    {
                        // get string "test" from Properties/Languages.resx

                        ShowToVoidLegacy(Languages.tclauncher_supports_forge_skip_ad);
                        Thread.Sleep(1000);
                        Process.Start(adUrl + guid);

                        var trials = 100;
                        for (var j = 0; j < trials; j++)
                        {
                            if (File.Exists(forgeAdFile))
                            {
                                var content = File.ReadAllText(forgeAdFile);
                                if (Guid.TryParse(content, out var guidResult) && guidResult == Guid.Empty)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }

                            Thread.Sleep(600);
                        }

                        if (File.Exists(forgeAdFile))
                        {
                            File.Delete(forgeAdFile);
                        }

                        if (trials > 0)
                        {
                            Environment.Exit(0);
                        }
                    }
                }
            } catch { }

            try
            {
                IoUtils.Tcl.CreateDirectries();
            }
            catch (Exception exception)
            {
                var result = MessageBox.Show(Languages.error_creating_folder_structure + exception.Message, Languages.initialization_error, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel) Environment.Exit(1);
            }

            Launcher = new CMLauncher(new MinecraftPath(IoUtils.Tcl.DefaultPath));

            LoginHandler = new JELoginHandlerBuilder()
                .WithAccountManager(Path.Combine(IoUtils.Tcl.UdataPath, "tcl_accounts.json"))
                .Build();

            if (LoadUI) ShowUI();
            TryAutoLogin();

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
                var accounts = LoginHandler.AccountManager.GetAccounts();
                foreach (var account in accounts)
                {
                    if (!(account is JEGameAccount jeGameAccount)) continue;
                    if (jeGameAccount?.Profile?.UUID != Settings.Default.LastAccountUUID) continue;

                    try
                    {
                        var session = await LoginHandler.Authenticate(jeGameAccount);

                        MainWin.SetDisplayAccount(session?.Username);
                        Session = session;
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

        private async Task ProcessAppArgs(StartupEventArgs e)
        {
            for (var i = 0; i != e.Args.Length; ++i)
            {
                switch (e.Args[i])
                {
                    case "--uninstallCheck":
                        try
                        {
                            var targetDir = e.Args[i + 1] ?? throw new ArgumentNullException();
                            var instancesDir = IoUtils.Tcl.InstancesPath;
                            if (!targetDir.StartsWith(instancesDir)) throw new DirectoryNotFoundException(Languages.target_dir_not_instances_dir);
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
                            ShowToVoid(string.Format(Languages.package_installed_named, e.Args[i + 1]));
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
                            ShowToVoid(string.Format(Languages.package_config_updated_named, e.Args[i + 1]));
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
                            ShowToVoid(string.Format(Languages.package_uninstalled_named, e.Args[i + 1]));
                        }
                        catch
                        {
                            ShowToVoid(Languages.package_uninstalled);
                        }
                        break;
                    case "--installPackage":
                        try
                        {
                            var filePath = e.Args[i + 1];
                            if (!File.Exists(filePath)) throw new FileNotFoundException();
                            var fileName = Path.GetFileName(filePath);
                            var dialog = new CustomButtonDialog(DialogButtons.YesNo, string.Format(Languages.prompt_install_package, fileName));
                            dialog.ShowDialog();

                            var result = await dialog.Result;
                            if (result != DialogButton.Yes) break;
                            if (Path.GetExtension(filePath) != ".tcl") throw new FileFormatException();

                            try
                            {
                                AppUtils.ImportInstance(filePath);
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

                foreach (string arg in URIArgs.Keys)
                {
                    switch (arg)
                    {
                        case "forgeAdValidationKey":
                            var forgeAdFile = Path.Combine(IoUtils.Tcl.UdataPath, "forge.adtcl");
                            if (File.Exists(forgeAdFile))
                            {
                                var guid = Guid.Parse(File.ReadAllText(forgeAdFile));
                                if (guid != Guid.Empty && guid == Guid.Parse(URIArgs[arg]))
                                {
                                    File.Delete(forgeAdFile);
                                }
                            }
                            break;
                    }
                }
            }
            catch {}
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
            if (kill_old)
            {
                MainWin.ContentRendered += KillOldProcesses;
                MainWin.Opacity = 0;
            }
            MainWin.Show();
        }

        private void KillOldProcesses(object sender, EventArgs e)
        {
            Process current = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(current.ProcessName))
            {
                if (process.Id != current.Id)
                {
                    process.Kill();
                    break;
                }
            }
            MainWin.Opacity = 1;
        }
    }
}