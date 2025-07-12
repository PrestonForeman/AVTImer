using System;
using System.Globalization;
using System.Windows.Data;

namespace PresenterTimerApp
{
    public class AspectRatioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string ratio)
            {
                var parts = ratio.Split('/');
                if (parts.Length == 2 && double.TryParse(parts[0], out double numerator) && double.TryParse(parts[1], out double denominator))
                {
                    return width * (denominator / numerator);
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}