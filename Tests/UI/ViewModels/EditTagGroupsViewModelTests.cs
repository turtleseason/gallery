namespace Tests
{
    using System;

    using Gallery.Data;
    using Gallery.Entities;
    using Gallery.UI.ViewModels;

    using Moq;

    using NUnit.Framework;

    internal class EditTagGroupsViewModelTests
    {
        private Mock<IDataService> _dataService;

        private EditTagGroupsViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            var testData = TestUtil.GetMockDatabase(true);
            _dataService = testData.Db;

            _vm = new EditTagGroupsViewModel(_dataService.Object);
        }

        [Test]
        public void SaveCommand_CreatesAndReturnsGroup()
        {
            TagGroup group = new TagGroup("MyGroup", "#ABC123");

            _vm.EditGroup = false;
            _vm.Name = group.Name;
            _vm.Color = group.Color;

            _dataService.Setup(mock => mock.CreateTagGroup(group));

            object dialogResult = null;
            _vm.CloseCommand.Subscribe(res => dialogResult = res);

            _vm.SaveCommand.Execute().Subscribe();

            _dataService.Verify(mock => mock.CreateTagGroup(group), Times.Once);
            Assert.AreEqual(dialogResult, ((TagGroup? Original, TagGroup Result))(null, group));
        }

        [Test]
        public void SaveCommand_EditsAndReturnsGroup()
        {
            TagGroup original = TestUtil.TestTagGroups[1];
            TagGroup edited = new TagGroup(original.Name + " edited", "#000000");

            _vm.EditGroup = true;
            _vm.SelectedGroup = original;

            _vm.Name = edited.Name;
            _vm.Color = edited.Color;

            _dataService.Setup(mock => mock.UpdateTagGroup(original, edited));

            object dialogResult = null;
            _vm.CloseCommand.Subscribe(res => dialogResult = res);

            _vm.SaveCommand.Execute().Subscribe();

            _dataService.Verify(mock => mock.UpdateTagGroup(original, edited), Times.Once);
            Assert.AreEqual(dialogResult, (Original: original, Result: edited));
        }

        [TestCase("")]
        [TestCase("#12345")]
        [TestCase("#12345 ")]
        [TestCase("#1234567")]
        [TestCase("ff66ff")]
        [TestCase("#ff66gf")]
        public void SaveCommand_CannotExecuteWithInvalidColor(string color)
        {
            bool? canExecute = null;
            _vm.SaveCommand.CanExecute.Subscribe(x => canExecute = x);

            _vm.Color = color;

            Assert.IsFalse(canExecute);
        }

        [TestCase("")]
        [TestCase("    ")]
        [TestCase("\n")]
        public void SaveCommand_CannotExecuteWithInvalidName(string name)
        {
            bool? canExecute = null;
            _vm.SaveCommand.CanExecute.Subscribe(x => canExecute = x);

            _vm.Name = name;

            Assert.IsFalse(canExecute);
        }

        [Test]
        public void SaveCommand_CannotExecuteWithDuplicateName()
        {
            bool? canExecute = null;
            _vm.SaveCommand.CanExecute.Subscribe(x => canExecute = x);

            _vm.EditGroup = false;
            _vm.Name = _vm.TagGroups[0].Name;

            Assert.IsFalse(canExecute);

            _vm.EditGroup = true;
            _vm.SelectedGroup = _vm.TagGroups[1];
            _vm.Name = _vm.TagGroups[0].Name;

            Assert.IsFalse(canExecute);
        }

        [Test]
        public void SaveCommand_CannotExecuteWithDefaultGroupName()
        {
            bool? canExecute = null;
            _vm.SaveCommand.CanExecute.Subscribe(x => canExecute = x);

            _vm.EditGroup = false;
            _vm.Name = TagGroup.DefaultGroupName;

            Assert.IsFalse(canExecute);

            _vm.EditGroup = true;
            _vm.Name = TagGroup.DefaultGroupName;

            Assert.IsFalse(canExecute);
        }

        [Test]
        public void SaveCommand_CanExecuteWhenEditingAndNameMatchesSelectedGroup()
        {
            bool? canExecute = null;
            _vm.SaveCommand.CanExecute.Subscribe(x => canExecute = x);

            _vm.EditGroup = true;
            _vm.Name = _vm.SelectedGroup.Name;

            Assert.IsTrue(canExecute);
        }
    }
}
