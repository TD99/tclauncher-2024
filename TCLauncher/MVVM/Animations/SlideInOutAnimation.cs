using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace TCLauncher.MVVM.Animations
{
    public class SlideInOutAnimation : Behavior<FrameworkElement>
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
            frameworkElement.RenderTransform = new TranslateTransform();
            var doubleAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(400)),
                From = frameworkElement.ActualHeight / 8, // Change from ActualWidth to ActualHeight
                To = 0,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("RenderTransform.(TranslateTransform.Y)")); // Change from X to Y
            Storyboard.SetTarget(doubleAnimation, frameworkElement);
            var storyboard = new Storyboard();
            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }
    }
}