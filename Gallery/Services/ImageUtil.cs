namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;

    using Avalonia.Media.Imaging;

    public static class ImageUtil
    {
        public static readonly int ThumbnailSize = 200;

        private static readonly ISet<string> _knownImageExtensions = GetKnownExtensions();

        public static Bitmap? LoadBitmap(string path)
        {
            // Save some time by skipping files unlikely to be successfully decoded
            if (!_knownImageExtensions.Contains(Path.GetExtension(path).ToLower()))
            {
                return null;
            }

            // Bitmap.DecodeToWidth/Height throws NullReferenceException, Bitmap ctor throws ArgumentException
            try
            {
                using (Stream s = File.OpenRead(path))
                {
                    return new Bitmap(s);
                }
            }
            catch (ArgumentException)
            {
                // Not an image (that we know how to parse)
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.GetType() + " " + e.Message);
                return null;
            }
        }

        public static Bitmap? LoadThumbnail(string path)
        {
            // Save some time by skipping files unlikely to be successfully decoded
            if (!_knownImageExtensions.Contains(Path.GetExtension(path).ToLower()))
            {
                return null;
            }

            try
            {
                using (Stream s = File.OpenRead(path))
                {
                    return Bitmap.DecodeToHeight(s, 200);
                }
            }
            catch (NullReferenceException)
            {
                // Not an image (that we know how to parse)
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.GetType() + " " + e.Message);
                return null;
            }
        }

        // Todo: is there a way to get specifically the formats that Avalonia/Skia can decode?
        // (And/or leverage System.Drawing.Imaging if it supports formats Avalonia.Media.Imaging doesn't?)
        private static ISet<string> GetKnownExtensions()
        {
            var set = ImageCodecInfo.GetImageDecoders()
                .SelectMany(codec => codec.FilenameExtension?.Split(';') ?? Array.Empty<string>())
                .Select(x => x.ToLower().Trim('*'))
                .ToHashSet();
            Debug.WriteLine(string.Join(',', set));
            return set;
        }
    }
}
