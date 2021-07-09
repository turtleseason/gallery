namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Media.Imaging;
    using Avalonia.Platform;

    using Gallery.Entities;
    using Gallery.Util;

    using ReactiveUI;

    public class GalleryThumbnailViewModel : ViewModelBase
    {
        private GalleryFile _file;
        private Bitmap? _thumbnail;
        private bool _isSelected = false;

        public GalleryThumbnailViewModel(GalleryFile file)
        {
            _file = file;
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
                return null;
            }
            else
            {
                return await ImageUtil.LoadThumbnail(File.FullPath);
            }
        }
    }
}
