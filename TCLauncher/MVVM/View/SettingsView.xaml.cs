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

            hostBtn.Content = "Debug-Server " + (App.DbgHttpServer == null ? "starten" : "stoppen");
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
            string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
            string instanceFolder = Path.Combine(tclFolder, "Instances");

            this.Cursor = Cursors.Wait;
            EditorWindow editorWindow = new EditorWindow(instanceFolder, true);
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
    }
}
