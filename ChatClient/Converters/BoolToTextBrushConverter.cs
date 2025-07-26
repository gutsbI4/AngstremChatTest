using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ChatClient.Converters
{
    // Конвертер для изменения цвета текста в зависимости от того, является ли он плейсхолдером
    public class BoolToTextBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaceholder)
            {
                return isPlaceholder
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")) // TextLightBrush
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827")); // TextBrush
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}