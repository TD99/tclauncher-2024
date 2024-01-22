using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.Sessions;
using CmlLib.Core.Installer.FabricMC;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;
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

        public static SimpleHttpServer DbgHttpServer;

        public static JELoginHandler LoginHandler;

        public static MinecraftPath MinecraftPath { get; set; }
        public static CMLauncher Launcher { get; set; }
        public static MLaunchOption LaunchOption { get; set; }
        public static MainWindow MainWin { get; set; }

        public App()
        {
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

            try
            {
                IoUtils.Tcl.CreateDirectries();
            }
            catch (Exception exception)
            {
                var result = MessageBox.Show("Ein Fehler beim Erstellen der Ordnerstruktur ist aufgetreten!" + exception.Message, "Initialisierungsfehler", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel) Environment.Exit(1);
            }

            Launcher = new CMLauncher(new MinecraftPath(IoUtils.Tcl.DefaultPath));

            LoginHandler = new JELoginHandlerBuilder()
                .WithAccountManager(Path.Combine(IoUtils.Tcl.UdataPath, "tcl_accounts.json"))
                .Build();
            ShowUI();
            TryAutoLogin();
        }

        private async void TryAutoLogin()
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
                catch (Exception e)
                {
                    // ignored
                }

                break;
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
                            if (!targetDir.StartsWith(instancesDir)) throw new DirectoryNotFoundException("Target directory is not in instances directory.");
                            if (Directory.Exists(targetDir)) Directory.Delete(targetDir);
                        }
                        catch (Exception err)
                        {
                            ShowToVoid($"Ein Fehler ist beim Bereinigen aufgetreten. Bitte lösche die Instanz manuell. Fehlermeldung:\n" + err.Message);
                        }
                        break;
                    case "--installSuccess":
                        is_silent = true;
                        try
                        {
                            ShowToVoid($"Das Paket '{e.Args[i + 1]}' wurde erfolgreich installiert.");
                        }
                        catch
                        {
                            ShowToVoid($"Das Paket wurde erfolgreich installiert.");
                        }
                        break;
                    case "--updateSuccess":
                        is_silent = true;
                        try
                        {
                            ShowToVoid($"Die Konfiguration des Pakets '{e.Args[i + 1]}' wurde erfolgreich aktualisiert.");
                        }
                        catch
                        {
                            ShowToVoid($"Die Konfiguration wurde erfolgreich aktualisiert.");
                        }
                        break;
                    case "--uninstallSuccess":
                        is_silent = true;
                        try
                        {
                            ShowToVoid($"Das Paket '{e.Args[i + 1]}' wurde erfolgreich deinstalliert.");
                        }
                        catch
                        {
                            ShowToVoid($"Das Paket wurde erfolgreich deinstalliert.");
                        }
                        break;
                    case "--installPackage":
                        try
                        {
                            var filePath = e.Args[i + 1];
                            if (!File.Exists(filePath)) throw new FileNotFoundException();
                            var fileName = Path.GetFileName(filePath);
                            var dialog = new CustomButtonDialog(DialogButtons.YesNo, $"Möchtest du das Paket '{fileName}' installieren?");
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
                                ShowToVoid($"Das Paket '{fileName}' konnte nicht installiert werden: {exception}");
                            }
                        }
                        catch (Exception exception)
                        {
                            ShowToVoid($"Das Paket konnte nicht geladen werden: {exception}");
                        }
                        break;
                    case "--silent":
                        is_silent = true;
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
                    // TODO: Handle App URI Args
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
                MessageBox.Show("An error occured while registering URI Schemes: " + e.Message);
            }
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