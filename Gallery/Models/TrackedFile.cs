namespace Gallery.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Avalonia;
    using Avalonia.Media.Imaging;
    using Avalonia.Platform;

    public class TrackedFile : GalleryFile
    {
        // Temporarily share a single thumbnail for all files
        private static Bitmap? _tempThumbnail;

        static TrackedFile()
        {
            try
            {
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                using (Stream s = assets.Open(new Uri("avares://Gallery/Assets/thumbnail_placeholder.png")))
                {
                    _tempThumbnail = Bitmap.DecodeToWidth(s, 200);
                }
            }
            catch (NullReferenceException)
            {
                // Happens in test runner (Avalonia.Current has nothing registered);
                // ignore for now since the placeholder image setup is temporary
            }
        }

        public TrackedFile()
        {
            Thumbnail = _tempThumbnail;
            Tags = new HashSet<Tag>();
        }

        public ISet<Tag> Tags { get; }

        public string? Description { get; set; }
    }
}