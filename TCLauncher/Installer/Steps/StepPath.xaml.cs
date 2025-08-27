using System.IO;
using System.Threading.Tasks;
using System.Windows;
using TCLauncher.Core;
using TCLauncher.Properties;
using static TCLauncher.Properties.Settings;

namespace TCLauncher.Installer.Steps
{
    public partial class StepPath
    {
        public StepPath()
        {
            InitializeComponent();

            UpdateUi();
        }

        private void UpdateUi()
        {
            AppDataPath.Text = Default.VirtualAppDataPath == "" ? IoUtils.FileSystem.RealAppDataPath : Default.VirtualAppDataPath;
        }

        private void ApplyPath(string path)
        {
            var oldParentPath = Default.VirtualAppDataPath;
            var newParentPath = path == "" ? IoUtils.FileSystem.RealAppDataPath : path;

            var oldPath = Path.Combine(oldParentPath, "TCL");
            var newPath = Path.Combine(newParentPath, "TCL");


            if (new DirectoryInfo(newParentPath).Name == "TCL")
            {
                var correctedNewParentPath = Path.GetDirectoryName(newParentPath);
                var result = MessageBox.Show("There is a mistake in the path you have entered. Should the path '" + newParentPath + "' be corrected to '" + correctedNewParentPath + "'?", "TCLauncher", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    newParentPath = correctedNewParentPath ?? IoUtils.FileSystem.RealAppDataPath;
                }
            }

            if (!IoUtils.FileSystem.HasFullAccess(newParentPath))
            {
                MessageBox.Show(Languages.invalid_path_error_message);
                return;
            }

            Default.VirtualAppDataPath = newParentPath;
            Default.Save();

            UpdateUi();

            if (Directory.Exists(oldPath) && oldPath != newPath)
            {
                var result2 = MessageBox.Show(Languages.path_saved_prompt, Languages.path_saved, MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result2 == MessageBoxResult.Yes)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Directory.Move(oldPath, newPath);
                            MessageBox.Show(Languages.files_migrated_success_message);
                        }
                        catch
                        {
                            MessageBox.Show(Languages.copy_error_message);
                        }
                    });
                }
            }

            App.HotReloadInstaller();
        }

        private void AppDataPathBrowseBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the folder where the application data should be stored.",
                ShowNewFolderButton = true,
                SelectedPath = AppDataPath.Text
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            ApplyPath(dialog.SelectedPath);
        }

        private void AppDataPathResetBtn_OnClick(object sender, RoutedEventArgs e)
        {
            ApplyPath("");
        }
    }
}