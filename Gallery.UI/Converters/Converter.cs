namespace Gallery.UI.Converters
{
    public class Converter
    {
        public static readonly IsTrackedConverter IsTracked = new();

        public static readonly ContrastTextConverter ContrastText = new();

        public static readonly BoolToIntConverter BoolToInt = new();
    }
}
