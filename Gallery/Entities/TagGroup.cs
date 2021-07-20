namespace Gallery.Entities
{
    public readonly struct TagGroup
    {
        public static readonly string DefaultGroupName = "None";
        public static readonly string DefaultGroupColor = "#7f077f";

        public TagGroup(string name, string? color = null)
        {
            Name = name;
            Color = color ?? DefaultGroupColor;
        }

        public string Name { get; init; }

        public string Color { get; init; }

        public override string ToString()
        {
            return $"TagGroup {Name} ({Color})";
        }
    }
}
