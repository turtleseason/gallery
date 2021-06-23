namespace Gallery.Entities.SearchParameters
{
    using System.Linq;

    public class Tagged : ISearchParameter
    {
        private readonly Tag _tag;
        private readonly bool _ignoreValue;

        public Tagged(Tag tag, bool ignoreValue = false)
        {
            _tag = tag;
            _ignoreValue = ignoreValue;
        }

        public bool Matches(GalleryFile file)
        {
            if (file is not TrackedFile tracked)
            {
                return false;
            }

            if (_ignoreValue)
            {
                return tracked.Tags.Any(tag => tag.Name == _tag.Name);
            }
            else
            {
                return tracked.Tags.Contains(_tag);
            }
        }
    }
}
