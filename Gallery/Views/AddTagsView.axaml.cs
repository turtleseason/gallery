namespace Gallery.Views
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    public partial class AddTagsView : UserControl
    {
        public AddTagsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
