using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace TCLauncher.MVVM.Controls
{
    public enum ScrollShadowOrientation
    {
        Vertical,
        Horizontal,
        All
    }

    public partial class ScrollShadowLayout : UserControl
    {
        private const double ScrollTolerance = 1;
        private ScrollViewer _scrollViewer;

        public static readonly DependencyProperty ScrollContentProperty =
            DependencyProperty.Register(nameof(ScrollContent), typeof(object), typeof(ScrollShadowLayout),
                new PropertyMetadata(null, OnScrollContentChanged));

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(ScrollShadowOrientation), typeof(ScrollShadowLayout),
                new PropertyMetadata(ScrollShadowOrientation.Vertical, OnVisualPropertyChanged));

        public static readonly DependencyProperty ShadowSizeProperty =
            DependencyProperty.Register(nameof(ShadowSize), typeof(double), typeof(ScrollShadowLayout),
                new PropertyMetadata(12d, OnVisualPropertyChanged));

        public static readonly DependencyProperty ShadowColorProperty =
            DependencyProperty.Register(nameof(ShadowColor), typeof(Brush), typeof(ScrollShadowLayout),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(185, 21, 29, 40)), OnVisualPropertyChanged));

        public static readonly DependencyProperty ShadowsEnabledProperty =
            DependencyProperty.Register(nameof(ShadowsEnabled), typeof(bool), typeof(ScrollShadowLayout),
                new PropertyMetadata(true, OnVisualPropertyChanged));

        public object ScrollContent
        {
            get => GetValue(ScrollContentProperty);
            set => SetValue(ScrollContentProperty, value);
        }

        public ScrollShadowOrientation Orientation
        {
            get => (ScrollShadowOrientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public double ShadowSize
        {
            get => (double)GetValue(ShadowSizeProperty);
            set => SetValue(ShadowSizeProperty, value);
        }

        public Brush ShadowColor
        {
            get => (Brush)GetValue(ShadowColorProperty);
            set => SetValue(ShadowColorProperty, value);
        }

        public bool ShadowsEnabled
        {
            get => (bool)GetValue(ShadowsEnabledProperty);
            set => SetValue(ShadowsEnabledProperty, value);
        }

        public ScrollViewer ScrollViewer => _scrollViewer;

        public ScrollShadowLayout()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private static void OnScrollContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var layout = (ScrollShadowLayout)sender;
            layout.DetachScrollViewer();
            layout.Dispatcher.BeginInvoke(new Action(layout.AttachScrollViewer), DispatcherPriority.Loaded);
        }

        private static void OnVisualPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
            ((ScrollShadowLayout)sender).UpdateShadows();

        private void OnLoaded(object sender, RoutedEventArgs e) => AttachScrollViewer();

        private void OnUnloaded(object sender, RoutedEventArgs e) => DetachScrollViewer();

        private void AttachScrollViewer()
        {
            if (!IsLoaded || _scrollViewer != null) return;
            _scrollViewer = FindScrollViewer(ScrollContent as DependencyObject);
            if (_scrollViewer == null)
            {
                Dispatcher.BeginInvoke(new Action(AttachScrollViewer), DispatcherPriority.Loaded);
                return;
            }

            _scrollViewer.ScrollChanged += ScrollViewer_OnScrollChanged;
            _scrollViewer.SizeChanged += ScrollViewer_OnSizeChanged;
            UpdateShadows();
            Dispatcher.BeginInvoke(new Action(UpdateShadows), DispatcherPriority.Loaded);
        }

        private void DetachScrollViewer()
        {
            if (_scrollViewer == null) return;
            _scrollViewer.ScrollChanged -= ScrollViewer_OnScrollChanged;
            _scrollViewer.SizeChanged -= ScrollViewer_OnSizeChanged;
            _scrollViewer = null;
            HideAllShadows();
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateShadows();

        private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateShadows();

        private void UpdateShadows()
        {
            if (!IsLoaded || _scrollViewer == null) return;
            if (!ShadowsEnabled)
            {
                HideAllShadows();
                return;
            }

            _scrollViewer.Resources[SystemColors.ControlBrushKey] =
                new SolidColorBrush(Color.FromRgb(0x20, 0x2A, 0x38));
            var showVerticalShadows = Orientation == ScrollShadowOrientation.Vertical ||
                                      Orientation == ScrollShadowOrientation.All;
            var showHorizontalShadows = Orientation == ScrollShadowOrientation.Horizontal ||
                                        Orientation == ScrollShadowOrientation.All;
            var hasVerticalScrollbar = _scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible;
            var hasHorizontalScrollbar = _scrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible;
            var verticalScrollbar = FindScrollBar(_scrollViewer, System.Windows.Controls.Orientation.Vertical);
            var horizontalScrollbar = FindScrollBar(_scrollViewer, System.Windows.Controls.Orientation.Horizontal);
            var verticalScrollbarWidth = hasVerticalScrollbar
                ? Math.Max(1, verticalScrollbar?.ActualWidth ?? SystemParameters.VerticalScrollBarWidth)
                : 0;
            var horizontalScrollbarHeight = hasHorizontalScrollbar
                ? Math.Max(1, horizontalScrollbar?.ActualHeight ?? SystemParameters.HorizontalScrollBarHeight)
                : 0;
            SetShadow(TopShadow, showVerticalShadows && _scrollViewer.VerticalOffset > ScrollTolerance,
                ShadowSide.Top, verticalScrollbarWidth, horizontalScrollbarHeight);
            SetShadow(BottomShadow, showVerticalShadows &&
                _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight < _scrollViewer.ExtentHeight - ScrollTolerance,
                ShadowSide.Bottom, verticalScrollbarWidth, horizontalScrollbarHeight);
            SetShadow(LeftShadow, showHorizontalShadows && _scrollViewer.HorizontalOffset > ScrollTolerance,
                ShadowSide.Left, verticalScrollbarWidth, horizontalScrollbarHeight);
            SetShadow(RightShadow, showHorizontalShadows &&
                _scrollViewer.HorizontalOffset + _scrollViewer.ViewportWidth < _scrollViewer.ExtentWidth - ScrollTolerance,
                ShadowSide.Right, verticalScrollbarWidth, horizontalScrollbarHeight);

        }

        private void SetShadow(Border shadow, bool visible, ShadowSide side, double verticalScrollbarWidth,
            double horizontalScrollbarHeight)
        {
            shadow.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!visible) return;

            shadow.Width = side == ShadowSide.Left || side == ShadowSide.Right ? ShadowSize : double.NaN;
            shadow.Height = side == ShadowSide.Top || side == ShadowSide.Bottom ? ShadowSize : double.NaN;
            shadow.Margin = side == ShadowSide.Top
                ? new Thickness(0, 0, verticalScrollbarWidth, 0)
                : side == ShadowSide.Bottom
                    ? new Thickness(0, 0, verticalScrollbarWidth, horizontalScrollbarHeight)
                    : side == ShadowSide.Left
                        ? new Thickness(0, 0, 0, horizontalScrollbarHeight)
                        : new Thickness(0, 0, verticalScrollbarWidth, horizontalScrollbarHeight);
            shadow.Background = CreateGradient(side);
        }

        private LinearGradientBrush CreateGradient(ShadowSide side)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = side == ShadowSide.Top ? new Point(0, 0) :
                    side == ShadowSide.Bottom ? new Point(0, 1) :
                    side == ShadowSide.Left ? new Point(0, 0) : new Point(1, 0),
                EndPoint = side == ShadowSide.Top ? new Point(0, 1) :
                    side == ShadowSide.Bottom ? new Point(0, 0) :
                    side == ShadowSide.Left ? new Point(1, 0) : new Point(0, 0)
            };
            var solidColor = ShadowColor as SolidColorBrush;
            brush.GradientStops.Add(new GradientStop(solidColor?.Color ?? Colors.Transparent, 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
            return brush;
        }

        private void HideAllShadows()
        {
            TopShadow.Visibility = Visibility.Collapsed;
            BottomShadow.Visibility = Visibility.Collapsed;
            LeftShadow.Visibility = Visibility.Collapsed;
            RightShadow.Visibility = Visibility.Collapsed;
        }

        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            if (root == null) return null;
            if (root is ScrollViewer scrollViewer) return scrollViewer;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var result = FindScrollViewer(VisualTreeHelper.GetChild(root, index));
                if (result != null) return result;
            }
            return null;
        }

        private static ScrollBar FindScrollBar(DependencyObject root, Orientation orientation)
        {
            if (root == null) return null;
            if (root is ScrollBar scrollBar && scrollBar.Orientation == orientation) return scrollBar;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                var result = FindScrollBar(VisualTreeHelper.GetChild(root, index), orientation);
                if (result != null) return result;
            }
            return null;
        }

        private enum ShadowSide
        {
            Top,
            Bottom,
            Left,
            Right
        }
    }
}
