using System;
using Windows.UI.Xaml.Data;

namespace Flusher.Uwp.Converters
{
    internal class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
                return false;
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
                return false;
            else
                return true;
        }
    }
}
