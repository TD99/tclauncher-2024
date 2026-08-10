using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TCLauncher.Core.Services
{
    public enum OverlayKind
    {
        Sheet,
        Drawer
    }

    public enum ToastTone
    {
        Success,
        Warning,
        Error
    }

    public sealed class OverlayToast
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public ToastTone Tone { get; set; }
        public string ActionText { get; set; }
        public ICommand ActionCommand { get; set; }
        public bool HasAction => ActionCommand != null;
        public bool IsPersistent { get; set; }
    }

    public sealed class OverlaySurface
    {
        public OverlayKind Kind { get; set; }
        public string Title { get; set; }
        public object Content { get; set; }
        public string PrimaryText { get; set; }
        public string SecondaryText { get; set; }
        public bool IsConfirmation { get; set; }
        public bool AllowOutsideDismiss { get; set; }
        internal TaskCompletionSource<bool> Completion { get; set; }
        internal IInputElement PreviousFocus { get; set; }
    }

    public sealed class OverlayHostViewModel : INotifyPropertyChanged
    {
        private OverlaySurface _current;

        public OverlaySurface Current
        {
            get => _current;
            internal set
            {
                _current = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOpen));
                OnPropertyChanged(nameof(IsDrawer));
            }
        }

        public bool IsOpen => Current != null;
        public bool IsDrawer => Current?.Kind == OverlayKind.Drawer;
        public ObservableCollection<OverlayToast> Toasts { get; } = new ObservableCollection<OverlayToast>();
        public ICommand CloseCommand { get; internal set; }
        public ICommand ConfirmCommand { get; internal set; }
        public ICommand CancelCommand { get; internal set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public interface IOverlayService
    {
        OverlayHostViewModel Host { get; }
        Task ShowSheetAsync(string title, object content, bool allowOutsideDismiss = true);

        Task<bool> ConfirmAsync(string title, object content, string primaryText = "Continue",
            string secondaryText = "Cancel");

        void ShowDrawer(string title, object content);
        void Close(bool accepted = false);
        void DismissFromOutside();

        void ShowToast(string title, string message, ToastTone tone = ToastTone.Success,
            string actionText = null, Action action = null, bool persistent = false);
    }

    public sealed class OverlayService : IOverlayService
    {
        private readonly Dispatcher _dispatcher;
        public OverlayHostViewModel Host { get; } = new OverlayHostViewModel();

        public OverlayService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            Host.CloseCommand = new RelayCommand(_ => Close());
            Host.ConfirmCommand = new RelayCommand(_ => Close(true));
            Host.CancelCommand = new RelayCommand(_ => Close(false));
        }

        public Task ShowSheetAsync(string title, object content, bool allowOutsideDismiss = true)
        {
            return ShowAsync(new OverlaySurface
            {
                Kind = OverlayKind.Sheet,
                Title = title,
                Content = content,
                AllowOutsideDismiss = allowOutsideDismiss
            });
        }

        public async Task<bool> ConfirmAsync(string title, object content, string primaryText = "Continue",
            string secondaryText = "Cancel")
        {
            var surface = new OverlaySurface
            {
                Kind = OverlayKind.Sheet,
                Title = title,
                Content = content,
                PrimaryText = primaryText,
                SecondaryText = secondaryText,
                IsConfirmation = true,
                AllowOutsideDismiss = false
            };
            await ShowAsync(surface);
            return surface.Completion.Task.IsCompleted && surface.Completion.Task.Result;
        }

        public void ShowDrawer(string title, object content)
        {
            _dispatcher.Invoke(() => Open(new OverlaySurface
            {
                Kind = OverlayKind.Drawer,
                Title = title,
                Content = content,
                AllowOutsideDismiss = true
            }));
        }

        public void Close(bool accepted = false)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke(() => Close(accepted));
                return;
            }

            var current = Host.Current;
            if (current == null) return;
            Host.Current = null;
            current.Completion?.TrySetResult(accepted);
            _dispatcher.BeginInvoke(new Action(() => current.PreviousFocus?.Focus()), DispatcherPriority.Input);
        }

        public void DismissFromOutside()
        {
            if (Host.Current?.AllowOutsideDismiss == true) Close();
        }

        public void ShowToast(string title, string message, ToastTone tone = ToastTone.Success,
            string actionText = null, Action action = null, bool persistent = false)
        {
            _dispatcher.Invoke(() =>
            {
                if (persistent)
                {
                    var existing = Host.Toasts.FirstOrDefault(toast =>
                        toast.IsPersistent && string.Equals(toast.Title, title, StringComparison.Ordinal));
                    if (existing != null) return;
                }

                var toast = new OverlayToast
                {
                    Title = title,
                    Message = message,
                    Tone = tone,
                    ActionText = actionText,
                    ActionCommand = action == null ? null : new RelayCommand(_ => action()),
                    IsPersistent = persistent
                };
                Host.Toasts.Add(toast);
                if (persistent) return;
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (sender, args) =>
                {
                    timer.Stop();
                    Host.Toasts.Remove(toast);
                };
                timer.Start();
            });
        }

        private Task ShowAsync(OverlaySurface surface)
        {
            surface.Completion = new TaskCompletionSource<bool>();
            _dispatcher.Invoke(() => Open(surface));
            return surface.Completion.Task;
        }

        private void Open(OverlaySurface surface)
        {
            if (Host.Current != null) Close();
            surface.PreviousFocus = Keyboard.FocusedElement;
            surface.Completion = surface.Completion ?? new TaskCompletionSource<bool>();
            Host.Current = surface;
        }
    }
}