using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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

    public sealed class OverlayToast : INotifyPropertyChanged
    {
        private bool _isDismissing;
        private string _title;
        private string _message;
        private ToastTone _tone;
        private bool _isInProgress;
        private bool _canCancel;
        private double _progressPercent;

        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetField(ref _message, value);
        }

        public ToastTone Tone
        {
            get => _tone;
            set => SetField(ref _tone, value);
        }

        public string ActionText { get; set; }
        public ICommand ActionCommand { get; set; }
        public bool HasAction => ActionCommand != null;
        public bool IsPersistent { get; set; }
        public bool CanDismiss { get; set; }
        public ICommand DismissCommand { get; internal set; }
        public bool IsInProgress
        {
            get => _isInProgress;
            set => SetField(ref _isInProgress, value);
        }

        public bool CanCancel
        {
            get => _canCancel;
            set => SetField(ref _canCancel, value);
        }

        public ICommand CancelCommand { get; set; }
        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetField(ref _progressPercent, value);
        }
        public bool IsDismissing
        {
            get => _isDismissing;
            internal set
            {
                if (_isDismissing == value) return;
                _isDismissing = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDismissing)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class ToastStackPreview : INotifyPropertyChanged
    {
        private OverlayToast _toast;
        private double _offset;
        private double _left;
        private double _width;
        private double _opacity;
        private double _scale;

        public OverlayToast Toast { get => _toast; set => SetField(ref _toast, value); }
        public double Offset { get => _offset; set => SetField(ref _offset, value); }
        public double Left { get => _left; set => SetField(ref _left, value); }
        public double Width { get => _width; set => SetField(ref _width, value); }
        public double Opacity { get => _opacity; set => SetField(ref _opacity, value); }
        public double Scale { get => _scale; set => SetField(ref _scale, value); }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
        private bool _isToastStackExpanded;
        private bool _requiresToastStack;
        private bool _isToastAreaHovered;
        private ActiveOperation _activeOperation;
        private OverlayToast _operationToast;

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
        public ObservableCollection<ToastStackPreview> StackPreviewItems { get; } =
            new ObservableCollection<ToastStackPreview>();
        public ICollectionView ToastItems { get; }
        public bool IsToastStackExpanded
        {
            get => _isToastStackExpanded;
            private set
            {
                if (_isToastStackExpanded == value) return;
                _isToastStackExpanded = value;
                ToastItems.Refresh();
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasHiddenToasts));
                OnPropertyChanged(nameof(IsToastDismissalBlocked));
            }
        }
        public bool RequiresToastStack
        {
            get => _requiresToastStack;
            private set
            {
                if (_requiresToastStack == value) return;
                _requiresToastStack = value;
                ToastItems.Refresh();
                UpdateStackPreview();
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasHiddenToasts));
            }
        }
        public bool IsToastAreaHovered
        {
            get => _isToastAreaHovered;
            set
            {
                if (_isToastAreaHovered == value) return;
                _isToastAreaHovered = value;
                OnPropertyChanged();
            }
        }
        public bool IsToastDismissalBlocked => IsToastStackExpanded;
        public bool HasActiveNonDismissibleToast =>
            Toasts.Any(toast => toast.IsInProgress && !toast.CanDismiss);
        public OverlayToast ForegroundToast => GetTopToast();
        public bool HasHiddenToasts => RequiresToastStack && !IsToastStackExpanded && Toasts.Count > 1;
        public int HiddenToastCount => HasHiddenToasts ? Math.Max(0, Toasts.Count - 1) : 0;
        public ICommand ToggleToastStackCommand { get; }
        public ICommand CloseCommand { get; internal set; }
        public ICommand ConfirmCommand { get; internal set; }
        public ICommand CancelCommand { get; internal set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public OverlayHostViewModel()
        {
            ToastItems = new ListCollectionView((IList)Toasts);
            ToastItems.Filter = item => IsToastStackExpanded || !RequiresToastStack || ReferenceEquals(item, GetTopToast());
            ToggleToastStackCommand = new RelayCommand(_ =>
            {
                if (IsToastStackExpanded) CollapseToastStack();
                else ExpandToastStack();
            });
            Toasts.CollectionChanged += (sender, args) =>
            {
                if (Toasts.Count <= 1) IsToastStackExpanded = false;
                UpdateStackPreview();
                ToastItems.Refresh();
                OnPropertyChanged(nameof(HasHiddenToasts));
                OnPropertyChanged(nameof(HiddenToastCount));
                OnPropertyChanged(nameof(HasActiveNonDismissibleToast));
            };
        }

        public void ExpandToastStack()
        {
            if (HasHiddenToasts) IsToastStackExpanded = true;
        }

        public void CollapseToastStack() => IsToastStackExpanded = false;

        public void SetToastOverflow(bool requiresStack)
        {
            if (Toasts.Count <= 1) requiresStack = false;
            RequiresToastStack = requiresStack;
            if (!requiresStack && IsToastStackExpanded) IsToastStackExpanded = false;
            OnPropertyChanged(nameof(HasHiddenToasts));
        }

        public void SetCollapsedStackOffset(double offset)
        {
            if (StackPreviewItems.Count > 0)
                StackPreviewItems[0].Offset = Math.Max(0, offset);
        }

        public void SetOperationNotification(ActiveOperation operation, Action cancel)
        {
            if (ReferenceEquals(_activeOperation, operation)) return;

            if (_activeOperation != null) _activeOperation.PropertyChanged -= ActiveOperation_OnPropertyChanged;
            _activeOperation = operation;

            // Clear the reference before removing the old item. CollectionChanged handlers
            // immediately recalculate the foreground toast, so they must not see a stale
            // in-progress notification as the current top item.
            var previousOperationToast = _operationToast;
            _operationToast = null;
            if (previousOperationToast != null) Toasts.Remove(previousOperationToast);

            if (operation == null)
            {
                return;
            }

            _operationToast = new OverlayToast
            {
                IsInProgress = true,
                IsPersistent = true,
                CanDismiss = false,
                CancelCommand = new RelayCommand(_ => cancel?.Invoke())
            };
            Toasts.Insert(0, _operationToast);
            operation.PropertyChanged += ActiveOperation_OnPropertyChanged;
            UpdateOperationToast();
        }

        private void UpdateStackPreview()
        {
            var topToast = GetTopToast();
            var preview = RequiresToastStack
                ? Toasts.Where(toast => !ReferenceEquals(toast, topToast)).Reverse().Take(1).Reverse().ToList()
                : Enumerable.Empty<OverlayToast>().ToList();

            while (StackPreviewItems.Count > preview.Count)
                StackPreviewItems.RemoveAt(StackPreviewItems.Count - 1);
            for (var index = 0; index < preview.Count; index++)
            {
                var item = index < StackPreviewItems.Count ? StackPreviewItems[index] : new ToastStackPreview();
                item.Toast = preview[index];
                item.Offset = 20;
                item.Left = 5;
                item.Width = 330;
                item.Opacity = 0.80;
                item.Scale = 0.96;
                if (index >= StackPreviewItems.Count) StackPreviewItems.Add(item);
            }
        }

        private OverlayToast GetTopToast() => _operationToast ?? Toasts.LastOrDefault();

        private void ActiveOperation_OnPropertyChanged(object sender, PropertyChangedEventArgs e) => UpdateOperationToast();

        private void UpdateOperationToast()
        {
            if (_operationToast == null || _activeOperation == null) return;
            _operationToast.Title = _activeOperation.Title;
            _operationToast.Message = _activeOperation.Message;
            _operationToast.ProgressPercent = _activeOperation.Percent;
            _operationToast.CanCancel = _activeOperation.CanCancel && !_activeOperation.IsCancelling;
        }

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
            string actionText = null, Action action = null, bool persistent = false, bool canDismiss = true,
            TimeSpan? duration = null);
    }

    public sealed class OverlayService : IOverlayService
    {
        private readonly Dispatcher _dispatcher;
        private readonly System.Collections.Generic.Dictionary<OverlayToast, ToastTimerState> _toastTimers =
            new System.Collections.Generic.Dictionary<OverlayToast, ToastTimerState>();
        public OverlayHostViewModel Host { get; } = new OverlayHostViewModel();

        public OverlayService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            Host.CloseCommand = new RelayCommand(_ => Close());
            Host.ConfirmCommand = new RelayCommand(_ => Close(true));
            Host.CancelCommand = new RelayCommand(_ => Close(false));
            Host.PropertyChanged += Host_OnPropertyChanged;
            Host.Toasts.CollectionChanged += Toasts_OnCollectionChanged;
        }

        private void Host_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OverlayHostViewModel.IsToastStackExpanded) ||
                e.PropertyName == nameof(OverlayHostViewModel.IsToastAreaHovered) ||
                e.PropertyName == nameof(OverlayHostViewModel.RequiresToastStack) ||
                e.PropertyName == nameof(OverlayHostViewModel.HasActiveNonDismissibleToast))
                UpdateToastTimers();
        }

        private void Toasts_OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (OverlayToast toast in e.NewItems)
                    toast.PropertyChanged += Toast_OnPropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (OverlayToast toast in e.OldItems)
                {
                    toast.PropertyChanged -= Toast_OnPropertyChanged;
                    if (!_toastTimers.TryGetValue(toast, out var state)) continue;
                    state.Timer.Stop();
                    _toastTimers.Remove(toast);
                }
            }
            UpdateToastTimers();
        }

        private void Toast_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OverlayToast.IsInProgress) ||
                e.PropertyName == nameof(OverlayToast.CanDismiss))
                UpdateToastTimers();
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
            string actionText = null, Action action = null, bool persistent = false, bool canDismiss = true,
            TimeSpan? duration = null)
        {
            _dispatcher.Invoke(() =>
            {
                if (persistent) duration = null;
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
                    IsPersistent = persistent,
                    CanDismiss = canDismiss
                };
                toast.DismissCommand = canDismiss ? new RelayCommand(_ => DismissToast(toast)) : null;
                toast.ActionCommand = action == null
                    ? null
                    : new RelayCommand(_ =>
                    {
                        try
                        {
                            action();
                        }
                        finally
                        {
                            DismissToast(toast);
                        }
                    });
                Host.Toasts.Add(toast);
                // Persistence always wins over duration: persistent notices never start an auto-dismiss timer.
                if (toast.IsPersistent) return;
                var state = new ToastTimerState(toast, duration ?? TimeSpan.FromSeconds(5));
                state.Timer.Tick += (sender, args) =>
                {
                    state.Timer.Stop();
                    _toastTimers.Remove(toast);
                    DismissToast(toast);
                };
                _toastTimers[toast] = state;
                if (!IsToastTimerPaused(toast)) state.Start();
            });
        }

        private bool IsToastTimerPaused(OverlayToast toast)
        {
            if (Host.IsToastStackExpanded || Host.IsToastAreaHovered || Host.HasActiveNonDismissibleToast)
                return true;

            // In a collapsed overflow stack, keep hidden notifications readable when
            // they become foreground instead of letting their timers expire unseen.
            return Host.RequiresToastStack &&
                   !ReferenceEquals(toast, Host.ForegroundToast);
        }

        private void UpdateToastTimers()
        {
            foreach (var state in _toastTimers.Values)
            {
                if (IsToastTimerPaused(state.Toast))
                {
                    state.Pause();
                }
                else
                {
                    state.Start();
                }
            }
        }

        private void DismissToast(OverlayToast toast)
        {
            if (!toast.CanDismiss || toast.IsDismissing) return;
            toast.IsDismissing = true;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                Host.Toasts.Remove(toast);
            };
            timer.Start();
        }

        private sealed class ToastTimerState
        {
            private DateTime _dueAt;
            private TimeSpan _remaining;

            public ToastTimerState(OverlayToast toast, TimeSpan duration)
            {
                Toast = toast;
                _remaining = duration;
                Timer = new DispatcherTimer();
            }

            public OverlayToast Toast { get; }
            public DispatcherTimer Timer { get; }

            public void Start()
            {
                if (Timer.IsEnabled) return;
                _dueAt = DateTime.UtcNow + _remaining;
                Timer.Interval = _remaining <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : _remaining;
                Timer.Start();
            }

            public void Pause()
            {
                if (!Timer.IsEnabled) return;
                _remaining = _dueAt - DateTime.UtcNow;
                Timer.Stop();
            }
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
