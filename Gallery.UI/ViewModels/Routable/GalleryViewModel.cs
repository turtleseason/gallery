namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading.Tasks;

    using DynamicData;
    using DynamicData.Aggregation;
    using DynamicData.Binding;

    using Gallery.Data;
    using Gallery.Entities;
    using Gallery.Entities.SearchParameters;
    using Gallery.UI;

    using ReactiveUI;

    using Splat;

    public class GalleryViewModel : ViewModelBase, IRoutableViewModel
    {
        private static readonly int _maxParallelThumbnailLoads = 10;

        private readonly ISelectedFilesService _sfService;
        private readonly IDataService _dbService;

        private ISourceCache<GalleryThumbnailViewModel, string> _selectedItems;
        private Subject<GalleryThumbnailViewModel> _thumbnailsToLoad;

        private ReadOnlyObservableCollection<GalleryThumbnailViewModel>? _items;

        private bool _selectMode = false;

        public GalleryViewModel(IScreen screen, ISelectedFilesService? sfService = null, IDataService? dbService = null)
        {
            HostScreen = screen;

            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();

            _selectedItems = new SourceCache<GalleryThumbnailViewModel, string>(vm => vm.File.FullPath);

            SelectionCount = _selectedItems.Connect().Count().StartWith(0);
            HasSelection = SelectionCount.Select(count => count > 0);

            AddTagCommand = ReactiveCommand.CreateFromTask(AddTag, HasSelection);
            EditTagsCommand = ReactiveCommand.CreateFromTask(EditTags, HasSelection);

            ToggleSelectModeCommand = ReactiveCommand.Create(ToggleSelectMode);

            ToggleSelectCommand = ReactiveCommand.Create<GalleryThumbnailViewModel>(ToggleSelect);
            SelectAllCommand = ReactiveCommand.Create(SelectAll);
            DeselectAllCommand = ReactiveCommand.Create(DeselectAll);

            ClearSearchCommand = ReactiveCommand.Create(ClearSearch);

            _thumbnailsToLoad = new Subject<GalleryThumbnailViewModel>();
            _thumbnailsToLoad.Select(vm =>
                Observable.FromAsync(async () =>
                    {
                        if (_items!.Contains(vm))
                        {
                            vm.Thumbnail = await vm.LoadThumbnail();
                        }
                    }, RxApp.MainThreadScheduler))
                .Merge(_maxParallelThumbnailLoads)
                .Subscribe(_ => { }, error => RxApp.DefaultExceptionHandler.OnNext(error));

            HasSearch = _sfService.SearchParameters.Select(parameters => parameters.Any());
            SearchString = _sfService.SearchParameters.Select(parameters =>
                parameters.Any() ? "Showing files " + string.Join(" + files ", parameters) : null);

            // This subscription isn't disposed on deactivation, since repopulating can be a bit slow for a large collection;
            // this operates under the assumption that the app will reuse a single GalleryViewModel for its whole lifetime,
            // so cleaning up resources properly isn't too big of a concern
            _sfService.SelectedFiles.ToObservableChangeSet()
                .Transform(file => new GalleryThumbnailViewModel(file))
                .Bind(out _items)
                .ActOnEveryObject(OnItemAdded, OnItemRemoved);
        }

        public string? UrlPathSegment => "Gallery";

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit, Unit> ToggleSelectModeCommand { get; }

        public ReactiveCommand<Unit, Unit> AddTagCommand { get; }
        public ReactiveCommand<Unit, Unit> EditTagsCommand { get; }

        public ReactiveCommand<GalleryThumbnailViewModel, Unit> ToggleSelectCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }
        public ReactiveCommand<Unit, Unit> DeselectAllCommand { get; }

        public ReactiveCommand<Unit, Unit> ClearSearchCommand { get; }

        public ReadOnlyObservableCollection<GalleryThumbnailViewModel>? Items => _items;

        public bool SelectMode { get => _selectMode; set => this.RaiseAndSetIfChanged(ref _selectMode, value); }

        public IObservable<int> SelectionCount { get; }
        public IObservable<bool> HasSelection { get; }

        public IObservable<bool> HasSearch { get; }
        public IObservable<string?> SearchString { get; }

        private void ToggleSelectMode()
        {
            SelectMode = !SelectMode;
        }

        private void ToggleSelect(GalleryThumbnailViewModel item)
        {
            bool isAdding = !_selectedItems.Lookup(item.File.FullPath).HasValue;

            if (isAdding)
            {
                _selectedItems.AddOrUpdate(item);
            }
            else
            {
                _selectedItems.Remove(item);
            }

            item.IsSelected = isAdding;
        }

        private void SelectAll()
        {
            _selectedItems.Edit(updater =>
            {
                foreach (var vm in _items!)
                {
                    updater.AddOrUpdate(vm);
                    vm.IsSelected = true;
                }
            });
        }

        private void DeselectAll()
        {
            foreach (var vm in _selectedItems.Items)
            {
                vm.IsSelected = false;
            }

            _selectedItems.Clear();
        }

        private void OnItemAdded(GalleryThumbnailViewModel item)
        {
            bool isSelected = _selectedItems.Lookup(item.File.FullPath).HasValue;
            item.IsSelected = isSelected;
            if (isSelected)
            {
                _selectedItems.AddOrUpdate(item);
            }

            _thumbnailsToLoad.OnNext(item);
        }

        private void OnItemRemoved(GalleryThumbnailViewModel item)
        {
            _selectedItems.Remove(key: item.File.FullPath);
            item.Thumbnail?.Dispose();
            item.Thumbnail = null;
        }

        private async Task AddTag()
        {
            var files = _selectedItems.Items.Select(vm => vm.File.FullPath).ToArray();

            Tag? tag = (Tag?)await Interactions.ShowDialog.Handle(new AddTagsViewModel(_dbService));

            if (tag != null)
            {
                await _dbService.AddTag((Tag)tag, files);
            }
        }

        private async Task EditTags()
        {
            var files = _selectedItems.Items.Select(vm => vm.File).ToArray();
            await Interactions.ShowDialog.Handle(new EditTagsViewModel(_dbService, files));
        }

        private void ClearSearch()
        {
            _sfService.SetSearchParameters(new List<ISearchParameter>());
        }
    }
}
