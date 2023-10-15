using System;
using System.Windows;
using System.Windows.Media;

namespace T_Craft_Game_Launcher.MVVM.Controls
{
    public partial class SkeletonLoader
    {
        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.Register(nameof(Rotation), typeof(double), typeof(SkeletonLoader), new PropertyMetadata(0.0));

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(SkeletonLoader), new PropertyMetadata(Brushes.LightGray));

        public static readonly DependencyProperty BorderRadiusProperty =
            DependencyProperty.Register(nameof(BorderRadius), typeof(double), typeof(SkeletonLoader), new PropertyMetadata(0.0));

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register(nameof(AnimationDuration), typeof(Duration), typeof(SkeletonLoader), new PropertyMetadata(new Duration(TimeSpan.FromSeconds(1))));

        public double Rotation
        {
            get => (double)GetValue(RotationProperty);
            set => SetValue(RotationProperty, value);
        }

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public double BorderRadius
        {
            get => (double)GetValue(BorderRadiusProperty);
            set => SetValue(BorderRadiusProperty, value);
        }

        public Duration AnimationDuration
        {
            get => (Duration)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        public SkeletonLoader()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }

}
