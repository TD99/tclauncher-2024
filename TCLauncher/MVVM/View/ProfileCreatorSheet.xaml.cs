using System;
using System.Windows;
using Microsoft.Win32;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.ViewModel;

namespace TCLauncher.MVVM.View
{
    public partial class ProfileCreatorSheet
    {
        private readonly ProfileCreatorViewModel _viewModel;

        public ProfileCreatorSheet(InstalledInstance cloneSource, Action<InstalledInstance> created)
        {
            InitializeComponent();
            var draft = cloneSource == null ? null : AppServices.Profiles.CloneDraft(cloneSource);
            _viewModel = new ProfileCreatorViewModel(AppServices.Profiles, IoUtils.Tcl.InstancesPath, draft);
            _viewModel.ProfileCreated += (sender, instance) =>
            {
                created?.Invoke(instance);
                AppServices.Overlays.Close(true);
                AppServices.Overlays.ShowToast("Profile created", instance.DisplayName);
            };
            DataContext = _viewModel;
        }

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var picker = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
            if (picker.ShowDialog() != true) return;
            _viewModel.Draft.IconPath = picker.FileName;
            _viewModel.RefreshDraft();
        }
    }
}