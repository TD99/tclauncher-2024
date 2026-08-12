using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TCLauncher.MVVM.Animations;

namespace TCLauncher.Controls.Gallery
{
    [Story("Page Transitions", Description = "Explore the shared directional crossfade used throughout the launcher.")]
    public partial class PageTransitionsPage : UserControl
    {
        private readonly string[] _pageTitles = { "Overview", "Activity", "Details" };
        private ContentControl _activeHost;
        private ContentControl _inactiveHost;
        private int _currentPage;
        private int _pendingPage = -1;
        private bool _transitioning;

        public PageTransitionsPage()
        {
            InitializeComponent();
            _activeHost = CurrentDemoHost;
            _inactiveHost = PreviousDemoHost;
            _activeHost.Content = CreateDemoPage(0);
            SpeedSlider.Value = 1;
        }

        private void Previous_OnClick(object sender, RoutedEventArgs e) => RequestPage(_currentPage - 1);

        private void Next_OnClick(object sender, RoutedEventArgs e) => RequestPage(_currentPage + 1);

        private void RequestPage(int page)
        {
            page = (page + _pageTitles.Length) % _pageTitles.Length;
            if (_transitioning)
            {
                _pendingPage = page;
                return;
            }

            StartTransition(page);
        }

        private void StartTransition(int page)
        {
            if (page == _currentPage) return;

            _transitioning = true;
            var outgoingHost = _activeHost;
            var incomingHost = _inactiveHost;
            var direction = page > _currentPage ? 1 : -1;

            PageTransition.Reset(outgoingHost);
            PageTransition.Reset(incomingHost);
            incomingHost.Content = CreateDemoPage(page);
            PageTransition.Begin(outgoingHost, incomingHost, direction,
                () => CompleteTransition(outgoingHost, incomingHost, page), SpeedSlider.Value);
        }

        private void CompleteTransition(ContentControl outgoingHost, ContentControl incomingHost, int page)
        {
            outgoingHost.Content = null;
            PageTransition.Reset(outgoingHost, false);
            PageTransition.Reset(incomingHost);
            _activeHost = incomingHost;
            _inactiveHost = outgoingHost;
            _currentPage = page;
            _transitioning = false;

            if (_pendingPage >= 0 && _pendingPage != _currentPage)
            {
                var nextPage = _pendingPage;
                _pendingPage = -1;
                StartTransition(nextPage);
            }
        }

        private void SpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedLabel != null) SpeedLabel.Text = $"{e.NewValue:0.0}x";
        }

        private Border CreateDemoPage(int page)
        {
            var border = new Border
            {
                Padding = new Thickness(28),
                Background = new SolidColorBrush(Color.FromRgb(34, 45, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(52, 66, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };

            var content = new StackPanel();
            content.Children.Add(new TextBlock
            {
                FontFamily = TryFindResource("PixelifySans") as FontFamily ?? new FontFamily("Segoe UI"),
                FontSize = 25,
                FontWeight = FontWeights.SemiBold,
                Text = _pageTitles[page]
            });
            content.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(174, 184, 199)),
                Text = "Old content fades and recedes while the next page settles into place."
            });
            content.Children.Add(new Border
            {
                Height = 72,
                Margin = new Thickness(0, 28, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(42, 58, 77)),
                CornerRadius = new CornerRadius(8)
            });
            border.Child = content;
            return border;
        }
    }
}
