using System.Windows.Controls;
using TCLauncher.Core.Services;

namespace TCLauncher.Controls.Gallery
{
    [Story("OverlayHost")]
    public partial class OverlayPage : UserControl
    {
        public OverlayPage() => InitializeComponent();

        private void ShowSampleOverlay_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            AppServices.Overlays.ShowDrawer(
                "Gallery overlay",
                new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "This is rendered by OverlayHost.", FontSize = 16 },
                        new TextBlock { Text = "The drawer is supplied by the launcher overlay service.", Margin = new System.Windows.Thickness(0, 10, 0, 0), TextWrapping = System.Windows.TextWrapping.Wrap }
                    }
                });
        }
    }
}
