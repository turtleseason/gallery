namespace Gallery.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;

    using DynamicData;

    using Gallery.Models;
    using Gallery.Services;

    using ReactiveUI;

    using Splat;

    public class AddTagsViewModel : DialogViewModelBase, IActivatableViewModel
    {
        private static readonly Regex HexColorRegex = new("^#[a-fA-F0-9]{6}$");

        private readonly IDatabaseService _dbService;

        private readonly IObservableCache<Tag, string> _tagsCache;

        private readonly ReadOnlyObservableCollection<Tag> _tags;
        private readonly ReadOnlyObservableCollection<TagGroup> _tagGroups;

        private string _name = string.Empty;
        private string _value = string.Empty;
        private TagGroup _selectedGroup;
        private bool _selectedGroupMismatch = false;
        private bool _isAddingGroup = false;
        private string _groupName = string.Empty;
        private string _groupColor = "#FF66FF";

        public AddTagsViewModel(IDatabaseService? dbService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDatabaseService>();

            WindowTitle = "Add tags";

            var canAddTags = this.WhenAnyValue(
                x => x.Name,
                x => x.IsAddingGroup,
                (name, isAddingGroup) => !string.IsNullOrWhiteSpace(name) && !isAddingGroup);

            var canAddGroup = this.WhenAnyValue(
                x => x.IsAddingGroup,
                x => x.GroupName,
                x => x.Color,
                (isAddingGroup, groupName, color) => isAddingGroup && !string.IsNullOrWhiteSpace(groupName) && HexColorRegex.IsMatch(color));

            AddTagsCommand = ReactiveCommand.Create(AddTagsAndClose, canAddTags);
            AddGroupCommand = ReactiveCommand.Create(AddGroup, canAddGroup);

            LastValidColor = this.WhenAnyValue(x => x.Color).Where(color => HexColorRegex.IsMatch(color));

            var tags = _dbService.Tags();
            _tagsCache = tags.AsObservableCache();

            var tagSubscription = tags.Bind(out _tags).Subscribe();

            var tagGroupSubscription = _dbService.TagGroups()
                .Bind(out _tagGroups)
                .Subscribe();

            _selectedGroup = _tagGroups.First();

            this.WhenAnyValue(x => x.SelectedGroup).Subscribe(_ => CheckIfSelectedGroupMismatch());
            this.WhenAnyValue(x => x.Name).Subscribe(_ => SelectedGroupMismatch = false);

            this.WhenActivated(disposables =>
            {
                tagSubscription.DisposeWith(disposables);
                tagGroupSubscription.DisposeWith(disposables);
            });
        }

        public AddTagsViewModel() : this(null) { }

        public ViewModelActivator Activator => new ViewModelActivator();

        public ReactiveCommand<Unit, Unit> AddTagsCommand { get; }
        public ReactiveCommand<Unit, Unit> AddGroupCommand { get; }

        public ReadOnlyObservableCollection<Tag> Tags => _tags;
        public ReadOnlyObservableCollection<TagGroup> AvailableGroups => _tagGroups;

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
            var lookup = _tagsCache.Lookup(Name);
            if (lookup.HasValue)
            {
                SelectedGroup = lookup.Value.Group;
            }

            SelectedGroupMismatch = false;
        }

        private void CheckIfSelectedGroupMismatch()
        {
            var lookup = _tagsCache.Lookup(Name);
            SelectedGroupMismatch = lookup.HasValue && !lookup.Value.Group.Equals(SelectedGroup);
        }

        private void AddTagsAndClose()
        {
            if (SelectedGroup.Name == null)
            {
                SelectedGroup = _tagGroups.First();
            }

            string? tagValue = string.IsNullOrWhiteSpace(Value) ? null : Value;
            Tag tag = new Tag(Name, tagValue, SelectedGroup);

            CloseCommand.Execute(tag).Subscribe();
        }

        private void AddGroup()
        {
            var group = new TagGroup(GroupName, Color);

            _dbService.CreateTagGroup(group);

            // Is it possible for the CreateTagGroup call to return before _tagGroups
            // has received the update with the new tag group?
            // (If so, this will set SelectedGroup to TagGroup's default value instead;
            // there's a safeguard in AddTagsAndClose just in case)
            SelectedGroup = group;
            IsAddingGroup = false;
        }
    }
}
