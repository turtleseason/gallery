namespace Gallery.ViewModels
{
    using System;
    using System.Diagnostics;
    using System.Reactive;
    using System.Reactive.Linq;

    using ReactiveUI;

    public class MainShellViewModel : ViewModelBase, IScreen
    {
        public MainShellViewModel()
        {
            GalleryCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel>(
                _ => Router.Navigate.Execute(new GalleryViewModel(this)));

            SearchCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel>(
                _ => Router.Navigate.Execute(new SearchViewModel(this)));

            Header = new HeaderViewModel();
            FolderList = new FolderListViewModel();
        }

        public RoutingState Router { get; } = new RoutingState();

        public ReactiveCommand<Unit, IRoutableViewModel> SearchCommand { get; }
        public ReactiveCommand<Unit, IRoutableViewModel> GalleryCommand { get; }

        public ReactiveCommand<Unit, Unit> BackCommand => Router.NavigateBack;

        public HeaderViewModel Header { get; }

        public FolderListViewModel FolderList { get; }
    }
}
