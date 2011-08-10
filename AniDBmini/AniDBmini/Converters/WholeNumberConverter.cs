using System;
using System.Globalization;
using System.Windows.Data;

namespace AniDBmini
{
    [ValueConversion(typeof(double), typeof(double))]
    class WholeNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Round((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
