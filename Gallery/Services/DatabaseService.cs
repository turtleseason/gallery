namespace Gallery.Services
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;

    using Dapper;

    using DynamicData;

    using Gallery.Models;

    using Microsoft.Data.Sqlite;

    using Splat;

    // Todo: Handling SQL exceptions?
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Use instance methods to ensure that the DB is initialized (via the constructor) before accessing it")]
    public class DatabaseService : IDatabaseService
    {
        private IFileSystemService _fsService;

        private ISourceCache<string, string> _trackedFolders;
        private ISourceCache<Tag, string> _tags;
        private ISourceCache<TagGroup, string> _tagGroups;

        public DatabaseService(IFileSystemService? fsService = null)
        {
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();

            _trackedFolders = new SourceCache<string, string>(x => x);
            _tags = new SourceCache<Tag, string>(x => x.Name);
            _tagGroups = new SourceCache<TagGroup, string>(x => x.Name);

            CreateTables();
            _trackedFolders.AddOrUpdate(GetTrackedFolders());
            _tags.AddOrUpdate(GetUniqueTags());
            _tagGroups.AddOrUpdate(GetTagGroups());
        }

        public event EventHandler? OnChange;

        // The fallback value is to avoid breaking the XAML previewer (the default connection string is null at design time)
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["Default"]?.ConnectionString
            ?? $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Application.db")}";

        public IObservable<IChangeSet<string, string>> TrackedFolders()
        {
            return _trackedFolders.Connect();
        }

        public IObservable<bool> IsTracked(string folderPath)
        {
            return _trackedFolders.Watch(folderPath)
                .Select(x => _trackedFolders.Lookup(x.Key).HasValue)
                .DistinctUntilChanged()  // avoid sending double "true" if the folder is tracked at time of Subscribe
                .StartWith(_trackedFolders.Lookup(folderPath).HasValue);
        }

        public IObservable<IChangeSet<Tag, string>> Tags()
        {
            return _tags.Connect();
        }

        public IObservable<IChangeSet<TagGroup, string>> TagGroups()
        {
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
            string addFileSql = @"INSERT INTO File(path, folder_id) VALUES(@Path, @FolderId)";

            IEnumerable<GalleryFile>? files = _fsService.GetFiles(folderPath);
            if (files == null)
            {
                Trace.TraceError($"TrackFolder: Can't read files in {folderPath}, skipping folder.");
                return;
            }

            using (var conn = new SqliteConnection(ConnectionString))
            {
                int folderId = conn.Query<int>(addFolderSql, new { Path = folderPath }).First();

                foreach (var file in files)
                {
                    conn.Execute(addFileSql, new { Path = file.FullPath, FolderId = folderId });
                }
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
            OnChange?.Invoke(this, new EventArgs());
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

        ////public void AddTagToGroup(string tag, string groupName)
        ////{
        ////    // also allow update?
        ////    string insertSql = @"INSERT INTO TagInGroup(group_id, tag)
        ////                         SELECT TagGroups.group_id, @Tag
        ////                           FROM TagGroups
        ////                          WHERE TagGroups.name = @Name";

        ////    using (var conn = new SqliteConnection(ConnectionString))
        ////    {
        ////        conn.Execute(insertSql, new { Tag = tag, Name = groupName });
        ////    }

        ////    OnChange?.Invoke(this, new EventArgs());
        ////}

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

        private IEnumerable<string> GetTrackedFolders()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                return conn.Query<string>("SELECT path FROM Folder");
            }
        }

        private IEnumerable<Tag> GetUniqueTags()
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
