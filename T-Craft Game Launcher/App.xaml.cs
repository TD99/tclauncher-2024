using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Auth;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.MVVM.Windows;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.Sessions;
using T_Craft_Game_Launcher.Models;

namespace T_Craft_Game_Launcher
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
                T_Craft_Game_Launcher.Properties.Settings.Default.LastAccountUUID = value?.UUID ?? "";
                T_Craft_Game_Launcher.Properties.Settings.Default.Save();
            }
        }

        public static SimpleHttpServer DbgHttpServer;

        public static JELoginHandler LoginHandler;

        public static CMLauncher Launcher { get; set; }
        public static MainWindow MainWin { get; set; }

        public App()
        {
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            UriArgs = Get_AppURI(e.Args);
            AppArgs = string.Join(" ", e.Args);

            if (UriArgs == null)
            {
                ProcessAppArgs(e);
            }
            else
            {
                ProcessAppURI(UriArgs);
            }

            bool createdNew;
            mutex = new Mutex(true, FRIENDLY_NAME, out createdNew);
            if (!createdNew)
            {
                var multiInstances = T_Craft_Game_Launcher.Properties.Settings.Default.MultiInstances;
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
                if (jeGameAccount?.Profile?.UUID != T_Craft_Game_Launcher.Properties.Settings.Default.LastAccountUUID) continue;

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
                    string.Equals(uri.Scheme, URI_SCHEME, StringComparison.OrdinalIgnoreCase))
                {
                    return uri;
                }
            }

            return null;
        }

        private void ProcessAppArgs(StartupEventArgs e)
        {
            for (int i = 0; i != e.Args.Length; ++i)
            {
                switch (e.Args[i])
                {
                    case "--installSuccess":
                        is_silent = true;
                        try
                        {
                            MessageBoxUtils.ShowToVoid($"Das Paket '{e.Args[i + 1]}' wurde erfolgreich installiert.");
                        }
                        catch
                        {
                            MessageBoxUtils.ShowToVoid($"Das Paket wurde erfolgreich installiert.");
                        }
                        break;
                    case "--updateSuccess":
                        is_silent = true;
                        try
                        {
                            MessageBoxUtils.ShowToVoid($"Die Konfiguration des Pakets '{e.Args[i + 1]}' wurde erfolgreich aktualisiert.");
                        }
                        catch
                        {
                            MessageBoxUtils.ShowToVoid($"Die Konfiguration wurde erfolgreich aktualisiert.");
                        }
                        break;
                    case "--uninstallSuccess":
                        is_silent = true;
                        try
                        {
                            MessageBoxUtils.ShowToVoid($"Das Paket '{e.Args[i + 1]}' wurde erfolgreich deinstalliert.");
                        }
                        catch
                        {
                            MessageBoxUtils.ShowToVoid($"Das Paket wurde erfolgreich deinstalliert.");
                        }
                        break;
                    case "--silent":
                        is_silent = true;
                        break;
                    default:
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