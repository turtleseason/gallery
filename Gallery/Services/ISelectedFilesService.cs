namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;

    using DynamicData;

    using Gallery.Models;

    public interface ISelectedFilesService
    {
        IObservable<IChangeSet<GalleryFile, string>> Connect();

        ////void LoadFileCollection(FileCollection collection, bool ignoreSourceFolders = false);

        void SetSearchParameters(IList<ISearchParameter> parameters);

        void AddDirectory(string path);

        void RemoveDirectory(string path);
    }
}