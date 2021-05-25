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
        private static Tag[] tags =
        {
            new Tag("Tag", null),
            new Tag("Value", "Added"),
            new Tag("Hotel", "Trivago", TestUtil.TestTagGroups[1]),
        };

        private Mock<IDatabaseService> dbService;
        private ISourceCache<TagGroup, string> tagGroups;

        private AddTagsViewModel vm;

        [SetUp]
        public void SetUp()
        {
            var testData = TestUtil.GetMockDatabase(true);
            dbService = testData.Db;
            tagGroups = testData.TagGroups;

            vm = new AddTagsViewModel(dbService.Object);
        }

        [TestCaseSource(nameof(tags))]
        public void AddTagsCommand_ClosesAndReturnsTag(Tag input)
        {
            vm.Name = input.Name;
            vm.Value = input.Value;
            vm.SelectedGroup = input.Group;

            object result = null;
            vm.CloseCommand.Subscribe(x => result = x);

            vm.AddTagsCommand.Execute().Subscribe();

            Assert.AreEqual(result, input);
        }

        [Test]
        public void AddTagsCommand_UsesDefaultGroupIfSelectedGroupIsInvalid()
        {
            vm.Name = "Name";
            vm.SelectedGroup = new TagGroup(null, null);

            object result = null;
            vm.CloseCommand.Subscribe(x => result = x);

            vm.AddTagsCommand.Execute().Subscribe();

            Assert.AreEqual((result as Tag?)?.Group.Name, Tag.DefaultGroupName);
        }

        [Test]
        public void AddGroupCommand_CreatesAndSelectsGroup()
        {
            TagGroup group = new TagGroup("MyGroup", "#ABC123");

            vm.IsAddingGroup = true;
            vm.GroupName = group.Name;
            vm.Color = group.Color;

            dbService.Setup(mock => mock.CreateTagGroup(group)).Callback(() => tagGroups.AddOrUpdate(group));

            vm.AddGroupCommand.Execute().Subscribe();

            dbService.Verify(mock => mock.CreateTagGroup(group), Times.Once);
            Assert.AreEqual(group, vm.SelectedGroup);
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
            vm.AddGroupCommand.CanExecute.Subscribe(x => canExecute = x);

            vm.IsAddingGroup = true;
            vm.GroupName = name;
            vm.Color = color;

            Assert.IsFalse(canExecute);
        }
    }
}
