namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;

    using Dapper;

    using DynamicData;

    using Gallery.Models;
    using Gallery.Services;

    using Microsoft.Data.Sqlite;

    using Moq;

    using NUnit.Framework;

    public class DatabaseServiceTests
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;

        private DatabaseService database;
        private SqliteConnection conn;
        private Mock<IFileSystemService> mockFileSystem;

        /// Gets all files in the given folder(s) in the current mocked file system (for testing against results from the database).
        public IEnumerable<GalleryFile> GetMockFiles(params string[] paths)
        {
            List<GalleryFile> result = new();
            foreach (string path in paths)
            {
                result.AddRange(mockFileSystem.Object.GetFiles(path));
            }

            return result;
        }

        public IEnumerable<string> GetMockFilePaths(params string[] paths)
        {
            return GetMockFiles(paths).Select(file => file.FullPath);
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // App.config isn't where ConfigurationManager expects it to be by default, so copy it there
            // https://github.com/dotnet/runtime/issues/22720#issuecomment-621273186

            string configPath = Assembly.GetExecutingAssembly().Location + ".config";
            string configOutputPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
            File.Copy(configPath, configOutputPath, true);
        }

        [SetUp]
        public void Setup()
        {
            // Keep an open connection so that the in-memory DB persists through the whole test
            conn = new SqliteConnection(connectionString);
            conn.Open();

            mockFileSystem = new Mock<IFileSystemService>(MockBehavior.Strict);
            mockFileSystem.Setup(mock => mock.GetFiles(It.IsAny<string>())).Returns((string path) =>
                new List<GalleryFile>()
                {
                    new GalleryFile { FullPath = Path.Combine(path, "File1.png") },
                    new GalleryFile { FullPath  = Path.Combine(path, "file2.jpg") },
                    new GalleryFile { FullPath  = Path.Combine(path, "a.txt") },
                });
            database = new DatabaseService(mockFileSystem.Object);
        }

        [TearDown]
        public void TearDown()
        {
            conn.Dispose();
        }

        [Test]
        public void TrackFolder_AddsFolderAndFilesToDB()
        {
            string folderPath = @"C:\fakepath";

            database.TrackFolder(folderPath);

            var folders = conn.Query<string>("SELECT path FROM Folders").ToList();
            Assert.AreEqual(folders.Count, 1);
            Assert.Contains(folderPath, folders);

            var files = conn.Query<string>("SELECT path FROM Files").ToList();
            var expectedFiles = GetMockFilePaths(folderPath);
            Assert.That(files, Is.EquivalentTo(expectedFiles));
        }

        [Test]
        public void UntrackFolder_DeletesTrackedFolderAndFiles()
        {
            string untrackedPath = @"C:\fakepath";
            string trackedPath = @"C:\other";

            database.TrackFolder(untrackedPath);
            database.TrackFolder(trackedPath);

            database.UntrackFolder(untrackedPath);

            var folders = conn.Query<string>("SELECT path FROM Folders").ToList();
            Assert.AreEqual(folders.Count, 1);
            Assert.Contains(trackedPath, folders);

            var untrackedFiles = conn.Query<string>("SELECT path FROM Files WHERE path = @Path", new { Path = untrackedPath }).ToList();
            Assert.Zero(untrackedFiles.Count);

            // Make sure *only* files associated with the untracked folder are deleted
            var allFiles = conn.Query<string>("SELECT path FROM Files").ToList();
            Assert.That(allFiles, Is.EquivalentTo(GetMockFilePaths(trackedPath)));
        }

        [Test]
        public void GetFiles_FiltersByFolder()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };
            foreach (string path in paths)
            {
                database.TrackFolder(path);
            }

            var path1Files = database.GetFiles(new[] { paths[0] }).Select(x => x.FullPath);
            Assert.That(path1Files, Is.EquivalentTo(GetMockFilePaths(paths[0])));

            var allFiles = database.GetFiles(paths).Select(x => x.FullPath);
            Assert.That(allFiles, Is.EquivalentTo(GetMockFilePaths(paths)));
        }

        [Test]
        public void GetFiles_ReturnsAllFilesWhenNoFolderSpecified()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };
            foreach (string path in paths)
            {
                database.TrackFolder(path);
            }

            var files = database.GetFiles(new List<string>()).Select(x => x.FullPath);

            Assert.That(files, Is.EquivalentTo(GetMockFilePaths(paths)));
        }

        [Test]
        public void GetFiles_ReturnsFileTags()
        {
            Tag tag = new Tag("I am a tag", "with a value");
            string folder = @"C:\fakepath";
            string file = GetMockFilePaths(folder).First();

            database.TrackFolder(folder);
            database.AddTag(tag, file);

            IEnumerable<TrackedFile> results = database.GetFiles(new[] { folder });
            foreach (TrackedFile result in results)
            {
                if (result.FullPath == file)
                {
                    Assert.That(result.Tags, Is.EquivalentTo(new[] { tag }));
                }
                else
                {
                    Assert.IsFalse(result.Tags.Any());
                }
            }
        }

        [Test]
        public void TrackedFolders_ReturnsAllCurrentFolders()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };
            foreach (string path in paths)
            {
                database.TrackFolder(path);
            }

            IReadOnlyCollection<string> result = null;
            database.TrackedFolders().ToCollection().Subscribe(folders => result = folders);

            Assert.That(result, Is.EquivalentTo(paths));
        }

        [Test]
        public void TrackedFolders_UpdatesOnTrackAndUntrack()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };

            database.TrackFolder(paths[0]);
            database.TrackFolder(paths[1]);

            IReadOnlyCollection<string> result = null;
            database.TrackedFolders().ToCollection().Subscribe(folders => result = folders);

            database.UntrackFolder(paths[0]);
            Assert.That(result, Is.EquivalentTo(new string[] { paths[1] }));

            database.TrackFolder(paths[2]);
            Assert.That(result, Is.EquivalentTo(new string[] { paths[1], paths[2] }));

            database.TrackFolder(paths[0]);
            Assert.That(result, Is.EquivalentTo(paths));
        }

        [Test]
        public void IsTracked_GivesCorrectInitialValue()
        {
            string untrackedPath = @"C:\fakepath";
            string trackedPath = @"C:\other";

            database.TrackFolder(trackedPath);

            bool? result = null;
            database.IsTracked(untrackedPath).Take(1).Subscribe(x => result = x);
            Assert.IsFalse(result);

            database.IsTracked(trackedPath).Take(1).Subscribe(x => result = x);
            Assert.IsTrue(result);
        }

        [Test]
        public void IsTracked_UpdatesOnTrackAndUntrack()
        {
            string path = @"C:\fakepath";

            bool? result = null;
            database.IsTracked(path).Subscribe(x => result = x);

            database.TrackFolder(path);
            Assert.IsTrue(result);

            database.UntrackFolder(path);
            Assert.IsFalse(result);
        }
    }
}