using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TCLauncher.Core
{
    public sealed class AsyncRelayCommand : ICommand, IDisposable
    {
        private readonly Func<CancellationToken, Task> _execute;
        private readonly Func<bool> _canExecute;
        private CancellationTokenSource _cancellation;
        private bool _isExecuting;

        public bool IsExecuting => _isExecuting;
        public event EventHandler CanExecuteChanged;

        public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            _cancellation = new CancellationTokenSource();
            RaiseCanExecuteChanged();
            try
            {
                await _execute(_cancellation.Token);
            }
            finally
            {
                _isExecuting = false;
                _cancellation.Dispose();
                _cancellation = null;
                RaiseCanExecuteChanged();
            }
        }

        public void Cancel() => _cancellation?.Cancel();
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public void Dispose() => _cancellation?.Dispose();
    }
}