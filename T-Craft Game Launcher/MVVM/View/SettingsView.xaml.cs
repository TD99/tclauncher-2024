using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using T_Craft_Game_Launcher.Core;
using T_Craft_Game_Launcher.Models;
using T_Craft_Game_Launcher.MVVM.Windows;

namespace T_Craft_Game_Launcher.MVVM.View
{
    public partial class SettingsView
    {
        public SettingsView()
        {
            InitializeComponent();
            assemblyVersion.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            string tagToSelect = Properties.Settings.Default.StartBehaviour.ToString();
            foreach (ComboBoxItem item in Behaviour.Items)
            {
                if ((string) item.Tag == tagToSelect)
                {
                    Behaviour.SelectedItem = item;
                    break;
                }
            }
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

        //private void HostBtn_OnClick(object sender, RoutedEventArgs e)
        //{
        //    var server = new SimpleHttpServer(SendResponse, "http://localhost:4535/");
        //    Process.Start("http://localhost:4535/");
        //    server.Run();
        //}

        //public string SendResponse(HttpListenerRequest request)
        //{
        //    return "Hello World!";
        //}
    }
}
