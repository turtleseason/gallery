namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Media.Imaging;
    using Avalonia.Platform;

    using Gallery.Entities;
    using Gallery.Util;

    using ReactiveUI;

    public class GalleryThumbnailViewModel : ViewModelBase
    {
        private static Bitmap? _defaultTrackedThumbnail;  // temp

        private GalleryFile _file;
        private Bitmap? _thumbnail;
        private bool _isSelected = false;

        static GalleryThumbnailViewModel()  // temp
        {
            try
            {
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                using (Stream s = assets.Open(new Uri("avares://Gallery.UI/Assets/thumbnail_placeholder.png")))
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

            Observable.FromAsync(LoadThumbnail, RxApp.MainThreadScheduler)
                .Subscribe(bitmap => Thumbnail = bitmap);
        }

        public GalleryFile File => _file;

        // Use this to avoid binding errors from binding directly to File.Tags with untracked files
        public ISet<Tag>? Tags => (File as TrackedFile)?.Tags;

        public Bitmap? Thumbnail { get => _thumbnail; set => this.RaiseAndSetIfChanged(ref _thumbnail, value); }

        public bool IsSelected { get => _isSelected; set => this.RaiseAndSetIfChanged(ref _isSelected, value); }

        public async Task<Bitmap?> LoadThumbnail()
        {
            if (File.Thumbnail != null)
            {
                return await ImageUtil.LoadBitmap(File.Thumbnail);
            }
            else if (File is TrackedFile)
            {
                // Known non-image file (could just return null & use the default file icon)
                return _defaultTrackedThumbnail;
            }
            else
            {
                return await ImageUtil.LoadThumbnail(File.FullPath);
            }
        }
    }
}
