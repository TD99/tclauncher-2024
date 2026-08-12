using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.Controls.Gallery
{
    [Story("Notifications", Description = "Toast states, persistent actions, and non-dismissible in-progress work.")]
    public partial class NotificationsPage : UserControl
    {
        public NotificationsPage() => InitializeComponent();

        private void Success_OnClick(object sender, RoutedEventArgs e) => ShowToast("Saved", "Your changes were saved successfully.", ToastTone.Success);

        private void Warning_OnClick(object sender, RoutedEventArgs e) => ShowToast("Needs attention", "Some optional settings could not be applied.", ToastTone.Warning);

        private void Error_OnClick(object sender, RoutedEventArgs e) => ShowToast("Could not connect", "Check your connection and try again.", ToastTone.Error);

        private void Restart_OnClick(object sender, RoutedEventArgs e)
        {
            AppServices.Overlays.ShowToast(
                "Restart required",
                "Restart the launcher to apply your changes.",
                ToastTone.Warning,
                "Restart now",
                () => AppServices.Overlays.ShowToast("Restart queued", "The action was accepted.", ToastTone.Success),
                persistent: PersistentToggle.IsChecked == true,
                canDismiss: DismissibleToggle.IsChecked == true,
                duration: PersistentToggle.IsChecked == true ? (TimeSpan?)null : GetDuration());
        }

        private async void Progress_OnClick(object sender, RoutedEventArgs e)
        {
            await AppServices.Operations.RunAsync("Synchronizing launcher data", false,
                async (progress, cancellationToken) =>
                {
                    for (var step = 0; step <= 5; step++)
                    {
                        progress.Report(new OperationProgress
                        {
                            Stage = OperationStage.Downloading,
                            Message = "Downloading package " + (step + 1) + " of 6…",
                            Percent = step * 20
                        });
                        await Task.Delay(500, cancellationToken);
                    }

                    return OperationResult<string>.Success("Complete");
                });
        }

        private void ShowToast(string title, string message, ToastTone tone) =>
            AppServices.Overlays.ShowToast(
                title,
                message,
                tone,
                null,
                null,
                false,
                DismissibleToggle.IsChecked == true,
                GetDuration());

        private TimeSpan GetDuration()
        {
            return double.TryParse(DurationText.Text, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(Math.Min(seconds, 60))
                : TimeSpan.FromSeconds(5);
        }

        private void PersistentToggle_OnChanged(object sender, RoutedEventArgs e)
        {
            if (DurationText != null) DurationText.IsEnabled = true;
        }
    }
}
