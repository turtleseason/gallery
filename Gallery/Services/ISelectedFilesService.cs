namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Gallery.Models;

    public interface ISelectedFilesService
    {
        ReadOnlyObservableCollection<GalleryFile> SelectedFiles();

        ////void LoadFileCollection(FileCollection collection, bool ignoreSourceFolders = false);

        void SetSearchParameters(IList<ISearchParameter> parameters);

        void AddDirectory(string path);

        void RemoveDirectory(string path);

        void ShowAllTrackedFiles();
    }
}