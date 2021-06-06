namespace Gallery.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
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

    public class GalleryViewModel : ViewModelBase, IRoutableViewModel
    {
        private readonly ISelectedFilesService _sfService;
        private readonly IDataService _dbService;

        private ReadOnlyObservableCollection<GalleryThumbnailViewModel>? _items;

        public GalleryViewModel(IScreen screen, ISelectedFilesService? sfService = null, IDataService? dbService = null)
        {
            HostScreen = screen;

            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();

            SelectedItems = new ObservableCollection<GalleryThumbnailViewModel>();

            AddTagCommand = ReactiveCommand.CreateFromTask<Tag?, Unit>(async tag =>
            {
                await AddTag(tag);
                return Unit.Default;
            });

            this.WhenActivated((CompositeDisposable disposables) =>
            {
                _sfService.Connect()
                .Transform(file => new GalleryThumbnailViewModel(file))
                .Sort(SortExpressionComparer<GalleryThumbnailViewModel>.Ascending(vm => vm.File.FullPath))  // todo: sort in SFS (so it applies everywhere)
                .Bind(out _items)
                .Subscribe()
                .DisposeWith(disposables);

                this.RaisePropertyChanged(nameof(Items));
            });
        }

        // Need to declare parameterless constructor explicitly for the XAML designer preview to work
        public GalleryViewModel() : this(null!, null, null)
        { }

        public string? UrlPathSegment => "Gallery";

        public IScreen HostScreen { get; }

        public ReactiveCommand<Tag?, Unit> AddTagCommand { get; }

        public ReadOnlyObservableCollection<GalleryThumbnailViewModel>? Items => _items;

        public ObservableCollection<GalleryThumbnailViewModel> SelectedItems { get; }

        private async Task AddTag(Tag? tag)
        {
            var files = SelectedItems.Select(x => x.File.FullPath).ToArray();

            tag ??= (Tag?)await Interactions.ShowDialog.Handle(new AddTagsViewModel(_dbService));

            if (tag != null)
            {
                _dbService.AddTag((Tag)tag, files);
            }
        }
    }
}