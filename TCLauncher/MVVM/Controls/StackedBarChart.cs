using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TCLauncher.Models;

namespace TCLauncher.MVVM.Controls
{
    public class StackedBarChart : UserControl
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data), typeof(List<StackedBarItem>), typeof(StackedBarChart), new PropertyMetadata(OnDataChanged));
        public static readonly DependencyProperty DescriptionShownProperty = DependencyProperty.Register(
            nameof(DescriptionShown), typeof(bool), typeof(StackedBarChart), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty ChartHeightProperty = DependencyProperty.Register(
            nameof(ChartHeight), typeof(double), typeof(StackedBarChart), new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public List<StackedBarItem> Data
        {
            get => (List<StackedBarItem>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public bool DescriptionShown
        {
            get => (bool)GetValue(DescriptionShownProperty);
            set => SetValue(DescriptionShownProperty, value);
        }

        public double ChartHeight
        {
            get => (double)GetValue(ChartHeightProperty);
            set => SetValue(ChartHeightProperty, value);
        }

        public StackedBarChart()
        {
            this.SizeChanged += (sender, args) => GenerateChart();
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StackedBarChart)d;
            control.GenerateChart();
        }

        private void GenerateChart()
        {
            var root = new StackPanel() { Orientation = Orientation.Vertical, Height = Height };
            var chartPanel = new StackPanel() { Orientation = Orientation.Horizontal, Height = ChartHeight };
            var descriptionPanel = new StackPanel() { Orientation = Orientation.Vertical };

            // Handle null Data
            if (Data == null)
            {
                var loadingIcon = new FontAwesome.WPF.FontAwesome
                {
                    Icon = FontAwesome.WPF.FontAwesomeIcon.SquareOutline,
                    Spin = true,
                    FontSize = 40,
                    Foreground = new SolidColorBrush(Colors.White),
                    Cursor = Cursors.Wait,
                    RenderTransformOrigin = new Point(0.5, 0.5) // Set the rotation origin to the center
                };
                root.Children.Add(loadingIcon);
                Content = root;
                return;
            }

            var total = Data.Sum(item => item.Value);

            foreach (var rectangle in Data.Select(item => new Rectangle()
                     {
                         Width = Math.Max(0, (item.Value / total) * ActualWidth), // Ensure width is not negative
                         Height = ChartHeight,
                         Fill = new SolidColorBrush(item.Color ?? Colors.Blue),
                         ToolTip = new ToolTip { Content = $"{item.Name}: {item.Value} {item.Unit}", Style = (Style)FindResource("ModernToolTip") }
                     }))
            {
                chartPanel.Children.Add(rectangle);
            }

            if (DescriptionShown)
            {
                foreach (var description in Data.Select(item => new TextBlock
                         {
                             Text = $"{item.Name}: {item.Value} {item.Unit}",
                             Margin = new Thickness(5) // Add some margin for better readability
                         }))
                {
                    descriptionPanel.Children.Add(description);
                }
            }

            root.Children.Add(chartPanel);
            root.Children.Add(descriptionPanel);
            Content = root;
        }

    }
}