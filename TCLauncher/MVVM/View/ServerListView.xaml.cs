using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.ViewModel;
using TCLauncher.MVVM.Windows;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.View
{
    /// <summary>
    /// Interaction logic for ServerListView.xaml
    /// </summary>
    public partial class ServerListView
    {
        private Instance current { get; set; }

        public ServerListView()
        {
            InitializeComponent();
        }

        private void ServerItem_Clicked(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            Instance instance = (Instance)border.DataContext;
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(instance.ThumbnailURL, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                itemFocusBanner.Source = bitmap;
            }
            catch
            {
                // TODO: Add default image
            }
            itemFocusName.Text = instance.DisplayName;
            itemFocusPatch.Text = $"{instance.GetCurrentPatch()?.Name ?? "local"}@{instance.Version}";
            itemFocusPackage.Text = "ch.tcraft." + instance.Name;
            itemFocusType.Text = instance.Type;
            itemFocusMCVersion.Text = instance.McVersion;

            specialFocusBtn.Content = (instance.Is_Installed) ? Languages.uninstall : Languages.install;
            openFolderBtn.Visibility = (instance.Is_Installed) ? Visibility.Visible : Visibility.Collapsed;
            healthBtn.Visibility = instance.Is_Installed ? Visibility.Visible : Visibility.Collapsed;
            exportProfileBtn.Visibility = instance.Is_Installed ? Visibility.Visible : Visibility.Collapsed;
            cloneProfileBtn.Visibility = instance.Is_Installed ? Visibility.Visible : Visibility.Collapsed;
            reconfigDef.Visibility = (instance.Is_Installed && !instance.Is_LocalSource) ? Visibility.Visible : Visibility.Collapsed;
            //editConfig.Visibility = (instance.Is_Installed) ? Visibility.Visible : Visibility.Collapsed;
            itemFocusMCWorkingDirDesc.Children.Clear();

            current = instance;

            // TODO: Fix bug where installed instances don't show up
            if (instance.WorkingDirDesc != null)
            {
                foreach (KeyValuePair<string, List<string>> entry in instance.WorkingDirDesc)
                {
                    AddTextBlock(itemFocusMCWorkingDirDesc, entry.Key, 20);

                    foreach (string description in entry.Value)
                    {
                        AddTextBlock(itemFocusMCWorkingDirDesc, description, 16);
                    }
                }
            }
            else
            {
                propsText.Visibility = Visibility.Collapsed;
            }

            itemFocus.Visibility = Visibility.Visible;
        }

        private void AddTextBlock(Panel panel, string text, int fontSize)
        {
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = fontSize
            };
            panel.Children.Add(textBlock);
        }

        private void closeFocusBtn_Click(object sender, RoutedEventArgs e)
        {
            itemFocus.Visibility = Visibility.Collapsed;
            itemFocusBanner.Source = new BitmapImage(new Uri("/Assets/Images/nothumb.png", UriKind.RelativeOrAbsolute));
            itemFocusName.Text = "";
            itemFocusPatch.Text = "";
            itemFocusPackage.Text = "";
            itemFocusType.Text = "";
            itemFocusMCVersion.Text = "";
            specialFocusBtn.Content = Languages.action;
            itemFocusMCWorkingDirDesc.Children.Clear();
            propsText.Visibility = Visibility.Visible;

            current = null;
        }

        private void forceUninstallBtn_Click(object sender, RoutedEventArgs e)
        {
            uninstallInstance(current);
        }

        private void uninstallInstance(Instance instance, bool force = false)
        {
            try
            {
                var instanceFolder = Path.Combine(IoUtils.Tcl.InstancesPath, instance.Guid.ToString());
                if (!Directory.Exists(instanceFolder))
                {
                    MessageBox.Show(Languages.no_data_found_message_delete_instance, Languages.delete_instance);
                    return;
                }

                var result = MessageBox.Show(Languages.confirm_delete_instance_message, Languages.delete_instance,
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
                
                Directory.Delete(instanceFolder, true);
                if (DataContext is ServerListViewModel viewModel) viewModel.ServerList.Remove(instance);
                closeFocusBtn_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Languages.error_occurred_delete_instance_message, ex.Message), Languages.delete_instance);
            }
        }

        private async void specialFocusBtn_Click(object sender, RoutedEventArgs e)
        {
            if (current.Is_Installed)
            {
                uninstallInstance(current);
            }
            else
            {
                await InstallInstanceModern(current);
            }

            if (current != null) specialFocusBtn.Content = current.Is_Installed ? Languages.uninstall : Languages.install;
        }

        private async Task InstallInstanceModern(Instance instance)
        {
            var cancellation = new CancellationTokenSource();
            var window = new OperationWindow { Owner = App.MainWin };
            window.CancelRequested += (sender, args) => cancellation.Cancel();
            var progress = new Progress<OperationProgress>(window.Update);
            specialFocusBtn.IsEnabled = false;
            window.Show();
            try
            {
                var result = await AppServices.InstanceOperations.InstallOrUpdateAsync(instance, progress, cancellation.Token);
                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.Message + "\n\nReference: " + result.OperationId, Languages.error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (DataContext is ServerListViewModel viewModel)
                {
                    var index = viewModel.ServerList.IndexOf(instance);
                    if (index >= 0) viewModel.ServerList[index] = result.Value;
                }
                current = result.Value;
                specialFocusBtn.Content = Languages.uninstall;
                openFolderBtn.Visibility = Visibility.Visible;
                healthBtn.Visibility = Visibility.Visible;
                exportProfileBtn.Visibility = Visibility.Visible;
            }
            finally
            {
                specialFocusBtn.IsEnabled = true;
                cancellation.Dispose();
                window.Close();
            }
        }

        private OperationResult<InstalledInstance> reconfigure(Instance instance)
        {
            var installedInstance = new InstalledInstance(instance);
            var result = AppServices.InstanceConfigs.Save(installedInstance, installedInstance.ConfigFile);
            return result.IsSuccess
                ? OperationResult<InstalledInstance>.Success(installedInstance, result.OperationId)
                : OperationResult<InstalledInstance>.Failure(result.ErrorCode, result.Message, result.Exception, result.OperationId);
        }

        private void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            string dataFolder = Path.Combine(IoUtils.Tcl.InstancesPath, current.Guid.ToString(), "data");
            Process.Start("explorer.exe", dataFolder);
        }

        private async void reconfigDef_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = await LauncherHttpClient.Instance.GetAsync(Settings.Default.DownloadMirror + "?guid=" + current.Guid);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var instance = JsonConvert.DeserializeObject<ObservableCollection<Instance>>(content)[0];

                    var saved = reconfigure(instance);
                    if (!saved.IsSuccess) throw new InvalidDataException(saved.Message);
                    if (DataContext is ServerListViewModel viewModel)
                    {
                        var index = viewModel.ServerList.IndexOf(current);
                        if (index >= 0) viewModel.ServerList[index] = saved.Value;
                    }
                    current = saved.Value;
                    ServerItem_Clicked(new Border { DataContext = current }, null);
                    return;
                }
                MessageBox.Show(string.Format(Languages.reconfiguration_failed_message, current.Name));
            }
            catch
            {
                MessageBox.Show(string.Format(Languages.reconfiguration_failed_message, current.Name));
            }
        }

        private void ExportServerBtn_OnClick()
        {
            if (!(current is InstalledInstance installed))
            {
                MessageBox.Show("Open an installed profile first, then choose Export .tcl.", Languages.package_create);
                return;
            }
            ExportProfile(installed);
        }

        private void ImportServerBtn_OnClick()
        {
            var dialog = new OpenFileDialog { DefaultExt = ".tcl", Filter = Languages.tcl_package + " (*.tcl)|*.tcl" };
            if (dialog.ShowDialog() != true) return;
            var preview = AppServices.Packages.PreviewImport(dialog.FileName);
            if (!preview.IsSuccess)
            {
                MessageBox.Show(preview.Message, Languages.package_import, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var loader = preview.Value.Manifest.Instance.GetEffectiveLoader();
            var includesSaves = preview.Value.Manifest.Files?.Any(file => file.Path.StartsWith("saves/", StringComparison.OrdinalIgnoreCase)) == true;
            var summary = preview.Value.Manifest.Instance.DisplayName + "\nMinecraft " + preview.Value.Manifest.Instance.McVersion +
                          " • " + loader.Type + (string.IsNullOrWhiteSpace(loader.Version) ? "" : " " + loader.Version) +
                          "\nPack " + preview.Value.Manifest.Instance.Version + " • " + (preview.Value.PackageBytes / 1024d / 1024d).ToString("0.##") + " MB" +
                          "\nSaves: " + (includesSaves ? "included" : "not included") + (preview.Value.IsLegacy ? "\nLegacy v1 package" : "\nVerified v2 package");
            if (MessageBox.Show(summary, Languages.package_import, MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var resolution = ImportConflictResolution.Cancel;
            if (preview.Value.HasConflict)
            {
                var choice = MessageBox.Show($"{preview.Value.Manifest.Instance.DisplayName} is already installed.\n\nYes: replace it\nNo: import as a copy\nCancel: stop", Languages.package_import, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (choice == MessageBoxResult.Yes) resolution = ImportConflictResolution.Replace;
                else if (choice == MessageBoxResult.No) resolution = ImportConflictResolution.ImportAsCopy;
                else return;
            }

            var result = AppServices.Packages.Import(dialog.FileName, resolution);
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.Message, Languages.package_import, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (DataContext is ServerListViewModel viewModel)
            {
                var existing = viewModel.ServerList.FirstOrDefault(item => item.Guid == result.Value.Guid);
                if (existing != null) viewModel.ServerList.Remove(existing);
                viewModel.ServerList.Add(result.Value);
            }
        }

        private void CreateBlankBtn_OnClick()
        {
            var window = new ProfileCreatorWindow { Owner = App.MainWin };
            if (window.ShowDialog() == true && DataContext is ServerListViewModel viewModel)
                viewModel.ServerList.Add(window.CreatedInstance);
        }

        private void healthBtn_Click(object sender, RoutedEventArgs e)
        {
            if (current is InstalledInstance installed)
                new InstanceHealthWindow(installed) { Owner = App.MainWin }.ShowDialog();
        }

        private void exportProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (current is InstalledInstance installed) ExportProfile(installed);
        }

        private void cloneProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(current is InstalledInstance installed)) return;
            var window = new ProfileCreatorWindow(installed) { Owner = App.MainWin };
            if (window.ShowDialog() == true && DataContext is ServerListViewModel viewModel)
                viewModel.ServerList.Add(window.CreatedInstance);
        }

        private void ExportProfile(InstalledInstance installed)
        {
            new PackageExportWindow(installed) { Owner = App.MainWin }.ShowDialog();
        }

        private void ActionComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBoxItem) ActionComboBox.SelectedItem).Tag)
            {
                case "CreateBlankBtn":
                    CreateBlankBtn_OnClick();
                    break;
                case "ExportBtn":
                    ExportServerBtn_OnClick();
                    break;
                case "ImportBtn":
                    ImportServerBtn_OnClick();
                    break;
            }

            ActionComboBox.SelectedItem = ActionDefaultLabel;
        }

        private void ListView_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            if (!(sender is ListView listView)) return;
            if (!(listView.DataContext is ServerListViewModel viewModel)) return;

            var scaleWidth = 1 + e.Delta / 1000.0;
            var scaleHeight = 1 + e.Delta / 1000.0;

            var newWidth = viewModel.ItemWidth * scaleWidth;
            var newHeight = viewModel.ItemHeight * scaleHeight;

            if (!(newWidth < viewModel.ItemMinWidth) && !(newWidth > viewModel.ItemMaxWidth)) viewModel.ItemWidth = newWidth;
            if (!(newHeight < viewModel.ItemMinHeight) && !(newHeight > viewModel.ItemMaxHeight)) viewModel.ItemHeight = newHeight;

            e.Handled = true;
        }
    }
}
