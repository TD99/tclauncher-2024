using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TCLauncher.MVVM.Converters
{
    public sealed class WizardStepVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var step = value is int ? (int)value : 0;
            var mode = parameter as string;
            var visible = mode == "Last" ? step == 5 : mode == "NotLast" ? step < 5 : step > 0;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}