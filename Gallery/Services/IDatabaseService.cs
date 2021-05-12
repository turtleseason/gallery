using System;
using System.Collections.Generic;

using DynamicData;

using Gallery.Models;

namespace Gallery.Services
{
    public interface IDatabaseService
    {
        event EventHandler? OnChange;

        IObservable<IChangeSet<string, string>> TrackedFolders();
        IObservable<bool> IsTracked(string folderPath);

        IEnumerable<TrackedFile> GetFiles(IEnumerable<string> folders);

        void TrackFolder(string folderPath);
        void UntrackFolder(string folderPath);
    }
}
