using System;
using System.Globalization;
using System.Windows.Data;

namespace PresenterTimerApp
{
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height && parameter is string param && double.TryParse(param, out double percentage))
            {
                return height * percentage;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}