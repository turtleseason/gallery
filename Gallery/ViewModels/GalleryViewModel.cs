namespace Gallery.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using DynamicData;
    using DynamicData.Binding;

    using Gallery;
    using Gallery.Models;
    using Gallery.Services;

    using ReactiveUI;

    using Splat;

    public class GalleryViewModel : ViewModelBase, IActivatableViewModel
    {
        private readonly ISelectedFilesService _sfService;
        private readonly IDatabaseService _dbService;

        private readonly ReadOnlyObservableCollection<GalleryFile> _items;

        public GalleryViewModel(ISelectedFilesService? sfService = null, IDatabaseService? dbService = null)
        {
            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();
            _dbService = dbService ?? Locator.Current.GetService<IDatabaseService>();

            IDisposable subscription = _sfService.Connect()
                .Sort(SortExpressionComparer<GalleryFile>.Ascending(file => file.FullPath))  // todo: sort in SFS (so it applies everywhere)
                .Bind(out _items)
                .Subscribe();

            SelectedItems = new ObservableCollection<GalleryFile>();

            AddTagCommand = ReactiveCommand.CreateFromTask<Tag?, Unit>(async tag =>
                {
                    await AddTag(tag);
                    return Unit.Default;
                });

            this.WhenActivated((CompositeDisposable disposables) => subscription.DisposeWith(disposables));
        }

        // Need to declare parameterless constructor explicitly for the XAML designer preview to work
        public GalleryViewModel() : this(null, null)
        { }

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public ReadOnlyObservableCollection<GalleryFile> Items => _items;

        public ObservableCollection<GalleryFile> SelectedItems { get; }

        public ReactiveCommand<Tag?, Unit> AddTagCommand { get; }

        private async Task AddTag(Tag? tag)
        {
            tag ??= (Tag?)await Interactions.ShowDialog.Handle(new AddTagsViewModel());
            if (tag == null)
            {
                return;
            }

            var files = SelectedItems.Select(x => x.FullPath).ToArray();
            _dbService.AddTag((Tag)tag, files);
        }
    }
}