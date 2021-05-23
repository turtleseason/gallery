namespace Gallery.Models
{
    using Gallery.Services;

    public readonly struct Tag
    {
        public Tag(string name, string? value = null, TagGroup? group = null)
        {
            Name = name;
            Value = value;
            Group = group ?? new TagGroup(IDatabaseService.DefaultTagGroup);
        }

        /// Name should always be initialized to a non-null (and non-empty/whitespace) value
        public string Name { get; init; }

        public string? Value { get; init; }

        public TagGroup Group { get; init; }

        public string FullString => ToString();

        public override string ToString()
        {
            return Name + (Value != null ? $": {Value}" : string.Empty);
        }
    }
}