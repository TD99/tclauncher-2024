using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using T_Craft_Game_Launcher.MVVM.Model;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for ServerListView.xaml
    /// </summary>
    public partial class ServerListView : UserControl
    {
        private Instance current { get; set; }

        public ServerListView()
        {
            InitializeComponent();
        }

        private void ServerItem_Clicked(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            Instance instance = (Instance)border.DataContext;

            itemFocusBanner.Source = new BitmapImage(new Uri(instance.ThumbnailURL, UriKind.RelativeOrAbsolute));
            itemFocusName.Text = instance.DisplayName;
            itemFocusVersion.Text = instance.Version;
            itemFocusPackage.Text = "ch.tcraft." + instance.Name;
            itemFocusType.Text = instance.Type;
            itemFocusMCVersion.Text = instance.McVersion;
            specialFocusBtn.Content = (instance.Is_Installed) ? "Deinstallieren" : "Installieren";

            current = instance;

            if (instance.WorkingDirDesc != null)
            {
                foreach (string key in instance.WorkingDirDesc.Keys)
                {
                    TextBlock keyTextBlock = new TextBlock
                    {
                        Text = key,
                        Foreground = Brushes.White,
                        FontSize = 25
                    };
                    itemFocusMCWorkingDirDesc.Children.Add(keyTextBlock);

                    foreach (string description in instance.WorkingDirDesc[key])
                    {
                        TextBlock descTextBlock = new TextBlock
                        {
                            Text = description,
                            Foreground = Brushes.White,
                            FontSize = 16
                        };
                        itemFocusMCWorkingDirDesc.Children.Add(descTextBlock);
                    }
                }
            }

            itemFocus.Visibility = Visibility.Visible;
        }

        private void closeFocusBtn_Click(object sender, RoutedEventArgs e)
        {
            itemFocus.Visibility = Visibility.Collapsed;
            itemFocusBanner.Source = new BitmapImage(new Uri("/Images/nothumb.png", UriKind.RelativeOrAbsolute));
            itemFocusName.Text = "";
            itemFocusVersion.Text = "";
            itemFocusPackage.Text = "";
            itemFocusType.Text = "";
            itemFocusMCVersion.Text = "";
            specialFocusBtn.Content = "Aktion";

            current = null;
        }

        private void forceUninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            uninstallInstance(current);
        }

        private void uninstallInstance(Instance instance)
        {
            try
            {
                string instanceFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", current.Guid.ToString());
                if (!Directory.Exists(instanceFolder))
                {
                    MessageBox.Show("Es wurden keine Daten gefunden!");
                    return;
                }

                MessageBoxResult result = MessageBox.Show("Willst du die Instanz wirklich löschen?", "Instanz löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Directory.Delete(instanceFolder, true);
                    MessageBox.Show("Instanz erfolgreich gelöscht!", "Instanz löschen", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                MessageBox.Show("Ein Fehler ist aufgetreten.");
            }

            instance.Is_Installed = false;
        }

        private void installInstance(Instance instance)
        {
            string instanceFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL", "Instances", instance.Guid.ToString());

            try
            {
                string configFile = System.IO.Path.Combine(instanceFolder, "config.json");
                
                Directory.CreateDirectory(instanceFolder);

                instance.Is_Installed = true;

                var json = JsonConvert.SerializeObject(instance);
                File.WriteAllText(configFile, json);
            }
            catch
            {
                MessageBox.Show("Ein Fehler ist aufgetreten (CONFIG_COULD_NOT_BE_SAVED).");
            }

            try
            {
                string installFolder = System.IO.Path.Combine(instanceFolder, "data");

                if (!URLExists(instance.WorkingDirZipURL))
                {
                    MessageBox.Show("Ein Fehler beim Holen der Abhängigkeiten ist aufgetreten.");
                    return;
                }

                Directory.CreateDirectory(installFolder);

                using (var client = new WebClient())
                {
                    client.DownloadFile(instance.WorkingDirZipURL, System.IO.Path.Combine(instanceFolder, "base.zip"));
                }
            }
            catch
            {
                MessageBox.Show("Ein Fehler beim Holen der Abhängigkeiten ist aufgetreten.");
            }
        }

        public bool URLExists(string url)
        {
            bool result = true;

            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Timeout = 1200;
            webRequest.Method = "HEAD";

            try
            {
                webRequest.GetResponse();
            }
            catch
            {
                result = false;
            }

            return result;
        }

        private void specialFocusBtn_Click(object sender, RoutedEventArgs e)
        {
            if (current.Is_Installed)
            {
                uninstallInstance(current);
            }
            else
            {
                installInstance(current);
            }

            specialFocusBtn.Content = (current.Is_Installed) ? "Deinstallieren" : "Installieren";
        }
    }
}
