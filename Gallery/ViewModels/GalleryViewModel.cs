namespace Gallery.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using DynamicData;
    using DynamicData.Aggregation;
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

        private ISourceCache<GalleryThumbnailViewModel, string> _selectedItems;

        private ReadOnlyObservableCollection<GalleryThumbnailViewModel>? _items;

        public GalleryViewModel(IScreen screen, ISelectedFilesService? sfService = null, IDataService? dbService = null)
        {
            HostScreen = screen;

            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();

            _selectedItems = new SourceCache<GalleryThumbnailViewModel, string>(vm => vm.File.FullPath);

            SelectionCount = _selectedItems.Connect().Count().StartWith(0);
            HasSelection = SelectionCount.Select(count => count > 0);

            AddTagCommand = ReactiveCommand.CreateFromTask<Tag?, Unit>(async tag =>
            {
                await AddTag(tag);
                return Unit.Default;
            });

            ToggleSelectCommand = ReactiveCommand.Create<GalleryThumbnailViewModel>(ToggleSelect);
            DeselectAllCommand = ReactiveCommand.Create(DeselectAll);

            this.WhenActivated((CompositeDisposable disposables) =>
            {
                _sfService.Connect()
                .Transform(file => new GalleryThumbnailViewModel(file))
                .ForEachChange(UpdateSelectionOnItemsChanged)
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
        public ReactiveCommand<GalleryThumbnailViewModel, Unit> ToggleSelectCommand { get; }
        public ReactiveCommand<Unit, Unit> DeselectAllCommand { get; }

        public ReadOnlyObservableCollection<GalleryThumbnailViewModel>? Items => _items;

        public IObservable<int> SelectionCount { get; }
        public IObservable<bool> HasSelection { get; }

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

        private void DeselectAll()
        {
            foreach (var vm in _selectedItems.Items)
            {
                vm.IsSelected = false;
            }

            _selectedItems.Clear();
        }

        private void UpdateSelectionOnItemsChanged(Change<GalleryThumbnailViewModel, string> change)
        {
            switch (change.Reason)
            {
                case DynamicData.ChangeReason.Add:
                case DynamicData.ChangeReason.Refresh:
                case DynamicData.ChangeReason.Update:
                    bool isSelected = _selectedItems.Lookup(change.Current.File.FullPath).HasValue;
                    change.Current.IsSelected = isSelected;
                    if (isSelected)
                    {
                        _selectedItems.AddOrUpdate(change.Current);
                    }
                    break;
                case DynamicData.ChangeReason.Remove:
                    _selectedItems.Remove(key: change.Current.File.FullPath);
                    break;
            }
        }

        private async Task AddTag(Tag? tag)
        {
            var files = _selectedItems.Items.Select(vm => vm.File.FullPath).ToArray();

            tag ??= (Tag?)await Interactions.ShowDialog.Handle(new AddTagsViewModel(_dbService));

            if (tag != null)
            {
                await _dbService.AddTag((Tag)tag, files);
            }
        }
    }
}