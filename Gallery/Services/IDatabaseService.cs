namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;

    using DynamicData;

    using Gallery.Models;

    public interface IDatabaseService
    {
        static readonly string DefaultTagGroup = "None";

        event EventHandler? OnChange;

        IObservable<IChangeSet<string, string>> TrackedFolders();
        IObservable<bool> IsTracked(string folderPath);

        IObservable<IChangeSet<Tag, string>> Tags();
        IObservable<IChangeSet<TagGroup, string>> TagGroups();

        IEnumerable<TrackedFile> GetFiles(IEnumerable<string> folders);

        void TrackFolder(string folderPath);
        void UntrackFolder(string folderPath);

        void AddTag(Tag tag, params string[] filePaths);
        void CreateTagGroup(TagGroup group);
        ////void AddTagToGroup(string tagName, string groupName);
    }
}
