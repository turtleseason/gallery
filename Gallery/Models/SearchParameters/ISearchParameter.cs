namespace Gallery.Models
{
    using System.Collections.Generic;

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
