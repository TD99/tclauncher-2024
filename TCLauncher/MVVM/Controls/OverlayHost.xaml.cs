using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TCLauncher.Core.Services;
using TCLauncher.MVVM.Animations;

namespace TCLauncher.MVVM.Controls
{
    public partial class OverlayHost : UserControl
    {
        private const double StackBackgroundHeight = 66;
        private const double StackBackgroundScale = 0.96;
        private const double StackRestingPeek = 10;
        private const double StackHoverTravel = 7;
        private const double StackHoverScale = 1.012;
        private readonly Dictionary<Border, PropertyChangedEventHandler> _toastHandlers =
            new Dictionary<Border, PropertyChangedEventHandler>();
        private readonly Dictionary<OverlayToast, double> _toastHeights =
            new Dictionary<OverlayToast, double>();
        private int _stackAnimationVersion;
        private bool _overflowUpdateQueued;

        public static readonly DependencyProperty OverlayServiceProperty =
            DependencyProperty.Register(nameof(OverlayService), typeof(IOverlayService), typeof(OverlayHost));

        public static readonly DependencyProperty NotificationTopInsetProperty =
            DependencyProperty.Register(nameof(NotificationTopInset), typeof(double), typeof(OverlayHost),
                new PropertyMetadata(20d, OnNotificationTopInsetChanged));

        public IOverlayService OverlayService
        {
            get => (IOverlayService)GetValue(OverlayServiceProperty);
            set => SetValue(OverlayServiceProperty, value);
        }

        public double NotificationTopInset
        {
            get => (double)GetValue(NotificationTopInsetProperty);
            set => SetValue(NotificationTopInsetProperty, value);
        }

        private IOverlayService CurrentService => OverlayService ?? AppServices.Overlays;
        private OverlayHostViewModel CurrentHost => CurrentService.Host;
        private bool UsesApplicationOperations => ReferenceEquals(CurrentService, AppServices.Overlays);
        private ScrollViewer ToastScrollViewer => ToastScrollShadow?.ScrollViewer;

        private static void OnNotificationTopInsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
            ((OverlayHost)sender).ApplyNotificationTopInset();

        public OverlayHost()
        {
            InitializeComponent();
            Loaded += OverlayHost_OnLoaded;
            Unloaded += OverlayHost_OnUnloaded;
            SizeChanged += OverlayHost_OnSizeChanged;
            PreviewMouseDown += OverlayHost_OnPreviewMouseDown;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OverlayHost_OnLoaded(object sender, RoutedEventArgs e)
        {
            Root.DataContext = CurrentHost;
            CurrentHost.PropertyChanged += Host_OnPropertyChanged;
            CurrentHost.Toasts.CollectionChanged += Toasts_OnCollectionChanged;
            if (UsesApplicationOperations) AppServices.Operations.PropertyChanged += Operations_OnPropertyChanged;
            SyncOperationNotification();
            UpdateStackPreviewVisibility(CurrentHost.HasHiddenToasts);
            UpdateRootHitTesting();
            ApplyNotificationTopInset();
            UpdateToastViewport();
            QueueOverflowUpdate();
        }

        private void OverlayHost_OnUnloaded(object sender, RoutedEventArgs e)
        {
            CurrentHost.PropertyChanged -= Host_OnPropertyChanged;
            CurrentHost.Toasts.CollectionChanged -= Toasts_OnCollectionChanged;
            if (UsesApplicationOperations) AppServices.Operations.PropertyChanged -= Operations_OnPropertyChanged;
        }

        private void OverlayHost_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateToastViewport();

        private void UpdateToastViewport()
        {
            if (ToastViewport == null || ActualHeight <= 0) return;
            ToastViewport.MaxHeight = ActualHeight * 0.75;
            QueueOverflowUpdate();
        }

        private void Toasts_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CurrentHost.IsToastStackExpanded && e.Action == NotifyCollectionChangedAction.Add)
                Dispatcher.BeginInvoke(new Action(() => ToastScrollViewer?.ScrollToTop()), DispatcherPriority.Background);
            QueueOverflowUpdate();
        }

        private void QueueOverflowUpdate()
        {
            if (_overflowUpdateQueued || !IsLoaded) return;
            _overflowUpdateQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _overflowUpdateQueued = false;
                if (ActualHeight <= 0) return;
                var availableHeight = Math.Max(1, ActualHeight * 0.75);
                var totalHeight = CurrentHost.Toasts.Sum(GetToastHeight);
                CurrentHost.SetToastOverflow(totalHeight > availableHeight + 2);
                UpdateStackPreviewVisibility(CurrentHost.HasHiddenToasts);
            }), DispatcherPriority.Loaded);
        }

        private double GetToastHeight(OverlayToast toast)
        {
            if (_toastHeights.TryGetValue(toast, out var measuredHeight) && measuredHeight > 0)
                return measuredHeight + 16;
            return 112;
        }

        private void Operations_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IOperationCoordinator.Active)) SyncOperationNotification();
        }

        private void SyncOperationNotification()
        {
            if (!UsesApplicationOperations) return;
            CurrentHost.SetOperationNotification(
                AppServices.Operations.Active,
                () => AppServices.Operations.RequestCancellation());
        }

        private void OverlayHost_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var host = CurrentHost;
            if (!host.IsToastStackExpanded || IsInside(e.OriginalSource as DependencyObject, ToastViewport)) return;
            host.CollapseToastStack();
        }

        private void Host_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OverlayHostViewModel.IsToastStackExpanded) ||
                e.PropertyName == nameof(OverlayHostViewModel.IsOpen))
                UpdateRootHitTesting();

            if (e.PropertyName == nameof(OverlayHostViewModel.IsToastStackExpanded))
            {
                var expanded = CurrentHost.IsToastStackExpanded;
                UpdateStackPreviewVisibility(!expanded);
                AnimateExpandedToastList(expanded);
                if (expanded)
                {
                    Dispatcher.BeginInvoke(new Action(() => ToastScrollViewer?.ScrollToTop()), DispatcherPriority.Background);
                }
                return;
            }

            if (e.PropertyName == nameof(OverlayHostViewModel.HasHiddenToasts))
            {
                UpdateStackPreviewVisibility(CurrentHost.HasHiddenToasts);
                Dispatcher.BeginInvoke(new Action(UpdateCollapsedStackGeometry), DispatcherPriority.Loaded);
                return;
            }

            if (e.PropertyName == nameof(OverlayHostViewModel.RequiresToastStack))
            {
                QueueOverflowUpdate();
                return;
            }

            if (e.PropertyName != nameof(OverlayHostViewModel.IsOpen) || !CurrentHost.IsOpen) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Surface.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                if (!SystemParameters.ClientAreaAnimation) return;
                Surface.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
                SurfaceTranslate.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(CurrentHost.IsDrawer ? 28 : 0, 0, TimeSpan.FromMilliseconds(190))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }), DispatcherPriority.Input);
        }

        private void UpdateRootHitTesting()
        {
            if (Root == null) return;
            Root.Background = CurrentHost.IsToastStackExpanded || CurrentHost.IsOpen
                ? Brushes.Transparent
                : null;
        }

        private void ApplyNotificationTopInset()
        {
            if (ToastPanel != null)
                ToastPanel.Margin = new Thickness(0, Math.Max(0, NotificationTopInset), 20, 0);
        }

        private void AnimateExpandedToastList(bool expanded)
        {
            var transforms = GetWritableTransformGroup(ToastScrollShadow);
            if (transforms == null) return;
            var scale = transforms.Children.OfType<ScaleTransform>().FirstOrDefault();
            if (scale == null) return;

            ToastScrollShadow.BeginAnimation(OpacityProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            var startScale = expanded ? 0.985 : 0.995;
            var startOpacity = expanded ? 0.78 : 0.86;
            ToastScrollShadow.Opacity = startOpacity;
            scale.ScaleX = startScale;
            scale.ScaleY = startScale;

            ToastScrollShadow.BeginAnimation(OpacityProperty,
                MotionAnimations.CreateSoft(startOpacity, 1, expanded ? 260 : 210));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                MotionAnimations.CreateSoft(startScale, 1, expanded ? 300 : 230));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                MotionAnimations.CreateSoft(startScale, 1, expanded ? 300 : 230));
        }

        private void UpdateStackPreviewVisibility(bool show)
        {
            if (StackPreview == null) return;
            var transforms = GetWritableTransformGroup(StackPreview);
            var scale = transforms?.Children.OfType<ScaleTransform>().FirstOrDefault();
            var translate = transforms?.Children.OfType<TranslateTransform>().FirstOrDefault();
            var version = ++_stackAnimationVersion;
            if (show)
            {
                StackPreview.Visibility = Visibility.Visible;
                StackPreview.Opacity = 0;
                if (scale != null) { scale.ScaleX = 1; scale.ScaleY = 1; }
                if (translate != null) translate.Y = 6;

                StackPreview.BeginAnimation(OpacityProperty,
                    MotionAnimations.CreatePlayful(0, 0.82, 1, 300));
                scale?.BeginAnimation(ScaleTransform.ScaleXProperty,
                    MotionAnimations.CreatePlayful(0.985, 0.998, 1, 320));
                scale?.BeginAnimation(ScaleTransform.ScaleYProperty,
                    MotionAnimations.CreatePlayful(0.985, 0.998, 1, 320));
                translate?.BeginAnimation(TranslateTransform.YProperty,
                    MotionAnimations.CreatePlayful(6, 1.5, 0, 320));
                return;
            }

            StackPreview.BeginAnimation(OpacityProperty,
                MotionAnimations.CreatePlayful(1, 0.35, 0, 220));
            scale?.BeginAnimation(ScaleTransform.ScaleXProperty,
                MotionAnimations.CreatePlayful(scale?.ScaleX ?? 1, 0.99, 0.985, 220));
            scale?.BeginAnimation(ScaleTransform.ScaleYProperty,
                MotionAnimations.CreatePlayful(scale?.ScaleY ?? 1, 0.99, 0.985, 220));
            translate?.BeginAnimation(TranslateTransform.YProperty,
                MotionAnimations.CreatePlayful(translate?.Y ?? 0, 3.5, 4, 220));
            var hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
            hideTimer.Tick += (sender, args) =>
            {
                hideTimer.Stop();
                if (version == _stackAnimationVersion) StackPreview.Visibility = Visibility.Collapsed;
            };
            hideTimer.Start();
        }

        private void ToastViewport_OnMouseEnter(object sender, MouseEventArgs e)
        {
            CurrentHost.IsToastAreaHovered = true;
            if (CurrentHost.IsToastStackExpanded || StackPreview.Visibility != Visibility.Visible) return;
            if (GetWritableTransformGroup(StackPreview) is TransformGroup transforms)
            {
                var translate = transforms.Children.OfType<TranslateTransform>().FirstOrDefault();
                var scale = transforms.Children.OfType<ScaleTransform>().FirstOrDefault();
                translate?.BeginAnimation(TranslateTransform.YProperty,
                    MotionAnimations.CreateSoft(translate.Y, StackHoverTravel, 230));
                scale?.BeginAnimation(ScaleTransform.ScaleXProperty,
                    MotionAnimations.CreateSoft(scale.ScaleX, StackHoverScale, 230));
                scale?.BeginAnimation(ScaleTransform.ScaleYProperty,
                    MotionAnimations.CreateSoft(scale.ScaleY, StackHoverScale, 230));
            }
        }

        private void ToastViewport_OnMouseLeave(object sender, MouseEventArgs e)
        {
            CurrentHost.IsToastAreaHovered = false;
            if (GetWritableTransformGroup(StackPreview) is TransformGroup transforms)
            {
                var translate = transforms.Children.OfType<TranslateTransform>().FirstOrDefault();
                var scale = transforms.Children.OfType<ScaleTransform>().FirstOrDefault();
                translate?.BeginAnimation(TranslateTransform.YProperty,
                    MotionAnimations.CreateSoft(translate.Y, 0, 230));
                scale?.BeginAnimation(ScaleTransform.ScaleXProperty,
                    MotionAnimations.CreateSoft(scale.ScaleX, 1, 230));
                scale?.BeginAnimation(ScaleTransform.ScaleYProperty,
                    MotionAnimations.CreateSoft(scale.ScaleY, 1, 230));
            }
        }

        private void ToastViewport_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var host = CurrentHost;
            if (!host.IsToastStackExpanded && host.HasHiddenToasts &&
                !IsInside(e.OriginalSource as DependencyObject, typeof(Button)))
            {
                host.ExpandToastStack();
                e.Handled = true;
            }
        }

        private void ToastBorder_OnLoaded(object sender, RoutedEventArgs e)
        {
            var border = sender as Border;
            var toast = border?.DataContext as OverlayToast;
            if (border == null || toast == null || _toastHandlers.ContainsKey(border)) return;

            var transforms = GetWritableTransformGroup(border);
            var scale = transforms?.Children.Count > 0 ? transforms.Children[0] as ScaleTransform : null;
            var translate = transforms?.Children.Count > 1 ? transforms.Children[1] as TranslateTransform : null;
            if (scale == null || translate == null) return;

            border.Opacity = 0;
            scale.ScaleX = 0.985;
            scale.ScaleY = 0.985;
            translate.Y = 0;
            border.BeginAnimation(OpacityProperty, MotionAnimations.CreatePlayful(0, 0.82, 1, 280));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, MotionAnimations.CreatePlayful(0.985, 0.998, 1, 340));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, MotionAnimations.CreatePlayful(0.985, 0.998, 1, 340));

            PropertyChangedEventHandler handler = (changeSender, args) =>
            {
                if (args.PropertyName != nameof(OverlayToast.IsDismissing)) return;
                AnimateToastOut(border, scale, translate);
            };
            _toastHandlers[border] = handler;
            toast.PropertyChanged += handler;
            border.Unloaded += ToastBorder_OnUnloaded;
            border.SizeChanged += ToastBorder_OnSizeChanged;
            _toastHeights[toast] = border.ActualHeight;
            UpdateCollapsedStackGeometry(border);
            QueueOverflowUpdate();
        }

        private void ToastBorder_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Border border && border.DataContext is OverlayToast toast && border.ActualHeight > 0)
            {
                _toastHeights[toast] = border.ActualHeight;
                UpdateCollapsedStackGeometry(border);
                QueueOverflowUpdate();
            }
        }

        private void UpdateCollapsedStackGeometry()
        {
            var foreground = CurrentHost.ForegroundToast;
            var border = _toastHandlers.Keys.FirstOrDefault(candidate =>
                ReferenceEquals(candidate.DataContext, foreground));
            if (border != null) UpdateCollapsedStackGeometry(border);
        }

        private void UpdateCollapsedStackGeometry(Border foregroundBorder)
        {
            var host = CurrentHost;
            if (!host.HasHiddenToasts || host.IsToastStackExpanded || foregroundBorder.ActualHeight <= 0 ||
                !ReferenceEquals(foregroundBorder.DataContext, host.ForegroundToast)) return;

            var foregroundBottom = 4 + foregroundBorder.ActualHeight;
            var renderedBackgroundHeight = StackBackgroundHeight * StackBackgroundScale;
            var backgroundTop = foregroundBottom + StackRestingPeek - renderedBackgroundHeight;
            host.SetCollapsedStackOffset(backgroundTop);

            // Reserve the resting peek plus the complete hover travel so neither border can be clipped.
            StackPreview.Height = foregroundBottom + StackRestingPeek + StackHoverTravel + 4;
        }

        private void ToastBorder_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var host = CurrentHost;
            if (!host.HasHiddenToasts || IsInside(e.OriginalSource as DependencyObject, typeof(Button))) return;
            host.ExpandToastStack();
            e.Handled = true;
        }

        private void ToastBorder_OnUnloaded(object sender, RoutedEventArgs e)
        {
            var border = sender as Border;
            if (border == null || !_toastHandlers.TryGetValue(border, out var handler)) return;
            if (border.DataContext is OverlayToast toast) toast.PropertyChanged -= handler;
            if (border.DataContext is OverlayToast measuredToast) _toastHeights.Remove(measuredToast);
            border.SizeChanged -= ToastBorder_OnSizeChanged;
            _toastHandlers.Remove(border);
        }

        private static void AnimateToastOut(Border border, ScaleTransform scale, TranslateTransform translate)
        {
            translate.BeginAnimation(TranslateTransform.YProperty, null);
            translate.Y = 0;
            border.BeginAnimation(OpacityProperty, MotionAnimations.CreatePlayful(1, 0.28, 0, 220));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, MotionAnimations.CreatePlayful(1, 0.97, 0.985, 220));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, MotionAnimations.CreatePlayful(1, 0.97, 0.985, 220));
        }

        private static TransformGroup GetWritableTransformGroup(FrameworkElement element)
        {
            if (element?.RenderTransform is not TransformGroup transforms) return null;
            if (!transforms.IsFrozen && transforms.Children.All(child => !child.IsFrozen)) return transforms;

            var writable = transforms.Clone();
            element.RenderTransform = writable;
            return writable;
        }

        private static bool IsInside(DependencyObject source, DependencyObject ancestor)
        {
            for (var current = source; current != null; current = VisualTreeHelper.GetParent(current))
                if (ReferenceEquals(current, ancestor)) return true;
            return false;
        }

        private static bool IsInside(DependencyObject source, Type ancestorType)
        {
            for (var current = source; current != null; current = VisualTreeHelper.GetParent(current))
                if (ancestorType.IsInstanceOfType(current)) return true;
            return false;
        }

        private void Backdrop_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CurrentService.DismissFromOutside();
            e.Handled = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || !CurrentHost.IsOpen) return;
            CurrentService.DismissFromOutside();
            e.Handled = true;
        }
    }

}
