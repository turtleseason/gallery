namespace Gallery.UI.Converters
{
    using System;
    using System.Globalization;

    using Avalonia.Data.Converters;
    using Avalonia.Media;

    public class ContrastTextConverter : IValueConverter
    {
        /// Returns either a black or white brush, whichever provides better contrast with the given color.
        /// Contrast is based on luminance (ignoring gamma):
        /// https://graphicdesign.stackexchange.com/a/77747
        /// https://www.w3.org/TR/WCAG20-TECHS/G17.html#G17-tests
        ///
        /// The input value should be a Color, SolidColorBrush, or a string that can be parsed into a color.
        /// Assumes the color is opaque (transparency isn't taken into account).
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color;

            switch (value)
            {
                case Color colorValue:
                    color = colorValue;
                    break;
                case SolidColorBrush brushValue:
                    color = brushValue.Color;
                    break;
                case string stringValue:
                    if (!Color.TryParse(stringValue, out color))
                    {
                        goto default;
                    }

                    break;
                default:
                    return Brushes.White;
            }

            double luminance = (0.2126 * color.R / 255) + (.7152 * color.G / 255) + (.0722 * color.B / 255);

            return luminance < 0.5 ? Brushes.White : Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
