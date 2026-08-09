using System.Linq;
using System.Windows;
using Microsoft.Win32;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.View
{
    public partial class HealthSheet
    {
        private readonly InstalledInstance _instance;
        public HealthSheet(InstalledInstance instance)
        {
            _instance = instance; InitializeComponent();
            Checks.ItemsSource = AppServices.Health.Inspect(instance).Checks;
            var preview = AppServices.SupportBundles.Preview(instance);
            BundlePreview.Text = "Support bundle preview\nIncludes: " + string.Join(", ", preview.IncludedFiles) + "\nExcludes: " + string.Join(", ", preview.ExcludedData);
        }
        private void Backups_Click(object sender, RoutedEventArgs e) => _ = AppServices.Overlays.ShowSheetAsync("Manage backups", new BackupManagerSheet(_instance), false);
        private async void Support_Click(object sender, RoutedEventArgs e)
        {
            if (!await AppServices.Overlays.ConfirmAsync("Export support bundle", BundlePreview.Text + "\n\nNothing is sent automatically.", "Choose destination", "Cancel")) return;
            var picker = new SaveFileDialog { FileName = "TCLauncher-support.zip", DefaultExt = ".zip", Filter = "ZIP archive (*.zip)|*.zip" };
            if (picker.ShowDialog() != true) return;
            var result = AppServices.SupportBundles.Export(picker.FileName, _instance);
            AppServices.Overlays.ShowToast(result.IsSuccess ? "Support bundle exported" : "Export failed", result.IsSuccess ? picker.FileName : result.Message, result.IsSuccess ? ToastTone.Success : ToastTone.Error);
        }
    }
}
