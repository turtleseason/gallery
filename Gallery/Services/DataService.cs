namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Media.Imaging;

    using DynamicData;

    using Gallery.Models;
    using Gallery.Persistence;

    using Splat;

    public class DataService : IDataService
    {
        private static string _thumbnailFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JellyfishGallery", "Thumbnails");

        private Database _database;
        private IFileSystemService _fsService;

        private ISourceCache<string, string> _trackedFolders;

        public DataService(IFileSystemService? fsService = null)
        {
            _database = new Database();

            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();

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
        public void UntrackFolder(string folderPath)
        {
            _database.DeleteFolder(folderPath);

            _trackedFolders.RemoveKey(folderPath);
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

            var change = new DataChange(tag, Models.ChangeReason.Add, ChangeEntity.Tag, filePaths);
            OnChange?.Invoke(this, new DataChangedEventArgs(change));
        }

        // Can be used to update the color; may want to make a separate method to edit/rename groups instead?
        public void CreateTagGroup(TagGroup group)
        {
            _database.AddTagGroup(group);
        }

        // Note: the TrackedFile in the update notification doesn't include tags
        // (for efficiency/simplicity, since tag updates have a separate change entity type)
        public void UpdateDescription(string description, string filePath)
        {
            TrackedFile? file = _database.UpdateDescription(description, filePath);

            if (file != null)
            {
                var change = new DataChange(file, Models.ChangeReason.Update, ChangeEntity.File);
                OnChange?.Invoke(this, new DataChangedEventArgs(change));
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
                await AddImageInfo(file, img, Path.Combine(_thumbnailFolder, folderId.ToString()));
            }

            await _database.AddFile(path, folderId, file.Thumbnail);

            foreach (var tag in file.Tags)
            {
                await _database.AddTag(tag, path);
            }

            // Make sure the tags on the created TrackedFile match the tag groups in the DB
            // (keeping an in memory tag -> tagGroup lookup might help with this)
            var change = new DataChange(file, Models.ChangeReason.Add, ChangeEntity.File);
            OnChange?.Invoke(this, new DataChangedEventArgs(change));
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
    }
}
