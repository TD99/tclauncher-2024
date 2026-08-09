using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.Windows
{
    public partial class PackageExportWindow
    {
        private readonly InstalledInstance _instance;

        public PackageExportWindow(InstalledInstance instance)
        {
            _instance = instance;
            InitializeComponent();
            var loader = instance.GetEffectiveLoader();
            ProfileName.Text = instance.DisplayName;
            ProfileDetails.Text = "Minecraft " + instance.McVersion + " • " + loader.Type +
                                  (string.IsNullOrWhiteSpace(loader.Version) ? "" : " " + loader.Version) +
                                  " • pack " + instance.Version;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { FileName = _instance.Name + ".tcl", DefaultExt = ".tcl", Filter = Languages.tcl_package + " (*.tcl)|*.tcl" };
            if (dialog.ShowDialog(this) == true) Destination.Text = dialog.FileName;
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Destination.Text))
            {
                Status.Text = "Choose a destination for the package.";
                return;
            }
            IsEnabled = false;
            Status.Text = "Creating and verifying package…";
            var result = await Task.Run(() => AppServices.Packages.Export(_instance, Destination.Text, IncludeSaves.IsChecked == true));
            IsEnabled = true;
            if (!result.IsSuccess)
            {
                Status.Text = result.Message;
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
