using System;
using System.Globalization;
using System.Windows.Data;

namespace ChatClient.Converters
{
    public class BoolToOnlineStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOnline)
            {
                return isOnline ? "онлайн" : "не в сети";
            }
            return "не в сети";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
