namespace Gallery.UI.DesignData
{
    using System.Collections.ObjectModel;
    using System.Linq;

    using Gallery.Entities;
    using Gallery.UI.ViewModels;

    public class DesignGalleryViewModel
    {
        public DesignGalleryViewModel()
        {
            var items = new GalleryFile[]
            {
                new TrackedFile() { FullPath = @"C:\folder\tracked_file.gif" },
                new TrackedFile() { FullPath = @"C:\folder\another_tracked_file.png" },
                new GalleryFile() { FullPath = @"C:\folder\file1.png" },
                new GalleryFile() { FullPath = @"C:\folder\file2.txt" },
                new GalleryFile() { FullPath = @"C:\folder\very_long_file_name_helloooooooooo.gif" },
                new GalleryFile() { FullPath = @"C:\folder\even_longer_file_name_so_long_that_it_can_span_three_lines.gif" },
                new GalleryFile() { FullPath = @"C:\folder\file4.gif" },
            };

            (items[0] as TrackedFile)!.Tags.Add(new Tag("Tag", "Value"));
            (items[0] as TrackedFile)!.Tags.Add(new Tag("Also Tag", group: new TagGroup("Group1", "#eebbee")));
            (items[0] as TrackedFile)!.Tags.Add(new Tag("Looooooooooooooooooooooooong tag"));

            Items = new ObservableCollection<GalleryThumbnailViewModel>(items.Select(file =>
                new GalleryThumbnailViewModel(file)));
        }

        public ObservableCollection<GalleryThumbnailViewModel> Items { get; }
    }
}
