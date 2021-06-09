namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Dapper;

    using DynamicData;

    using Gallery.Models;
    using Gallery.Services;

    using Microsoft.Data.Sqlite;

    using Moq;

    using NUnit.Framework;

    using Parameter = Gallery.Models.SearchParameters;

    public class DatabaseServiceTests
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;

        private DataService _database;
        private SqliteConnection _conn;
        private Mock<IFileSystemService> _mockFileSystem;

        /// Gets all files in the given folder(s) in the current mocked file system (for testing against results from the database).
        public IEnumerable<GalleryFile> GetMockFiles(params string[] paths)
        {
            List<GalleryFile> result = new();
            foreach (string path in paths)
            {
                result.AddRange(_mockFileSystem.Object.GetFiles(path));
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
            _conn = new SqliteConnection(_connectionString);
            _conn.Open();

            _mockFileSystem = new Mock<IFileSystemService>(MockBehavior.Strict);
            _mockFileSystem.Setup(mock => mock.GetFiles(It.IsAny<string>())).Returns((string path) =>
                new List<GalleryFile>()
                {
                    new GalleryFile { FullPath = Path.Combine(path, "File1.png") },
                    new GalleryFile { FullPath  = Path.Combine(path, "file2.jpg") },
                    new GalleryFile { FullPath  = Path.Combine(path, "a.txt") },
                });
            _database = new DataService(_mockFileSystem.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _conn.Dispose();
        }

        [Test]
        public async Task TrackFolder_AddsFolderAndFilesToDB()
        {
            string folderPath = @"C:\fakepath";

            await _database.TrackFolder(folderPath);

            var folders = _conn.Query<string>("SELECT path FROM Folder").ToList();
            Assert.AreEqual(folders.Count, 1);
            Assert.Contains(folderPath, folders);

            var files = _conn.Query<string>("SELECT path FROM File").ToList();
            var expectedFiles = GetMockFilePaths(folderPath);
            Assert.That(files, Is.EquivalentTo(expectedFiles));
        }

        [Test]
        public async Task UntrackFolder_DeletesTrackedFolderAndFiles()
        {
            string untrackedPath = @"C:\fakepath";
            string trackedPath = @"C:\other";

            await _database.TrackFolder(untrackedPath);
            await _database.TrackFolder(trackedPath);

            _database.UntrackFolder(untrackedPath);

            var folders = _conn.Query<string>("SELECT path FROM Folder").ToList();
            Assert.AreEqual(folders.Count, 1);
            Assert.Contains(trackedPath, folders);

            var untrackedFiles = _conn.Query<string>("SELECT path FROM File WHERE path = @Path", new { Path = untrackedPath }).ToList();
            Assert.Zero(untrackedFiles.Count);

            // Make sure *only* files associated with the untracked folder are deleted
            var allFiles = _conn.Query<string>("SELECT path FROM File").ToList();
            Assert.That(allFiles, Is.EquivalentTo(GetMockFilePaths(trackedPath)));
        }

        [Test]
        public async Task GetFiles_FiltersByFolder()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };
            foreach (string path in paths)
            {
                await _database.TrackFolder(path);
            }

            var path1Files = _database.GetFiles(folders: paths[0]).Select(x => x.FullPath);
            Assert.That(path1Files, Is.EquivalentTo(GetMockFilePaths(paths[0])));

            var allFiles = _database.GetFiles(folders: paths).Select(x => x.FullPath);
            Assert.That(allFiles, Is.EquivalentTo(GetMockFilePaths(paths)));
        }

        [Test]
        public async Task GetFiles_FiltersBySearchParameters()
        {
            string path = @"C:\fakepath";
            string[] taggedFiles = { Path.Combine(path, "File1.png"), Path.Combine(path, "a.txt") };

            Tag tag = TestUtil.TestTags[2];

            await _database.TrackFolder(path);
            _database.CreateTagGroup(tag.Group);
            await _database.AddTag(tag, taggedFiles);

            ISearchParameter[] searchParameters = { new Parameter.Tagged(tag) };

            var result = _database.GetFiles(searchParameters, path).Select(file => file.FullPath);

            Assert.That(result, Is.EquivalentTo(taggedFiles));
        }

        [Test]
        public async Task GetFiles_ReturnsAllFilesWhenNoFolderSpecified()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };
            foreach (string path in paths)
            {
                await _database.TrackFolder(path);
            }

            var files = _database.GetFiles().Select(x => x.FullPath);

            Assert.That(files, Is.EquivalentTo(GetMockFilePaths(paths)));
        }

        [Test]
        public async Task GetFiles_ReturnsFileTags()
        {
            Tag tag = new Tag("I am a tag", "with a value");
            string folder = @"C:\fakepath";
            string file = GetMockFilePaths(folder).First();

            await _database.TrackFolder(folder);
            await _database.AddTag(tag, file);

            IEnumerable<TrackedFile> results = _database.GetFiles(folders: folder);
            foreach (TrackedFile result in results)
            {
                if (result.FullPath == file)
                {
                    Assert.IsTrue(result.Tags.Contains(tag));
                }
            }
        }

        [Test]
        public async Task TrackedFolders_ReturnsAllCurrentFolders()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };
            foreach (string path in paths)
            {
                await _database.TrackFolder(path);
            }

            IReadOnlyCollection<string> result = null;
            _database.TrackedFolders().ToCollection().Subscribe(folders => result = folders);

            Assert.That(result, Is.EquivalentTo(paths));
        }

        [Test]
        public async Task TrackedFolders_UpdatesOnTrackAndUntrack()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };

            await _database.TrackFolder(paths[0]);
            await _database.TrackFolder(paths[1]);

            IReadOnlyCollection<string> result = null;
            _database.TrackedFolders().ToCollection().Subscribe(folders => result = folders);

            _database.UntrackFolder(paths[0]);
            Assert.That(result, Is.EquivalentTo(new string[] { paths[1] }));

            await _database.TrackFolder(paths[2]);
            Assert.That(result, Is.EquivalentTo(new string[] { paths[1], paths[2] }));

            await _database.TrackFolder(paths[0]);
            Assert.That(result, Is.EquivalentTo(paths));
        }

        [Test]
        public async Task IsTracked_GivesCorrectInitialValue()
        {
            string untrackedPath = @"C:\fakepath";
            string trackedPath = @"C:\other";

            await _database.TrackFolder(trackedPath);

            bool? result = null;
            _database.IsTracked(untrackedPath).Take(1).Subscribe(x => result = x);
            Assert.IsFalse(result);

            _database.IsTracked(trackedPath).Take(1).Subscribe(x => result = x);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task IsTracked_UpdatesOnTrackAndUntrack()
        {
            string path = @"C:\fakepath";

            bool? result = null;
            _database.IsTracked(path).Subscribe(x => result = x);

            await _database.TrackFolder(path);
            Assert.IsTrue(result);

            _database.UntrackFolder(path);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetAllTags_ReturnsAllUniqueTags()
        {
            string[] paths = GetMockFilePaths(@"C:\fakepath").ToArray();

            await _database.TrackFolder(@"C:\fakepath");

            TagGroup group = TestUtil.TestTagGroups[1];
            _database.CreateTagGroup(group);

            Tag[] tags = { new Tag("Tag"), new Tag("Tag2", "Potato", group), new Tag("Tag2", group: group) };
            await _database.AddTag(tags[0], paths);
            await _database.AddTag(tags[1], paths[0], paths[1]);
            await _database.AddTag(tags[2], paths[2]);

            var result = _database.GetAllTags();

            Assert.That(tags, Is.SubsetOf(result));
        }

        [Test]
        public void GetAllTagGroups_ReturnsAllUniqueTagGroups()
        {
            TagGroup group = TestUtil.TestTagGroups[1];

            _database.CreateTagGroup(group);

            var result = _database.GetAllTagGroups();

            Assert.AreEqual(result.Count(), 2);
            Assert.That(result.Contains(group));
            Assert.That(result.Contains(new TagGroup(Tag.DefaultGroupName)));
        }
    }
}