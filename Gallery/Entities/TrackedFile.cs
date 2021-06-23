namespace Gallery.Entities
{
    using System.Collections.Generic;

    public class TrackedFile : GalleryFile
    {
        public TrackedFile()
        {
            Tags = new HashSet<Tag>();
        }

        public ISet<Tag> Tags { get; }

        public string? Description { get; set; }
    }
}
