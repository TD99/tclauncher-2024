using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using System.Windows.Documents;
using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string URI_SCHEME = "tcl";
        private const string FRIENDLY_NAME = "TCLauncher";

        private bool is_silent = false;


        public App()
        {
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Uri uri = Get_AppURI(e.Args);

            if (uri == null)
            {
                ProcessAppArgs(e);
            }
            else
            {
                ProcessAppURI(uri);
            }

            RegisterURIScheme();

            ShowUI();
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
                            ModalTools.ShowToVoid($"Das Paket '{e.Args[i + 1]}' wurde erfolgreich installiert.");
                        }
                        catch
                        {
                            ModalTools.ShowToVoid($"Das Paket wurde erfolgreich installiert.");
                        }
                        break;
                    case "--updateSuccess":
                        is_silent = true;
                        try
                        {
                            ModalTools.ShowToVoid($"Die Konfiguration des Pakets '{e.Args[i + 1]}' wurde erfolgreich aktualisiert.");
                        }
                        catch
                        {
                            ModalTools.ShowToVoid($"Die Konfiguration wurde erfolgreich aktualisiert.");
                        }
                        break;
                    case "--uninstallSuccess":
                        is_silent = true;
                        try
                        {
                            ModalTools.ShowToVoid($"Das Paket '{e.Args[i + 1]}' wurde erfolgreich deinstalliert.");
                        }
                        catch
                        {
                            ModalTools.ShowToVoid($"Das Paket wurde erfolgreich deinstalliert.");
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
                    MessageBox.Show(arg + "//" + URIArgs[arg]);

                    switch (arg)
                    {
                        case "message":
                            MessageBox.Show(URIArgs[arg], "Nachricht");
                            break;
                        default:
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
                MessageBox.Show("An Error occured while registering URI Schemes: " + e.Message);
            }
        }
        private void ShowUI()
        {
            MainWindow mainWindow = new MainWindow(is_silent);
            mainWindow.Show();
        }
    }
}