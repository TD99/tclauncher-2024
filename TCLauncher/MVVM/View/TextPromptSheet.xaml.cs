using System;
using System.Windows;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.View
{
    public partial class TextPromptSheet
    {
        private readonly Func<string, OperationResult> _submit;

        public TextPromptSheet(string prompt, Func<string, OperationResult> submit)
        {
            _submit = submit;
            InitializeComponent();
            Prompt.Text = prompt;
            Loaded += (sender, args) => Value.Focus();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var result = _submit(Value.Text);
            if (!result.IsSuccess)
            {
                Error.Text = result.Message;
                return;
            }

            AppServices.Overlays.Close(true);
        }
    }
}