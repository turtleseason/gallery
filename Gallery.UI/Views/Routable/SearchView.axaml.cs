namespace Gallery.UI.Views
{
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI.ViewModels;

    using ReactiveUI;

    public partial class SearchView : ReactiveUserControl<SearchViewModel>
    {
        public SearchView()
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
