using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.View
{
    public partial class PackageExportSheet
    {
        private readonly InstalledInstance _instance;
        public PackageExportSheet(InstalledInstance instance)
        {
            _instance = instance;
            InitializeComponent();
            var loader = instance.GetEffectiveLoader();
            ProfileDetails.Text = instance.DisplayName + "\nMinecraft " + instance.McVersion + " • " + loader.Type +
                                  (string.IsNullOrWhiteSpace(loader.Version) ? "" : " " + loader.Version) + " • pack " + instance.Version;
        }
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var picker = new SaveFileDialog { FileName = _instance.Name + ".tcl", DefaultExt = ".tcl", Filter = Languages.tcl_package + " (*.tcl)|*.tcl" };
            if (picker.ShowDialog() == true) Destination.Text = picker.FileName;
        }
        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Destination.Text)) { Status.Text = "Choose a destination first."; return; }
            var result = await AppServices.Operations.RunAsync("Exporting " + _instance.DisplayName, true,
                (progress, token) => Task.Run(() => AppServices.Packages.Export(_instance, Destination.Text, IncludeSaves.IsChecked == true), token));
            if (!result.IsSuccess) { Status.Text = result.Message; return; }
            AppServices.Overlays.Close(true);
            AppServices.Overlays.ShowToast("Package exported", result.Value);
        }
    }
}
