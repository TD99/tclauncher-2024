using System.Windows;
using System.Windows.Controls;

namespace TCLauncher.Controls.Gallery
{
    [Story("Button", Description = "Variants, sizes, and async loading behavior.")]
    public partial class ButtonPage : UserControl
    {
        public ButtonPage() => InitializeComponent();

        private void LoadingToggle_OnChanged(object sender, RoutedEventArgs e)
        {
            if (LoadingButton != null) LoadingButton.IsLoading = LoadingToggle.IsChecked == true;
        }
    }
}
