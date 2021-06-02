namespace Gallery.Models
{
    using System;
    using System.IO;

    public class GalleryFile
    {
        private string? _name;

        // The default value (empty string) really shouldn't be used, it should be an error;
        // is there a way to do that while still taking advantage of Dapper's object mapping?
        //
        // (Probably have to wait for this https://github.com/dotnet/csharplang/issues/3630)
        public string FullPath { get; init; } = string.Empty;

        public string Name => _name ??= Path.GetFileName(FullPath);

        public int? Width { get; set; } = null;

        public int? Height { get; set; } = null;

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
