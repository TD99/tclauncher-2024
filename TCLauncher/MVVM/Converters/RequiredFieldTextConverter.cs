using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TCLauncher.MVVM.Converters
{
    public class RequiredFieldTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue) return null;

            string caption = (string)values[0];
            bool isRequired = (bool)values[1];

            if (isRequired)
                return caption + " *";

            return caption;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
