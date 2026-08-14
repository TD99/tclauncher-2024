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
using System.Windows.Forms;
using System.Windows.Threading;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.MVVM.Animations;
using TCLauncher.Properties;
using Application = System.Windows.Application;
using ComboBox = System.Windows.Controls.ComboBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace TCLauncher.MVVM.View
{
    public partial class SettingsView
    {
        private readonly DispatcherTimer _textSaveTimer;
        private bool _initializing = true;
        private StackPanel[] _sections;
        private Border[] _cards;
        private StackPanel _activeSection;
        private StackPanel _pendingSection;
        private bool _sectionTransitionQueued;
        private bool _sectionTransitioning;

        public SettingsView()
        {
            InitializeComponent();
            _sections = new[]
                { GeneralSection, MinecraftSection, StorageSection, ServicesSection, DiagnosticsSection, AboutSection };
            _activeSection = GeneralSection;
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
            JavaStatus.Text += GetJavaVersion();
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

        private void AboutMascotHost_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            AboutMascot.Height = e.NewSize.Height * 0.8;
        }

        private static void SelectByTag(ComboBox comboBox, string tag) => comboBox.SelectedItem =
            comboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => Equals(item.Tag, tag));

        private static string GetJavaVersion()
        {
            try
            {
                using (var process = Process.Start(new ProcessStartInfo
                       {
                           FileName = "java",
                           Arguments = "-version",
                           UseShellExecute = false,
                           RedirectStandardError = true,
                           CreateNoWindow = true
                       }))
                {
                    var version = process.StandardError.ReadToEnd().Trim();
                    process.WaitForExit(2000);
                    return version.Length == 0 ? "Java was not found on PATH." : version;
                }
            }
            catch
            {
                return "Java was not found on PATH.";
            }
        }

        private void SetSaved(string message = "Saved", bool restart = false)
        {
            if (restart)
                ShowRestartNotice(message);
        }

        private void SetError(string message)
        {
            AppServices.Overlays.ShowToast("Could not save settings", message, ToastTone.Error);
        }

        private static void ShowRestartNotice(string message) =>
            AppServices.Overlays.ShowToast("Restart required",
                "Restart TCLauncher to apply your changes.", ToastTone.Warning,
                "Restart now", RestartLauncher, true);

        private static void RestartLauncher()
        {
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Application.Current.Shutdown();
        }

        private void SectionList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(SectionList.SelectedItem is ListBoxItem selected)) return;

            var selectedSection = _sections.FirstOrDefault(section => Equals(section.Tag, selected.Tag));
            if (selectedSection == null) return;

            if (_initializing)
            {
                _activeSection = selectedSection;
                foreach (var section in _sections)
                    section.Visibility = ReferenceEquals(section, selectedSection)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                return;
            }

            _pendingSection = selectedSection;
            if (_sectionTransitionQueued || _sectionTransitioning) return;

            _sectionTransitionQueued = true;
            Dispatcher.BeginInvoke(StartSectionTransition, DispatcherPriority.DataBind);
        }

        private void StartSectionTransition()
        {
            _sectionTransitionQueued = false;
            if (_pendingSection == null || ReferenceEquals(_activeSection, _pendingSection)) return;

            var outgoingSection = _activeSection;
            var incomingSection = _pendingSection;
            var direction = Array.IndexOf(_sections, incomingSection) > Array.IndexOf(_sections, outgoingSection)
                ? 1
                : -1;

            if (!SystemParameters.ClientAreaAnimation)
            {
                CompleteSectionTransition(outgoingSection, incomingSection);
                return;
            }

            _sectionTransitioning = true;
            foreach (var section in _sections)
                if (!ReferenceEquals(section, outgoingSection) && !ReferenceEquals(section, incomingSection))
                    section.Visibility = Visibility.Collapsed;

            outgoingSection.RenderTransformOrigin = new Point(0.5, 0.02);
            incomingSection.RenderTransformOrigin = new Point(0.5, 0.02);
            PageTransition.Reset(outgoingSection);
            PageTransition.Reset(incomingSection);
            PageTransition.Begin(outgoingSection, incomingSection, direction,
                () => CompleteSectionTransition(outgoingSection, incomingSection));
        }

        private void CompleteSectionTransition(StackPanel outgoingSection, StackPanel incomingSection)
        {
            outgoingSection.Visibility = Visibility.Collapsed;
            PageTransition.Reset(incomingSection);
            _activeSection = incomingSection;
            _sectionTransitioning = false;

            if (_pendingSection != null && !ReferenceEquals(_activeSection, _pendingSection))
                StartSectionTransition();
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
            try
            {
                Settings.Default.Save();
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
        }

        private void LanguageSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || !((sender as ComboBox)?.SelectedItem is ComboBoxItem item)) return;
            Settings.Default.Language = (string)item.Tag;
            try
            {
                Settings.Default.Save();
                ShowRestartNotice("Language changes are ready.");
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
        }

        private void PixelFont_OnChanged(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;
            Settings.Default.UsePixelFontEverywhere = CheckBoxUsePixelFontEverywhere.IsChecked == true;
            try
            {
                Settings.Default.Save();
                ShowRestartNotice("The new appearance will apply after restarting.");
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
        }

        private void DebouncedTextSetting_OnChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing) return;
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
            try
            {
                Settings.Default.Save();
                if (storageChanged) ShowRestartNotice("The storage location changed.");
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
        }

        private void BrowseDataLocation_OnClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
                   {
                       Description = "Select the folder where launcher data should be stored.",
                       ShowNewFolderButton = true,
                       SelectedPath = AppDataPath.Text.Trim()
                   })
            {
                if (dialog.ShowDialog() == DialogResult.OK) AppDataPath.Text = dialog.SelectedPath;
            }
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
                ShowRestartNotice("The launcher data was moved successfully.");
            }
            catch (Exception exception)
            {
                AppServices.Log.Error("settings.storage_move_failed", exception);
                SetError(exception.Message);
            }
        }

        private async void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            updateBtn.IsLoading = true;
            try
            {
                var check = await AppServices.Updates.CheckAsync(Assembly.GetExecutingAssembly().GetName().Version,
                    CancellationToken.None);
                if (!check.IsSuccess)
                {
                    ShowUpdateNotification("Update check failed", check.Message, ToastTone.Error);
                    return;
                }

                if (!check.Value.IsUpdateAvailable)
                {
                    ShowUpdateNotification("Launcher is up to date", "No update is available.", ToastTone.Success);
                    return;
                }

                if (!check.Value.IsCompatible)
                {
                    ShowUpdateNotification("Update unavailable", check.Value.CompatibilityMessage, ToastTone.Warning);
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
                    ShowUpdateNotification("Update download failed", download.Message, ToastTone.Error);
                    return;
                }

                Process.Start(new ProcessStartInfo("msiexec.exe", "/i \"" + download.Value + "\"")
                    { UseShellExecute = true });
                ShowUpdateNotification("Update ready", "The verified installer has been opened.", ToastTone.Success);
            }
            catch (Exception exception)
            {
                AppServices.Log.Error("settings.update_failed", exception);
                ShowUpdateNotification("Update failed", exception.Message, ToastTone.Error);
            }
            finally
            {
                updateBtn.IsLoading = false;
            }
        }

        private static void ShowUpdateNotification(string title, string message, ToastTone tone) =>
            AppServices.Overlays.ShowToast(title, message, tone);

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
            try
            {
                Settings.Default.Reset();
                Settings.Default.Save();
                ShowRestartNotice("Defaults restored.");
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
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
                AppServices.Overlays.ShowToast("Data removed", "Run setup again to continue.", ToastTone.Warning);
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
