namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Reactive;
    using System.Reactive.Linq;

    using Gallery.Entities;
    using Gallery.UI;

    using ReactiveUI;

    public class MainShellViewModel : ViewModelBase, IScreen
    {
        private readonly GalleryViewModel _galleryVm;
        private readonly SearchViewModel _searchVm;

        public MainShellViewModel()
        {
            _galleryVm = new GalleryViewModel(this);
            _searchVm = new SearchViewModel(this);

            GalleryCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel>(
                _ => Router.Navigate.Execute(_galleryVm));

            SearchCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel>(
                _ => Router.Navigate.Execute(_searchVm));

            FileViewCommand = ReactiveCommand.CreateFromObservable<GalleryFile, IRoutableViewModel>(
                file => Router.Navigate.Execute(new SingleFileViewModel(this, file)));

            Title = Router.CurrentViewModel.Select(vm => vm?.UrlPathSegment ?? "null");

            ExecutingCommands = new ObservableCollection<CommandStatusViewModel>();

            FolderList = new FolderListViewModel();

            IDisposable disposable = Interactions.ShowCommandProgress.RegisterHandler(context =>
            {
                CommandStatusViewModel command = context.Input;
                ExecutingCommands.Add(command);
                command.IsExecuting.Where(x => !x).Take(1)
                    .Subscribe(_ => ExecutingCommands.Remove(command));
                context.SetOutput(Unit.Default);
            });

            this.WhenActivated(d => d(disposable));
        }

        public RoutingState Router { get; } = new RoutingState();

        public ReactiveCommand<Unit, IRoutableViewModel> SearchCommand { get; }
        public ReactiveCommand<Unit, IRoutableViewModel> GalleryCommand { get; }
        public ReactiveCommand<GalleryFile, IRoutableViewModel> FileViewCommand { get; }

        public ReactiveCommand<Unit, Unit> BackCommand => Router.NavigateBack;

        public IObservable<string> Title { get; }

        // Commands that the view should show progress indicators for
        public ObservableCollection<CommandStatusViewModel> ExecutingCommands { get; }

        public FolderListViewModel FolderList { get; }
    }
}
