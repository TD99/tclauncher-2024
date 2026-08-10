using System;
using System.Windows;
using System.Windows.Input;

namespace TCLauncher.Core
{
    public static class InputModality
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(InputModality), new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly DependencyPropertyKey IsKeyboardInputPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsKeyboardInput", typeof(bool), typeof(InputModality), new PropertyMetadata(false));

        public static readonly DependencyProperty IsKeyboardInputProperty =
            IsKeyboardInputPropertyKey.DependencyProperty;

        public static void SetIsEnabled(DependencyObject element, bool value) =>
            element.SetValue(IsEnabledProperty, value);

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static bool GetIsKeyboardInput(DependencyObject element) =>
            (bool)element.GetValue(IsKeyboardInputProperty);

        private static void OnIsEnabledChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            if (!(dependencyObject is Window window)) return;
            if ((bool)args.NewValue)
            {
                window.PreviewKeyDown += OnPreviewKeyDown;
                window.PreviewMouseDown += OnPreviewMouseDown;
                window.Deactivated += OnDeactivated;
            }
            else
            {
                window.PreviewKeyDown -= OnPreviewKeyDown;
                window.PreviewMouseDown -= OnPreviewMouseDown;
                window.Deactivated -= OnDeactivated;
            }
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Tab || args.Key == Key.Left || args.Key == Key.Right || args.Key == Key.Up ||
                args.Key == Key.Down)
                ((Window)sender).SetValue(IsKeyboardInputPropertyKey, true);
        }

        private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs args) =>
            ((Window)sender).SetValue(IsKeyboardInputPropertyKey, false);

        private static void OnDeactivated(object sender, EventArgs args) =>
            ((Window)sender).SetValue(IsKeyboardInputPropertyKey, false);
    }
}