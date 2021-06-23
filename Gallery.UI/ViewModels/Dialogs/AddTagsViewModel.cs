namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;

    using Gallery.Data;
    using Gallery.Entities;

    using ReactiveUI;

    using Splat;

    public class AddTagsViewModel : DialogViewModelBase
    {
        private static readonly Regex _hexColorRegex = new("^#[a-fA-F0-9]{6}$");

        private readonly IDataService _dbService;

        private string _name = string.Empty;
        private string _value = string.Empty;
        private TagGroup _selectedGroup;
        private bool _selectedGroupMismatch = false;
        private bool _isAddingGroup = false;
        private string _groupName = string.Empty;
        private string _groupColor = "#FF66FF";

        public AddTagsViewModel(IDataService? dbService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();

            WindowTitle = "Add tags";

            var canAddTags = this.WhenAnyValue(
                x => x.Name,
                x => x.IsAddingGroup,
                (name, isAddingGroup) => !string.IsNullOrWhiteSpace(name) && !isAddingGroup);

            var canAddGroup = this.WhenAnyValue(
                x => x.IsAddingGroup,
                x => x.GroupName,
                x => x.Color,
                (isAddingGroup, groupName, color) => isAddingGroup && !string.IsNullOrWhiteSpace(groupName) && _hexColorRegex.IsMatch(color));

            AddTagsCommand = ReactiveCommand.Create(AddTagsAndClose, canAddTags);
            AddGroupCommand = ReactiveCommand.Create(AddGroup, canAddGroup);

            LastValidColor = this.WhenAnyValue(x => x.Color).Where(color => _hexColorRegex.IsMatch(color));

            Tags = new ObservableCollection<Tag>(_dbService.GetAllTags()
                .GroupBy(x => x.Name)
                .Select(group => new Tag(group.Key, group: group.First().Group)));
            AvailableGroups = new ObservableCollection<TagGroup>(_dbService.GetAllTagGroups());

            _selectedGroup = AvailableGroups.First();

            this.WhenAnyValue(x => x.SelectedGroup).Subscribe(_ => CheckIfSelectedGroupMismatch());
            this.WhenAnyValue(x => x.Name).Subscribe(_ => SelectedGroupMismatch = false);
        }

        public AddTagsViewModel() : this(null) { }

        public ReactiveCommand<Unit, Unit> AddTagsCommand { get; }
        public ReactiveCommand<Unit, Unit> AddGroupCommand { get; }

        public ObservableCollection<Tag> Tags { get; set; }
        public ObservableCollection<TagGroup> AvailableGroups { get; set; }

        public string Name { get => _name; set => this.RaiseAndSetIfChanged(ref _name, value); }
        public string Value { get => _value; set => this.RaiseAndSetIfChanged(ref _value, value); }

        public TagGroup SelectedGroup { get => _selectedGroup; set => this.RaiseAndSetIfChanged(ref _selectedGroup, value); }

        // SelectedGroupMismatch is true when Name matches an existing tag whose associated group is not SelectedGroup.
        // (It's not checked while the user is actively typing in the Name field.)
        public bool SelectedGroupMismatch { get => _selectedGroupMismatch; set => this.RaiseAndSetIfChanged(ref _selectedGroupMismatch, value); }

        public bool IsAddingGroup { get => _isAddingGroup; set => this.RaiseAndSetIfChanged(ref _isAddingGroup, value); }

        public string GroupName { get => _groupName; set => this.RaiseAndSetIfChanged(ref _groupName, value); }
        public string Color { get => _groupColor; set => this.RaiseAndSetIfChanged(ref _groupColor, value); }
        public IObservable<string> LastValidColor { get; }

        public void ToggleAddGroupControls()
        {
            IsAddingGroup = !IsAddingGroup;
        }

        public void SetTagGroupIfExists()
        {
            // TODO: translate tags to some kind of dictionary/hashtable? Or call a db method..?
            // (maybe db offers "Get tag names" dictionary tagName key -> tagGroup value?)
            var lookup = Tags.FirstOrDefault(x => x.Name == Name);
            if (lookup.Name != null)
            {
                SelectedGroup = lookup.Group;
            }

            SelectedGroupMismatch = false;
        }

        private void CheckIfSelectedGroupMismatch()
        {
            // again, do faster lookup somehow
            var lookup = Tags.FirstOrDefault(x => x.Name == Name);
            SelectedGroupMismatch = lookup.Name != null && !lookup.Group.Equals(SelectedGroup);
        }

        private void AddTagsAndClose()
        {
            if (SelectedGroup.Name == null)
            {
                SelectedGroup = AvailableGroups.First();
            }

            string? tagValue = string.IsNullOrWhiteSpace(Value) ? null : Value;
            var tag = new Tag(Name, tagValue, SelectedGroup);
            // use SelectedTag & take that group if it exists? since updating is a separate thing
            // (or we could just call the separate update method if there's a mismatch)

            CloseCommand.Execute(tag).Subscribe();
        }

        private void AddGroup()
        {
            var group = new TagGroup(GroupName, Color);

            _dbService.CreateTagGroup(group);

            // TODO: check whether the add was successful first (ignore if dupe)
            AvailableGroups.Add(group);
            SelectedGroup = group;

            IsAddingGroup = false;
        }
    }
}
