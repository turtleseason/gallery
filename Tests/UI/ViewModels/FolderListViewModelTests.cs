namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using DynamicData;

    using Gallery.Data;
    using Gallery.UI;
    using Gallery.UI.ViewModels;
    using Gallery.Util;

    using Moq;

    using NUnit.Framework;

    using ReactiveUI;

    internal class FolderListViewModelTests
    {
        private FolderListViewModel _vm;

        private Mock<IDataService> _mockDb;
        private Mock<IFileSystemUtil> _mockFileSystem;
        private Mock<ISelectedFilesService> _mockFiles;

        private ISourceCache<string, string> _trackedFolders;

        private IDisposable _disposable;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _disposable = Interactions.ShowCommandProgress.RegisterHandler(context => context.SetOutput(Unit.Default));
        }

        [SetUp]
        public void SetUp()
        {
            _mockDb = new Mock<IDataService>(MockBehavior.Strict);
            _mockFileSystem = new Mock<IFileSystemUtil>(MockBehavior.Strict);
            _mockFiles = new Mock<ISelectedFilesService>(MockBehavior.Loose);

            _trackedFolders = new SourceCache<string, string>(x => x);

            DriveInfo[] mockDrives = { new DriveInfo("C"), new DriveInfo("Q") };
            _mockFileSystem.Setup(mock => mock.GetAvailableDrives()).Returns(mockDrives);

            string[] mockPaths = { @"fakepath", @"fakepath\nested_folder", @"another\folder" };
            _mockFileSystem.Setup(mock => mock.GetDirectories(It.IsAny<string>()))
                .Returns((string path) => mockPaths.Select(folder => Path.Combine(path, folder)).ToList());

            _mockDb.Setup(mock => mock.TrackedFolders()).Returns(_trackedFolders.Connect());
            _mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns((string path) =>
                _trackedFolders.Watch(path)
                .Select(x => _trackedFolders.Lookup(x.Key).HasValue)
                .DistinctUntilChanged()
                .StartWith(_trackedFolders.Lookup(path).HasValue));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _disposable.Dispose();
        }

        public async Task WaitForTopLevelChildrenToLoad()
        {
            await Observable.Zip(_vm.Items.Select(x => x.WhenAnyValue(x => x.HasLoadedChildren).Where(x => x)))
                .Take(1);
        }

        [Test]
        public async Task TrackSelectedFoldersCommand_TracksAllSelectedFolders()
        {
            _mockDb.Setup(mock => mock.TrackFolder(It.IsAny<string>())).Returns(Task.CompletedTask);

            _vm = new FolderListViewModel(dbService: _mockDb.Object, fsService: _mockFileSystem.Object, sfService: _mockFiles.Object);
            await WaitForTopLevelChildrenToLoad();

            IEnumerable<FolderListItemViewModel> items = _vm.Items.Concat(_vm.Items.SelectMany(item => item.Children.Take(2)));
            IEnumerable<FolderListItemViewModel> notSelected = _vm.Items.SelectMany(item => item.Children.TakeLast(item.Children.Count - 2));
            _vm.SelectedItems.Add(items);

            await _vm.TrackCommand.Execute();

            foreach (var item in items)
            {
                _mockDb.Verify(mock => mock.TrackFolder(item.FullPath), Times.Once);
            }

            foreach (var item in notSelected)
            {
                _mockDb.Verify(mock => mock.TrackFolder(item.FullPath), Times.Never);
            }
        }

        [Test]
        public async Task TrackSelectedFoldersCommand_CanExecute_OnlyWhenUntrackedFoldersAreSelected()
        {
            IEnumerable<string> trackedPaths = _mockFileSystem.Object.GetDirectories(@"C:\");
            _trackedFolders.AddOrUpdate(trackedPaths);

            _vm = new FolderListViewModel(dbService: _mockDb.Object, fsService: _mockFileSystem.Object, sfService: _mockFiles.Object);
            await WaitForTopLevelChildrenToLoad();

            bool? canExecute = null;
            _vm.TrackCommand.CanExecute.Subscribe(x => canExecute = x);

            var trackedAndUntrackedItems = _vm.Items.SelectMany(item => item.Children.Take(2));
            _vm.SelectedItems.Add(trackedAndUntrackedItems);
            Assert.IsTrue(canExecute);

            var untrackedItems = _vm.SelectedItems.Where(x => !x.FullPath.StartsWith("C")).ToArray();
            _vm.SelectedItems.Remove(untrackedItems);
            Assert.IsFalse(canExecute);
        }

        [Test]
        public async Task TrackSelectedFoldersCommand_CanExecute_UpdatesWhenTrackedFoldersChange()
        {
            _vm = new FolderListViewModel(dbService: _mockDb.Object, fsService: _mockFileSystem.Object, sfService: _mockFiles.Object);
            await WaitForTopLevelChildrenToLoad();

            var selectedItems = _vm.Items.SelectMany(item => item.Children.Take(2));
            _vm.SelectedItems.Add(selectedItems);

            bool? canExecute = null;
            _vm.TrackCommand.CanExecute.Subscribe(x => canExecute = x);

            Assert.IsTrue(canExecute);

            _trackedFolders.AddOrUpdate(selectedItems.Select(x => x.FullPath));
            Assert.IsFalse(canExecute);
        }
    }
}
