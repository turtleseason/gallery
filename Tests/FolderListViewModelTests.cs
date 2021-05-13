namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;

    using DynamicData;

    using Gallery.Services;
    using Gallery.ViewModels;

    using Moq;

    using NUnit.Framework;

    internal class FolderListViewModelTests
    {
        private FolderListViewModel vm;

        private Mock<IDatabaseService> mockDb;
        private Mock<IFileSystemService> mockFileSystem;
        private Mock<ISelectedFilesService> mockFiles;

        private ISourceCache<string, string> trackedFolders;

        [SetUp]
        public void SetUp()
        {
            mockDb = new Mock<IDatabaseService>(MockBehavior.Strict);
            mockFileSystem = new Mock<IFileSystemService>(MockBehavior.Strict);
            mockFiles = new Mock<ISelectedFilesService>(MockBehavior.Loose);

            trackedFolders = new SourceCache<string, string>(x => x);

            DriveInfo[] mockDrives = { new DriveInfo("C"), new DriveInfo("Q") };
            mockFileSystem.Setup(mock => mock.GetAvailableDrives()).Returns(mockDrives);

            string[] mockPaths = { @"fakepath", @"fakepath\nested_folder", @"another\folder" };
            mockFileSystem.Setup(mock => mock.GetDirectories(It.IsAny<string>()))
                .Returns((string path) => mockPaths.Select(folder => Path.Combine(path, folder)).ToList());

            mockDb.Setup(mock => mock.TrackedFolders()).Returns(trackedFolders.Connect());
            mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns((string path) =>
                trackedFolders.Watch(path)
                .Select(x => trackedFolders.Lookup(x.Key).HasValue)
                .DistinctUntilChanged()
                .StartWith(trackedFolders.Lookup(path).HasValue));
        }

        [Test]
        public void TrackSelectedFoldersCommand_TracksAllSelectedFolders()
        {
            mockDb.Setup(mock => mock.TrackFolder(It.IsAny<string>()));

            vm = new FolderListViewModel(dbService: mockDb.Object, fsService: mockFileSystem.Object, sfService: mockFiles.Object);

            IEnumerable<FolderListItemViewModel> items = vm.Items.Concat(vm.Items.SelectMany(item => item.Children.Take(2)));
            IEnumerable<FolderListItemViewModel> notSelected = vm.Items.SelectMany(item => item.Children.TakeLast(item.Children.Count - 2));
            vm.SelectedItems.Add(items);

            vm.TrackSelectedFoldersCommand.Execute().Take(1).Subscribe();

            foreach (var item in items)
            {
                mockDb.Verify(mock => mock.TrackFolder(item.FullPath), Times.Once);
            }

            foreach (var item in notSelected)
            {
                mockDb.Verify(mock => mock.TrackFolder(item.FullPath), Times.Never);
            }
        }

        [Test]
        public void TrackSelectedFoldersCommand_CanExecute_OnlyWhenUntrackedFoldersAreSelected()
        {
            IEnumerable<string> trackedPaths = mockFileSystem.Object.GetDirectories(@"C:\");
            trackedFolders.AddOrUpdate(trackedPaths);

            vm = new FolderListViewModel(dbService: mockDb.Object, fsService: mockFileSystem.Object, sfService: mockFiles.Object);

            bool? canExecute = null;
            vm.TrackSelectedFoldersCommand.CanExecute.Subscribe(x => canExecute = x);

            var trackedAndUntrackedItems = vm.Items.SelectMany(item => item.Children.Take(2));
            vm.SelectedItems.Add(trackedAndUntrackedItems);
            Assert.IsTrue(canExecute);

            var untrackedItems = vm.SelectedItems.Where(x => !x.FullPath.StartsWith("C")).ToArray();
            vm.SelectedItems.Remove(untrackedItems);
            Assert.IsFalse(canExecute);
        }

        [Test]
        public void TrackSelectedFoldersCommand_CanExecute_UpdatesWhenTrackedFoldersChange()
        {
            vm = new FolderListViewModel(dbService: mockDb.Object, fsService: mockFileSystem.Object, sfService: mockFiles.Object);

            var selectedItems = vm.Items.SelectMany(item => item.Children.Take(2));
            vm.SelectedItems.Add(selectedItems);

            bool? canExecute = null;
            vm.TrackSelectedFoldersCommand.CanExecute.Subscribe(x => canExecute = x);

            Assert.IsTrue(canExecute);

            trackedFolders.AddOrUpdate(selectedItems.Select(x => x.FullPath));
            Assert.IsFalse(canExecute);
        }
    }
}