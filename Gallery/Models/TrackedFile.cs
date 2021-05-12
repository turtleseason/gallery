using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gallery.Models
{
    public class TrackedFile : GalleryFile
    {
        //public IEnumerable<Tag>? Tags { get; init; } = null;  // Todo (add Tag class)

        public string? Description { get; set; }
    }
}