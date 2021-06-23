namespace Tests
{
    using System.Linq;
    using System.Reactive.Linq;

    using Gallery.Data;
    using Gallery.UI.ViewModels;

    using Moq;

    using NUnit.Framework;

    internal class SearchViewModelTests
    {
        private Mock<IDataService> _mockDb;
        private Mock<ISelectedFilesService> _mockSf;

        private SearchViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockDb = TestUtil.GetMockDatabase(false).Db;
            _mockSf = new Mock<ISelectedFilesService>(MockBehavior.Strict);

            _vm = new SearchViewModel(null, _mockDb.Object, _mockSf.Object);
            _vm.Activator.Activate();
        }

        [TearDown]
        public void TearDown()
        {
            _vm.Activator.Deactivate();
        }

        [Test]
        public void FieldsAreValidOnActivationIfDbContainsTags()
        {
            Assert.IsNotNull(_vm.SelectedTag.Name);
            Assert.IsNotNull(_vm.SelectedValue);
        }

        [Test]
        public void FieldsAreValidAfterChangingTag()
        {
            _vm.SelectedTag = _vm.Tags.Where(x => x.Name == TestUtil.TestTags[1].Name).Single();
            _vm.SelectedValue = _vm.Values.Where(x => x.Value == TestUtil.TestTags[1].Value).Single();

            _vm.SelectedTag = _vm.Tags.Where(x => x.Name == TestUtil.TestTags[0].Name).Single();

            Assert.IsNotNull(_vm.SelectedValue);
        }

        [Test]
        // This test isn't very robust to changes in TestUtils.TestTags;
        // if it's failing, check whether TestTags has changed (and maybe write a better test lol)
        public void ChangingTagUpdatesValueOptions()
        {
            _vm.SelectedTag = _vm.Tags.Where(x => x.Name == TestUtil.TestTags[1].Name).Single();

            Assert.NotZero(_vm.Values.Where(x => x.Value == TestUtil.TestTags[1].Value).Count());

            _vm.SelectedTag = _vm.Tags.Where(x => x.Name == TestUtil.TestTags[2].Name).Single();

            Assert.Zero(_vm.Values.Where(x => x.Value == TestUtil.TestTags[1].Value).Count());
            Assert.NotZero(_vm.Values.Where(x => x.Value == TestUtil.TestTags[2].Value).Count());
        }
    }
}
