using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.View
{
    public partial class SettingsView
    {
        private readonly DispatcherTimer _textSaveTimer;
        private bool _initializing = true;
        private StackPanel[] _sections;
        private Border[] _cards;

        public SettingsView()
        {
            InitializeComponent();
            _sections = new[]
                { GeneralSection, MinecraftSection, StorageSection, DownloadsSection, PrivacySection, AboutSection };
            _cards = new[]
            {
                StartupCard, LanguageCard, AppearanceCard, JavaCard, MultiCard, SandboxCard, PathCard, MirrorCard,
                UpdateCard, DiagnosticsCard, ResetCard, AboutCard
            };
            _textSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(550) };
            _textSaveTimer.Tick += (sender, args) =>
            {
                _textSaveTimer.Stop();
                SaveTextSettings();
            };

            assemblyVersion.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version;
            frameworkVersion.Text = RuntimeInformation.FrameworkDescription;
            CopyrightCompilationYear.Text = "Copyright © T-Craft " + AppUtils.GetCompilationDate().Year;
            JavaStatus.Text = Environment.Is64BitOperatingSystem
                ? "64-bit Windows detected"
                : "32-bit Windows detected";
            SelectByTag(Behaviour, Settings.Default.StartBehaviour.ToString());
            SelectByTag(MultiInstances, Settings.Default.MultiInstances.ToString());
            SelectByTag(SandboxLevel, Settings.Default.SandboxLevel.ToString());
            SelectByTag(LanguageSelector, Settings.Default.Language);
            AppDataPath.Text = Settings.Default.VirtualAppDataPath;
            DownloadMirror.Text = Settings.Default.DownloadMirror;
            CheckBoxUsePixelFontEverywhere.IsChecked = Settings.Default.UsePixelFontEverywhere;
            SectionList.SelectedIndex = 0;
            _initializing = false;
        }

        private static void SelectByTag(ComboBox comboBox, string tag) => comboBox.SelectedItem =
            comboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => Equals(item.Tag, tag));

        private void SetSaved(string message = "Saved", bool restart = false)
        {
            SaveStatus.Text = restart ? "Restart required • " + message : message;
            SaveStatus.Foreground = restart
                ? Brushes.Goldenrod
                : new SolidColorBrush(Color.FromRgb(131, 213, 187));
        }

        private void SetError(string message)
        {
            SaveStatus.Text = "Error • " + message;
            SaveStatus.Foreground =
                new SolidColorBrush(Color.FromRgb(255, 154, 154));
        }

        private void SectionList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(SectionList.SelectedItem is ListBoxItem selected)) return;
            SearchSettings.Text = string.Empty;
            foreach (var section in _sections)
                section.Visibility = Equals(section.Tag, selected.Tag) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchSettings_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_sections == null) return;
            var query = SearchSettings.Text.Trim();
            if (query.Length == 0)
            {
                if (SectionList.SelectedItem is ListBoxItem selected)
                    foreach (var section in _sections)
                        section.Visibility = Equals(section.Tag, selected.Tag)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                foreach (var card in _cards) card.Visibility = Visibility.Visible;
                return;
            }

            foreach (var section in _sections) section.Visibility = Visibility.Visible;
            foreach (var card in _cards)
                card.Visibility =
                    ((card.Tag as string) ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
        }

        private void Behaviour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrySelectedByte(sender, out var value))
            {
                Settings.Default.StartBehaviour = value;
                Save();
            }
        }

        private void MultiInstances_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrySelectedByte(sender, out var value))
            {
                Settings.Default.MultiInstances = value;
                Save();
            }
        }

        private void SandboxLevel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrySelectedByte(sender, out var value))
            {
                Settings.Default.SandboxLevel = value;
                Save();
            }
        }

        private bool TrySelectedByte(object sender, out byte value)
        {
            value = 0;
            if (_initializing || !((sender as ComboBox)?.SelectedItem is ComboBoxItem item)) return false;
            if (byte.TryParse(item.Tag as string, out value)) return true;
            SetError("Invalid selection.");
            return false;
        }

        private void Save()
        {
            Settings.Default.Save();
            SetSaved();
        }

        private void LanguageSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || !((sender as ComboBox)?.SelectedItem is ComboBoxItem item)) return;
            Settings.Default.Language = (string)item.Tag;
            Settings.Default.Save();
            SetSaved("Language saved", true);
        }

        private void PixelFont_OnChanged(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;
            Settings.Default.UsePixelFontEverywhere = CheckBoxUsePixelFontEverywhere.IsChecked == true;
            Settings.Default.Save();
            SetSaved("Appearance saved", true);
        }

        private void DebouncedTextSetting_OnChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing) return;
            SaveStatus.Text = "Saving…";
            _textSaveTimer.Stop();
            _textSaveTimer.Start();
        }

        private void SaveTextSettings()
        {
            if (!Uri.TryCreate(DownloadMirror.Text.Trim(), UriKind.Absolute, out var mirror) ||
                mirror.Scheme != Uri.UriSchemeHttps)
            {
                SetError("Download service must be a secure HTTPS address.");
                return;
            }

            var path = AppDataPath.Text.Trim();
            if (path.Length > 0 && !IoUtils.FileSystem.HasFullAccess(path))
            {
                SetError("The storage path is not writable.");
                return;
            }

            var storageChanged = !string.Equals(Settings.Default.VirtualAppDataPath ?? string.Empty, path,
                StringComparison.OrdinalIgnoreCase);
            Settings.Default.DownloadMirror = mirror.ToString();
            Settings.Default.VirtualAppDataPath = path;
            Settings.Default.Save();
            SetSaved(storageChanged ? "Storage path saved" : "Settings saved", storageChanged);
        }

        private async void MigrateStorage_OnClick(object sender, RoutedEventArgs e)
        {
            var basePath = AppDataPath.Text.Trim();
            if (basePath.Length == 0) basePath = IoUtils.FileSystem.RealAppDataPath;
            if (!IoUtils.FileSystem.HasFullAccess(basePath))
            {
                SetError("The destination is not writable.");
                return;
            }

            var source = Path.GetFullPath(IoUtils.Tcl.RootPath);
            var destination = Path.GetFullPath(Path.Combine(basePath, "TCL"));
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            {
                SetSaved("Data is already here");
                return;
            }

            if (!await AppServices.Overlays.ConfirmAsync("Move launcher data",
                    "Move all TCLauncher data to:\n" + destination + "\n\nClose games using these files first.",
                    "Move data", "Cancel")) return;
            try
            {
                await Task.Run(() => Directory.Move(source, destination));
                Settings.Default.VirtualAppDataPath = AppDataPath.Text.Trim();
                Settings.Default.Save();
                SetSaved("Data moved", true);
                AppServices.Overlays.ShowToast("Storage moved", "Restart TCLauncher when convenient.");
            }
            catch (Exception exception)
            {
                AppServices.Log.Error("settings.storage_move_failed", exception);
                SetError(exception.Message);
            }
        }

        private async void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            updateBtn.IsEnabled = false;
            UpdateStatus.Text = "Checking…";
            try
            {
                var check = await AppServices.Updates.CheckAsync(Assembly.GetExecutingAssembly().GetName().Version,
                    CancellationToken.None);
                if (!check.IsSuccess || !check.Value.IsUpdateAvailable)
                {
                    UpdateStatus.Text = check.IsSuccess ? "TCLauncher is up to date." : check.Message;
                    return;
                }

                if (!check.Value.IsCompatible)
                {
                    UpdateStatus.Text = check.Value.CompatibilityMessage;
                    return;
                }

                var manifest = check.Value.Manifest;
                if (!await AppServices.Overlays.ConfirmAsync("Update available",
                        manifest.ReleaseNotes + "\n\nInstall " + manifest.Version + "?", "Download update",
                        "Later")) return;
                var staging = Path.Combine(IoUtils.Tcl.RootPath, "Updates", manifest.Version);
                var download =
                    await AppServices.Updates.DownloadAndVerifyAsync(manifest, staging, CancellationToken.None);
                if (!download.IsSuccess)
                {
                    UpdateStatus.Text = download.Message;
                    return;
                }

                Process.Start(new ProcessStartInfo("msiexec.exe", "/i \"" + download.Value + "\"")
                    { UseShellExecute = true });
                UpdateStatus.Text = "Verified installer opened.";
            }
            catch (Exception exception)
            {
                AppServices.Log.Error("settings.update_failed", exception);
                UpdateStatus.Text = exception.Message;
            }
            finally
            {
                updateBtn.IsEnabled = true;
            }
        }

        private async void SupportBundle_Click(object sender, RoutedEventArgs e)
        {
            var preview = AppServices.SupportBundles.Preview(null);
            var text = "Includes:\n• " + string.Join("\n• ", preview.IncludedFiles) + "\n\nExcludes:\n• " +
                       string.Join("\n• ", preview.ExcludedData) + "\n\nNothing is sent automatically.";
            if (!await AppServices.Overlays.ConfirmAsync("Export support bundle", text, "Choose destination", "Cancel"))
                return;
            var picker = new SaveFileDialog
            {
                Filter = "ZIP archive (*.zip)|*.zip",
                FileName = "TCLauncher-support-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".zip"
            };
            if (picker.ShowDialog() != true) return;
            var result = AppServices.SupportBundles.Export(picker.FileName, null);
            AppServices.Overlays.ShowToast(result.IsSuccess ? "Support bundle exported" : "Export failed",
                result.IsSuccess ? picker.FileName : result.Message,
                result.IsSuccess ? ToastTone.Success : ToastTone.Error);
        }

        private void OpenLogs_OnClick(object sender, RoutedEventArgs e) =>
            Process.Start("explorer.exe", AppServices.Log.LogDirectory);

        private async void resetSettBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await AppServices.Overlays.ConfirmAsync("Reset settings",
                    "Restore all launcher settings to their defaults?", "Reset settings", "Cancel")) return;
            Settings.Default.Reset();
            Settings.Default.Save();
            SetSaved("Defaults restored", true);
        }

        private async void resetDataBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await AppServices.Overlays.ConfirmAsync("Reset all TCLauncher data",
                    "This permanently removes local profiles, account storage, backups, settings, and logs.",
                    "Delete all data", "Cancel")) return;
            try
            {
                Directory.Delete(Path.GetFullPath(IoUtils.Tcl.RootPath), true);
                Settings.Default.Reset();
                Settings.Default.Save();
                AppServices.Overlays.ShowToast("Data removed", "Run setup again to continue.");
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
        }

        private async void ReSetupBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (!await AppServices.Overlays.ConfirmAsync("Run setup again",
                    "Close TCLauncher and open the setup experience?", "Run setup", "Cancel")) return;
            Process.Start(Process.GetCurrentProcess().MainModule.FileName, "--installer-part-welcome");
            Application.Current.Shutdown();
        }
    }
}