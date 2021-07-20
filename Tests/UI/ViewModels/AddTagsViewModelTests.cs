namespace Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using Gallery.Data;
    using Gallery.Entities;
    using Gallery.UI.ViewModels;

    using Moq;

    using NUnit.Framework;

    internal class AddTagsViewModelTests
    {
        private static Tag[] _testTags = TestUtil.TestTags;

        private Mock<IDataService> _dbService;

        private AddTagsViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            var testData = TestUtil.GetMockDatabase(true);
            _dbService = testData.Db;

            _vm = new AddTagsViewModel(_dbService.Object);
        }

        [TestCaseSource(nameof(_testTags))]
        public void AddTagsCommand_ClosesAndReturnsTag(Tag input)
        {
            _vm.Name = input.Name;
            _vm.Value = input.Value;
            _vm.SelectedGroup = input.Group;

            object result = null;
            _vm.CloseCommand.Subscribe(x => result = x);

            _vm.AddTagsCommand.Execute().Subscribe();

            Assert.AreEqual(result, input);
        }

        [Test]
        public void SetTagGroupIfTagExists_SetsCorrectValues_WhenTagExists()
        {
            TagGroup initialGroup = _vm.SelectedGroup;

            Tag tag = TestUtil.TestTags[2];
            Assert.AreNotEqual(initialGroup, tag.Group);

            _vm.Name = tag.Name;
            _vm.SetTagGroupIfTagExists();

            Assert.IsTrue(_vm.LockSelectedGroup);
            Assert.AreEqual(tag.Group, _vm.SelectedGroup);
        }

        [Test]
        public void SetTagGroupIfTagExists_SetsCorrectValues_WhenTagDoesNotExist()
        {
            TagGroup initialGroup = _vm.SelectedGroup;

            string notATag = "Nonexistent Tag Name";
            Assert.That(!_dbService.Object.GetAllTags().Any(tag => tag.Name == notATag));

            _vm.Name = notATag;
            _vm.SetTagGroupIfTagExists();

            Assert.AreEqual(initialGroup, _vm.SelectedGroup);
            Assert.IsFalse(_vm.LockSelectedGroup);
        }
    }
}
