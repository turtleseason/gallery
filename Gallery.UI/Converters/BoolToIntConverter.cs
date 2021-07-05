namespace Gallery.UI.Converters
{
    using System;
    using System.Globalization;

    using Avalonia.Data.Converters;

    public class BoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as bool? ?? false) ? 1 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as int? ?? 0) != 0;
        }
    }
}
