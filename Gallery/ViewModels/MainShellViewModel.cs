namespace Gallery.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Reactive;
    using System.Reactive.Linq;

    using Gallery.Models;

    using ReactiveUI;

    public class MainShellViewModel : ViewModelBase, IScreen
    {
        public MainShellViewModel()
        {
            GalleryCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel>(
                _ => Router.Navigate.Execute(new GalleryViewModel(this)));

            SearchCommand = ReactiveCommand.CreateFromObservable<Unit, IRoutableViewModel>(
                _ => Router.Navigate.Execute(new SearchViewModel(this)));

            Title = Router.CurrentViewModel.Select(vm => vm?.UrlPathSegment ?? "null");

            ExecutingCommands = new ObservableCollection<CommandProgressInfo>();

            FolderList = new FolderListViewModel();

            IDisposable disposable = Interactions.ShowCommandProgress.RegisterHandler(context =>
            {
                CommandProgressInfo command = context.Input;
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

        public ReactiveCommand<Unit, Unit> BackCommand => Router.NavigateBack;

        public IObservable<string> Title { get; }

        // Commands that the view should show progress indicators for
        public ObservableCollection<CommandProgressInfo> ExecutingCommands { get; }

        public FolderListViewModel FolderList { get; }
    }
}
