using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TCLauncher.MVVM.Animations
{
    /// <summary>Applies the shared playful motion curve to an Expander template's ExpandSite presenter.</summary>
    public static class ExpanderMotion
    {
        private sealed class AnimationState { public int Version; }
        private static readonly Dictionary<Expander, AnimationState> States = new Dictionary<Expander, AnimationState>();

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(ExpanderMotion), new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (!(dependencyObject is Expander expander)) return;
            if ((bool)args.NewValue)
            {
                States[expander] = new AnimationState();
                expander.Loaded += Expander_OnLoaded;
                expander.Unloaded += Expander_OnUnloaded;
                expander.Expanded += Expander_OnExpanded;
                expander.Collapsed += Expander_OnCollapsed;
            }
            else
            {
                expander.Loaded -= Expander_OnLoaded;
                expander.Unloaded -= Expander_OnUnloaded;
                expander.Expanded -= Expander_OnExpanded;
                expander.Collapsed -= Expander_OnCollapsed;
                States.Remove(expander);
            }
        }

        private static void Expander_OnLoaded(object sender, RoutedEventArgs e)
        {
            var expander = (Expander)sender;
            var content = GetContentSite(expander);
            if (content == null) return;
            content.BeginAnimation(FrameworkElement.HeightProperty, null);
            content.BeginAnimation(UIElement.OpacityProperty, null);
            content.Height = double.NaN;
            content.Opacity = 1;
            content.Visibility = expander.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void Expander_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Expander expander && States.TryGetValue(expander, out var state)) state.Version++;
        }

        private static void Expander_OnExpanded(object sender, RoutedEventArgs e) => Animate((Expander)sender, true);
        private static void Expander_OnCollapsed(object sender, RoutedEventArgs e) => Animate((Expander)sender, false);

        private static void Animate(Expander expander, bool expanding)
        {
            if (!SystemParameters.ClientAreaAnimation)
            {
                SetFinalState(expander, expanding);
                return;
            }

            var content = GetContentSite(expander);
            if (content == null || !States.TryGetValue(expander, out var state)) return;
            var version = ++state.Version;
            var currentHeight = content.ActualHeight;

            content.BeginAnimation(FrameworkElement.HeightProperty, null);
            content.BeginAnimation(UIElement.OpacityProperty, null);

            if (expanding)
            {
                content.Visibility = Visibility.Visible;
                content.Height = double.NaN;
                content.Measure(new Size(Math.Max(0, expander.ActualWidth), double.PositiveInfinity));
                var targetHeight = Math.Max(0, content.DesiredSize.Height);
                content.Height = currentHeight > 0 ? currentHeight : 0;
                content.Opacity = currentHeight > 0 ? content.Opacity : 0;
                // Keep only a very small settle so the panel does not visibly bump
                // past its final height while expanding.
                var heightAnimation = MotionAnimations.CreatePlayful(content.Height, targetHeight * 1.003, targetHeight, 340);
                Complete(content, state, version, true, heightAnimation);
                content.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);
                content.BeginAnimation(UIElement.OpacityProperty,
                    MotionAnimations.CreatePlayful(content.Opacity, 0.82, 1, 280));
            }
            else
            {
                if (currentHeight <= 0) currentHeight = content.DesiredSize.Height;
                content.Height = currentHeight;
                var heightAnimation = MotionAnimations.CreatePlayful(currentHeight, currentHeight * 0.99, 0, 220);
                Complete(content, state, version, false, heightAnimation);
                content.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);
                content.BeginAnimation(UIElement.OpacityProperty,
                    MotionAnimations.CreatePlayful(content.Opacity, 0.28, 0, 220));
            }
        }

        private static void Complete(FrameworkElement content, AnimationState state, int version, bool expanded,
            DoubleAnimationUsingKeyFrames heightAnimation)
        {
            heightAnimation.Completed += (sender, args) =>
            {
                if (state.Version != version) return;
                content.BeginAnimation(FrameworkElement.HeightProperty, null);
                content.BeginAnimation(UIElement.OpacityProperty, null);
                content.Height = double.NaN;
                content.Opacity = 1;
                content.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        private static void SetFinalState(Expander expander, bool expanded)
        {
            var content = GetContentSite(expander);
            if (content == null) return;
            content.BeginAnimation(FrameworkElement.HeightProperty, null);
            content.BeginAnimation(UIElement.OpacityProperty, null);
            content.Height = double.NaN;
            content.Opacity = 1;
            content.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private static FrameworkElement GetContentSite(Expander expander) =>
            expander.Template?.FindName("ExpandSite", expander) as FrameworkElement;
    }
}
