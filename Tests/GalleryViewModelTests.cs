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
        private Mock<ISelectedFilesService> mockFiles;
        private Mock<IDatabaseService> mockDb;

        private GalleryViewModel vm;

        [SetUp]
        public void SetUp()
        {
            mockFiles = new Mock<ISelectedFilesService>();
            mockDb = TestUtil.GetMockDatabase(true).Db;

            var files = new SourceCache<GalleryFile, string>(x => x.FullPath);
            files.AddOrUpdate(new TrackedFile() { FullPath = @"C:\fakepath\file1.png" });
            files.AddOrUpdate(new TrackedFile() { FullPath = @"C:\fakepath\file_2.jpg" });
            files.AddOrUpdate(new GalleryFile() { FullPath = @"C:\fakepath\filethree.png" });

            mockFiles.Setup(mock => mock.Connect()).Returns(files.Connect());

            vm = new GalleryViewModel(dbService: mockDb.Object, sfService: mockFiles.Object);
        }

        [Test]
        public void AddTagCommand_UsesDialogResultWhenParameterIsNull()
        {
            Tag tag = new Tag("TestTag", "TagValue");
            Interactions.ShowDialog.RegisterHandler(interaction => interaction.SetOutput(tag));

            vm.SelectedItems.AddRange(vm.Items);
            var expectedPaths = vm.SelectedItems.Select(x => x.FullPath).ToArray();

            mockDb.Setup(mock => mock.AddTag(tag, expectedPaths));

            vm.AddTagCommand.Execute().Subscribe();

            mockDb.Verify(mock => mock.AddTag(tag, expectedPaths), Times.Once);
        }

        [Test]
        public void AddTagCommand_DoesNothingWhenDialogReturnsNull()
        {
            Interactions.ShowDialog.RegisterHandler(interaction => interaction.SetOutput(null));

            vm.SelectedItems.AddRange(vm.Items);

            vm.AddTagCommand.Execute().Subscribe();

            mockDb.Verify(mock => mock.AddTag(It.IsAny<Tag>(), It.IsAny<string[]>()), Times.Never);
        }
    }
}
