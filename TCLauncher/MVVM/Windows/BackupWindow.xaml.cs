using System;
using System.Windows;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.Windows
{
    public partial class BackupWindow
    {
        private readonly InstalledInstance _instance;

        public BackupWindow(InstalledInstance instance)
        {
            _instance = instance;
            InitializeComponent();
            ProfileName.Text = instance.DisplayName;
            Refresh();
        }

        private void Refresh()
        {
            BackupList.ItemsSource = null;
            BackupList.ItemsSource = AppServices.Backups.List(_instance.Guid);
        }

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomInputDialog("Backup name") { Owner = this };
            dialog.Show();
            if (!await dialog.Result) return;
            var result = AppServices.Backups.Create(_instance, dialog.ResponseText, false, FullBackup.IsChecked == true);
            if (!result.IsSuccess) MessageBox.Show(this, result.Message, "TCLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
            Refresh();
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            var selected = BackupList.SelectedItem as BackupInfo;
            if (selected == null) return;
            var choice = MessageBox.Show(this, "Restore this backup?\n\nYes: overwrite this profile (with rollback)\nNo: restore as a copy\nCancel: stop", "TCLauncher", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (choice == MessageBoxResult.Cancel) return;
            if (choice == MessageBoxResult.No)
            {
                var copy = AppServices.Backups.RestoreAsCopy(_instance, selected.Path);
                MessageBox.Show(this, copy.IsSuccess ? "Backup restored as " + copy.Value.DisplayName + "." : copy.Message, "TCLauncher", MessageBoxButton.OK,
                    copy.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Error);
                return;
            }
            var result = AppServices.Backups.Restore(_instance, selected.Path);
            MessageBox.Show(this, result.IsSuccess ? "Backup restored." : result.Message, "TCLauncher", MessageBoxButton.OK,
                result.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
    }
}
