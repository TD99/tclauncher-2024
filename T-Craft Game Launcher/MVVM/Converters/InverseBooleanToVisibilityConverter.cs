using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System;

namespace T_Craft_Game_Launcher.MVVM.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string thresholdString && double.TryParse(thresholdString, out double threshold))
            {
                return width < threshold ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}