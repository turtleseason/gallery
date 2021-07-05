namespace Gallery.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Media.Imaging;

    public static class ImageUtil
    {
        public static readonly int ThumbnailSize = 200;

        private static readonly ISet<string> _knownImageExtensions = GetKnownExtensions();

        private static readonly object _lock = new();

        public static async Task<Bitmap?> LoadBitmap(string path)
        {
            // Save some time by skipping files unlikely to be successfully decoded
            if (!_knownImageExtensions.Contains(Path.GetExtension(path).ToLower()))
            {
                return null;
            }

            return await Task.Run(() =>
            {
                // Bitmap.DecodeToWidth/Height throws NullReferenceException, Bitmap ctor throws ArgumentException
                try
                {
                    lock (_lock)
                    {
                        using (Stream s = File.OpenRead(path))
                        {
                            return new Bitmap(s);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Not an image (that we know how to parse)
                    return null;
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unexpected exception in LoadBitmap:\n{e}");
                    return null;
                }
            });
        }

        public static async Task<Bitmap?> LoadThumbnail(string path)
        {
            // Save some time by skipping files unlikely to be successfully decoded
            if (!_knownImageExtensions.Contains(Path.GetExtension(path).ToLower()))
            {
                return null;
            }

            return await Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        using (Stream s = File.OpenRead(path))
                        {
                            return Bitmap.DecodeToHeight(s, 200);
                        }
                    }
                }
                catch (NullReferenceException)
                {
                    // Not an image (that we know how to parse)
                    return null;
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Unexpected exception in LoadThumbnail:\n{e}");
                    return null;
                }
            });
        }

        public static async Task SaveThumbnail(Bitmap bitmap, string savePath, PixelSize thumbnailSize)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    using (Stream s = File.Create(savePath))
                    {
                        bitmap.CreateScaledBitmap(thumbnailSize)
                            .Save(s);
                    }
                }
            });
        }

        // Todo: is there a way to get specifically the formats that Avalonia/Skia can decode?
        // (And/or leverage System.Drawing.Imaging if it supports formats Avalonia.Media.Imaging doesn't?)
        private static ISet<string> GetKnownExtensions()
        {
            var extensions = ImageCodecInfo.GetImageDecoders()
                .SelectMany(codec => codec.FilenameExtension?.Split(';') ?? Array.Empty<string>())
                .Select(x => x.ToLower().Trim('*'))
                .ToHashSet();
            Trace.TraceInformation($"Known image file extensions: {string.Join(',', extensions)}");
            return extensions;
        }
    }
}
