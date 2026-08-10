using System;
using System.Threading.Tasks;
using System.Windows;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.View
{
    public partial class BackupManagerSheet
    {
        private readonly InstalledInstance _instance;
        private readonly Action<InstalledInstance> _changed;

        public BackupManagerSheet(InstalledInstance instance, Action<InstalledInstance> changed = null)
        {
            _instance = instance;
            _changed = changed;
            InitializeComponent();
            Refresh();
        }

        private void Refresh()
        {
            BackupList.ItemsSource = null;
            BackupList.ItemsSource = AppServices.Backups.List(_instance.Guid);
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            var result = await AppServices.Operations.RunAsync("Backing up " + _instance.DisplayName, true,
                (progress, token) =>
                    Task.Run(
                        () => AppServices.Backups.Create(_instance, BackupName.Text, false,
                            FullBackup.IsChecked == true), token));
            if (!result.IsSuccess)
            {
                AppServices.Overlays.ShowToast("Backup failed", result.Message, ToastTone.Error);
                return;
            }

            BackupName.Clear();
            Refresh();
            AppServices.Overlays.ShowToast("Backup complete",
                result.Value.Manifest.CreatedAtUtc.ToLocalTime().ToString("g"));
        }

        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (!(BackupList.SelectedItem is BackupInfo backup)) return;
            if (!await AppServices.Overlays.ConfirmAsync("Restore backup",
                    "Current profile data will be replaced after a rollback snapshot is created.", "Restore",
                    "Cancel")) return;
            var result = await AppServices.Operations.RunAsync<object>("Restoring " + _instance.DisplayName, true,
                (progress, token) => Task.Run(() =>
                {
                    var restored = AppServices.Backups.Restore(_instance, backup.Path);
                    return restored.IsSuccess
                        ? OperationResult<object>.Success(null, restored.OperationId)
                        : OperationResult<object>.Failure(restored.ErrorCode, restored.Message, restored.Exception,
                            restored.OperationId);
                }, token));
            AppServices.Overlays.ShowToast(result.IsSuccess ? "Backup restored" : "Restore failed",
                result.IsSuccess ? "Rollback data was preserved." : result.Message,
                result.IsSuccess ? ToastTone.Success : ToastTone.Error);
            if (result.IsSuccess) _changed?.Invoke(_instance);
        }

        private async void RestoreCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!(BackupList.SelectedItem is BackupInfo backup)) return;
            var result = await AppServices.Operations.RunAsync("Restoring as a copy", true,
                (progress, token) => Task.Run(() => AppServices.Backups.RestoreAsCopy(_instance, backup.Path), token));
            AppServices.Overlays.ShowToast(result.IsSuccess ? "Copy restored" : "Restore failed",
                result.IsSuccess ? result.Value.DisplayName : result.Message,
                result.IsSuccess ? ToastTone.Success : ToastTone.Error);
            if (result.IsSuccess) _changed?.Invoke(result.Value);
        }
    }
}