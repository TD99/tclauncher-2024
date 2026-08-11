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
            UpdateButtonContent();
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

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == SpoilerTextProperty || e.Property == SpoilerContentProperty)
                UpdateButtonContent();
        }

        private void UpdateButtonContent()
        {
            if (_button != null) _button.Content = _isContentVisible ? SpoilerContent : SpoilerText;
        }

        private void SpoilerControl_Click(object sender, RoutedEventArgs e)
        {
            if (_isContentVisible)
            {
                _isContentVisible = false;
            }
            else
            {
                _isContentVisible = true;
            }

            UpdateButtonContent();
        }
    }
}
