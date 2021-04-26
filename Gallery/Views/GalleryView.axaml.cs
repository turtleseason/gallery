using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views
{
    public class GalleryView : UserControl
    {
        public GalleryView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
