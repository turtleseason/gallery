using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Gallery.ViewModels;

using ReactiveUI;

namespace Gallery.Views
{
    public class GalleryView : ReactiveUserControl<GalleryViewModel>
    {
        public GalleryView()
        {
            InitializeComponent();
            this.WhenActivated(d => { });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
