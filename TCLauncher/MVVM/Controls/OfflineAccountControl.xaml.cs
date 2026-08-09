using TCLauncher.Models;

namespace TCLauncher.MVVM.Controls
{
    public partial class OfflineAccountControl
    {
        public OfflineProfile Profile { get; }

        public OfflineAccountControl(OfflineProfile profile, bool selected)
        {
            Profile = profile;
            InitializeComponent();
            DataContext = profile;
            LoginBtn.Content = selected ? "Selected" : "Login";
            LoginBtn.IsEnabled = !selected;
        }
    }
}
