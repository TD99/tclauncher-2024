using CmlLib.Core;
using CmlLib.Core.Installer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;
using static TCLauncher.Core.MessageBoxUtils;

namespace TCLauncher.MVVM.View
{
    /// <summary>
    /// Interaction logic for ServerListView.xaml
    /// </summary>
    public partial class ServerListView
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
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(instance.ThumbnailURL);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                itemFocusBanner.Source = bitmap;
            }
            catch
            {
                // TODO: Add default image
            }
            itemFocusName.Text = instance.DisplayName;
            itemFocusPatch.Text = $"{instance.GetCurrentPatch()?.Name ?? "local"}@{instance.Version}";
            itemFocusPackage.Text = "ch.tcraft." + instance.Name;
            itemFocusType.Text = instance.Type;
            itemFocusMCVersion.Text = instance.McVersion;

            string minRam = (instance.MinimumRamMb / 1000 ?? 0).ToString();
            string maxRam = (instance.MaximumRamMb / 1000 ?? 0).ToString();
            string totalPhysicalMemory = SystemInfoUtils.GetTotalPhysicalMemoryInGb().ToString();
            itemFocusRamMin.Text = $"{minRam} GB / {totalPhysicalMemory} GB";
            itemFocusRamMax.Text = $"{maxRam} GB / {totalPhysicalMemory} GB";

            specialFocusBtn.Content = (instance.Is_Installed) ? "Deinstallieren" : "Installieren";
            openFolderBtn.Visibility = (instance.Is_Installed) ? Visibility.Visible : Visibility.Collapsed;
            reconfigDef.Visibility = (instance.Is_Installed && !instance.Is_LocalSource) ? Visibility.Visible : Visibility.Collapsed;
            editConfig.Visibility = (instance.Is_Installed) ? Visibility.Visible : Visibility.Collapsed;
            itemFocusMCWorkingDirDesc.Children.Clear();

            current = instance;

            // TODO: Fix bug where installed instances don't show up
            if (instance.WorkingDirDesc != null)
            {
                foreach (KeyValuePair<string, List<string>> entry in instance.WorkingDirDesc)
                {
                    AddTextBlock(itemFocusMCWorkingDirDesc, entry.Key, 20);

                    foreach (string description in entry.Value)
                    {
                        AddTextBlock(itemFocusMCWorkingDirDesc, description, 16);
                    }
                }
            }
            else
            {
                propsText.Visibility = Visibility.Collapsed;
            }

            itemFocus.Visibility = Visibility.Visible;
        }

        private void AddTextBlock(Panel panel, string text, int fontSize)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = fontSize
            };
            panel.Children.Add(textBlock);
        }

        private void closeFocusBtn_Click(object sender, RoutedEventArgs e)
        {
            itemFocus.Visibility = Visibility.Collapsed;
            itemFocusBanner.Source = new BitmapImage(new Uri("/Images/nothumb.png", UriKind.RelativeOrAbsolute));
            itemFocusName.Text = "";
            itemFocusPatch.Text = "";
            itemFocusPackage.Text = "";
            itemFocusType.Text = "";
            itemFocusMCVersion.Text = "";
            specialFocusBtn.Content = "Aktion";
            itemFocusMCWorkingDirDesc.Children.Clear();
            propsText.Visibility = Visibility.Visible;

            current = null;
        }

        private void forceUninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            uninstallInstance(current);
        }

        private void uninstallInstance(Instance instance, bool force = false)
        {
            try
            {
                var instanceFolder = Path.Combine(
                    IoUtils.Tcl.InstancesPath,
                    current.Guid.ToString());
                if (!Directory.Exists(instanceFolder))
                {
                    MessageBox.Show("Es wurden keine Daten gefunden!", "Instanz löschen");
                    return;
                }

                var result = MessageBox.Show("Willst du die Instanz wirklich löschen?", "Instanz löschen",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
                
                Directory.Delete(instanceFolder, true);

                var appPath = Process.GetCurrentProcess().MainModule?.FileName;
                Process.Start(appPath, $"--uninstallCheck {instanceFolder} --uninstallSuccess {instance.DisplayName}");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ein Fehler ist aufgetreten: {ex.Message}", "Instanz löschen");
            }
        }

        private async void installInstance(Instance instance)
        {
            string instanceFolder = System.IO.Path.Combine(IoUtils.Tcl.InstancesPath, instance.Guid.ToString());
            string installFolder = System.IO.Path.Combine(instanceFolder, "data");
            string payloadFile = System.IO.Path.Combine(instanceFolder, "base.zip");

            try
            {
                Directory.CreateDirectory(instanceFolder);
            }
            catch
            {
                MessageBox.Show("Fehler beim Erstellen der Instanz!");
            }

            try
            {
                if (!URLExists(instance.WorkingDirZipURL))
                {
                    MessageBox.Show("Ein Fehler beim Holen der Abhängigkeiten ist aufgetreten.");
                    return;
                }

                Directory.CreateDirectory(installFolder);

                if (instance.UsePatch != true)
                {
                    using (var client = new WebClient())
                    {
                        string fileSize = await GetFileSizeAsync(instance.WorkingDirZipURL);

                        ActionWindow action = new ActionWindow($"Installieren des Pakets 'ch.tcraft.{current.Name}'\nGrösse: {fileSize}\nInfo: Dies kann einige Zeit in Anspruch nehmen!");
                        action.Show();

                        client.DownloadProgressChanged += (sender, e) =>
                        {
                            action.percent = e.ProgressPercentage;
                        };

                        action.Closed += (sender, e) =>
                        {
                            client.CancelAsync();
                        };

                        try
                        {
                            await client.DownloadFileTaskAsync(new Uri(instance.WorkingDirZipURL), payloadFile);
                        }
                        catch
                        {
                            MessageBox.Show("Download abgebrochen!");
                            uninstallInstance(current, true);
                            action.Close();
                            return;
                        }

                        action.Close();
                    }

                    ActionWindow action2 = new ActionWindow($"Konfigurieren des Pakets 'ch.tcraft.{current.Name}'");
                    action2.Show();

                    await Task.Run(() => ZipFile.ExtractToDirectory(payloadFile, installFolder));

                    action2.Close();

                    ActionWindow action3 = new ActionWindow($"Aufräumen des Pakets 'ch.tcraft.{current.Name}'");
                    action3.Show();

                    try
                    {
                        await Task.Run(() =>
                        {
                            File.Delete(payloadFile);
                        });
                    }
                    catch { }

                    action3.Close();
                }
                else
                {
                    foreach (var patch in instance.Patches.OrderBy(p => p.ID))
                    {
                        using (var client = new WebClient())
                        {
                            string fileSize = await GetFileSizeAsync(patch.URL);

                            ActionWindow action = new ActionWindow($"Installieren des Pakets 'ch.tcraft.{current.Name}@{patch.Name}:{patch.ID}'\nGrösse: {fileSize}\nInfo: Dies kann einige Zeit in Anspruch nehmen!");
                            action.Show();

                            client.DownloadProgressChanged += (sender, e) =>
                            {
                                action.percent = e.ProgressPercentage;
                            };

                            action.Closed += (sender, e) =>
                            {
                                client.CancelAsync();
                            };

                            bool err = false;

                            try
                            {
                                await client.DownloadFileTaskAsync(new Uri(patch.URL), payloadFile);
                            }
                            catch
                            {
                                MessageBox.Show("Paketfehler, die Installation ist beschädigt!");
                                err = true;
                            }

                            if (!err)
                            {
                                action.Close();

                                ActionWindow action2 = new ActionWindow($"Konfigurieren des Pakets 'ch.tcraft.{current.Name}@{patch.Name}:{patch.ID}'");
                                action2.Show();

                                await Task.Run(() => ZipFile.ExtractToDirectory(payloadFile, installFolder));

                                action2.Close();

                                ActionWindow action3 = new ActionWindow($"Aufräumen des Pakets 'ch.tcraft.{current.Name}@{patch.Name}:{patch.ID}'");
                                action3.Show();

                                try
                                {
                                    await Task.Run(() =>
                                    {
                                        File.Delete(payloadFile);
                                    });
                                }
                                catch { }

                                action3.Close();
                            }
                        }
                    }
                }

                string appPath = Process.GetCurrentProcess().MainModule.FileName;

                var guid = Guid.NewGuid();

                if (instance.UseForge == true)
                {             
                    var forgeAdFile = Path.Combine(IoUtils.Tcl.UdataPath, "forge.adtcl");
                    File.WriteAllText(forgeAdFile, guid.ToString());
                }

                Process.Start(appPath, $"--installSuccess {instance.DisplayName}");
                Application.Current.Shutdown();
            }
            catch
            {
                MessageBox.Show("Ein Fehler beim Holen der Abhängigkeiten ist aufgetreten.");
            }

            reconfigure(instance);

            if (Properties.Settings.Default.FirstTime)
            {
                Properties.Settings.Default.FirstTime = false;
                Properties.Settings.Default.Save();
            }
        }

        public async Task<string> GetFileSizeAsync(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        long fileSize = response.Content.Headers.ContentLength ?? 0;
                        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                        int order = 0;
                        while (fileSize >= 1024 && order < sizes.Length - 1)
                        {
                            fileSize = fileSize / 1024;
                            order++;
                        }
                        return string.Format("{0:0.##} {1}", fileSize, sizes[order]);
                    }
                }
            }
            catch { }

            return "Unbekannt";
        }

        private bool URLExists(string url)
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

        private void reconfigure(Instance instance)
        {
            var installedInstance = new InstalledInstance(instance);
            try
            {
                var json = JsonConvert.SerializeObject(installedInstance);
                File.WriteAllText(installedInstance.ConfigFile, json);
            }
            catch
            {
                MessageBox.Show("Ein Fehler bei der Neukonfiguration ist aufgetreten.");
            }
        }

        private void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            string dataFolder = Path.Combine(IoUtils.Tcl.InstancesPath, current.Guid.ToString(), "data");
            Process.Start("explorer.exe", dataFolder);
        }

        private async void reconfigDef_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string instanceFolder = System.IO.Path.Combine(IoUtils.Tcl.InstancesPath, current.Guid.ToString());

                HttpClient _httpClient = new HttpClient();
                var response = await _httpClient.GetAsync(Properties.Settings.Default.DownloadMirror + "?guid=" + current.Guid.ToString());
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var instance = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content)[0];

                    reconfigure(instance);

                    string appPath = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appPath, $"--updateSuccess {instance.DisplayName}");
                    Application.Current.Shutdown();

                    return;
                }
                MessageBox.Show($"Die Neukonfiguration von '{current.Name}' ist fehlgeschlagen.");
            }
            catch
            {
                MessageBox.Show($"Die Neukonfiguration von '{current.Name}' ist fehlgeschlagen.");
            }
        }

        private void editConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string instanceFolder = Path.Combine(IoUtils.Tcl.InstancesPath, current.Guid.ToString());
                string configFile = Path.Combine(instanceFolder, @"config.json");

                this.Cursor = Cursors.Wait;
                EditorWindow editorWindow = new EditorWindow(configFile, false);
                editorWindow.Show();
                this.Cursor = null;
            }
            catch
            {
                MessageBox.Show($"Die Konfiguration von '{current.Name}' ist fehlgeschlagen.");
            }
        }

        private async void AddServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await AppUtils.LoadInstanceBuilder();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ein Fehler ist aufgetreten (Cache leeren kann helfen): " + ex.Message, "Paket erstellen");
            }
        }

        private void ImportServerBtn_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                AppUtils.LoadInstanceImporter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ein Fehler ist aufgetreten (Cache leeren kann helfen): " + ex.Message, "Paket importieren");
            }
        }

        private void CreateBlankBtn_OnClick(object sender, RoutedEventArgs e)
        {
            AppUtils.CreateTemplateInstance();
        }
    }
}
