namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;

    using DynamicData;
    using DynamicData.Binding;

    using Gallery.Entities;

    using ReactiveUI;
    using ReactiveUI.Validation.Abstractions;
    using ReactiveUI.Validation.Contexts;
    using ReactiveUI.Validation.Extensions;

    using Parameter = Gallery.Entities.SearchParameters;

    public class SearchCriteriaViewModel : ViewModelBase, IValidatableViewModel
    {
        private ReadOnlyObservableCollection<Tag> _tagNames;
        private ReadOnlyObservableCollection<ValueOption> _values;

        private Tag _selectedTag;
        private ValueOption _selectedValue;

        public SearchCriteriaViewModel(IObservableCache<Tag, Tag> tagsSource)
        {
            var tags = tagsSource.Connect();

            ValueOption anyOption = new() { Filter = ValueFilter.Any, Value = "(Any value)" };

            // Tag names dropdown
            tags.Transform(tag => new Tag(tag.Name, group: tag.Group))
                .DistinctValues(tag => tag)
                .Sort(SortExpressionComparer<Tag>.Ascending(tag => tag.Group.Name)
                                                 .ThenBy(tag => tag.Name))
                .Bind(out _tagNames)
                .Subscribe();

            // Tag values dropdown
            var selectedTagFilter = this.WhenAnyValue(vm => vm.SelectedTag)
                .Select<Tag, Func<Tag, bool>>(selectedTag => tag => tag.Name == selectedTag.Name);

            tags.Filter(selectedTagFilter)
                .Transform(tag => tag.Value == null ?
                    new ValueOption { Filter = ValueFilter.None, Value = "(No value)" }
                    : new ValueOption { Filter = ValueFilter.Value, Value = tag.Value! })
                .StartWithItem(anyOption, default)
                .Sort(
                    SortExpressionComparer<ValueOption>
                        .Ascending(option => option.Filter)
                        .ThenByAscending(option => option.Value))
                .Bind(out _values)
                .Subscribe();

            // Reset selected value to default when invalid
            this.WhenAnyValue(vm => vm.SelectedValue)
                .Where(value => value == default)
                .Subscribe(_ => SelectedValue = anyOption);

            SelectedTag = _tagNames.Count > 0 ? _tagNames[0] : default;
            _selectedValue = anyOption;

            this.ValidationRule(vm => vm.SelectedTag, tag => tag.Name != null, "Selected tag is invalid");
            this.ValidationRule(vm => vm.SelectedValue, value => value != null, "Selected value is invalid");
        }

        public enum ValueFilter { Any = 0, None = 1, Value = 2 }

        public ValidationContext ValidationContext { get; } = new ValidationContext();

        public ReadOnlyObservableCollection<Tag> Tags => _tagNames;
        public ReadOnlyObservableCollection<ValueOption> Values => _values;

        public Tag SelectedTag { get => _selectedTag; set => this.RaiseAndSetIfChanged(ref _selectedTag, value); }
        public ValueOption SelectedValue { get => _selectedValue; set => this.RaiseAndSetIfChanged(ref _selectedValue, value); }

        public Parameter.ISearchParameter ToParameter()
        {
            if (SelectedTag.Name == null || SelectedValue == null)
            {
                throw new InvalidOperationException("Search parameter inputs are invalid (Tag and/or Value is null)");
            }

            string? value = SelectedValue.Filter == ValueFilter.Value ? SelectedValue.Value : null;
            bool ignoreValue = SelectedValue.Filter == ValueFilter.Any;

            return new Parameter.Tagged(new Tag(SelectedTag.Name, value, SelectedTag.Group), ignoreValue);
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
