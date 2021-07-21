namespace Gallery.Data
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Media.Imaging;

    using DynamicData;

    using Gallery.Entities;
    using Gallery.Entities.SearchParameters;
    using Gallery.Util;

    using Splat;

    public interface IDataService
    {
        event EventHandler<DataChangedEventArgs> OnChange;

        IObservable<IChangeSet<string, string>> TrackedFolders();
        IObservable<bool> IsTracked(string folderPath);

        IEnumerable<TrackedFile> GetFiles(IEnumerable<ISearchParameter>? searchParams = null, params string[] folders);

        IEnumerable<Tag> GetAllTags();
        IEnumerable<TagGroup> GetAllTagGroups();

        Task TrackFolder(string folderPath);
        Task UntrackFolders(params string[] folderPath);

        Task AddTag(Tag tag, params string[] filePaths);
        Task DeleteTags(IEnumerable<Tag> tags, params string[] filePaths);

        void CreateTagGroup(TagGroup group);
        void UpdateTagGroup(TagGroup original, TagGroup updated);

        void UpdateDescription(string description, string filePath);
    }

    public class DataService : IDataService
    {
        private static string _thumbnailFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JellyfishGallery", "Thumbnails");

        private Database _database;
        private IFileSystemUtil _fsService;

        private ISourceCache<string, string> _trackedFolders;

        public DataService(IFileSystemUtil? fsService = null)
        {
            _database = new Database();

            _fsService = fsService ?? Locator.Current.GetService<IFileSystemUtil>();

            _trackedFolders = new SourceCache<string, string>(x => x);
            _trackedFolders.AddOrUpdate(_database.GetTrackedFolders());
        }

        public event EventHandler<DataChangedEventArgs>? OnChange;

        public IObservable<IChangeSet<string, string>> TrackedFolders()
        {
            return _trackedFolders.Connect().StartWithEmpty();
        }

        public IObservable<bool> IsTracked(string folderPath)
        {
            return _trackedFolders.Watch(folderPath)
                .Select(x => _trackedFolders.Lookup(x.Key).HasValue)
                .DistinctUntilChanged()  // avoid sending double "true" if the folder is tracked at time of Subscribe
                .StartWith(_trackedFolders.Lookup(folderPath).HasValue);
        }

        /// Retrieves all files in the database matching the given criteria.
        /// If no folders are specified, this returns files from all folders.
        //
        // (Probably more efficient to let DB handle filtering (need to translate search parameters into SQL),
        //  but at least for now this has the advantage of discarding unused objects right away)
        public IEnumerable<TrackedFile> GetFiles(IEnumerable<ISearchParameter>? searchParams = null, params string[] folders)
        {
            var files = _database.GetFiles(folders);
            if (searchParams == null)
            {
                return files;
            }
            else
            {
                return files.Where(file => ISearchParameter.MatchesAllParameters(file, searchParams));
            }
        }

        public IEnumerable<Tag> GetAllTags()
        {
            return _database.GetTags();
        }

        public IEnumerable<TagGroup> GetAllTagGroups()
        {
            return _database.GetTagGroups();
        }

        /// Adds the given folder and all the files in it to the database (non-recursively).
        /// Does nothing (prints a warning) if the folder is already tracked.
        public async Task TrackFolder(string folderPath)
        {
            if (_trackedFolders.Lookup(folderPath).HasValue)
            {
                Trace.TraceWarning($"TrackFolder: Path is already tracked ({folderPath})");
                return;
            }

            IEnumerable<GalleryFile>? files = _fsService.GetFiles(folderPath);
            if (files == null)
            {
                Trace.TraceError($"TrackFolder: Can't read files in {folderPath}, skipping folder.");
                return;
            }

            int folderId = await _database.AddFolder(folderPath);

            foreach (var file in files)
            {
                await TrackFile(file.FullPath, folderId);
            }

            _trackedFolders.AddOrUpdate(folderPath);
        }

        /// Removes the given folder and any files in it from the database.
        /// (If the folder is not tracked, nothing happens.)
        public async Task UntrackFolders(params string[] folderPaths)
        {
            _trackedFolders.RemoveKeys(folderPaths);

            var deletedIds = await _database.DeleteFolders(folderPaths);

            foreach (string path in folderPaths)
            {
                NotifyChange(path, DataChangeReason.Remove, DataChangeEntity.Folder);
            }

            // Clean up remaining data
            await Task.Run(() =>
            {
                foreach (int folderId in deletedIds)
                {
                    _fsService.DeleteDirectory(GetThumbnailFolder(folderId));
                }
            });

            await _database.DeleteUnusedTags();
        }

        /// Adds a tag to the given files, skipping any untracked files.
        /// Skips duplicate tags (where the given file already has a tag with the same name and value);
        /// a change event may be raised for a duplicate tag, though.
        public async Task AddTag(Tag tag, params string[] filePaths)
        {
            if (filePaths.Length == 0)
            {
                return;
            }

            await _database.AddTag(tag, filePaths);

            // Todo: get the actual tag obj back from database (in case group is inaccurate)
            // [or keep tag -> tagGroups map in-memory for easier reference]

            NotifyChange(tag, DataChangeReason.Add, DataChangeEntity.Tag, files: filePaths);
        }

        public async Task DeleteTags(IEnumerable<Tag> tags, params string[] filePaths)
        {
            if (filePaths.Length == 0 || !tags.Any())
            {
                return;
            }

            foreach (Tag tag in tags)
            {
                await _database.DeleteTag(tag, filePaths);
                NotifyChange(tag, DataChangeReason.Remove, DataChangeEntity.Tag, files: filePaths);
            }

            await _database.DeleteUnusedTags();
        }

        public void CreateTagGroup(TagGroup group)
        {
            _database.AddTagGroup(group);

            NotifyChange(group, DataChangeReason.Add, DataChangeEntity.TagGroup);
        }

        public void UpdateTagGroup(TagGroup original, TagGroup updated)
        {
            _database.UpdateTagGroup(original, updated);

            NotifyChange(updated, DataChangeReason.Update, DataChangeEntity.TagGroup, original);
        }

        // Note: the TrackedFile in the update notification doesn't include tags
        // (for efficiency/simplicity, since tag updates have a separate change entity type)
        public void UpdateDescription(string description, string filePath)
        {
            TrackedFile? file = _database.UpdateDescription(description, filePath);

            if (file != null)
            {
                NotifyChange(file, DataChangeReason.Update, DataChangeEntity.File);
            }
        }

        private async Task TrackFile(string path, int folderId)
        {
            var file = new TrackedFile() { FullPath = path };

            var info = new FileInfo(path);
            file.Tags.Add(new Tag("Date", info.CreationTime.ToString()));
            file.Tags.Add(new Tag("Edited", info.LastWriteTime.ToString()));

            Bitmap? img = await ImageUtil.LoadBitmap(path);
            if (img != null)
            {
                await AddImageInfo(file, img, GetThumbnailFolder(folderId));
            }

            await _database.AddFile(path, folderId, file.Thumbnail);

            foreach (var tag in file.Tags)
            {
                await _database.AddTag(tag, path);
            }

            // Make sure the tags on the created TrackedFile match the tag groups in the DB
            // (keeping an in memory tag -> tagGroup lookup might help with this)
            NotifyChange(file, DataChangeReason.Add, DataChangeEntity.File);
        }

        // Adds image-specific default tags to the TrackedFile and saves a thumbnail.
        private async Task AddImageInfo(TrackedFile file, Bitmap bitmap, string thumbnailFolder)
        {
            file.Tags.Add(new Tag("Width", bitmap.PixelSize.Width.ToString()));
            file.Tags.Add(new Tag("Height", bitmap.PixelSize.Height.ToString()));

            var aspect = bitmap.PixelSize.AspectRatio;
            PixelSize thumbnailSize = aspect > 1
                ? new PixelSize(200, (int)(200 / aspect))
                : new PixelSize((int)(200 * aspect), 200);

            Directory.CreateDirectory(thumbnailFolder);

            file.Thumbnail = Path.Combine(thumbnailFolder, Path.GetFileName(file.FullPath).Replace('.', '_') + ".png");

            await ImageUtil.SaveThumbnail(bitmap, file.Thumbnail, thumbnailSize);
        }

        private string GetThumbnailFolder(int folderId)
        {
            return Path.Combine(_thumbnailFolder, folderId.ToString());
        }

        private void NotifyChange(object item, DataChangeReason reason, DataChangeEntity entity, object? original = null, params string[] files)
        {
            var change = new DataChange(item, reason, entity, original, files);
            OnChange?.Invoke(this, new DataChangedEventArgs(change));
        }
    }
}
