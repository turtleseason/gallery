namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;

    using DynamicData;

    using Gallery.Data;
    using Gallery.Entities;
    using Gallery.Entities.SearchParameters;

    using ReactiveUI;

    using Splat;

    public class SearchViewModel : ViewModelBase, IRoutableViewModel
    {
        private readonly IDataService _dbService;
        private readonly ISelectedFilesService _sfService;

        private IEnumerable<Tag> _allTags;
        private ISourceList<SearchCriteriaViewModel> _parameters;
        private ReadOnlyObservableCollection<SearchCriteriaViewModel> _parametersView;

        public SearchViewModel(IScreen screen, IDataService? dbService = null, ISelectedFilesService? sfService = null)
        {
            HostScreen = screen;

            _dbService = dbService ?? Locator.Current.GetService<IDataService>();
            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();

            _allTags = _dbService.GetAllTags();

            _parameters = new SourceList<SearchCriteriaViewModel>();
            _parameters.Add(new SearchCriteriaViewModel(_allTags));

            // canSearch is true whenever all of the child SearchCriteriaViewModels are valid; code based on
            // https://github.com/RolandPheasant/DynamicData.Snippets/blob/master/DynamicData.Snippets/InspectItems/InspectCollectionWithObservable.cs
            var source = _parameters.Connect();

            var validStateChanged = source.MergeMany(vm => vm.ValidationContext.Valid);
            var collectionChanged = source.ToCollection();

            var canSearch = collectionChanged.CombineLatest(validStateChanged, (items, _) =>
                items.Any() && items.All(vm => vm.ValidationContext.GetIsValid()));

            SearchCommand = ReactiveCommand.Create(DoSearch, canSearch);

            source.Bind(out _parametersView).Subscribe();
        }

        public SearchViewModel() : this(null!, null, null)
        { }

        public string? UrlPathSegment => "Search";

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit, Unit> SearchCommand { get; }

        public ReadOnlyObservableCollection<SearchCriteriaViewModel> Parameters => _parametersView;

        public void AddParameter()
        {
            _parameters.Add(new SearchCriteriaViewModel(_allTags));
        }

        public void DoSearch()
        {
            IEnumerable<ISearchParameter> allParams = Parameters.Select(x => x.ToParameter());

            _sfService.SetSearchParameters(new List<ISearchParameter> { allParams });

            (HostScreen as MainShellViewModel)?.GalleryCommand.Execute().Subscribe();
        }

        public void ClearSearch()
        {
            _parameters.Edit(list =>
            {
                list.Clear();
                list.Add(new SearchCriteriaViewModel(_allTags));
            });
        }
    }
}
