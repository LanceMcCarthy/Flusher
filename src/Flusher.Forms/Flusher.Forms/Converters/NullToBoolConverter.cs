using System;
using System.Globalization;
using Xamarin.Forms;

namespace Flusher.Forms.Converters
{
    public class NullToBoolConverter : IValueConverter
    {
        public bool IsInverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                if (IsInverted)
                {
                    return !string.IsNullOrEmpty(text);
                }
                else
                {
                    return string.IsNullOrEmpty(text);
                }
            }
            else
            {
                if (IsInverted)
                {
                    return value != null;
                }
                else
                {
                    return value == null;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
