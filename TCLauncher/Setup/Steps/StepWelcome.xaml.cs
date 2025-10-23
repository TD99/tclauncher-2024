using System.Reflection;
using System.Windows;
using TCLauncher.Properties;

namespace TCLauncher.Setup.Steps
{
    public partial class StepWelcome
    {
        public StepWelcome()
        {
            InitializeComponent();
        }


        private void StepWelcome_OnLoaded(object sender, RoutedEventArgs e)
        {
            VersionLabel.Content = string.Format(Languages.version_text, Assembly.GetExecutingAssembly().GetName().Version);
        }
    }
}
