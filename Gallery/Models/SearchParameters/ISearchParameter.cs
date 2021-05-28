namespace Gallery.Models
{
    public interface ISearchParameter
    {
        bool Matches(GalleryFile file);
    }
}
