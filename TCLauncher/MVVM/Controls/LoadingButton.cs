using System.Windows;

namespace TCLauncher.MVVM.Controls
{
    public class LoadingButton : System.Windows.Controls.Button
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(LoadingButton),
                new FrameworkPropertyMetadata(false));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }
    }
}
