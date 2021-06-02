namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;

    using Avalonia;
    using Avalonia.Media.Imaging;

    using Dapper;

    using DynamicData;

    using Gallery.Models;

    using Microsoft.Data.Sqlite;

    using Splat;

    // Todo: Handling SQL exceptions?
    //       Consider splitting into smaller classes? (maybe split by resource type?)
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Use instance methods to ensure that the DB is initialized (via the constructor) before accessing it")]
    public class DatabaseService : IDatabaseService
    {
        private IFileSystemService _fsService;

        private ISourceCache<string, string> _trackedFolders;
        private ISourceCache<Tag, Tag> _tags;
        private ISourceCache<Tag, string> _tagNames;
        private ISourceCache<TagGroup, string> _tagGroups;

        public DatabaseService(IFileSystemService? fsService = null)
        {
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();

            _trackedFolders = new SourceCache<string, string>(x => x);
            _tags = new SourceCache<Tag, Tag>(x => new Tag(x.Name, x.Value));
            _tagNames = new SourceCache<Tag, string>(x => x.Name);
            _tagGroups = new SourceCache<TagGroup, string>(x => x.Name);

            CreateTables();
            _trackedFolders.AddOrUpdate(GetTrackedFolders());
            _tags.AddOrUpdate(GetTags());
            _tagNames.AddOrUpdate(GetTagNames());
            _tagGroups.AddOrUpdate(GetTagGroups());
        }

        public event EventHandler? OnChange;

        // The fallback value is to avoid breaking the XAML previewer (the default connection string is null at design time)
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["Default"]?.ConnectionString
            ?? $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Application.db")}";

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

        /// Returns an updating set of all the unique tags in the database, including values.
        public IObservable<IChangeSet<Tag, Tag>> Tags()
        {
            return _tags.Connect().StartWithEmpty();
        }

        /// Returns an updating set of all the unique tag names in the database, ignoring values.
        /// (May be somewhat redundant now; keeping it for convenience atm,
        /// but any using class could replace it with Tags + a little custom filtering)
        public IObservable<IChangeSet<Tag, string>> TagNames()
        {
            return _tagNames.Connect().StartWithEmpty();
        }

        public IObservable<IChangeSet<TagGroup, string>> TagGroups()
        {
            // Doesn't need to start with empty since the default group should always exist
            return _tagGroups.Connect();
        }

        /// Retrieves all files in the database matching the given criteria.
        /// If folders is empty, this returns files from all folders.
        //
        // Todo: - pull other tracked data from the DB (thumbnail, etc.)
        //       - add other query parameters (construct SQL at runtime)
        //       - (join order?)
        public IEnumerable<TrackedFile> GetFiles(IEnumerable<string> folders)
        {
            string querySql = @$"
                SELECT File.path as {nameof(TrackedFile.FullPath)},
                       File.thumbnail as {nameof(TrackedFile.Thumbnail)},
                       Tag.name as {nameof(Tag.Name)},
                       FileTag.tag_value as {nameof(Tag.Value)},
                       TagGroup.name as {nameof(TagGroup.Name)},
                       TagGroup.color as {nameof(TagGroup.Color)}
                  FROM File
                  LEFT JOIN FileTag
                    ON FileTag.file_id = File.file_id
                  LEFT JOIN Tag
                    ON Tag.tag_id = FileTag.tag_id
                  LEFT JOIN TagGroup
                    ON Tag.group_id = TagGroup.group_id
            ";

            if (folders.Any())
            {
                querySql += @"
                    INNER JOIN Folder
                       ON File.folder_id = Folder.folder_id
                    WHERE Folder.path in @Folders
                ";
            }

            using (var conn = new SqliteConnection(ConnectionString))
            {
                var result = conn.Query<TrackedFile, Tag, TagGroup, TrackedFile>(
                    querySql,
                    (file, tag, group) =>
                    {
                        if (tag.Name != null)
                        {
                            tag = new Tag(tag.Name, tag.Value, group);
                            file.Tags.Add(tag);
                        }

                        return file;
                    },
                    param: new { Folders = folders },
                    splitOn: nameof(Tag.Name));

                return result.GroupBy(file => file.FullPath)
                    .Select(group =>
                    {
                        TrackedFile file = group.First();
                        file.Tags.UnionWith(group.Where(x => x.Tags.Count > 0).Select(x => x.Tags.Single()));
                        return file;
                    });
            }
        }

        /// Adds the given folder and all the files in it to the database (non-recursively).
        /// Does nothing (prints a warning) if the folder is already tracked; however, this is not thread-safe
        /// (could fail with a SQL exception if two threads try to track the same folder at once)
        //
        // Todo: allow excluding files, auto-adding new files
        // (Return true/false for success/failure? Or throw exception if failed...?)
        public void TrackFolder(string folderPath)
        {
            if (_trackedFolders.Lookup(folderPath).HasValue)
            {
                Trace.TraceWarning($"TrackFolder: Path is already tracked ({folderPath})");
                return;
            }

            string addFolderSql = @"INSERT INTO Folder(path) VALUES(@Path);
                                    SELECT last_insert_rowid();";

            IEnumerable<GalleryFile>? files = _fsService.GetFiles(folderPath);
            if (files == null)
            {
                Trace.TraceError($"TrackFolder: Can't read files in {folderPath}, skipping folder.");
                return;
            }

            int folderId;
            using (var conn = new SqliteConnection(ConnectionString))
            {
                folderId = conn.Query<int>(addFolderSql, new { Path = folderPath }).First();
            }

            foreach (var file in files)
            {
                TrackFile(file.FullPath, folderId);
            }

            _trackedFolders.AddOrUpdate(folderPath);
            OnChange?.Invoke(this, new EventArgs());
        }

        /// Removes the given folder and any files in it from the database.
        /// (If the folder is not tracked, nothing happens.)
        public void UntrackFolder(string folderPath)
        {
            string deleteFolderSql = @"DELETE FROM Folder WHERE path = @Path";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(deleteFolderSql, new { Path = folderPath });
            }

            _trackedFolders.RemoveKey(folderPath);
            OnChange?.Invoke(this, new EventArgs());
        }

        /// Adds a tag to the given files, skipping any untracked files.
        /// Skips duplicate tags (where the given file already has a tag with the same name and value).
        ///
        /// If the given Tag's Group doesn't match the group associated with that tag in the database,
        /// the group in the database will be updated (affecting all occurrences of that tag).
        public void AddTag(Tag tag, params string[] filePaths)
        {
            if (filePaths.Length == 0)
            {
                return;
            }

            // May or may not want to keep the update-on-conflict behavior? It's convenient but feels kind of illogical
            string insertSql = @"
                /* Set tag group for tag */
                INSERT INTO Tag(name, group_id)
                    SELECT @Name, group_id
                      FROM TagGroup
                     WHERE name = @Group
                    ON CONFLICT(name) DO UPDATE SET group_id = excluded.group_id;

                /* Set tag for file */
                INSERT OR IGNORE INTO FileTag(file_id, tag_id, tag_value)
                    SELECT File.file_id, Tag.tag_id, @Value
                      FROM File, Tag
                     WHERE File.path in @Paths AND Tag.name = @Name;
            ";

            var parameters = new
            {
                Paths = filePaths,
                tag.Name,
                tag.Value,
                Group = tag.Group.Name ?? Tag.DefaultGroupName,
            };

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(insertSql, parameters);
            }

            _tags.AddOrUpdate(tag);
            _tagNames.AddOrUpdate(new Tag(tag.Name, group: tag.Group));
            ////OnChange?.Invoke(this, new EventArgs());
        }

        // Can be used to update the color; may want to make a separate method to edit/rename groups instead?
        public void CreateTagGroup(TagGroup group)
        {
            string insertSql = @"INSERT INTO TagGroup(name, color)
                                    VALUES(@Name, @Color)
                                    ON CONFLICT(name) DO UPDATE SET color = excluded.color;";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(insertSql, new { group.Name, group.Color });
            }

            _tagGroups.AddOrUpdate(group);
            OnChange?.Invoke(this, new EventArgs());
        }

        /// Create the database file and tables if they don't already exist.
        private void CreateTables()
        {
            string createTablesSql = @$"
                CREATE TABLE IF NOT EXISTS Folder (
                    folder_id INTEGER PRIMARY KEY NOT NULL,
                    path VARCHAR UNIQUE NOT NULL
                );

                CREATE TABLE IF NOT EXISTS File (
                    file_id INTEGER PRIMARY KEY NOT NULL,
                    path VARCHAR UNIQUE NOT NULL,
                    folder_id INTEGER NOT NULL,
                    thumbnail VARCHAR,
                    FOREIGN KEY (folder_id) REFERENCES Folder(folder_id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS TagGroup (
                    group_id INTEGER PRIMARY KEY NOT NULL,
                    name VARCHAR UNIQUE NOT NULL,
                    color VARCHAR
                );

                CREATE TABLE IF NOT EXISTS Tag (
                    tag_id INTEGER PRIMARY KEY NOT NULL,
                    name VARCHAR UNIQUE NOT NULL,
                    group_id INTEGER NOT NULL,
                    FOREIGN KEY (group_id) REFERENCES TagGroup(group_id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS FileTag (
                    file_id INTEGER NOT NULL,
                    tag_id INTEGER NOT NULL,
                    tag_value VARCHAR,
                    UNIQUE (file_id, tag_id, tag_value),
                    FOREIGN KEY (file_id) REFERENCES File(file_id) ON DELETE CASCADE
                    FOREIGN KEY (tag_id) REFERENCES Tag(tag_id) ON DELETE CASCADE
                );

                INSERT OR IGNORE INTO TagGroup(name) VALUES('{Tag.DefaultGroupName}');
            ";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(createTablesSql);
            }
        }

        // Very temporary lol - just to get this working before refactoring
        private void TrackFile(string path, int folderId)
        {
            string addFileSql = @"INSERT INTO File(path, folder_id, thumbnail) VALUES(@Path, @FolderId, @Thumbnail)";

            DynamicParameters dp = new DynamicParameters();
            dp.Add("@Path", path);
            dp.Add("@FolderId", folderId);
            dp.Add("@Thumbnail", null);

            var info = new FileInfo(path);
            ISet<Tag> tags = new HashSet<Tag>
            {
                new Tag("Date", info.CreationTime.ToString()),
                new Tag("Edited", info.LastWriteTime.ToString()),
            };

            Bitmap? img = ImageUtil.LoadBitmap(path);
            if (img != null)
            {
                tags.Add(new Tag("Width", img.PixelSize.Width.ToString()));
                tags.Add(new Tag("Height", img.PixelSize.Height.ToString()));

                var aspect = img.PixelSize.AspectRatio;
                PixelSize thumbnailSize = aspect > 1
                    ? new PixelSize(200, (int)(200 / aspect))
                    : new PixelSize((int)(200 * aspect), 200);

                var thumbnailFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "JellyfishGallery",
                    "Thumbnails",
                    folderId.ToString());
                Directory.CreateDirectory(thumbnailFolder);

                var thumbnailPath = Path.Combine(thumbnailFolder, Path.GetFileName(path).Replace('.', '_') + ".png");
                using (Stream s = File.Create(thumbnailPath))
                {
                    img.CreateScaledBitmap(thumbnailSize).Save(s);
                }

                dp.Add("@Thumbnail", thumbnailPath);
            }

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(addFileSql, dp);
            }

            foreach (var tag in tags)
            {
                AddTag(tag, path);
            }
        }

        private IEnumerable<string> GetTrackedFolders()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                return conn.Query<string>("SELECT path FROM Folder");
            }
        }

        private IEnumerable<Tag> GetTags()
        {
            string sql = @"SELECT DISTINCT Tag.name as Name,
                                           FileTag.tag_value as Value,
                                           TagGroup.name as Name,
                                           TagGroup.color as Color
                             FROM FileTag
                            INNER JOIN Tag
                               ON FileTag.tag_id = Tag.tag_id
                            INNER JOIN TagGroup
                               ON Tag.group_id = TagGroup.group_id
            ";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                return conn.Query<Tag, TagGroup, Tag>(
                    sql,
                    (tag, tagGroup) => new Tag(tag.Name, tag.Value, tagGroup),
                    splitOn: nameof(TagGroup.Name));
            }
        }

        private IEnumerable<Tag> GetTagNames()
        {
            string sql = @"SELECT Tag.name as Name, TagGroup.name as Name, TagGroup.color as Color
                             FROM Tag
                            INNER JOIN TagGroup
                               ON Tag.group_id = TagGroup.group_id;
                ";
            using (var conn = new SqliteConnection(ConnectionString))
            {
                return conn.Query<Tag, TagGroup, Tag>(
                    sql,
                    (tag, group) => new Tag(tag.Name, group: group),
                    splitOn: nameof(TagGroup.Name));
            }
        }

        private IEnumerable<TagGroup> GetTagGroups()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                return conn.Query<TagGroup>("SELECT name as Name, color as Color from TagGroup");
            }
        }
    }
}
