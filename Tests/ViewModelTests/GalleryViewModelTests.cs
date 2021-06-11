namespace Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using DynamicData;

    using Gallery;
    using Gallery.Models;
    using Gallery.Services;
    using Gallery.ViewModels;

    using Moq;

    using NUnit.Framework;

    internal class GalleryViewModelTests
    {
        private Mock<ISelectedFilesService> _mockFiles;
        private Mock<IDataService> _mockDb;

        private SourceCache<GalleryFile, string> _files;

        private GalleryViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockFiles = new Mock<ISelectedFilesService>();
            _mockDb = TestUtil.GetMockDatabase(true).Db;

            _files = new SourceCache<GalleryFile, string>(x => x.FullPath);
            _files.AddOrUpdate(new TrackedFile() { FullPath = @"C:\fakepath\file1.png" });
            _files.AddOrUpdate(new TrackedFile() { FullPath = @"C:\fakepath\file_2.jpg" });
            _files.AddOrUpdate(new GalleryFile() { FullPath = @"C:\fakepath\filethree.png" });

            _mockFiles.Setup(mock => mock.Connect()).Returns(_files.Connect());

            _vm = new GalleryViewModel(null, dbService: _mockDb.Object, sfService: _mockFiles.Object);
            _vm.Activator.Activate();
        }

        [TearDown]
        public void TearDown()
        {
            _vm.Activator.Deactivate();
        }

        [Test]
        public void AddTagCommand_UsesDialogResultWhenParameterIsNull()
        {
            Tag tag = new Tag("TestTag", "TagValue");
            Interactions.ShowDialog.RegisterHandler(interaction => interaction.SetOutput(tag));

            var expectedPaths = _vm.Items.Select(x => x.File.FullPath).ToArray();

            // Select all items
            _vm.Items.Select(x => _vm.ToggleSelectCommand.Execute(x)).Concat().Subscribe();

            _mockDb.Setup(mock => mock.AddTag(tag, expectedPaths)).Returns(Task.CompletedTask);

            _vm.AddTagCommand.Execute().Subscribe();

            _mockDb.Verify(mock => mock.AddTag(tag, expectedPaths), Times.Once);
        }

        [Test]
        public void AddTagCommand_DoesNothingWhenDialogReturnsNull()
        {
            Interactions.ShowDialog.RegisterHandler(interaction => interaction.SetOutput(null));

            _vm.Items.Select(x => _vm.ToggleSelectCommand.Execute(x)).Concat().Subscribe();

            _vm.AddTagCommand.Execute().Subscribe();

            _mockDb.Verify(mock => mock.AddTag(It.IsAny<Tag>(), It.IsAny<string[]>()), Times.Never);
        }

        [Test]
        public void HasSelection_IsInitiallyFalse()
        {
            bool? result = null;
            _vm.HasSelection.Subscribe(x => result = x);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasSelection_UpdatesOnSelect()
        {
            bool? hasSelection = null;
            _vm.HasSelection.Subscribe(x => hasSelection = x);

            _vm.ToggleSelectCommand.Execute(_vm.Items[0]).Subscribe();
            Assert.IsTrue(hasSelection);
            Assert.IsTrue(_vm.Items[0].IsSelected);

            _vm.ToggleSelectCommand.Execute(_vm.Items[1]).Subscribe();
            Assert.IsTrue(hasSelection);
            Assert.IsTrue(_vm.Items[0].IsSelected);
            Assert.IsTrue(_vm.Items[1].IsSelected);
        }

        [Test]
        public void HasSelection_UpdatesOnDeselect()
        {
            bool? hasSelection = null;
            _vm.HasSelection.Subscribe(x => hasSelection = x);

            _vm.ToggleSelectCommand.Execute(_vm.Items[0]).Subscribe();
            _vm.ToggleSelectCommand.Execute(_vm.Items[1]).Subscribe();

            _vm.ToggleSelectCommand.Execute(_vm.Items[0]).Subscribe();
            Assert.IsTrue(hasSelection);
            Assert.IsFalse(_vm.Items[0].IsSelected);
            Assert.IsTrue(_vm.Items[1].IsSelected);

            _vm.ToggleSelectCommand.Execute(_vm.Items[1]).Subscribe();
            Assert.IsFalse(hasSelection);
            Assert.IsFalse(_vm.Items[0].IsSelected);
            Assert.IsFalse(_vm.Items[1].IsSelected);
        }

        [Test]
        public void IsSelected_ContinuesUpdatingAfterFileUpdate()
        {
            string path = _vm.Items[0].File.FullPath;

            _vm.ToggleSelectCommand.Execute(_vm.Items[0]).Subscribe();

            _files.AddOrUpdate(new TrackedFile() { FullPath = path, Description = "updated file with description" });
            _vm.DeselectAllCommand.Execute().Subscribe();

            Assert.IsFalse(_vm.Items.Single(x => x.File.FullPath == path).IsSelected);
        }
    }
}
