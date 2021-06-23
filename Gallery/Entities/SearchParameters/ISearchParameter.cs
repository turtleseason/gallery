namespace Gallery.Entities.SearchParameters
{
    using System.Collections.Generic;

    using Gallery.Entities;

    public interface ISearchParameter
    {
        public static bool MatchesAllParameters(GalleryFile file, IEnumerable<ISearchParameter> searchParams)
        {
            foreach (ISearchParameter param in searchParams)
            {
                if (!param.Matches(file))
                {
                    return false;
                }
            }

            return true;
        }

        bool Matches(GalleryFile file);
    }
}
