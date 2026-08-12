using System;
using System.Windows.Media.Animation;

namespace TCLauncher.MVVM.Animations
{
    /// <summary>Shared motion curves used by transient UI surfaces and disclosure controls.</summary>
    public static class MotionAnimations
    {
        public static DoubleAnimationUsingKeyFrames CreatePlayful(double from, double settle, double to, int milliseconds)
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                FillBehavior = FillBehavior.HoldEnd
            };
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(from, KeyTime.FromPercent(0), new CubicEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(settle, KeyTime.FromPercent(0.62), new CubicEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(to, KeyTime.FromPercent(1), new CubicEase { EasingMode = EasingMode.EaseInOut }));
            return animation;
        }

        public static DoubleAnimation CreateSoft(double from, double to, int milliseconds) =>
            new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(milliseconds))
            {
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
    }
}
