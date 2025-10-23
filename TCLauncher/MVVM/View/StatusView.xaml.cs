using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TCLauncher.MVVM.ViewModel;

namespace TCLauncher.MVVM.View
{
    public partial class StatusView : UserControl
    {
        private readonly StatusViewModel _vm;

        public StatusView()
        {
            InitializeComponent();
            _vm = (StatusViewModel)DataContext;
        }

        private void RefreshBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            // Create a RotateTransform and apply it to the button's Content
            RotateTransform rotateTransform = new RotateTransform(0);
            if (btn.Content is UIElement contentElement)
            {
                contentElement.RenderTransform = rotateTransform;
                contentElement.RenderTransformOrigin = new Point(0.5, 0.5); // to rotate around the center
            }

            // Create a DoubleAnimation to animate the RotateTransform
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)), // Set duration as per your requirement
                FillBehavior = FillBehavior.Stop
            };

            // When animation completed, reset angle to zero (for repeated clicks)
            animation.Completed += (s, a) => rotateTransform.Angle = 0;

            // Start the animation
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);

            _vm.RefreshData();
        }
    }
}
