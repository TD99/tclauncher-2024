using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
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
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
        }

        private void resetDataBtn_Click(object sender, RoutedEventArgs e)
        {
            string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");

            MessageBoxResult result = MessageBox.Show($"Möchtest du wirklich alle Daten löschen?", "Daten zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Directory.Delete(tclFolder, true);
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
            AppTools.HandleUpdates(true);
        }

        private void codeBtn_Click(object sender, RoutedEventArgs e)
        {
            EditorWindow editorWindow = new EditorWindow();
            editorWindow.Show();
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

        private void resetRuntime_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tclFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
                string runtimeFolder = Path.Combine(tclFolder, "Runtime");

                if (Directory.Exists(runtimeFolder))
                {
                    Directory.Delete(runtimeFolder, true);

                    string appPath = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appPath, $"--installSuccess runtime.core");
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("Die Runtime wurde nicht gefunden!");
                }
            }
            catch
            {
                MessageBox.Show("Während der Zurücksetzung der Runtime ist ein Fehler aufgetreten!");
            }
        }
    }
}
