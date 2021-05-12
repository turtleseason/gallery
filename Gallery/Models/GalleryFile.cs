using System.IO;


namespace Gallery.Models
{
    // Todo: automatically pull in some file metadata?
    public class GalleryFile
    {
        // The default value (empty string) really shouldn't be used, it should be an error;
        // is there a way to do that while still taking advantage of Dapper's object mapping?
        // 
        // (Probably have to wait for this https://github.com/dotnet/csharplang/issues/3630)
        public string FullPath { get; init; } = string.Empty;

        string? _name;
        public string Name {
            get
            {
                if (_name == null)
                { 
                    _name = Path.GetFileName(FullPath);
                }
                return _name;
            }
        }

        public int? Width { get; } = null;

        public int? Height { get; } = null;

        // Thumbnail

        public override bool Equals(object? obj)
        {
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.FullPath == (obj as GalleryFile)!.FullPath;
        }

        public override int GetHashCode()
        {
            return FullPath.GetHashCode();
        }
    }
}
