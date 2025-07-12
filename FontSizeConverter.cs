using System;
using System.Globalization;
using System.Windows.Data;

namespace PresenterTimerApp
{
    public class FontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double size)
            {
                return size; // Adjust if scaling is needed
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}