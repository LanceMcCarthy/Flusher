using System;
using Windows.UI.Xaml.Data;

namespace Flusher.Uwp.Converters
{
    public class PulseWidthDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double pulseWidth)
            {
                return $"{pulseWidth} µs";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string pulseWidthString)
            {
                var trimmedPulseWidthString = pulseWidthString.TrimEnd(' ', 'µ', 's');
                return System.Convert.ToDouble(trimmedPulseWidthString);
            }

            return value;
        }
    }
}
