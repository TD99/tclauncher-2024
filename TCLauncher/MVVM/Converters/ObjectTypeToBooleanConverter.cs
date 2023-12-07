using System;
using System.Globalization;
using System.Windows.Data;

namespace TCLauncher.MVVM.Converters
{
    public class ObjectTypeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is Type type)
            {
                return value?.GetType() == type;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}