using Microsoft.Win32;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;
using TCLauncher.MVVM.ViewModel;

namespace TCLauncher.MVVM.Windows
{
    public partial class ProfileCreatorWindow
    {
        public InstalledInstance CreatedInstance { get; private set; }

        public ProfileCreatorWindow(InstalledInstance cloneSource = null)
        {
            InitializeComponent();
            var draft = cloneSource == null ? null : AppServices.Profiles.CloneDraft(cloneSource);
            var viewModel = new ProfileCreatorViewModel(AppServices.Profiles, IoUtils.Tcl.InstancesPath, draft);
            viewModel.ProfileCreated += (sender, instance) => { CreatedInstance = instance; DialogResult = true; Close(); };
            DataContext = viewModel;
        }

        private void BrowseIcon_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
            if (dialog.ShowDialog(this) == true)
            {
                var viewModel = (ProfileCreatorViewModel)DataContext;
                viewModel.Draft.IconPath = dialog.FileName;
                viewModel.RefreshDraft();
            }
        }
    }
}
