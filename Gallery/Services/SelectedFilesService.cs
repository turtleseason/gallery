namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using DynamicData;

    using Gallery.Models;

    using Splat;

    public class SelectedFilesService : ISelectedFilesService
    {
        private IDataService _dbService;
        private IFileSystemService _fsService;

        private ISourceCache<GalleryFile, string> _files;
        private FileCollection _params;

        public SelectedFilesService(IDataService? dbService = null, IFileSystemService? fsService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();

            _params = new FileCollection() { IncludeUntracked = true };

            _files = new SourceCache<GalleryFile, string>(x => x.FullPath);
            _files.AddOrUpdate(_dbService.GetFiles());

            _dbService.OnChange += HandleChange;
        }

        /// Exposes the set of selected files via a DynamicData observable
        public IObservable<IChangeSet<GalleryFile, string>> Connect()
        {
            return _files.Connect();
        }

        /////// Replaces the current FileCollection with a new one
        /////// (for example, when running a search, or loading a saved search/collection).
        ///////
        /////// If ignoreSourceFolders is true or if the FileCollection doesn't have any SourceFolders,
        /////// the search parameters will be updated but the previous source folders will be kept.
        ////public void LoadFileCollection(FileCollection collection, bool ignoreSourceFolders = false)
        ////{
        ////    if (ignoreSourceFolders || collection.SourceFolders.Count == 0)
        ////    {
        ////        collection.SourceFolders = _params.SourceFolders;
        ////    }

        ////    // (Should this make a copy?)
        ////    _params = collection;
        ////}

        /// (Temp?)
        public void SetSearchParameters(IList<ISearchParameter> parameters)
        {
            _params.Parameters = parameters;

            _files.Clear();

            AddOrUpdateFiles(_params.Parameters, _params.SourceFolders.ToArray());
        }

        /// Adds the given folder to the current source folder(s) [doesn't check for duplicates].
        public void AddDirectory(string path)
        {
            if (_params.SourceFolders.Count() == 0)
            {
                _files.Clear();
            }

            _params.SourceFolders.Add(path);

            AddOrUpdateFiles(_params.Parameters, path);
        }

        /// Removes the given folder from the list of source folders
        public void RemoveDirectory(string path)
        {
            _params.SourceFolders.Remove(path);

            _files.Remove(_files.Items.Where(x => x.Directory == path));

            if (_params.SourceFolders.Count() == 0)
            {
                AddOrUpdateFiles(_params.Parameters);
            }
        }

        private void AddOrUpdateFiles(IList<ISearchParameter> parameters, params string[] folders)
        {
            _files.AddOrUpdate(_dbService.GetFiles(parameters, folders));

            if (_params.IncludeUntracked)
            {
                foreach (string path in folders)
                {
                    IEnumerable<GalleryFile>? files = _fsService.GetFiles(path);
                    if (files != null)
                    {
                        // Skip files that are already tracked & in the collection
                        _files.AddOrUpdate(files
                            .Where(file => !_files.Lookup(file.FullPath).HasValue)
                            .Where(file => ISearchParameter.MatchesAllParameters(file, parameters)));
                    }
                }
            }
        }

        // Todo: split (separate handler method for each change type/entity combo?)
        private void HandleChange(object? sender, DataChangedEventArgs e)
        {
            DataChange change = e.Change;

            if (change.Reason == Models.ChangeReason.Add && change.EntityType == ChangeEntity.Tag)
            {
                _files.Edit(updater =>
                {
                    foreach (string file in change.AffectedFiles)
                    {
                        var lookup = _files.Lookup(file);
                        if (lookup.HasValue && lookup.Value is TrackedFile trackedFile)
                        {
                            trackedFile.Tags.Add((Tag)change.Item);
                            _files.AddOrUpdate(trackedFile);

                            // If we introduce "not" search params, may need to remove file from _files
                            // if the update means it not longer matches
                        }
                    }
                });
            }
            else if (change.Reason == Models.ChangeReason.Add && change.EntityType == ChangeEntity.File)
            {
                TrackedFile file = (TrackedFile)change.Item;
                if (_params.SourceFolders.Contains(file.Directory)
                    && ISearchParameter.MatchesAllParameters(file, _params.Parameters))
                {
                    _files.AddOrUpdate(file);
                }
            }
        }
    }
}
