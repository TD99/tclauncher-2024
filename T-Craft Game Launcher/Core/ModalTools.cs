using System;
using System.Windows;

namespace T_Craft_Game_Launcher.Core
{
    public static class ModalTools
    {
        public static void ShowToVoid(string text, string title = null)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(text, title)));
        }
    }
}
