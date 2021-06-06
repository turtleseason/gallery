namespace Gallery.Models
{
    using System.IO;

    public class GalleryFile
    {
        private string _fullPath = string.Empty;

        // This should be required to initialize if/when that becomes possible
        // (https://github.com/dotnet/csharplang/issues/3630)
        public string FullPath
        {
            get => _fullPath;
            init
            {
                _fullPath = value;
                Name = Path.GetFileName(value);
                Directory = Path.GetDirectoryName(value)!;
            }
        }

        public string Name { get; protected init; } = string.Empty;
        public string Directory { get; protected init; } = string.Empty;

        public string? Thumbnail { get; set; } = null;

        ////public override bool Equals(object? obj)
        ////{
        ////    if (obj == null || this.GetType() != obj.GetType())
        ////    {
        ////        return false;
        ////    }

        ////    return this.FullPath == (obj as GalleryFile)!.FullPath;
        ////}

        ////public override int GetHashCode()
        ////{
        ////    return FullPath.GetHashCode();
        ////}
    }
}
