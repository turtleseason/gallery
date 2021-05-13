namespace Gallery.Services
{
    using System.Collections.Generic;
    using System.IO;

    using Gallery.Models;

    public interface IFileSystemService
    {
        IEnumerable<GalleryFile>? GetFiles(string path);

        IEnumerable<DriveInfo>? GetAvailableDrives();

        IEnumerable<string>? GetDirectories(string path);
    }
}