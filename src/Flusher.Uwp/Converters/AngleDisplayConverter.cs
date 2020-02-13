using System;
using Windows.UI.Xaml.Data;

namespace Flusher.Uwp.Converters
{
    public class AngleDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int angle)
            {
                return $"{angle}°";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string angleString)
            {
                var trimmedAngle = angleString.TrimEnd('°');
                return System.Convert.ToInt32(trimmedAngle);
            }

            return value;
        }
    }
}
