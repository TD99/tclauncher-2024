using System;
using System.Windows;
using TCLauncher.Core.Services;

namespace TCLauncher.MVVM.View
{
    public partial class OperationCloseSheet
    {
        private readonly Action<bool> _exitAfterOperation;
        private readonly Action _forceExit;

        public OperationCloseSheet(Action<bool> exitAfterOperation, Action forceExit)
        {
            _exitAfterOperation = exitAfterOperation;
            _forceExit = forceExit;
            InitializeComponent();
        }

        private void Wait_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Overlays.Close();
            _exitAfterOperation(false);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Overlays.Close();
            _exitAfterOperation(true);
        }

        private void ForceExit_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Overlays.Close();
            _forceExit?.Invoke();
        }

        private void Stay_Click(object sender, RoutedEventArgs e) => AppServices.Overlays.Close();
    }
}