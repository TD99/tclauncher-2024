using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using T_Craft_Game_Launcher.MVVM.Model;
using T_Craft_Game_Launcher.MVVM.ViewModel;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
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
                    MessageBox.Show("Ein Fehler ist aufgetreten!");
                    return;
                }

                string udataLoginFile = Path.Combine(udataFolder, "launcher_accounts.json");
                string targetLoginFile = Path.Combine(instanceDataFolder, "launcher_accounts.json");

                if (File.Exists(udataLoginFile))
                {
                    File.Copy(udataLoginFile, targetLoginFile, true);
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
    }
}
