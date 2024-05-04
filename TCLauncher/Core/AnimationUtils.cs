using System.Windows.Media.Animation;
using System.Windows;
using System;
using System.Windows.Controls;

namespace TCLauncher.Core
{
    /// <summary>
    /// Provides utility methods for animating UserControls.
    /// </summary>
    public static class AnimationUtils
    {
        /// <summary>
        /// Animates the opacity of a UserControl from 0 to 1 over a duration of one second.
        /// </summary>
        /// <param name="control">The UserControl whose opacity is to be animated.</param>
        public static void AnimateOpacity(Control control)
        {
            var da = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };

            var sb = new Storyboard();
            sb.Children.Add(da);

            Storyboard.SetTargetProperty(da, new PropertyPath("Opacity"));
            Storyboard.SetTarget(da, control);

            sb.Begin();
        }
    }
}
