using System.Collections.Generic;
using System.IO;

using Gallery.Models;

namespace Gallery.Services
{
    public interface IFileSystemService
    {
        IEnumerable<GalleryFile>? GetFiles(string path);

        IEnumerable<DriveInfo>? GetAvailableDrives();

        IEnumerable<string>? GetDirectories(string path);
    }
}