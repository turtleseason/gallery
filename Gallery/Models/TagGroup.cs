namespace Gallery.Models
{
    public readonly struct TagGroup
    {
        public TagGroup(string name, string? color = null)
        {
            Name = name;
            Color = color;
        }

        public string Name { get; init; }

        public string? Color { get; init; }
    }
}
