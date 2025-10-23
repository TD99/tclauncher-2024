using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TCLauncher.Models;

namespace TCLauncher.MVVM.Windows
{
    public partial class CustomButtonDialog
    {
        private readonly TaskCompletionSource<DialogButton> _tcs;
        public Task<DialogButton> Result => _tcs.Task;

        public CustomButtonDialog(IEnumerable<DialogButton> buttons, string title = "", string message = "")
        {
            InitializeComponent();

            TitleLabel.Content = title;

            if (!string.IsNullOrEmpty(message))
            {
                BodyTextBlock.Text = message;
                BodyTextBlock.Visibility = Visibility.Visible;
            }

            _tcs = new TaskCompletionSource<DialogButton>();

            foreach (var dialogButton in buttons)
            {
                var btn = new Button
                {
                    Content = dialogButton.Text,
                    Background = dialogButton.Background,
                    Foreground = dialogButton.Foreground,
                    IsDefault = dialogButton.IsDefault,
                    IsCancel = dialogButton.IsCancel,
                    Padding = new Thickness(15, 5, 15, 5),
                    Margin = new Thickness(5, 0, 0, 0),
                    Style = (Style)FindResource("ModernButton")
                };
                btn.Click += (sender, e) =>
                {
                    _tcs.TrySetResult(dialogButton);
                    Close();
                };
                ButtonStackPanel.Children.Add(btn);
            }
        }

        private void CustomButtonDialog_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CustomButtonDialog_OnClosing(object sender, CancelEventArgs e)
        {
            if (_tcs.Task.IsCompleted) return;
            _tcs.TrySetResult(null);
        }
        
    }
}
