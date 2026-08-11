using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace TCLauncher.MVVM.Animations
{
    /// <summary>
    /// Runs the shared directional crossfade used when replacing page content.
    /// </summary>
    public static class PageTransition
    {
        private static readonly TimeSpan EntryDelay = TimeSpan.FromMilliseconds(35);
        private static readonly Duration ExitDuration = new Duration(TimeSpan.FromMilliseconds(180));
        private static readonly Duration EntryDuration = new Duration(TimeSpan.FromMilliseconds(280));

        public static void Reset(FrameworkElement host, bool visible = true)
        {
            host.BeginAnimation(UIElement.OpacityProperty, null);
            host.Opacity = 1;
            host.Effect = null;
            host.RenderTransform = null;
            host.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public static void Begin(FrameworkElement outgoingHost, FrameworkElement incomingHost, int direction,
            Action completed, double durationScale = 1)
        {
            durationScale = Math.Max(0.1, durationScale);
            var entryDelay = ScaleTime(EntryDelay, durationScale);
            var exitDuration = Scale(ExitDuration, durationScale);
            var entryDuration = Scale(EntryDuration, durationScale);
            var outgoingTransform = CreateTransform(1, 0);
            var incomingTransform = CreateTransform(0.99, 12 * direction);
            outgoingHost.RenderTransform = outgoingTransform;
            incomingHost.RenderTransform = incomingTransform;
            outgoingHost.Opacity = 1;
            incomingHost.Opacity = 0;
            var outgoingBlur = new BlurEffect { Radius = 0 };
            var incomingBlur = new BlurEffect { Radius = 3.2 };
            outgoingHost.Effect = outgoingBlur;
            incomingHost.Effect = incomingBlur;

            Panel.SetZIndex(outgoingHost, 0);
            Panel.SetZIndex(incomingHost, 1);

            var exitEase = new CubicEase { EasingMode = EasingMode.EaseIn };
            outgoingHost.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0,
                Scale(TimeSpan.FromMilliseconds(160), durationScale)) { EasingFunction = exitEase });
            Animate((TranslateTransform)outgoingTransform.Children[1], TranslateTransform.YProperty, 0,
                -8 * direction, exitDuration, exitEase);
            Animate((ScaleTransform)outgoingTransform.Children[0], ScaleTransform.ScaleXProperty, 1, 0.995,
                exitDuration, exitEase);
            Animate((ScaleTransform)outgoingTransform.Children[0], ScaleTransform.ScaleYProperty, 1, 0.995,
                exitDuration, exitEase);
            Animate(outgoingBlur, BlurEffect.RadiusProperty, 0, 2.4, exitDuration, exitEase);

            var entryEase = new QuinticEase { EasingMode = EasingMode.EaseOut };
            var opacityAnimation = new DoubleAnimation(0, 1, entryDuration)
            {
                BeginTime = entryDelay,
                EasingFunction = entryEase
            };
            opacityAnimation.Completed += (_, _) => completed();
            incomingHost.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
            Animate((TranslateTransform)incomingTransform.Children[1], TranslateTransform.YProperty,
                12 * direction, 0, entryDuration, entryEase, entryDelay);
            Animate((ScaleTransform)incomingTransform.Children[0], ScaleTransform.ScaleXProperty,
                0.99, 1, entryDuration, entryEase, entryDelay);
            Animate((ScaleTransform)incomingTransform.Children[0], ScaleTransform.ScaleYProperty,
                0.99, 1, entryDuration, entryEase, entryDelay);
            Animate(incomingBlur, BlurEffect.RadiusProperty, 3.2, 0, entryDuration, entryEase, entryDelay);
        }

        private static Duration Scale(Duration duration, double multiplier) =>
            Scale(duration.TimeSpan, multiplier);

        private static Duration Scale(TimeSpan duration, double multiplier) =>
            new Duration(TimeSpan.FromTicks((long)(duration.Ticks * multiplier)));

        private static TimeSpan ScaleTime(TimeSpan duration, double multiplier) =>
            TimeSpan.FromTicks((long)(duration.Ticks * multiplier));

        private static void Animate(DependencyObject target, DependencyProperty property, double from, double to,
            Duration duration, IEasingFunction easing, TimeSpan? beginTime = null)
        {
            var animation = new DoubleAnimation(from, to, duration) { EasingFunction = easing };
            if (beginTime.HasValue) animation.BeginTime = beginTime;

            if (target is TranslateTransform translateTransform)
                translateTransform.BeginAnimation(property, animation);
            else if (target is ScaleTransform scaleTransform)
                scaleTransform.BeginAnimation(property, animation);
            else if (target is BlurEffect blurEffect)
                blurEffect.BeginAnimation(property, animation);
        }

        private static TransformGroup CreateTransform(double scale, double verticalOffset)
        {
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(scale, scale));
            transform.Children.Add(new TranslateTransform(0, verticalOffset));
            return transform;
        }
    }
}
