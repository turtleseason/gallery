namespace Tests
{
    using System;

    using DynamicData;

    using Gallery.Models;
    using Gallery.Services;
    using Gallery.ViewModels;

    using Moq;

    using NUnit.Framework;

    internal class AddTagsViewModelTests
    {
        private static Tag[] _testTags = TestUtil.TestTags;

        private Mock<IDataService> _dbService;
        private ISourceCache<TagGroup, string> _tagGroups;

        private AddTagsViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            var testData = TestUtil.GetMockDatabase(true);
            _dbService = testData.Db;
            _tagGroups = testData.TagGroups;

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
        public void AddTagsCommand_UsesDefaultGroupIfSelectedGroupIsInvalid()
        {
            _vm.Name = "Name";
            _vm.SelectedGroup = new TagGroup(null, null);

            object result = null;
            _vm.CloseCommand.Subscribe(x => result = x);

            _vm.AddTagsCommand.Execute().Subscribe();

            Assert.AreEqual((result as Tag?)?.Group.Name, Tag.DefaultGroupName);
        }

        [Test]
        public void AddGroupCommand_CreatesAndSelectsGroup()
        {
            TagGroup group = new TagGroup("MyGroup", "#ABC123");

            _vm.IsAddingGroup = true;
            _vm.GroupName = group.Name;
            _vm.Color = group.Color;

            _dbService.Setup(mock => mock.CreateTagGroup(group));

            _vm.AddGroupCommand.Execute().Subscribe();

            _dbService.Verify(mock => mock.CreateTagGroup(group), Times.Once);
            Assert.AreEqual(group, _vm.SelectedGroup);
        }

        [TestCase("Name", "")]
        [TestCase("", "#ff66ff")]
        [TestCase("    ", "#ff66ff")]
        [TestCase("Name", "#12345")]
        [TestCase("Name", "#12345 ")]
        [TestCase("Name", "#1234567")]
        [TestCase("Name", "ff66ff")]
        [TestCase("Name", "#ff66gf")]
        public void AddGroupCommand_CannotExecuteWithInvalidInput(string name, string color)
        {
            bool? canExecute = null;
            _vm.AddGroupCommand.CanExecute.Subscribe(x => canExecute = x);

            _vm.IsAddingGroup = true;
            _vm.GroupName = name;
            _vm.Color = color;

            Assert.IsFalse(canExecute);
        }
    }
}
