namespace Gallery.Models
{
    using System;
    using System.Collections.Generic;

    public class TrackedFile : GalleryFile
    {
        public TrackedFile()
        {
            Thumbnail = new Uri("avares://Gallery/Assets/thumbnail_placeholder.png");
            Tags = new HashSet<Tag>();
        }

        public ISet<Tag> Tags { get; }

        public string? Description { get; set; }
    }
}