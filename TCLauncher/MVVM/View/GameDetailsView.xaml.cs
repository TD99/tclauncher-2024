using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.View
{
    public partial class GameDetailsView
    {
        private readonly Action<Instance> _changed;
        public GameDetailsPresentation Presentation { get; }

        public GameDetailsView(Instance instance, Action<Instance> changed)
        {
            InitializeComponent();
            _changed = changed;
            Presentation = new GameDetailsPresentation(instance);
            DataContext = Presentation;
        }

        private async void MainAction_OnClick(object sender, RoutedEventArgs e)
        {
            if (Presentation.Game.Is_Installed && !Presentation.Game.Upgradeable && Presentation.IsHealthy)
            {
                await CreateBackupAsync();
                return;
            }
            await InstallOrRepairAsync();
        }

        private async Task InstallOrRepairAsync()
        {
            var result = await AppServices.Operations.RunAsync(
                Presentation.Game.Is_Installed ? "Repairing " + Presentation.Game.DisplayName : "Installing " + Presentation.Game.DisplayName,
                true,
                (progress, cancellationToken) => AppServices.InstanceOperations.InstallOrUpdateAsync(Presentation.Game, progress, cancellationToken));
            if (!result.IsSuccess)
            {
                AppServices.Overlays.ShowToast("Operation failed", result.Message, ToastTone.Error);
                return;
            }
            Presentation.SetGame(result.Value);
            _changed?.Invoke(result.Value);
            AppServices.Overlays.ShowToast("Profile ready", result.Value.DisplayName);
        }

        private async Task CreateBackupAsync()
        {
            if (!(Presentation.Game is InstalledInstance installed)) return;
            var result = await AppServices.Operations.RunAsync("Backing up " + installed.DisplayName, true,
                (progress, cancellationToken) => Task.Run(() =>
                {
                    progress.Report(new OperationProgress { Stage = OperationStage.Snapshotting, Message = "Collecting profile data" });
                    cancellationToken.ThrowIfCancellationRequested();
                    return AppServices.Backups.Create(installed, "Manual " + DateTime.Now.ToString("yyyy-MM-dd HH-mm"), false, false);
                }, cancellationToken));
            if (!result.IsSuccess)
            {
                AppServices.Overlays.ShowToast("Backup failed", result.Message, ToastTone.Error);
                return;
            }
            Presentation.RefreshHealth();
            AppServices.Overlays.ShowToast("Backup complete",
                result.Value.Manifest.CreatedAtUtc.ToLocalTime().ToString("g") + " • " + FormatBytes(result.Value.SizeBytes));
        }

        private void Play_OnClick(object sender, RoutedEventArgs e)
        {
            Settings.Default.LastSelected = Presentation.Game.Guid;
            Settings.Default.Save();
            AppServices.Overlays.Close();
            App.MainWin.navigateToHome();
        }

        private void Manage_OnClick(object sender, RoutedEventArgs e)
        {
            ManageButton.ContextMenu.PlacementTarget = ManageButton;
            ManageButton.ContextMenu.IsOpen = true;
        }

        private async void Configure_OnClick(object sender, RoutedEventArgs e) =>
            await AppServices.Overlays.ShowSheetAsync("Configure profile", "Profile configuration is moving into this sheet in the workflow polish phase.");

        private void Clone_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(Presentation.Game is InstalledInstance installed)) return;
            var window = new ProfileCreatorWindow(installed) { Owner = App.MainWin };
            if (window.ShowDialog() == true) _changed?.Invoke(window.CreatedInstance);
        }

        private void Export_OnClick(object sender, RoutedEventArgs e)
        {
            if (Presentation.Game is InstalledInstance installed) new PackageExportWindow(installed) { Owner = App.MainWin }.ShowDialog();
        }

        private void OpenFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (Presentation.Game is InstalledInstance installed && Directory.Exists(installed.DataDir))
                Process.Start("explorer.exe", installed.DataDir);
        }

        private void ManageBackups_OnClick(object sender, RoutedEventArgs e)
        {
            if (Presentation.Game is InstalledInstance installed) new BackupWindow(installed) { Owner = App.MainWin }.ShowDialog();
        }

        private async void Repair_OnClick(object sender, RoutedEventArgs e) => await InstallOrRepairAsync();

        private async void Uninstall_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(Presentation.Game is InstalledInstance installed)) return;
            if (!await AppServices.Overlays.ConfirmAsync("Uninstall " + installed.DisplayName,
                    "Remove the installed profile from this PC? Backups are kept unless you explicitly remove them next.", "Uninstall", "Cancel")) return;
            var removeBackups = await AppServices.Overlays.ConfirmAsync("Keep backups?",
                "Choose Remove backups to delete this profile's backup archives too.", "Remove backups", "Keep backups");
            try
            {
                if (Directory.Exists(installed.InstallationDir)) Directory.Delete(installed.InstallationDir, true);
                if (removeBackups)
                {
                    var backupDirectory = Path.Combine(IoUtils.Tcl.RootPath, "Backups", installed.Guid.ToString());
                    if (Directory.Exists(backupDirectory)) Directory.Delete(backupDirectory, true);
                }
                AppServices.Overlays.Close();
                _changed?.Invoke(installed);
                AppServices.Overlays.ShowToast("Profile uninstalled", removeBackups ? "Profile and backups removed." : "Backups were preserved.");
            }
            catch (Exception exception)
            {
                AppServices.Log.Error("profile.uninstall_failed", exception);
                AppServices.Overlays.ShowToast("Uninstall failed", exception.Message, ToastTone.Error);
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024 * 1024) return (bytes / 1024d).ToString("0.#") + " KB";
            return (bytes / 1024d / 1024d).ToString("0.##") + " MB";
        }
    }

    public sealed class GameDetailsPresentation : System.ComponentModel.INotifyPropertyChanged
    {
        private Instance _game;
        private InstanceHealthReport _health;
        public Instance Game => _game;
        public string Summary => string.IsNullOrWhiteSpace(Game.Type) ? "A Minecraft profile for TCLauncher." : Game.Type;
        public string LoaderLabel => Game.GetEffectiveLoader().Type + (string.IsNullOrWhiteSpace(Game.GetEffectiveLoader().Version) ? "" : " " + Game.GetEffectiveLoader().Version);
        public string StateLabel => Game.Upgradeable ? "Update available" : Game.Is_Installed ? "Installed" : "Available";
        public Visibility InstalledVisibility => Game.Is_Installed ? Visibility.Visible : Visibility.Collapsed;
        public bool IsHealthy => _health == null || _health.OverallSeverity < HealthSeverity.Warning;
        public string MainActionLabel => !Game.Is_Installed ? "Install" : Game.Upgradeable ? "Update" : !IsHealthy ? "Repair" : "Back up now";
        public string Overview => "Minecraft " + Game.McVersion + " • Pack " + (Game.Version ?? "local") + "\n" +
                                  (Game.Servers?.Count > 0 ? Game.Servers.Count + " configured server(s)" : "No default server");
        public string HealthSummary => !Game.Is_Installed ? "Install this game to enable health checks and backups." :
            _health.OverallSeverity + " • " + FormatBytes(_health.StorageBytes) + " • " +
            (_health.LatestBackup == null ? "No backup yet" : "Last backup " + _health.LatestBackup.Manifest.CreatedAtUtc.ToLocalTime().ToString("g"));

        public GameDetailsPresentation(Instance game) => SetGame(game);
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void SetGame(Instance game)
        {
            _game = game;
            RefreshHealth();
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(string.Empty));
        }

        public void RefreshHealth()
        {
            _health = Game is InstalledInstance installed ? AppServices.Health.Inspect(installed) : null;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(HealthSummary)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MainActionLabel)));
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024 * 1024) return (bytes / 1024d).ToString("0.#") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / 1024d / 1024d).ToString("0.#") + " MB";
            return (bytes / 1024d / 1024d / 1024d).ToString("0.##") + " GB";
        }
    }
}
