using Microsoft.Win32;
using System.Windows;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.Windows
{
    public partial class InstanceHealthWindow
    {
        private readonly InstalledInstance _instance;

        public InstanceHealthWindow(InstalledInstance instance)
        {
            _instance = instance;
            InitializeComponent();
            ProfileName.Text = instance.DisplayName;
            Checks.ItemsSource = AppServices.Health.Inspect(instance).Checks;
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            new BackupWindow(_instance) { Owner = this }.ShowDialog();
            Checks.ItemsSource = AppServices.Health.Inspect(_instance).Checks;
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            var preview = AppServices.SupportBundles.Preview(_instance);
            var message = "The bundle will include:\n• " + string.Join("\n• ", preview.IncludedFiles) + "\n\nIt will exclude:\n• " + string.Join("\n• ", preview.ExcludedData);
            if (MessageBox.Show(this, message, "TCLauncher", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;
            var dialog = new SaveFileDialog { FileName = "TCLauncher-support.zip", DefaultExt = ".zip", Filter = "ZIP archive (*.zip)|*.zip" };
            if (dialog.ShowDialog(this) != true) return;
            var result = AppServices.SupportBundles.Export(dialog.FileName, _instance);
            if (!result.IsSuccess) MessageBox.Show(this, result.Message, "TCLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
