using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using TCLauncher.Core;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;

namespace TCLauncher.MVVM.View
{
    public partial class SettingsView
    {
        public SettingsView()
        {
            InitializeComponent();
            assemblyVersion.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            string behaviourTagToSelect = Properties.Settings.Default.StartBehaviour.ToString();
            foreach (ComboBoxItem item in Behaviour.Items)
            {
                if ((string) item.Tag == behaviourTagToSelect)
                {
                    Behaviour.SelectedItem = item;
                    break;
                }
            }

            string multiInstancesTagToSelect = Properties.Settings.Default.MultiInstances.ToString();
            foreach (ComboBoxItem item in MultiInstances.Items)
            {
                if ((string)item.Tag == multiInstancesTagToSelect)
                {
                    MultiInstances.SelectedItem = item;
                    break;
                }
            }

            string sandboxLevelTagToSelect = Properties.Settings.Default.SandboxLevel.ToString();
            foreach (ComboBoxItem item in SandboxLevel.Items)
            {
                if ((string)item.Tag == sandboxLevelTagToSelect)
                {
                    SandboxLevel.SelectedItem = item;
                    break;
                }
            }

            hostBtn.Content = "Debug-Server " + (App.DbgHttpServer == null ? "starten" : "stoppen");

            AppDataPath.Text = Properties.Settings.Default.VirtualAppDataPath;

            frameworkVersion.Text = "WPF on " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }

        private void resetSettBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Möchtest du wirklich alle Einstellungen löschen?", "Einstellungen zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
        }

        private void resetDataBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Möchtest du wirklich alle TCL Daten löschen?", "Daten zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Directory.Delete(IoUtils.Tcl.RootPath, true);
                    Properties.Settings.Default.Reset();
                    Properties.Settings.Default.Save();

                    MessageBox.Show("Die Daten wurden erfolgreich zurückgesetzt.");
                    string appPath = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appPath);
                    Application.Current.Shutdown();
                }
                catch
                {
                    MessageBox.Show("Ein Fehler beim Löschen ist aufgetreten. Bitte starte den Launcher und seine Prozesse neu und stelle sicher, dass die Daten nicht verwendet werden.");
                }
            }
        }

        private void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.HandleUpdates(true);
        }

        private void codeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            EditorWindow editorWindow = new EditorWindow(IoUtils.Tcl.InstancesPath, true);
            editorWindow.Show();
            this.Cursor = null;
        }

        private void Behaviour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            string tag = (string)selectedItem.Tag;
            byte value;

            try
            {
                value = Byte.Parse(tag);
            } catch
            {
                MessageBox.Show("Das Startverhalten konnte nicht gesetzt werden.");
                return;
            }

            Properties.Settings.Default.StartBehaviour = value;
            Properties.Settings.Default.Save();
        }

        private async void HostBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (App.DbgHttpServer == null)
            {
                var dialog = new CustomInputDialog("Bitte gib die gewünschte Debug-Server-URL ein.")
                {
                    Owner = App.MainWin,
                    ResponseText = "http://localhost:4535/"
                };

                dialog.Show();

                if (!await dialog.Result) return;
                try
                {
                    App.DbgHttpServer = new SimpleHttpServer(SendResponse, dialog.ResponseText);
                    App.DbgHttpServer.Run();
                    hostBtn.Content = "Debug-Server stoppen";
                    Process.Start(dialog.ResponseText);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }

            }
            else
            {
                App.DbgHttpServer.Stop();
                App.DbgHttpServer = null;
                hostBtn.Content = "Debug-Server starten";
            }
        }

        private string SendResponse(HttpListenerRequest request)
        {
            return JsonConvert.SerializeObject(AppUtils.GetDebugObject().Result);
        }

        private void MultiInstances_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            string tag = (string)selectedItem.Tag;
            byte value;

            try
            {
                value = Byte.Parse(tag);
            }
            catch
            {
                MessageBox.Show("Die Multiinstanzen können nicht gesetzt werden.");
                return;
            }

            Properties.Settings.Default.MultiInstances = value;
            Properties.Settings.Default.Save();
        }

        //private async void FScreenBtn_OnClick(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new CustomButtonDialog(DialogButtons.OkCancel, "This will launch a mock instance. It will never load.")
        //    {
        //        Owner = App.MainWin,
        //        WindowStartupLocation = WindowStartupLocation.CenterOwner
        //    };
        //    dialog.ShowDialog();

        //    var result = await dialog.Result;
        //    if (result != DialogButton.Ok) return;

        //    var fsaWin = new FullScreenActionWindow
        //    {
        //        InstanceName = "T-Craft Server",
        //        InstanceVersion = "1.19.0",
        //        InstanceType = "Fabric",
        //        InstanceStatus = "Wird gestartet...",
        //        InstanceProgress = 50
        //    };
        //    fsaWin.Show();
        //}

        private void SandboxLevel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;
            string tag = (string)selectedItem.Tag;
            byte value;

            try
            {
                value = byte.Parse(tag);
            }
            catch
            {
                MessageBox.Show("Der Sandboxlevel kann nicht gesetzt werden.");
                return;
            }

            Properties.Settings.Default.SandboxLevel = value;
            Properties.Settings.Default.Save();
        }

        private void HotReloadBtn_OnClick(object sender, RoutedEventArgs e)
        {
            App.MainWin.loadingGrid.Visibility = Visibility.Visible;
            App.MainWin.mainBorder.Visibility = Visibility.Collapsed;
            App.MainWin.loadingAnim();
            App.MainWin.navigateToHome();
        }

        private void AppDataPathBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var oldPath = IoUtils.Tcl.RootPath;
            var newPath = AppDataPath.Text == "" ? IoUtils.FileSystem.RealAppDataPath : AppDataPath.Text;

            if (!IoUtils.FileSystem.HasFullAccess(newPath))
            {
                MessageBox.Show("Der angegebene Pfad kann nicht genutzt werden. Bitte wähle einen anderen Pfad.");
                return;
            }

            newPath = Path.Combine(newPath, "TCL");

            Properties.Settings.Default.VirtualAppDataPath = AppDataPath.Text;
            Properties.Settings.Default.Save();

            var result = MessageBox.Show("Der Pfad wurde erfolgreich gespeichert. Möchtest du die Launcher-Dateien migrieren und den Ordner überschreiben?", "Pfad gespeichert", MessageBoxButton.YesNo, MessageBoxImage.Information);
            
            if (result == MessageBoxResult.Yes)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Directory.Move(oldPath, newPath);
                        MessageBox.Show("Die Dateien wurden erfolgreich migriert.");
                    }
                    catch
                    {
                        MessageBox.Show("Ein Fehler beim Kopieren ist aufgetreten. Bitte stelle sicher, dass die Dateien nicht verwendet werden.");
                    }
                });
            }

            var appPath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(appPath);
            Application.Current.Shutdown();
        }
    }
}
