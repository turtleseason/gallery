namespace Gallery.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;

    using DynamicData;
    using DynamicData.Binding;

    using Gallery.Entities;
    using Gallery.Entities.SearchParameters;
    using Gallery.Util;

    using Splat;

    public interface ISelectedFilesService
    {
        ReadOnlyObservableCollection<GalleryFile> SelectedFiles();

        ////void LoadFileCollection(FileCollection collection, bool ignoreSourceFolders = false);

        void SetSearchParameters(IList<ISearchParameter> parameters);

        void AddDirectories(params string[] paths);

        void RemoveDirectories(params string[] paths);

        void ShowAllTrackedFiles();
    }

    public class SelectedFilesService : ISelectedFilesService
    {
        private IDataService _dbService;
        private IFileSystemUtil _fsService;

        private ISourceCache<GalleryFile, string> _filesCache;
        private FileCollection _params;

        private ReadOnlyObservableCollection<GalleryFile> _observableCollection;

        public SelectedFilesService(IDataService? dbService = null, IFileSystemUtil? fsService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemUtil>();

            _params = new FileCollection() { IncludeUntracked = true };

            _filesCache = new SourceCache<GalleryFile, string>(x => x.FullPath);
            _filesCache.AddOrUpdate(_dbService.GetFiles());

            _dbService.OnChange += HandleChange;

            _observableCollection = new ReadOnlyObservableCollection<GalleryFile>(new ObservableCollection<GalleryFile>());
            _filesCache.Connect()
                .Sort(SortExpressionComparer<GalleryFile>.Ascending(file => file.FullPath),
                    SortOptimisations.ComparesImmutableValuesOnly)
                .Bind(out _observableCollection)
                .Subscribe();
        }

        public ReadOnlyObservableCollection<GalleryFile> SelectedFiles()
        {
            return _observableCollection;
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

            _filesCache.Clear();

            AddOrUpdateFiles(_params.Parameters, _params.SourceFolders.ToArray());
        }

        /// Adds the given folders to the current source folders [doesn't check for duplicates].
        public void AddDirectories(params string[] paths)
        {
            if (paths.Length == 0)
            {
                return;
            }

            if (_params.SourceFolders.Count == 0)
            {
                _filesCache.Clear();
            }

            _params.SourceFolders.AddRange(paths);

            AddOrUpdateFiles(_params.Parameters, paths);
        }

        /// Removes the given folders from the list of source folders
        public void RemoveDirectories(params string[] paths)
        {
            if (paths.Length == 0)
            {
                return;
            }

            _params.SourceFolders.RemoveMany(paths);

            _filesCache.Remove(_filesCache.Items.Where(x => paths.Contains(x.Directory)));
        }

        /// Clears all source folders and shows tracked files from all folders.
        public void ShowAllTrackedFiles()
        {
            _params.SourceFolders.Clear();

            AddOrUpdateFiles(_params.Parameters);
        }

        private void AddOrUpdateFiles(IList<ISearchParameter> parameters, params string[] folders)
        {
            _filesCache.AddOrUpdate(_dbService.GetFiles(parameters, folders));

            if (_params.IncludeUntracked)
            {
                AddOrUpdateUntracked(parameters, folders);
            }
        }

        private void AddOrUpdateUntracked(IList<ISearchParameter> parameters, params string[] folders)
        {
            foreach (string path in folders)
            {
                IEnumerable<GalleryFile>? files = _fsService.GetFiles(path);
                if (files != null)
                {
                    // Skip files that are already tracked & in the collection
                    _filesCache.AddOrUpdate(files
                        .Where(file => !_filesCache.Lookup(file.FullPath).HasValue)
                        .Where(file => ISearchParameter.MatchesAllParameters(file, parameters)));
                }
            }
        }

        // Todo: split (separate handler method for each change type/entity combo?)
        private void HandleChange(object? sender, DataChangedEventArgs e)
        {
            DataChange change = e.Change;

            if (change.Reason == DataChangeReason.Add && change.EntityType == DataChangeEntity.Tag)
            {
                _filesCache.Edit(updater =>
                {
                    foreach (string file in change.AffectedFiles)
                    {
                        var lookup = _filesCache.Lookup(file);
                        if (lookup.HasValue && lookup.Value is TrackedFile trackedFile)
                        {
                            trackedFile.Tags.Add((Tag)change.Item);
                            _filesCache.AddOrUpdate(trackedFile);

                            // If we introduce "not" search params, may need to remove file from _files
                            // if the update means it not longer matches
                        }
                    }
                });
            }
            else if (change.Reason == DataChangeReason.Add && change.EntityType == DataChangeEntity.File)
            {
                var file = (TrackedFile)change.Item;
                if (_params.SourceFolders.Contains(file.Directory)
                    && ISearchParameter.MatchesAllParameters(file, _params.Parameters))
                {
                    _filesCache.AddOrUpdate(file);
                }
            }
            else if (change.Reason == DataChangeReason.Update && change.EntityType == DataChangeEntity.File)
            {
                var file = (TrackedFile)change.Item;
                var lookup = _filesCache.Lookup(file.FullPath);
                if (lookup.HasValue)
                {
                    var trackedFile = (TrackedFile)lookup.Value;
                    trackedFile.Description = file.Description;
                    _filesCache.AddOrUpdate(trackedFile);
                }
            }
            else if (change.Reason == DataChangeReason.Remove && change.EntityType == DataChangeEntity.Folder)
            {
                var path = (string)change.Item;
                _filesCache.Remove(_filesCache.Items.Where(file => file.Directory == path));

                // Ensure files are re-added to the collection as untracked files if necessary
                if (_params.IncludeUntracked)
                {
                    AddOrUpdateUntracked(_params.Parameters, path);
                }
            }
        }
    }
}
