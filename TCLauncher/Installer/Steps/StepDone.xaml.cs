using System.Reflection;
using System.Windows;
using static TCLauncher.Core.InternetUtils;

namespace TCLauncher.Installer.Steps
{
    public partial class StepDone
    {
        public StepDone()
        {
            InitializeComponent();
        }

        private void GitHubLinkButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenUrlInWebBrowser("https://github.com/TD99/T-Craft-Game-Launcher");
        }

        private void WebsiteLinkButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenUrlInWebBrowser("https://tcraft.link/tclauncher/");
        }

        private void SupportLinkButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenEmail("support@tcraft.ch", "Support request: TCLauncher " + Assembly.GetExecutingAssembly().GetName().Version);
        }
    }
}
