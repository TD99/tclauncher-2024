using System;
using System.Windows;

namespace T_Craft_Game_Launcher.Core
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
            Application.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(text, title)));
        }
    }
}
