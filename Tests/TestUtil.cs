namespace Tests
{
    using DynamicData;

    using Gallery.Data;
    using Gallery.Entities;

    using Moq;

    internal class TestUtil
    {
        public static readonly TagGroup[] TestTagGroups =
        {
            new TagGroup(Tag.DefaultGroupName),
            new TagGroup("Group", "#ff66ff"),
        };

        public static readonly Tag[] TestTags =
        {
            new Tag("Tag", null),
            new Tag("Value", "Added"),
            new Tag("Hotel", "Trivago", TestTagGroups[1]),
        };

        /// Returns a mock database pre-populated with some fake data.
        public static TestDatabaseUtils GetMockDatabase(bool strictMode)
        {
            var mockDb = new Mock<IDataService>(strictMode ? MockBehavior.Strict : MockBehavior.Loose);

            mockDb.Setup(x => x.GetAllTags()).Returns(TestTags);
            mockDb.Setup(x => x.GetAllTagGroups()).Returns(TestTagGroups);

            return new TestDatabaseUtils { Db = mockDb };
        }

        public class TestDatabaseUtils
        {
            public Mock<IDataService> Db { get; init; }
            public ISourceCache<TagGroup, string> TagGroups { get; init; }
            ////public ISourceCache<string, string> TrackedFolders { get; init; }
        }
    }
}
