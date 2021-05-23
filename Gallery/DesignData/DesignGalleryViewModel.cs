namespace Gallery.DesignData
{
    using System.Collections.ObjectModel;

    using Gallery.Models;

    public class DesignGalleryViewModel
    {
        public DesignGalleryViewModel()
        {
            Items = new ObservableCollection<GalleryFile>()
            {
                new TrackedFile() { FullPath = @"C:\folder\tracked_file.gif" },
                new TrackedFile() { FullPath = @"C:\folder\another_tracked_file.png" },
                new GalleryFile() { FullPath = @"C:\folder\file1.png" },
                new GalleryFile() { FullPath = @"C:\folder\file2.txt" },
                new GalleryFile() { FullPath = @"C:\folder\very_long_file_name_helloooooooooo.gif" },
                new GalleryFile() { FullPath = @"C:\folder\file3.gif" },
                new GalleryFile() { FullPath = @"C:\folder\file4.gif" },
            };

            (Items[0] as TrackedFile)!.Tags.Add(new Tag("Tag", "Value"));
            (Items[0] as TrackedFile)!.Tags.Add(new Tag("Also Tag", group: new TagGroup("Group1", "#eebbee")));
            (Items[0] as TrackedFile)!.Tags.Add(new Tag("Looooooooooooooooooooooooong tag"));
        }

        public ObservableCollection<GalleryFile> Items { get; }
    }
}
