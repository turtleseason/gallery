using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallery.Views
{
    public class MainShellView : UserControl
    {
        public MainShellView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
