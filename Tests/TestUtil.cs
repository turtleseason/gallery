namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using DynamicData;

    using Gallery.Models;
    using Gallery.Services;

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
            var mockDb = new Mock<IDatabaseService>(strictMode ? MockBehavior.Strict : MockBehavior.Loose);

            var tagGroups = new SourceCache<TagGroup, string>(x => x.Name);
            tagGroups.AddOrUpdate(TestTagGroups);
            mockDb.Setup(mock => mock.TagGroups()).Returns(tagGroups.Connect());

            var tagsNames = new SourceCache<Tag, string>(x => x.Name);
            tagsNames.AddOrUpdate(TestTags.Select(x => new Tag(x.Name, group: x.Group)).Distinct());
            mockDb.Setup(mock => mock.TagNames()).Returns(tagsNames.Connect());

            var tags = new SourceCache<Tag, Tag>(x => new Tag(x.Name, x.Value));
            tags.AddOrUpdate(TestTags);
            mockDb.Setup(mock => mock.Tags()).Returns(tags.Connect());

            return new TestDatabaseUtils { Db = mockDb, TagGroups = tagGroups };
        }

        public class TestDatabaseUtils
        {
            public Mock<IDatabaseService> Db { get; init; }
            public ISourceCache<TagGroup, string> TagGroups { get; init; }
            ////public ISourceCache<string, string> TrackedFolders { get; init; }
        }
    }
}
