namespace Gallery.ViewModels
{
    using System.Reactive;

    using ReactiveUI;

    public class MainShellViewModel : ViewModelBase
    {
        private ViewModelBase _primaryView;

        public MainShellViewModel()
        {
            _primaryView = new GalleryViewModel();

            Header = new HeaderViewModel();
            FolderList = new FolderListViewModel();

            SearchCommand = ReactiveCommand.Create<Unit, Unit>(vm =>
            {
                PrimaryView = new SearchViewModel();
                return Unit.Default;
            });

            GalleryCommand = ReactiveCommand.Create<Unit, Unit>(vm =>
            {
                PrimaryView = new GalleryViewModel();
                return Unit.Default;
            });
        }

        // todo: actual navigation lol
        public ReactiveCommand<Unit, Unit> SearchCommand { get; }
        public ReactiveCommand<Unit, Unit> GalleryCommand { get; }

        public HeaderViewModel Header { get; }

        public ViewModelBase PrimaryView { get => _primaryView; private set => this.RaiseAndSetIfChanged(ref _primaryView, value); }

        public FolderListViewModel FolderList { get; }
    }
}
