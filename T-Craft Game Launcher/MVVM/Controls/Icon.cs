using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace T_Craft_Game_Launcher.Controls
{
    public class Icon : Label
    {
        public static readonly DependencyProperty UnicodeProperty = DependencyProperty.Register(
            nameof(Unicode), typeof(string), typeof(Icon), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color), typeof(string), typeof(Icon), new PropertyMetadata(default(string)));

        public string Unicode
        {
            get => (string)GetValue(UnicodeProperty);
            set => SetValue(UnicodeProperty, value);
        }

        public string Color
        {
            get => (string)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public string Identifier { get; set; } = "";

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == UnicodeProperty)
            {
                var codePoint = int.Parse(Unicode, System.Globalization.NumberStyles.HexNumber);
                Content = char.ConvertFromUtf32(codePoint);
            } else if (e.Property == ColorProperty)
            {
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(Color);
            }

            Foreground = Brushes.White;
        }

    }
}
