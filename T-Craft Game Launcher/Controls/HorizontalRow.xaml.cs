using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace T_Craft_Game_Launcher.Controls
{
    public partial class HorizontalRow : UserControl
    {
        public static readonly DependencyProperty FillProperty = 
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(HorizontalRow), new PropertyMetadata(Brushes.Black));
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(HorizontalRow), new PropertyMetadata(1.0));
        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }

        public HorizontalRow()
        {
            InitializeComponent();
        }
    }
}
