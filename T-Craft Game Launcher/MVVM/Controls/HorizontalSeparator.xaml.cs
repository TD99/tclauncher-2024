using System.Windows;
using System.Windows.Media;

namespace T_Craft_Game_Launcher.MVVM.Controls
{
    public partial class HorizontalSeparator
    {
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(HorizontalSeparator),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(155, 255, 255, 255))));

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            nameof(Thickness),
            typeof(double),
            typeof(HorizontalSeparator),
            new PropertyMetadata(0.5));

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
