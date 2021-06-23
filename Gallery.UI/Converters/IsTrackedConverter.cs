namespace Gallery.UI.Converters
{
    using System;
    using System.Globalization;

    using Avalonia.Data.Converters;

    using Gallery.Entities;

    public class IsTrackedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as TrackedFile != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
