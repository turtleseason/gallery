namespace Tests
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using Gallery.Services;
    using Gallery.ViewModels;

    using Moq;

    using NUnit.Framework;

    internal class FolderListItemViewModelTests
    {
        private FolderListItemViewModel _vm;

        private Mock<IDataService> _mockDb;

        [SetUp]
        public void SetUp()
        {
            _mockDb = new Mock<IDataService>(MockBehavior.Strict);
            _vm = null;
        }

        [Test]
        public void IsTracked_IsFalseWhenInitiallyUntracked()
        {
            _mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns(Observable.Return(false));
            _vm = new FolderListItemViewModel(@"C:\fakepath", dbService: _mockDb.Object);

            bool? result = null;
            _vm.IsTracked.Subscribe(isTracked => result = isTracked);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsTracked_IsTrueWhenInitiallyTracked()
        {
            _mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns(Observable.Return(true));
            _vm = new FolderListItemViewModel(@"C:\fakepath", dbService: _mockDb.Object);

            bool? result = null;
            _vm.IsTracked.Subscribe(isTracked => result = isTracked);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsTracked_UpdatesWhenValueChanges()
        {
            var values = new Subject<bool>();
            _mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns(values);
            _vm = new FolderListItemViewModel(@"C:\fakepath", dbService: _mockDb.Object);

            bool? result = null;
            _vm.IsTracked.Subscribe(isTracked => result = isTracked);

            values.OnNext(false);
            Assert.IsFalse(result);

            values.OnNext(true);
            Assert.IsTrue(result);

            values.OnNext(false);
            Assert.IsFalse(result);
        }
    }
}
