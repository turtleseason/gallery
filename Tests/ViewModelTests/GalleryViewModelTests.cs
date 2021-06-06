namespace Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;

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

        private GalleryViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockFiles = new Mock<ISelectedFilesService>();
            _mockDb = TestUtil.GetMockDatabase(true).Db;

            var files = new SourceCache<GalleryFile, string>(x => x.FullPath);
            files.AddOrUpdate(new TrackedFile() { FullPath = @"C:\fakepath\file1.png" });
            files.AddOrUpdate(new TrackedFile() { FullPath = @"C:\fakepath\file_2.jpg" });
            files.AddOrUpdate(new GalleryFile() { FullPath = @"C:\fakepath\filethree.png" });

            _mockFiles.Setup(mock => mock.Connect()).Returns(files.Connect());

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

            _vm.SelectedItems.AddRange(_vm.Items);
            var expectedPaths = _vm.SelectedItems.Select(x => x.File.FullPath).ToArray();

            _mockDb.Setup(mock => mock.AddTag(tag, expectedPaths));

            _vm.AddTagCommand.Execute().Subscribe();

            _mockDb.Verify(mock => mock.AddTag(tag, expectedPaths), Times.Once);
        }

        [Test]
        public void AddTagCommand_DoesNothingWhenDialogReturnsNull()
        {
            Interactions.ShowDialog.RegisterHandler(interaction => interaction.SetOutput(null));

            _vm.SelectedItems.AddRange(_vm.Items);

            _vm.AddTagCommand.Execute().Subscribe();

            _mockDb.Verify(mock => mock.AddTag(It.IsAny<Tag>(), It.IsAny<string[]>()), Times.Never);
        }
    }
}
