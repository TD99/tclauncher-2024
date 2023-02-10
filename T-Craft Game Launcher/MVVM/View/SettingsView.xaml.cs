using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace T_Craft_Game_Launcher.MVVM.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            assemblyVersion.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private async void saveBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveNotification.Opacity = 0;
            SaveNotification.IsOpen = true;
            SaveNotification.Visibility = System.Windows.Visibility.Visible;

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
            };
            SaveNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);

            await Task.Delay(3000);

            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
            };

            fadeOutAnimation.Completed += (s, args) =>
            {
                SaveNotification.IsOpen = false;
                SaveNotification.Visibility = System.Windows.Visibility.Collapsed;
                SaveNotification.Opacity = 1;
            };
            
            SaveNotification.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
        }

        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();
        }
    }
}
