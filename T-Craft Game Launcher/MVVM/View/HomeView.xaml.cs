using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using T_Craft_Game_Launcher.MVVM.Model;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public ObservableCollection<Applet> Applets { get; set; }

        public HomeView()
        {
            InitializeComponent();
            loadAccount();
        }

        private void loadAccount()
        {
            try
            {
                string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
                string udataFolder = Path.Combine(tclFolder, "UData");
                string avatarsFolder = Path.Combine(udataFolder, "Avatars");
                string skinsFolder = Path.Combine(udataFolder, "Skins");
                string accountFile = Path.Combine(udataFolder, "launcher_accounts.json");

                string jsonText = File.ReadAllText(accountFile);
                JObject json = JObject.Parse(jsonText);

                string name = (string)json["accounts"][(string)json["activeAccountLocalId"]]["minecraftProfile"]["name"];
                string id = (string)json["accounts"][(string)json["activeAccountLocalId"]]["minecraftProfile"]["id"];

                string avatarSrc = $"https://mc-heads.net/avatar/{id}";
                string avatarCacheFile = Path.Combine(avatarsFolder, $"{id}.png");

                if (!Directory.Exists(avatarsFolder))
                {
                    Directory.CreateDirectory(avatarsFolder);
                }

                if (File.Exists(avatarCacheFile))
                {
                    avatarSrc = avatarCacheFile;
                }
                else
                {
                    var client = new WebClient();
                    client.DownloadFile(avatarSrc, avatarCacheFile);
                }

                string skinSrc = $"https://mc-heads.net/body/{id}";
                string skinCacheFile = Path.Combine(skinsFolder, $"{id}.png");

                if (!Directory.Exists(skinsFolder))
                {
                    Directory.CreateDirectory(skinsFolder);
                }

                if (File.Exists(skinCacheFile))
                {
                    skinSrc = skinCacheFile;
                }
                else
                {
                    var client = new WebClient();
                    client.DownloadFile(skinSrc, skinCacheFile);
                }

                BitmapImage avatarBitmap = new BitmapImage();
                avatarBitmap.BeginInit();
                avatarBitmap.UriSource = new Uri(avatarSrc);
                avatarBitmap.EndInit();

                BitmapImage skinBitmap = new BitmapImage();
                skinBitmap.BeginInit();
                skinBitmap.UriSource = new Uri(skinSrc);
                skinBitmap.EndInit();

                Dispatcher.Invoke(() =>
                {
                    textUserName.Text = name;
                    imageUserPicture.Source = avatarBitmap;
                    imageBodyPicture.Source = skinBitmap;
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    textUserName.Text = "Anonym";
                    imageUserPicture.Source = null;
                });
            }
        }

        private void discoverBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    (window as MainWindow).navigateToServer();
                }
            }
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            InstalledInstance selectedInstance = profileSelect.SelectedItem as InstalledInstance;

            Properties.Settings.Default.LastPlayed = selectedInstance.Guid;
            Properties.Settings.Default.Save();

            try
            {
                string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
                string instanceFolder = Path.Combine(tclFolder, "Instances");
                string runtimeFolder = Path.Combine(tclFolder, "Runtime");
                string udataFolder = Path.Combine(tclFolder, "UData");

                string selectedInstanceFolder = Path.Combine(instanceFolder, selectedInstance.Guid.ToString());
                string instanceDataFolder = Path.Combine(selectedInstanceFolder, "data");

                if (!Directory.Exists(selectedInstanceFolder))
                {
                    MessageBox.Show($"Die Instanz '{selectedInstance.Name}' konnte nicht gefunden werden!");
                    return;
                }

                string udataLoginFile = Path.Combine(udataFolder, "launcher_accounts.json");
                string targetLoginFile = Path.Combine(instanceDataFolder, "launcher_accounts.json");

                if (File.Exists(udataLoginFile))
                {
                    File.Copy(udataLoginFile, targetLoginFile, true);
                }

                string msaCredentialsFile = Path.Combine(udataFolder, "launcher_msa_credentials.bin");
                string targetMsaCredentialsFile = Path.Combine(instanceDataFolder, "launcher_msa_credentials.bin");

                if (File.Exists(msaCredentialsFile))
                {
                    File.Copy(msaCredentialsFile, targetMsaCredentialsFile, true);
                }

                string exeFile = Path.Combine(runtimeFolder, "Minecraft.exe");
                if (!File.Exists(exeFile))
                {
                    MessageBox.Show("Der MC Launcher existiert nicht!");
                    return;
                }

                Process launcher = new Process();
                launcher.StartInfo.FileName = exeFile;
                launcher.StartInfo.Arguments = $"--workDir=\"{instanceDataFolder}\"";
                launcher.EnableRaisingEvents = true;

                launcher.Exited += (sender1, e1) =>
                {
                    try
                    {
                        if (File.Exists(targetLoginFile) && new FileInfo(targetLoginFile).Length > 0)
                        {
                            File.Copy(targetLoginFile, udataLoginFile, true);
                        }
                        if (File.Exists(targetMsaCredentialsFile) && new FileInfo(targetMsaCredentialsFile).Length > 0)
                        {
                            File.Copy(targetMsaCredentialsFile, msaCredentialsFile, true);
                        }
                    }
                    catch { }
                };

                launcher.Start();
            }
            catch
            {
                MessageBox.Show("Ein Startfehler ist aufgetreten!");
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

            if (applet.ActionURL is null) return;
            
            SetAppletViewState(true);

            await Task.Delay(2000);

            webView.Source = new System.Uri(applet.ActionURL);
        }

        private void profileSelect_SelectionChanged(object sender, RoutedEventArgs e)
        {
            RefreshApplets();
        }

        private async void RefreshApplets()
        {
            mainApplets.ItemsSource = null;

            try
            {
                InstalledInstance selectedInstance = profileSelect.SelectedItem as InstalledInstance;
                if (selectedInstance is null) return;
                string appletsURL = selectedInstance.AppletURL;
                if (String.IsNullOrEmpty(appletsURL)) return;
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(appletsURL);
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Applets = new ObservableCollection<Applet>(JsonConvert.DeserializeObject<ObservableCollection<Applet>>(content).OrderByDescending(a => a.Weight));
                }
            }
            catch {}

            mainApplets.ItemsSource = Applets;
        }

        private void webViewBackButton_Click(object sender, RoutedEventArgs e)
        {
            SetAppletViewState(false);
        }
    }
}
