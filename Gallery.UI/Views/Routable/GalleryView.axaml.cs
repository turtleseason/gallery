namespace Gallery.UI.Views
{
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI.ViewModels;

    using ReactiveUI;

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
