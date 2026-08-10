using System;
using System.Diagnostics;
using System.Windows;
using TCLauncher.Core.Services;

namespace TCLauncher.MVVM.View
{
    public partial class LaunchErrorSheet
    {
        private readonly Action _retry;

        public LaunchErrorSheet(string message, Action retry)
        {
            InitializeComponent();
            DataContext = message;
            _retry = retry;
        }

        private void Logs_Click(object sender, RoutedEventArgs e) =>
            Process.Start("explorer.exe", AppServices.Log.LogDirectory);

        private void Repair_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Overlays.Close();
            App.MainWin.navigateToServer();
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Overlays.Close();
            _retry?.Invoke();
        }
    }
}