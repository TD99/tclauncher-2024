using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.ViewModel;
using TCLauncher.Properties;

namespace TCLauncher.MVVM.View
{
    public partial class ServerListView
    {
        public ServerListView()
        {
            InitializeComponent();
            PreviewKeyDown += (sender, args) =>
            {
                if (args.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    SearchBox.Focus();
                    args.Handled = true;
                }
            };
        }

        private void GameCard_OnClick(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is Instance instance)) return;
            DetailsContent.Content = new GameDetailsView(instance, OnGameChanged, CloseDetails);
            CatalogView.Visibility = Visibility.Collapsed;
            DetailsView.Visibility = Visibility.Visible;
        }

        private void BackFromDetails_OnClick(object sender, RoutedEventArgs e) => CloseDetails();

        private void CloseDetails()
        {
            DetailsView.Visibility = Visibility.Collapsed;
            CatalogView.Visibility = Visibility.Visible;
            DetailsContent.Content = null;
            SearchBox.Focus();
        }

        private async void OnGameChanged(Instance instance)
        {
            if (DataContext is ServerListViewModel viewModel)
                await viewModel.LoadAsync(CancellationToken.None);
        }

        private void GamesMenuButton_OnClick(object sender, RoutedEventArgs e)
        {
            GamesMenuButton.ContextMenu.PlacementTarget = GamesMenuButton;
            GamesMenuButton.ContextMenu.IsOpen = true;
        }

        private void CreateProfile_OnClick(object sender, RoutedEventArgs e)
        {
            _ = AppServices.Overlays.ShowSheetAsync("Create profile", new ProfileCreatorSheet(null, OnGameChanged),
                false);
        }

        private async void ImportPackage_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { DefaultExt = ".tcl", Filter = Languages.tcl_package + " (*.tcl)|*.tcl" };
            if (dialog.ShowDialog() != true) return;
            var preview = AppServices.Packages.PreviewImport(dialog.FileName);
            if (!preview.IsSuccess)
            {
                await AppServices.Overlays.ShowSheetAsync("Package could not be opened", preview.Message);
                return;
            }

            var loader = preview.Value.Manifest.Instance.GetEffectiveLoader();
            var includesSaves =
                preview.Value.Manifest.Files?.Any(file =>
                    file.Path.StartsWith("saves/", StringComparison.OrdinalIgnoreCase)) == true;
            var summary = preview.Value.Manifest.Instance.DisplayName + "\nMinecraft " +
                          preview.Value.Manifest.Instance.McVersion +
                          " • " + loader.Type +
                          (string.IsNullOrWhiteSpace(loader.Version) ? "" : " " + loader.Version) +
                          "\nPack " + preview.Value.Manifest.Instance.Version + " • " +
                          (preview.Value.PackageBytes / 1024d / 1024d).ToString("0.##") + " MB" +
                          "\nSaves: " + (includesSaves ? "included" : "not included") + (preview.Value.IsLegacy
                              ? "\nLegacy v1 package"
                              : "\nVerified v2 package");
            if (!await AppServices.Overlays.ConfirmAsync("Import package", summary, "Import", "Cancel")) return;

            var resolution = ImportConflictResolution.Cancel;
            if (preview.Value.HasConflict)
            {
                var replace = await AppServices.Overlays.ConfirmAsync("Profile already installed",
                    "Replace the installed profile? Choose Import as copy to preserve it.", "Replace",
                    "Import as copy");
                resolution = replace ? ImportConflictResolution.Replace : ImportConflictResolution.ImportAsCopy;
            }

            var result = AppServices.Packages.Import(dialog.FileName, resolution);
            if (!result.IsSuccess)
            {
                await AppServices.Overlays.ShowSheetAsync("Import failed", result.Message);
                return;
            }

            OnGameChanged(result.Value);
            AppServices.Overlays.ShowToast("Package imported", result.Value.DisplayName);
        }
    }
}