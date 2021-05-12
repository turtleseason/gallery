using System;

using DynamicData;

using Gallery.Models;

namespace Gallery.Services
{
    public interface ISelectedFilesService
    {
        IObservable<IChangeSet<GalleryFile, string>> Connect();

        void LoadFileCollection(FileCollection collection, bool ignoreSourceFolders = false);
        
        void AddDirectory(string path);
        
        void RemoveDirectory(string path);
    }
}