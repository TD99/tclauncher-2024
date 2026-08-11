using System.Windows;
using System.Windows.Controls;
using TCLauncher.Core.Services;

namespace TCLauncher.Controls.Gallery
{
    [Story("Overlays")]
    public partial class OverlaysPage : UserControl
    {
        private readonly OverlayService _overlayService;

        public OverlaysPage()
        {
            InitializeComponent();
            _overlayService = new OverlayService(Application.Current.Dispatcher);
            OverlayHost.OverlayService = _overlayService;
        }

        private void ShowDrawer_OnClick(object sender, RoutedEventArgs e)
        {
            _overlayService.ShowDrawer(
                "Gallery drawer",
                CreateContent("This drawer is rendered by the dedicated Overlays page."));
        }

        private async void ShowSheet_OnClick(object sender, RoutedEventArgs e)
        {
            await _overlayService.ShowSheetAsync(
                "Gallery sheet",
                CreateContent("This sheet uses the same isolated OverlayHost, with outside dismissal enabled."));
        }

        private async void ShowConfirmation_OnClick(object sender, RoutedEventArgs e)
        {
            var accepted = await _overlayService.ConfirmAsync(
                "Confirm overlay",
                CreateContent("This confirmation keeps the overlay open until you choose an action."),
                "Accept",
                "Cancel");

            _overlayService.ShowToast(
                accepted ? "Accepted" : "Cancelled",
                accepted ? "The confirmation was accepted." : "The confirmation was cancelled.",
                accepted ? ToastTone.Success : ToastTone.Warning,
                duration: System.TimeSpan.FromSeconds(3));
        }

        private static StackPanel CreateContent(string message) => new StackPanel
        {
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap }
            }
        };
    }
}
