using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Gallery.Models
{
    public class GalleryFile
    {
        // Todo: get file metadata
        public GalleryFile (string path)
        {
            FullPath = path;
            Name = Path.GetFileName(path);
        }

        public string FullPath { get; }
        
        public string Name { get; }

        public int? Width { get; } = null;

        public int? Height { get; } = null;

        // Thumbnail
    }
}
