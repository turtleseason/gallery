namespace Gallery.ViewModels
{
    using System;
    using System.IO;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Media.Imaging;
    using Avalonia.Platform;

    using Gallery.Models;

    using ReactiveUI;

    public class GalleryThumbnailViewModel : ViewModelBase
    {
        private GalleryFile _file;
        private Bitmap? _thumbnail;

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
            if (File.Thumbnail == null)
            {
                try
                {
                    using (Stream s = System.IO.File.OpenRead(File.FullPath))
                    {
                        return Bitmap.DecodeToHeight(s, 200);
                    }
                }
                catch (NullReferenceException)
                {
                    // Not an image (that we know how to parse)
                    return null;
                }
            }
            else
            {
                try
                {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    using (Stream s = assets.Open(File.Thumbnail))
                    {
                        return Bitmap.DecodeToWidth(s, 200);
                    }
                }
                catch (NullReferenceException)
                {
                    // Happens in test runner (Avalonia.Current has nothing registered);
                    // ignore for now since the placeholder image setup is temporary
                    return null;
                }
            }
        }
    }
}
