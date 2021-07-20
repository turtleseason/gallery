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

    using Gallery.Data;
    using Gallery.Entities;
    using Gallery.Entities.SearchParameters;
    using Gallery.Util;

    using Microsoft.Data.Sqlite;

    using Moq;

    using NUnit.Framework;

    using Parameter = Gallery.Entities.SearchParameters;

    public class DataServiceTests
    {
        private string _connectionString;

        private DataService _dataService;
        private SqliteConnection _conn;
        private Mock<IFileSystemUtil> _mockFileSystem;

        /// Gets all files in the given folder(s) in the current mocked file system (for testing against results from the database).
        public IEnumerable<GalleryFile> GetMockFiles(params string[] folderPaths)
        {
            List<GalleryFile> result = new();
            foreach (string path in folderPaths)
            {
                result.AddRange(_mockFileSystem.Object.GetFiles(path));
            }

            return result;
        }

        public IEnumerable<string> GetMockFilePaths(params string[] folderPaths)
        {
            return GetMockFiles(folderPaths).Select(file => file.FullPath);
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // App.config isn't where ConfigurationManager expects it to be by default, so copy it there
            // https://github.com/dotnet/runtime/issues/22720#issuecomment-621273186

            string configPath = Assembly.GetExecutingAssembly().Location + ".config";
            string configOutputPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
            File.Copy(configPath, configOutputPath, true);

            _connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        }

        [SetUp]
        public void Setup()
        {
            // Keep an open connection so that the in-memory DB persists through the whole test
            _conn = new SqliteConnection(_connectionString);
            _conn.Open();

            _mockFileSystem = new Mock<IFileSystemUtil>();
            _mockFileSystem.Setup(mock => mock.GetFiles(It.IsAny<string>())).Returns((string path) =>
                new List<GalleryFile>()
                {
                    new GalleryFile { FullPath = Path.Combine(path, "File1.png") },
                    new GalleryFile { FullPath  = Path.Combine(path, "file2.jpg") },
                    new GalleryFile { FullPath  = Path.Combine(path, "a.txt") },
                });
            _dataService = new DataService(_mockFileSystem.Object);
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

            await _dataService.TrackFolder(folderPath);

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

            await _dataService.TrackFolder(untrackedPath);
            await _dataService.TrackFolder(trackedPath);

            await _dataService.UntrackFolders(untrackedPath);

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
                await _dataService.TrackFolder(path);
            }

            var path1Files = _dataService.GetFiles(folders: paths[0]).Select(x => x.FullPath);
            Assert.That(path1Files, Is.EquivalentTo(GetMockFilePaths(paths[0])));

            var allFiles = _dataService.GetFiles(folders: paths).Select(x => x.FullPath);
            Assert.That(allFiles, Is.EquivalentTo(GetMockFilePaths(paths)));
        }

        [Test]
        public async Task GetFiles_FiltersBySearchParameters()
        {
            string path = @"C:\fakepath";
            string[] taggedFiles = { Path.Combine(path, "File1.png"), Path.Combine(path, "a.txt") };

            Tag tag = TestUtil.TestTags[2];

            await _dataService.TrackFolder(path);
            _dataService.CreateTagGroup(tag.Group);
            await _dataService.AddTag(tag, taggedFiles);

            ISearchParameter[] searchParameters = { new Parameter.Tagged(tag) };

            var result = _dataService.GetFiles(searchParameters, path).Select(file => file.FullPath);

            Assert.That(result, Is.EquivalentTo(taggedFiles));
        }

        [Test]
        public async Task GetFiles_ReturnsAllFilesWhenNoFolderSpecified()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };
            foreach (string path in paths)
            {
                await _dataService.TrackFolder(path);
            }

            var files = _dataService.GetFiles().Select(x => x.FullPath);

            Assert.That(files, Is.EquivalentTo(GetMockFilePaths(paths)));
        }

        [Test]
        public async Task GetFiles_ReturnsFileTags()
        {
            Tag tag = new Tag("I am a tag", "with a value");
            string folder = @"C:\fakepath";
            string file = GetMockFilePaths(folder).First();

            await _dataService.TrackFolder(folder);
            await _dataService.AddTag(tag, file);

            IEnumerable<TrackedFile> results = _dataService.GetFiles(folders: folder);
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
                await _dataService.TrackFolder(path);
            }

            IReadOnlyCollection<string> result = null;
            _dataService.TrackedFolders().ToCollection().Subscribe(folders => result = folders);

            Assert.That(result, Is.EquivalentTo(paths));
        }

        [Test]
        public async Task TrackedFolders_UpdatesOnTrackAndUntrack()
        {
            string[] paths = { @"C:\fakepath", @"C:\other", @"D:\fakepath" };

            await _dataService.TrackFolder(paths[0]);
            await _dataService.TrackFolder(paths[1]);

            IReadOnlyCollection<string> result = null;
            _dataService.TrackedFolders().ToCollection().Subscribe(folders => result = folders);

            await _dataService.UntrackFolders(paths[0]);
            Assert.That(result, Is.EquivalentTo(new string[] { paths[1] }));

            await _dataService.TrackFolder(paths[2]);
            Assert.That(result, Is.EquivalentTo(new string[] { paths[1], paths[2] }));

            await _dataService.TrackFolder(paths[0]);
            Assert.That(result, Is.EquivalentTo(paths));
        }

        [Test]
        public async Task IsTracked_GivesCorrectInitialValue()
        {
            string untrackedPath = @"C:\fakepath";
            string trackedPath = @"C:\other";

            await _dataService.TrackFolder(trackedPath);

            bool? result = null;
            _dataService.IsTracked(untrackedPath).Take(1).Subscribe(x => result = x);
            Assert.IsFalse(result);

            _dataService.IsTracked(trackedPath).Take(1).Subscribe(x => result = x);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task IsTracked_UpdatesOnTrackAndUntrack()
        {
            string path = @"C:\fakepath";

            bool? result = null;
            _dataService.IsTracked(path).Subscribe(x => result = x);

            await _dataService.TrackFolder(path);
            Assert.IsTrue(result);

            await _dataService.UntrackFolders(path);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetAllTags_ReturnsAllUniqueTags()
        {
            string[] paths = GetMockFilePaths(@"C:\fakepath").ToArray();

            await _dataService.TrackFolder(@"C:\fakepath");

            TagGroup group = TestUtil.TestTagGroups[1];
            _dataService.CreateTagGroup(group);

            Tag[] tags = { new Tag("Tag"), new Tag("Tag2", "Potato", group), new Tag("Tag2", group: group) };
            await _dataService.AddTag(tags[0], paths);
            await _dataService.AddTag(tags[1], paths[0], paths[1]);
            await _dataService.AddTag(tags[2], paths[2]);

            var result = _dataService.GetAllTags();

            Assert.That(tags, Is.SubsetOf(result));
        }

        [Test]
        public void GetAllTagGroups_ReturnsAllUniqueTagGroups()
        {
            TagGroup group = TestUtil.TestTagGroups[1];

            _dataService.CreateTagGroup(group);

            var result = _dataService.GetAllTagGroups();

            Assert.AreEqual(result.Count(), 2);
            Assert.That(result.Contains(group));
            Assert.That(result.Contains(new TagGroup(TagGroup.DefaultGroupName)));
        }

        [Test]
        public async Task UpdateTagGroup_UpdatesNameAndColor()
        {
            TagGroup original = new TagGroup("cool group", "#00ff00");
            TagGroup updated = new TagGroup("cooler group", "#0000ff");

            string folder = @"C:\fakepath";
            string file = GetMockFilePaths(folder).First();

            await _dataService.TrackFolder(folder);
            _dataService.CreateTagGroup(original);
            await _dataService.AddTag(new Tag("Test tag", group: original), file);

            _dataService.UpdateTagGroup(original, updated);

            TrackedFile updatedFile = _dataService.GetFiles(folders: folder).Single(x => x.FullPath == file);
            Assert.Contains(new Tag("Test tag", group: updated), updatedFile.Tags.ToArray());
        }

        [Test]
        public async Task DeleteTags_OnlyDeletesTagsWithMatchingValues()
        {
            string folder = @"C:\fakepath";
            string file = GetMockFilePaths(folder).First();
            await _dataService.TrackFolder(folder);

            Tag tag1 = new Tag("Tag", "with value");
            Tag tag2 = new Tag("Tag", "with a different value :O");

            await _dataService.AddTag(tag1, file);
            await _dataService.AddTag(tag2, file);

            await _dataService.DeleteTags(new[] { tag1 }, file);

            TrackedFile resultFile = _dataService.GetFiles(folders: folder).Single(x => x.FullPath == file);

            Assert.IsFalse(resultFile.Tags.Contains(tag1));
            Assert.IsTrue(resultFile.Tags.Contains(tag2));
        }

        [Test]
        public async Task DeleteTags_DeletesTagsWithNullValue()
        {
            string folder = @"C:\fakepath";
            string file = GetMockFilePaths(folder).First();
            await _dataService.TrackFolder(folder);

            Tag tag1 = new Tag("Tag");

            await _dataService.AddTag(tag1, file);

            await _dataService.DeleteTags(new[] { tag1 }, file);

            TrackedFile resultFile = _dataService.GetFiles(folders: folder).Single(x => x.FullPath == file);

            Assert.IsFalse(resultFile.Tags.Contains(tag1));
        }

        [Test]
        public async Task DeleteTags_CleansUpUnusedTags()
        {
            string folder = @"C:\fakepath";
            string[] files = GetMockFilePaths(folder).ToArray();
            await _dataService.TrackFolder(folder);

            Tag tag1 = new Tag("New tag that doesn't exist in the database :O");

            await _dataService.AddTag(tag1, files);
            Assert.IsTrue(_dataService.GetAllTags().Contains(tag1));

            await _dataService.DeleteTags(new[] { tag1 }, files);
            Assert.IsFalse(_dataService.GetAllTags().Contains(tag1));
        }

        [Test]
        public async Task DeleteTags_DeletesAllTagsOnAllFiles()
        {
            string folder = @"C:\fakepath";
            string[] files = GetMockFilePaths(folder).ToArray();
            Tag[] tagsToDelete = { new Tag("Tag1"), new Tag("Tag2", "abc") };
            Tag[] tagsToNotDelete = { new Tag("Tag3") };

            await _dataService.TrackFolder(folder);

            // File 0: Tag A
            // File 1: Tag A & B
            // File 2: Tag A & B & C
            await _dataService.AddTag(tagsToDelete[0], files);
            await _dataService.AddTag(tagsToDelete[1], files[1], files[2]);
            await _dataService.AddTag(tagsToNotDelete[0], files[2]);

            await _dataService.DeleteTags(tagsToDelete, files);

            IEnumerable<TrackedFile> resultFiles = _dataService.GetFiles(folders: folder);
            var file0Tags = resultFiles.Single(x => x.FullPath == files[0]).Tags;
            var file1Tags = resultFiles.Single(x => x.FullPath == files[1]).Tags;
            var file2Tags = resultFiles.Single(x => x.FullPath == files[2]).Tags;

            Assert.IsFalse(file0Tags.Concat(file1Tags).Concat(file2Tags).Contains(tagsToDelete[0]));
            Assert.IsFalse(file0Tags.Concat(file1Tags).Concat(file2Tags).Contains(tagsToDelete[1]));
            Assert.Contains(tagsToNotDelete[0], file2Tags.ToArray());
        }
    }
}
