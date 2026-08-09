using System;
using System.Windows;
using TCLauncher.Core.Services;

namespace TCLauncher.MVVM.View
{
    public partial class OperationCloseSheet
    {
        private readonly Action<bool> _exitAfterOperation;
        public OperationCloseSheet(Action<bool> exitAfterOperation) { _exitAfterOperation = exitAfterOperation; InitializeComponent(); }
        private void Wait_Click(object sender, RoutedEventArgs e) { AppServices.Overlays.Close(); _exitAfterOperation(false); }
        private void Cancel_Click(object sender, RoutedEventArgs e) { AppServices.Overlays.Close(); _exitAfterOperation(true); }
        private void Stay_Click(object sender, RoutedEventArgs e) => AppServices.Overlays.Close();
    }
}
