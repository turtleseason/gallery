namespace Gallery.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Dapper;

    using Gallery.Models;

    using Microsoft.Data.Sqlite;

    // Todo: Handling SQL exceptions?

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Use instance methods to ensure that the DB is initialized (via the constructor) before accessing it")]
    internal class Database
    {
        public Database()
        {
            CreateTables();
        }

        // The fallback value is to avoid breaking the XAML previewer (the default connection string is null at design time)
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["Default"]?.ConnectionString
            ?? $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Application.db")}";

        /// Returns the id of the new folder.
        ///
        // Todo: allow excluding files, auto-adding new files
        // (Return true/false for success/failure? Or throw exception if failed...?)
        public async Task<int> AddFolder(string folderPath)
        {
            string addFolderSql = @"INSERT INTO Folder(path) VALUES(@Path);
                                    SELECT last_insert_rowid();";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                var resultRowId = await conn.QueryAsync<int>(addFolderSql, new { Path = folderPath });
                return resultRowId.Single();
            }
        }

        public void DeleteFolder(string folderPath)
        {
            string deleteFolderSql = @"DELETE FROM Folder WHERE path = @Path";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(deleteFolderSql, new { Path = folderPath });
            }
        }

        public async Task AddFile(string filePath, int folderId, string? thumbnailPath = null)
        {
            string addFileSql = @"INSERT INTO File(path, folder_id, thumbnail) VALUES(@Path, @FolderId, @Thumbnail)";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                await conn.ExecuteAsync(addFileSql, new { Path = filePath, FolderId = folderId, Thumbnail = thumbnailPath });
            }
        }

        public async Task AddTag(Tag tag, params string[] filePaths)
        {
            string insertSql = @"
                /* Create tag if necessary */
                INSERT OR IGNORE INTO Tag(name, group_id)
                    SELECT @Name, group_id
                      FROM TagGroup
                     WHERE name = @Group;

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
                await conn.ExecuteAsync(insertSql, parameters);
            }
        }

        public void AddTagGroup(TagGroup group)
        {
            string insertSql = @"INSERT OR IGNORE INTO TagGroup(name, color) VALUES(@Name, @Color);";

            using (var conn = new SqliteConnection(ConnectionString))
            {
                conn.Execute(insertSql, new { group.Name, group.Color });
            }
        }

        public IEnumerable<string> GetTrackedFolders()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                return conn.Query<string>("SELECT path FROM Folder");
            }
        }

        public IEnumerable<Tag> GetTags()
        {
            string sql = @"
                SELECT DISTINCT Tag.name as Name,
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

        public IEnumerable<Tag> GetTagNames()
        {
            string sql = @"
                SELECT Tag.name as Name,
                       TagGroup.name as Name,
                       TagGroup.color as Color
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

        public IEnumerable<TagGroup> GetTagGroups()
        {
            using (var conn = new SqliteConnection(ConnectionString))
            {
                return conn.Query<TagGroup>("SELECT name as Name, color as Color from TagGroup");
            }
        }

        public IEnumerable<TrackedFile> GetFiles(params string[] folders)
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
                    })
                    .ToList();
            }
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
    }
}
