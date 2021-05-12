using System;
using System.Collections.Generic;
using System.Linq;

using DynamicData;

using Gallery.Models;

using Splat;

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

    public class SelectedFilesService : ISelectedFilesService
    {
        IDatabaseService _dbService;
        IFileSystemService _fsService;

        ISourceCache<GalleryFile, string> _files;
        FileCollection _selectedFiles;

        public SelectedFilesService(IDatabaseService? dbService = null, IFileSystemService? fsService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDatabaseService>();
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();

            _files = new SourceCache<GalleryFile, string>(x => x.FullPath);
            _selectedFiles = new FileCollection() { IncludeUntracked = true };

            _dbService.OnChange += OnDatabaseChanged;
        }

        /// Exposes the set of selected files via a DynamicData observable
        public IObservable<IChangeSet<GalleryFile, string>> Connect()
        {
            return _files.Connect();
        }

        /// Replaces the current FileCollection with a new one
        /// (for example, when running a search, or loading a saved search/collection).
        ///
        /// If ignoreSourceFolders is true or if the FileCollection doesn't have any SourceFolders,
        /// the search parameters will be updated but the previous source folders will be kept.
        public void LoadFileCollection(FileCollection collection, bool ignoreSourceFolders = false)
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

        // Todo: filter by parameters
        // (should probably update more efficiently too, instead of reloading the entire collection each time?)
        void PopulateFilesCollection()
        {
            _files.Clear();

            _files.AddOrUpdate(_dbService.GetFiles(_selectedFiles.SourceFolders));

            if (_selectedFiles.IncludeUntracked)
            {
                // If SourceFolders is empty, this will show all tracked files and no untracked files;
                // not sure if that's desirable or not
                foreach (string path in _selectedFiles.SourceFolders)
                {
                    IEnumerable<GalleryFile>? files = _fsService.GetFiles(path);
                    if (files != null)
                    {
                        // Skip files that are already tracked & in the collection
                        _files.AddOrUpdate(files.Where(file => !_files.Lookup(file.FullPath).HasValue));
                    }
                }
            }
        }

        void OnDatabaseChanged(object? sender, EventArgs e)
        {
            PopulateFilesCollection();
        }
    }
}
