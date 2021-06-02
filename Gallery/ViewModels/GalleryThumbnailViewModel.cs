namespace Gallery.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Media.Imaging;
    using Avalonia.Platform;

    using Gallery.Models;
    using Gallery.Services;

    using ReactiveUI;

    public class GalleryThumbnailViewModel : ViewModelBase
    {
        private static Bitmap? _defaultTrackedThumbnail;  // temp

        private GalleryFile _file;
        private Bitmap? _thumbnail;

        static GalleryThumbnailViewModel()  // temp
        {
            try
            {
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                using (Stream s = assets.Open(new Uri("avares://Gallery/Assets/thumbnail_placeholder.png")))
                {
                    _defaultTrackedThumbnail = Bitmap.DecodeToWidth(s, 200);
                }
            }
            catch (NullReferenceException)
            {
                // Happens in test runner (Avalonia.Current has nothing registered);
                // ignore for now since the placeholder image setup is temporary
            }
        }

        public GalleryThumbnailViewModel(GalleryFile file)
        {
            _file = file;

            // Todo: revisit this
            Observable.FromAsync(async () => await Task.Run(LoadThumbnail)).Subscribe(bitmap => Thumbnail = bitmap);
        }

        public GalleryFile File => _file;

        public Bitmap? Thumbnail { get => _thumbnail; set => this.RaiseAndSetIfChanged(ref _thumbnail, value); }

        public Bitmap? LoadThumbnail()
        {
            if (File.Thumbnail != null)
            {
                return ImageUtil.LoadBitmap(File.Thumbnail);
            }
            else if (File is TrackedFile)
            {
                // Known non-image file (could just return null & use the default file icon)
                return _defaultTrackedThumbnail;
            }
            else
            {
                return ImageUtil.LoadThumbnail(File.FullPath);
            }
        }
    }
}
