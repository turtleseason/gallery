using System.Collections.ObjectModel;

using Gallery.Models;
using Gallery.Services;

using Splat;

namespace Gallery.ViewModels
{
    public class GalleryViewModel : ViewModelBase
    {
        SelectedFilesService _sfService;


        public GalleryViewModel(SelectedFilesService? sfService=null)
        {
            _sfService = sfService ?? Locator.Current.GetService<SelectedFilesService>();
            Items = _sfService.GetFiles();
        }

        // Need to declare explicitly for the XAML designer preview to work
        public GalleryViewModel() : this(null) { }

        public ReadOnlyObservableCollection<GalleryFile> Items { get; }
    }
}
