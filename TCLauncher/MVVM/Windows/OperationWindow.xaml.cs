using System;
using System.Windows;
using TCLauncher.Models;

namespace TCLauncher.MVVM.Windows
{
    public partial class OperationWindow
    {
        public event EventHandler CancelRequested;

        public OperationWindow()
        {
            InitializeComponent();
        }

        public void Update(OperationProgress progress)
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => Update(progress)); return; }
            StageText.Text = progress.Stage.ToString();
            MessageText.Text = progress.Message;
            Progress.IsIndeterminate = !progress.Percent.HasValue;
            if (progress.Percent.HasValue) Progress.Value = progress.Percent.Value;
            BytesText.Text = progress.ProgressedBytes.HasValue
                ? Format(progress.ProgressedBytes.Value) + (progress.TotalBytes.HasValue ? " / " + Format(progress.TotalBytes.Value) : string.Empty)
                : string.Empty;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            MessageText.Text = "Cancelling safely…";
        }

        private static string Format(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double value = bytes;
            var index = 0;
            while (value >= 1024 && index < units.Length - 1) { value /= 1024; index++; }
            return value.ToString("0.##") + " " + units[index];
        }
    }
}
