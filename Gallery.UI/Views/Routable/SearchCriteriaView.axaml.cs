namespace Gallery.UI.Views
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    public partial class SearchCriteriaView : UserControl
    {
        public SearchCriteriaView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
