using System;
using System.Globalization;
using System.Windows.Data;

namespace PresenterTimerApp
{
    public class FontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderValue && parameter is string param)
            {
                if (param == "Message")
                {
                    double minSliderValue = 20;  // Minimum slider value
                    double maxSliderValue = 100; // Maximum slider value
                    double defaultValue = 40;    // Default/middle value
                    
                    // Map slider value to width percentage:
                    // Min (20) -> 40% of width
                    // Default (40) -> 75% of width
                    // Max (100) -> 95% of width
                    
                    double percentage;
                    if (sliderValue <= defaultValue)
                    {
                        // Linear mapping from min to default
                        percentage = 40.0 + (sliderValue - minSliderValue) * (75.0 - 40.0) / (defaultValue - minSliderValue);
                    }
                    else
                    {
                        // Linear mapping from default to max
                        percentage = 75.0 + (sliderValue - defaultValue) * (95.0 - 75.0) / (maxSliderValue - defaultValue);
                    }
                    
                    return percentage / 100.0; // Return as a factor for binding
                }
            }
            
            return value; // For other parameters, return the original value
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}