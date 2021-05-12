using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

using Gallery.ViewModels;

using ReactiveUI;

namespace Gallery.Views
{
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
