using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TCLauncher.Models;

namespace TCLauncher.Core.Services
{
    public sealed class ActiveOperation : INotifyPropertyChanged
    {
        private OperationProgress _progress;
        private bool _isCancelling;

        public string Id { get; set; }
        public string Title { get; set; }
        public bool CanCancel { get; set; }
        public OperationProgress Progress { get => _progress; internal set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(Stage)); OnPropertyChanged(nameof(Message)); OnPropertyChanged(nameof(Percent)); } }
        public bool IsCancelling { get => _isCancelling; internal set { _isCancelling = value; OnPropertyChanged(); OnPropertyChanged(nameof(Message)); } }
        public string Stage => Progress?.Stage.ToString() ?? OperationStage.Preparing.ToString();
        public string Message => IsCancelling ? "Cancelling safely…" : Progress?.Message ?? "Preparing…";
        public double Percent => Progress?.Percent ?? 0;
        internal CancellationTokenSource Cancellation { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public interface IOperationCoordinator : INotifyPropertyChanged
    {
        ActiveOperation Active { get; }
        bool IsBusy { get; }
        Task<OperationResult<T>> RunAsync<T>(string title, bool canCancel,
            Func<IProgress<OperationProgress>, CancellationToken, Task<OperationResult<T>>> operation);
        void RequestCancellation(bool force = false);
    }

    public sealed class OperationCoordinator : IOperationCoordinator
    {
        private readonly object _gate = new object();
        private ActiveOperation _active;

        public ActiveOperation Active { get => _active; private set { _active = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsBusy)); } }
        public bool IsBusy => Active != null;
        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<OperationResult<T>> RunAsync<T>(string title, bool canCancel,
            Func<IProgress<OperationProgress>, CancellationToken, Task<OperationResult<T>>> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            ActiveOperation current;
            lock (_gate)
            {
                if (Active != null)
                    return OperationResult<T>.Failure(LauncherErrorCode.Conflict,
                        $"Finish or cancel '{Active.Title}' before starting another operation.");

                current = new ActiveOperation
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = string.IsNullOrWhiteSpace(title) ? "Working" : title,
                    CanCancel = canCancel,
                    Cancellation = new CancellationTokenSource(),
                    Progress = new OperationProgress { Stage = OperationStage.Preparing, Message = "Preparing…" }
                };
                Active = current;
            }

            var progress = new Progress<OperationProgress>(value => current.Progress = value);
            try
            {
                return await operation(progress, current.Cancellation.Token);
            }
            catch (OperationCanceledException exception)
            {
                return OperationResult<T>.Failure(LauncherErrorCode.Cancelled, "Operation cancelled.", exception, current.Id);
            }
            catch (Exception exception)
            {
                return OperationResult<T>.Failure(LauncherErrorCode.Unexpected, exception.Message, exception, current.Id);
            }
            finally
            {
                current.Cancellation.Dispose();
                lock (_gate)
                {
                    if (ReferenceEquals(Active, current)) Active = null;
                }
            }
        }

        public void RequestCancellation(bool force = false)
        {
            lock (_gate)
            {
                if (Active == null || (!force && !Active.CanCancel) || (Active.IsCancelling && !force)) return;
                Active.IsCancelling = true;
                try
                {
                    Active.Cancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Completion won the race with a repeated/forced cancel click.
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
