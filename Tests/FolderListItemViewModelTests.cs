using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Gallery.Services;
using Gallery.ViewModels;

using Moq;

using NUnit.Framework;

namespace Tests
{
    class FolderListItemViewModelTests
    {
        FolderListItemViewModel vm;

        Mock<IDatabaseService> mockDb;

        [SetUp]
        public void SetUp()
        {
            mockDb = new Mock<IDatabaseService>(MockBehavior.Strict);
            vm = null;
        }

        [Test]
        public void IsTracked_IsFalseWhenInitiallyUntracked()
        {
            mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns(Observable.Return(false));
            vm = new FolderListItemViewModel(@"C:\fakepath", dbService: mockDb.Object);
            
            bool? result = null;
            vm.IsTracked.Subscribe(isTracked => result = isTracked);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsTracked_IsTrueWhenInitiallyTracked()
        {
            mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns(Observable.Return(true));
            vm = new FolderListItemViewModel(@"C:\fakepath", dbService: mockDb.Object);

            bool? result = null;
            vm.IsTracked.Subscribe(isTracked => result = isTracked);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsTracked_UpdatesWhenValueChanges()
        {
            var values = new Subject<bool>();
            mockDb.Setup(mock => mock.IsTracked(It.IsAny<string>())).Returns(values);
            vm = new FolderListItemViewModel(@"C:\fakepath", dbService: mockDb.Object);

            bool? result = null;
            vm.IsTracked.Subscribe(isTracked => result = isTracked);

            values.OnNext(false);
            Assert.IsFalse(result);

            values.OnNext(true);
            Assert.IsTrue(result);

            values.OnNext(false);
            Assert.IsFalse(result);
        }
    }
}
