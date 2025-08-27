using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace TCLauncher.MVVM.Animations
{
    public class SlideRightIn : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var frameworkElement = this.AssociatedObject as FrameworkElement;
            if (frameworkElement == null) return;

            // Set the initial position off-screen to the right
            frameworkElement.RenderTransform = new TranslateTransform { X = SystemParameters.PrimaryScreenWidth };

            var doubleAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                From = SystemParameters.PrimaryScreenWidth,
                To = 0, // End at the element's original position
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("RenderTransform.(TranslateTransform.X)"));
            Storyboard.SetTarget(doubleAnimation, frameworkElement);

            var storyboard = new Storyboard();
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }
    }
}