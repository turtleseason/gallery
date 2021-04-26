namespace Gallery.ViewModels
{
    public class MainShellViewModel : ViewModelBase
    {
        public MainShellViewModel()
        {
            PrimaryView = new GalleryViewModel();
            FolderList = new FolderListViewModel();
        }

        public ViewModelBase PrimaryView { get; }

        public FolderListViewModel FolderList { get; }
    }
}
