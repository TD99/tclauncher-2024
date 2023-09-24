using System.Windows;
using System.Windows.Media;

namespace T_Craft_Game_Launcher.Controls
{
    public partial class HorizontalSeparator
    {
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(HorizontalSeparator),
            new PropertyMetadata(Brushes.Black));

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            nameof(Thickness),
            typeof(double),
            typeof(HorizontalSeparator),
            new PropertyMetadata(1.0));

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public HorizontalSeparator()
        {
            InitializeComponent();
        }
    }
}
