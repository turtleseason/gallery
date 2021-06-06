namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;

    using DynamicData;

    using Gallery.Models;

    public interface IDataService
    {
        event EventHandler<DataChangedEventArgs> OnChange;

        IObservable<IChangeSet<string, string>> TrackedFolders();
        IObservable<bool> IsTracked(string folderPath);

        IEnumerable<TrackedFile> GetFiles(IEnumerable<ISearchParameter>? searchParams = null, params string[] folders);

        IEnumerable<Tag> GetAllTags();
        IEnumerable<TagGroup> GetAllTagGroups();

        void TrackFolder(string folderPath);
        void UntrackFolder(string folderPath);

        void AddTag(Tag tag, params string[] filePaths);
        void CreateTagGroup(TagGroup group);
    }
}
