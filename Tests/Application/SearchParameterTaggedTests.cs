namespace Tests
{
    using Gallery.Entities;
    using Gallery.Entities.SearchParameters;

    using NUnit.Framework;

    internal class SearchParameterTaggedTests
    {
        private static Tag[] _testTags = TestUtil.TestTags;

        [TestCaseSource(nameof(_testTags))]
        public void Matches_TrueWhenInputMatches(Tag input)
        {
            var file = new TrackedFile { FullPath = "C:/fakepath/file.png" };
            file.Tags.Add(input);

            var searchParam = new Tagged(new Tag(input.Name, input.Value, input.Group), false);

            Assert.IsTrue(searchParam.Matches(file));
        }

        [Test]
        public void Matches_FalseWhenNoTagsMatch()
        {
            var file = new TrackedFile { FullPath = "C:/fakepath/file.png" };
            file.Tags.Add(new Tag("Tag", "Value", new TagGroup("Group")));
            file.Tags.Add(new Tag("Tag"));
            file.Tags.Add(new Tag("tag", "Value"));

            var searchParam = new Tagged(new Tag("Tag", "Value"), false);

            Assert.IsFalse(searchParam.Matches(file));
        }

        [Test]
        public void Matches_FalseWhenInputHasNoTags()
        {
            var file = new TrackedFile { FullPath = "C:/fakepath/file.png" };

            var searchParam = new Tagged(new Tag("Tag", "Value"), false);

            Assert.IsFalse(searchParam.Matches(file));
        }

        [TestCase(null)]
        [TestCase("Value")]
        [TestCase("OtherValue")]
        [TestCase("")]
        public void Matches_TrueWhenInputMatchesIgnoringValue(string value)
        {
            var file = new TrackedFile { FullPath = "C:/fakepath/file.png" };
            file.Tags.Add(new Tag("Tag", value));

            var searchParam = new Tagged(new Tag("Tag", "Value"), true);

            Assert.IsTrue(searchParam.Matches(file));
        }
    }
}
