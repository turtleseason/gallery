using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views
{
    public class FolderListView : UserControl
    {
        public FolderListView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
