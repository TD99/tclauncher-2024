using System;
using System.Windows;
using TCLauncher.Models;
using TCLauncher.MVVM.Windows;

namespace TCLauncher.Core
{
    // TODO: Make it more universal / useful
    /// <summary>
    /// A utility class for handling message box operations.
    /// </summary>
    public static class MessageBoxUtils
    {
        /// <summary>
        /// Displays a message box with the specified text and title.
        /// </summary>
        /// <param name="text">The text to display in the message box.</param>
        /// <param name="title">The title of the message box. Default is an empty string.</param>
        public static void ShowToVoid(string text, string title = "")
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var dialog = string.IsNullOrEmpty(title)
                    ? new CustomButtonDialog(DialogButtons.Ok, text, null)
                    : new CustomButtonDialog(DialogButtons.Ok, title, text);
                dialog.ShowDialog();
            }));
        }

        /// <summary>
        /// Displays a message box with the specified text and title. (legacy)
        /// </summary>
        /// <param name="text">The text to display in the message box.</param>
        /// <param name="title">The title of the message box. Default is an empty string.</param>
        public static void ShowToVoidLegacy(string text, string title = "")
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(text, title)));
        }
    }
}
