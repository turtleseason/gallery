namespace Gallery.UI.Views
{
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI.ViewModels;

    using ReactiveUI;

    public class FolderListView : ReactiveUserControl<FolderListViewModel>
    {
        public FolderListView()
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
