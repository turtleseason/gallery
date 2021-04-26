using System.Collections.ObjectModel;

using DynamicData;

using Gallery.Models;

namespace Gallery.Services
{
    // File collection can change with:
    //  - File system updates
    //  - User data updates
    //    ^ listen for events from another service?
    //
    //  - Changes to selected folders
    //  - Changes to search parameters
    //    ^ Method calls to this service?

    public class SelectedFilesService
    {
        ObservableCollection<GalleryFile> _files;
        FileCollection _selectedFiles;

        public SelectedFilesService()
        {
            _files = new ObservableCollection<GalleryFile>();
            _selectedFiles = new FileCollection() { IncludeUntracked = true };
        }

        /// Replace the current FileCollection with a new one
        /// (for example, when running a search, or loading a saved search/collection).
        ///
        /// If ignoreSourceFolders is true or if the FileCollection doesn't have any SourceFolders,
        /// the search parameters will be updated but the previous source folders will be kept.
        public void LoadFileCollection(FileCollection collection, bool ignoreSourceFolders=false)
        {
            if (ignoreSourceFolders || collection.SourceFolders.Count == 0)
            {
                collection.SourceFolders = _selectedFiles.SourceFolders;
            }

            // (Should this make a copy?)
            _selectedFiles = collection;
        }

        /// (Temp?) Adds the given folder to the current source folder(s) [doesn't check for duplicates].
        public void AddDirectory(string path)
        {
            _selectedFiles.SourceFolders.Add(path);
            PopulateFilesCollection();
        }

        /// (Temp?) Removes the given folder from the list of source folders
        public void RemoveDirectory(string path)
        {
            _selectedFiles.SourceFolders.Remove(path);
            PopulateFilesCollection();
        }

        // property?
        public ReadOnlyObservableCollection<GalleryFile> GetFiles()
        {
            return new ReadOnlyObservableCollection<GalleryFile>(_files);
        }

        void PopulateFilesCollection()
        {
            _files.Clear();

            if (_selectedFiles == null)
            {
                return;
            }

            if (_selectedFiles.IncludeUntracked)
            {
                foreach (string path in _selectedFiles.SourceFolders)
                {
                    var files = FileSystemService.GetFiles(path);
                    if (files != null)
                    {
                        _files.AddRange(files);
                    }
                    // Todo: filter by parameters
                }
            }

            // Todo: query the DB for tracked files
        }
    }
}
