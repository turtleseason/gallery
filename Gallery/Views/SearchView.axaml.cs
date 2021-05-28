namespace Gallery.Views
{
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.ViewModels;

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
