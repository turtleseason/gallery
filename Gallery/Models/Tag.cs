namespace Gallery.Models
{
    public struct Tag
    {
        public Tag(string name, string? value = null)
        {
            Name = name;
            Value = value;
        }

        // Name should always be initialized to a non-null value
        public string Name { get; init; }

        public string? Value { get; init; }

        public string FullString => ToString();

        public override string ToString()
        {
            return Name + (Value != null ? $": {Value}" : string.Empty);
        }
    }
}