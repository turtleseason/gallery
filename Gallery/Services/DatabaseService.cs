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

        public DatabaseService(IFileSystemService? fsService = null)
        {
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();

            _trackedFolders = new SourceCache<string, string>(x => x);

            CreateTables();
            _trackedFolders.AddOrUpdate(GetTrackedFolders());
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

        /// Retrieves all files in the database matching the given criteria.
        /// If folders is empty, this returns files from all folders.
        //
        // Todo: - pull other tracked data from the DB (thumbnail, tags, etc.)
        //       - add other query parameters (construct SQL at runtime)
        //       - *should* it return files from all folders if empty?
        public IEnumerable<TrackedFile> GetFiles(IEnumerable<string> folders)
        {
            string querySql = @$"
                SELECT Files.path as {nameof(TrackedFile.FullPath)},
                       Tags.tag as {nameof(Tag.Name)},
                       Tags.tag_value as {nameof(Tag.Value)}
                  FROM Files
             LEFT JOIN Tags
                    ON Tags.file_id = Files.file_id
            ";

            if (folders.Any())
            {
                querySql += @"
                    INNER JOIN Folders
                       ON Files.folder_id = Folders.folder_id
                    WHERE Folders.path in @Folders
                ";
            }

            using (var conn = new SqliteConnection(ConnectionString))
            {
                var result = conn.Query<TrackedFile, Tag, TrackedFile>(
                    querySql,
                    (file, tag) =>
                    {
                        if (tag.Name != null)
                        {
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

            string addFolderSql = @"INSERT INTO Folders(path) VALUES(@Path);
                                    SELECT last_insert_rowid();";
            string addFileSql = @"INSERT INTO Files(path, folder_id) VALUES(@Path, @FolderId)";

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
            string deleteFolderSql = @"DELETE FROM Folders WHERE path = @Path";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(deleteFolderSql, new { Path = folderPath });
            }

            _trackedFolders.RemoveKey(folderPath);
            OnChange?.Invoke(this, new EventArgs());
        }

        /// Skips duplicate tags (where the given file already has a tag with the same name and value).
        /// Also skips any untracked files.
        public void AddTag(Tag tag, params string[] filePaths)
        {
            string insertSql = @"
                INSERT OR IGNORE INTO Tags(file_id, tag, tag_value)
                SELECT file_id, @Name, @Value
                  FROM Files
                 WHERE path in @Paths
            ";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(insertSql, new { Paths = filePaths, tag.Name, tag.Value });
            }

            OnChange?.Invoke(this, new EventArgs());
        }

        /// Create the database file and tables if they don't already exist.
        private void CreateTables()
        {
            string createTablesSql = @"
                CREATE TABLE IF NOT EXISTS Folders (
                    folder_id INTEGER PRIMARY KEY NOT NULL,
                    path VARCHAR UNIQUE NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Files (
                    file_id INTEGER PRIMARY KEY NOT NULL,
                    path VARCHAR UNIQUE NOT NULL,
                    folder_id INTEGER NOT NULL,
                    thumbnail VARCHAR,
                    FOREIGN KEY (folder_id) REFERENCES Folders(folder_id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Tags (
                    file_id INTEGER NOT NULL,
                    tag VARCHAR NOT NULL,
                    tag_value VARCHAR,
                    PRIMARY KEY (file_id, tag, tag_value),
                    FOREIGN KEY (file_id) REFERENCES Files(file_id) ON DELETE CASCADE
                );
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
                return conn.Query<string>("SELECT path FROM Folders");
            }
        }
    }
}
