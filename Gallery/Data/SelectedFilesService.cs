/// This class exposes and updates the set of currently selected files
/// based on the current search parameters and selected folders.
/// (These are the files displayed in GalleryView/cycled through in SingleFileView).

namespace Gallery.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Subjects;

    using DynamicData;
    using DynamicData.Binding;

    using Gallery.Entities;
    using Gallery.Entities.SearchParameters;
    using Gallery.Util;

    using Splat;

    public interface ISelectedFilesService
    {
        ReadOnlyObservableCollection<GalleryFile> SelectedFiles { get; }

        IObservable<IEnumerable<ISearchParameter>> SearchParameters { get; }

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

        private FileCollection _selectionParameters;

        // Instead of using a Subject, maybe implement change notifications in FileCollection & subscribe to that?
        // Not sure which approach makes more sense
        private BehaviorSubject<IList<ISearchParameter>> _parametersObservable;

        private ISourceCache<GalleryFile, string> _filesCache;
        private ReadOnlyObservableCollection<GalleryFile> _observableCollection;

        public SelectedFilesService(IDataService? dbService = null, IFileSystemUtil? fsService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemUtil>();

            _selectionParameters = new FileCollection() { IncludeUntracked = true };

            _parametersObservable = new BehaviorSubject<IList<ISearchParameter>>(_selectionParameters.Parameters);

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

        public ReadOnlyObservableCollection<GalleryFile> SelectedFiles => _observableCollection;

        /// This always returns the current value when subscribed.
        public IObservable<IEnumerable<ISearchParameter>> SearchParameters => _parametersObservable;

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
            _selectionParameters.Parameters = parameters;

            _parametersObservable.OnNext(parameters);

            _filesCache.Clear();

            AddOrUpdateFiles(_selectionParameters.Parameters, _selectionParameters.SourceFolders.ToArray());
        }

        /// Adds the given folders to the current source folders [doesn't check for duplicates].
        public void AddDirectories(params string[] paths)
        {
            if (paths.Length == 0)
            {
                return;
            }

            if (_selectionParameters.SourceFolders.Count == 0)
            {
                _filesCache.Clear();
            }

            _selectionParameters.SourceFolders.AddRange(paths);

            AddOrUpdateFiles(_selectionParameters.Parameters, paths);
        }

        /// Removes the given folders from the list of source folders
        public void RemoveDirectories(params string[] paths)
        {
            if (paths.Length == 0)
            {
                return;
            }

            _selectionParameters.SourceFolders.RemoveMany(paths);

            _filesCache.Remove(_filesCache.Items.Where(x => paths.Contains(x.Directory)));
        }

        /// Clears all source folders and shows tracked files from all folders.
        public void ShowAllTrackedFiles()
        {
            _selectionParameters.SourceFolders.Clear();

            AddOrUpdateFiles(_selectionParameters.Parameters);
        }

        private void AddOrUpdateFiles(IList<ISearchParameter> parameters, params string[] folders)
        {
            _filesCache.AddOrUpdate(_dbService.GetFiles(parameters, folders));

            if (_selectionParameters.IncludeUntracked)
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
            else if (change.Reason == DataChangeReason.Remove && change.EntityType == DataChangeEntity.Tag)
            {
                _filesCache.Edit(updater =>
                {
                    foreach (string file in change.AffectedFiles)
                    {
                        var lookup = _filesCache.Lookup(file);
                        if (lookup.HasValue && lookup.Value is TrackedFile trackedFile)
                        {
                            trackedFile.Tags.Remove((Tag)change.Item);

                            if (!ISearchParameter.MatchesAllParameters(trackedFile, _selectionParameters.Parameters))
                            {
                                _filesCache.Remove(trackedFile);
                            }
                            else
                            {
                                _filesCache.AddOrUpdate(trackedFile);
                            }
                        }
                    }
                });
            }
            else if (change.Reason == DataChangeReason.Add && change.EntityType == DataChangeEntity.File)
            {
                var file = (TrackedFile)change.Item;
                if (_selectionParameters.SourceFolders.Contains(file.Directory)
                    && ISearchParameter.MatchesAllParameters(file, _selectionParameters.Parameters))
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
                if (_selectionParameters.IncludeUntracked)
                {
                    AddOrUpdateUntracked(_selectionParameters.Parameters, path);
                }
            }
            else if (change.Reason == DataChangeReason.Update && change.EntityType == DataChangeEntity.TagGroup)
            {
                TagGroup newGroup = (TagGroup)change.Item;
                TagGroup original = (TagGroup)change.Original!;
                foreach (GalleryFile file in _filesCache.Items)
                {
                    if (file is TrackedFile tracked)
                    {
                        foreach (Tag tag in tracked.Tags.Where(tag => tag.Group.Equals(original)).ToList())
                        {
                            tracked.Tags.Remove(tag);
                            tracked.Tags.Add(new Tag(tag.Name, tag.Value, newGroup));
                        }
                    }
                }
            }
        }
    }
}
