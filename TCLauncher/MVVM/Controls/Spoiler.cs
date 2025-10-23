using System.Windows;
using System.Windows.Controls;

namespace TCLauncher.MVVM.Controls
{
    public class Spoiler : UserControl
    {
        public static readonly DependencyProperty SpoilerTextProperty = DependencyProperty.Register(
            nameof(SpoilerText), typeof(string), typeof(Spoiler), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty SpoilerContentProperty = DependencyProperty.Register(
            nameof(SpoilerContent), typeof(object), typeof(Spoiler), new PropertyMetadata(default(object)));

        private bool _isContentVisible;
        private readonly Button _button;

        public Spoiler()
        {
            _button = new Button();
            _button.Click += SpoilerControl_Click;
            Content = _button;
        }

        public string SpoilerText
        {
            get => (string)GetValue(SpoilerTextProperty);
            set => SetValue(SpoilerTextProperty, value);
        }

        public object SpoilerContent
        {
            get => GetValue(SpoilerContentProperty);
            set => SetValue(SpoilerContentProperty, value);
        }

        private void SpoilerControl_Click(object sender, RoutedEventArgs e)
        {
            if (_isContentVisible)
            {
                _button.Content = SpoilerText;
                _isContentVisible = false;
            }
            else
            {
                _button.Content = SpoilerContent;
                _isContentVisible = true;
            }
        }
    }
}