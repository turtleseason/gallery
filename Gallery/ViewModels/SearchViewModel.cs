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

    using DynamicData;

    using Gallery.Models;
    using Gallery.Services;

    using ReactiveUI;

    using Splat;

    public class SearchViewModel : ViewModelBase, IActivatableViewModel
    {
        private readonly IDatabaseService _dbService;
        private readonly ISelectedFilesService _sfService;

        private readonly ReadOnlyObservableCollection<Tag> _tags;
        private readonly ReadOnlyObservableCollection<ValueOption> _values;

        private Tag _selectedTag;
        private ValueOption _selectedValue;

        public SearchViewModel(IDatabaseService? dbService = null, ISelectedFilesService? sfService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDatabaseService>();
            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();

            var disposable_tagNames = _dbService.TagNames().Bind(out _tags).Subscribe();

            _selectedTag = _tags[0];

            var noneValueOption = new ValueOption { Filter = ValueFilter.None, Value = "(No value)" };
            var anyValueOption = new ValueOption { Filter = ValueFilter.Any, Value = "(Any value)" };

            var valuesFilter = this.WhenAnyValue(x => x.SelectedTag)
                .Select<Tag, Func<Tag, bool>>(selectedTag =>
                    (Tag tag) => tag.Name == selectedTag.Name && tag.Value != null);

            var disposable_tags = _dbService.Tags()
                .Filter(valuesFilter)
                .Transform(tag => new ValueOption { Filter = ValueFilter.Value, Value = tag.Value })
                .ChangeKey(x => x)
                .StartWithItem(noneValueOption, noneValueOption)
                .StartWithItem(anyValueOption, anyValueOption)
                .Bind(out _values)
                .Subscribe();

            _selectedValue = _values[0];

            this.WhenAnyValue(x => x.SelectedTag).Subscribe(_ => SelectedValue ??= _values[0]);

            this.WhenActivated(disposables =>
            {
                disposable_tagNames.DisposeWith(disposables);
                disposable_tags.DisposeWith(disposables);
            });
        }

        public enum ValueFilter { Any, None, Value }

        public ViewModelActivator Activator => new ViewModelActivator();

        public ReadOnlyObservableCollection<Tag> Tags => _tags;
        public ReadOnlyObservableCollection<ValueOption> Values => _values;

        public Tag SelectedTag { get => _selectedTag; set => this.RaiseAndSetIfChanged(ref _selectedTag, value); }
        public ValueOption SelectedValue { get => _selectedValue; set => this.RaiseAndSetIfChanged(ref _selectedValue, value); }

        public void DoSearch()
        {
            if (SelectedTag.Name == null)
            {
                return;
            }

            string? value = (SelectedValue.Filter == ValueFilter.Value) ? SelectedValue.Value : null;
            bool ignoreValue = SelectedValue.Filter == ValueFilter.Any;

            var searchParameter = new Models.SearchParameters.Tagged(new Tag(SelectedTag.Name, value, SelectedTag.Group), ignoreValue);

            _sfService.SetSearchParameters(new List<ISearchParameter> { searchParameter });
        }

        public void ClearSearch()
        {
            _sfService.SetSearchParameters(new List<ISearchParameter>());
        }

        public record ValueOption
        {
            public ValueFilter Filter { get; init; }

            // The string to display in the menu; if Filter = ValueFilter.Value,
            // this doubles as the value to search for, otherwise it's purely for display.
            public string? Value { get; init; }
        }
    }
}
