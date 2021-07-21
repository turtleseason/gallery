namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using DynamicData;

    using Gallery.Entities;

    using ReactiveUI;
    using ReactiveUI.Validation.Abstractions;
    using ReactiveUI.Validation.Contexts;
    using ReactiveUI.Validation.Extensions;

    using Parameter = Gallery.Entities.SearchParameters;

    public class SearchCriteriaViewModel : ViewModelBase, IValidatableViewModel
    {
        private static readonly ValueOption[] _defaultOptions = new ValueOption[]
        {
            new ValueOption { Filter = ValueFilter.Any, Value = "(Any value)" },
            ////new ValueOption { Filter = ValueFilter.None, Value = "(No value)" },
        };

        private IEnumerable<Tag> _tags;

        private Tag _selectedTag;
        private ValueOption _selectedValue;

        public SearchCriteriaViewModel(IEnumerable<Tag> tags)
        {
            _tags = tags;

            Tags = new ObservableCollection<Tag>(_tags.GroupBy(x => x.Name)
                                                      .Select(x => new Tag(x.Key, group: x.First().Group))
                                                      .OrderBy(x => x.Name));
            Values = new ObservableCollection<ValueOption>();

            this.WhenAnyValue(x => x.SelectedTag).Subscribe(_ => UpdateValues());

            SelectedTag = Tags.Count > 1 ? Tags[0] : default;
            _selectedValue = _defaultOptions[0];

            this.ValidationRule(vm => vm.SelectedTag, tag => tag.Name != null, "Selected tag is invalid");
            this.ValidationRule(vm => vm.SelectedValue, value => value != null, "Selected value is invalid");
        }

        public enum ValueFilter { Any, None, Value }

        public ValidationContext ValidationContext { get; } = new ValidationContext();

        public ObservableCollection<Tag> Tags { get; set; }
        public ObservableCollection<ValueOption> Values { get; set; }

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

        private void UpdateValues()
        {
            Values.Clear();
            Values.AddRange(_defaultOptions);
            Values.AddRange(_tags.Where(tag => tag.Name == SelectedTag.Name)
                                 .Select(tag => tag.Value == null ?
                                    new ValueOption { Filter = ValueFilter.None, Value = "(No value)" }
                                  : new ValueOption { Filter = ValueFilter.Value, Value = tag.Value! })
                                 .OrderBy(option => option.Filter == ValueFilter.None ? 0 : 1)
                                 .ThenBy(option => option.Value));
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
