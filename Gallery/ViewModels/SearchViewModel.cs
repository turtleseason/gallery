namespace Gallery.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;

    using DynamicData;

    using Gallery.Models;
    using Gallery.Services;

    using ReactiveUI;

    using Splat;

    using Parameter = Gallery.Models.SearchParameters;

    public class SearchViewModel : ViewModelBase, IRoutableViewModel
    {
        private static readonly ValueOption[] _defaultOptions = new ValueOption[]
        {
            new ValueOption { Filter = ValueFilter.Any, Value = "(Any value)" },
            new ValueOption { Filter = ValueFilter.None, Value = "(No value)" },
        };

        private readonly IDataService _dbService;
        private readonly ISelectedFilesService _sfService;

        private IEnumerable<Tag>? _allTags;

        private Tag _selectedTag;
        private ValueOption _selectedValue;

        public SearchViewModel(IScreen screen, IDataService? dbService = null, ISelectedFilesService? sfService = null)
        {
            HostScreen = screen;

            _dbService = dbService ?? Locator.Current.GetService<IDataService>();
            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();

            _allTags = _dbService.GetAllTags();

            Tags = new ObservableCollection<Tag>(_allTags.GroupBy(x => x.Name)
                                                         .Select(x => new Tag(x.Key, group: x.First().Group))
                                                         .OrderBy(x => x.Name));
            Values = new ObservableCollection<ValueOption>();

            this.WhenAnyValue(x => x.SelectedTag)
                .Subscribe(_ => UpdateValues());

            SelectedTag = Tags.Count() > 1 ? Tags[0] : default;
            _selectedValue = _defaultOptions[0];
        }

        public SearchViewModel() : this(null!, null, null)
        { }

        public enum ValueFilter { Any, None, Value }

        public string? UrlPathSegment => "Search";

        public IScreen HostScreen { get; }

        public ObservableCollection<Tag> Tags { get; set; }
        public ObservableCollection<ValueOption> Values { get; set; }

        public Tag SelectedTag { get => _selectedTag; set => this.RaiseAndSetIfChanged(ref _selectedTag, value); }
        public ValueOption SelectedValue { get => _selectedValue; set => this.RaiseAndSetIfChanged(ref _selectedValue, value); }

        public void DoSearch()
        {
            if (SelectedTag.Name == null || SelectedValue == null)
            {
                return;
            }

            string? value = (SelectedValue.Filter == ValueFilter.Value) ? SelectedValue.Value : null;
            bool ignoreValue = SelectedValue.Filter == ValueFilter.Any;

            var searchParameter = new Parameter.Tagged(new Tag(SelectedTag.Name, value, SelectedTag.Group), ignoreValue);

            _sfService.SetSearchParameters(new List<ISearchParameter> { searchParameter });

            (HostScreen as MainShellViewModel)?.BackCommand.Execute().Subscribe();
        }

        public void ClearSearch()
        {
            _sfService.SetSearchParameters(new List<ISearchParameter>());
            (HostScreen as MainShellViewModel)?.BackCommand.Execute().Subscribe();
        }

        private void UpdateValues()
        {
            Values.Clear();
            Values.AddRange(_defaultOptions);
            Values.AddRange(_allTags!.Where(tag => tag.Name == SelectedTag.Name && tag.Value != null)
                                     .Select(tag => new ValueOption { Filter = ValueFilter.Value, Value = tag.Value! })
                                     .OrderBy(option => option.Value));
            SelectedValue = Values[0];
        }

        public record ValueOption
        {
            public ValueFilter Filter { get; init; }

            // The string to display in the menu; if Filter = ValueFilter.Value,
            // this doubles as the value to search for, otherwise it's purely for display.
            public string Value { get; init; } = string.Empty;
        }
    }
}
