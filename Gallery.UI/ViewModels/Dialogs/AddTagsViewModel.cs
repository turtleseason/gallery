namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using DynamicData;
    using DynamicData.Binding;

    using Gallery.Data;
    using Gallery.Entities;

    using ReactiveUI;

    using Splat;

    public class AddTagsViewModel : DialogViewModelBase
    {
        private readonly IDataService _dbService;

        private ReadOnlyObservableCollection<string> _values;

        private string _name = string.Empty;
        private string _value = string.Empty;
        private TagGroup _selectedGroup;
        private bool _lockSelectedGroup = false;

        public AddTagsViewModel(IDataService? dbService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();

            WindowTitle = "Add tags";

            var canAddTag = this.WhenAnyValue(x => x.Name, name => !string.IsNullOrWhiteSpace(name));

            AddTagsCommand = ReactiveCommand.Create(AddTagsAndClose, canAddTag);
            EditTagGroupsCommand = ReactiveCommand.CreateFromTask(EditTagGroups);

            Tags = new ObservableCollection<Tag>(_dbService.GetAllTags());

            TagNames = new ObservableCollection<Tag>(Tags
                .GroupBy(x => x.Name)
                .Select(group => new Tag(group.Key, group: group.First().Group)));

            AvailableGroups = new ObservableCollection<TagGroup>(_dbService.GetAllTagGroups());
            _selectedGroup = AvailableGroups.First();

            Tags.ToObservableChangeSet(tag => tag)
                .Filter(this.WhenAnyValue(x => x.Name)
                            .Select<string, Func<Tag, bool>>(name => tag => tag.Name == name && tag.Value != null))
                .Transform(tag => tag.Value!)
                .Bind(out _values)
                .Subscribe();

            // Reset LockSelectedGroup when the user starts typing in the Name field
            this.WhenAnyValue(x => x.Name).Subscribe(_ => LockSelectedGroup = false);
        }

        public AddTagsViewModel() : this(null) { }

        public ReactiveCommand<Unit, Unit> AddTagsCommand { get; }
        public ReactiveCommand<Unit, Unit> EditTagGroupsCommand { get; }

        public ObservableCollection<Tag> Tags { get; set; }

        // Use Tags instead of strings in order to preserve the TagGroup
        public ObservableCollection<Tag> TagNames { get; set; }
        public ObservableCollection<TagGroup> AvailableGroups { get; set; }

        public ReadOnlyObservableCollection<string> Values => _values;

        public string Name { get => _name; set => this.RaiseAndSetIfChanged(ref _name, value); }
        public string Value { get => _value; set => this.RaiseAndSetIfChanged(ref _value, value); }

        public TagGroup SelectedGroup { get => _selectedGroup; set => this.RaiseAndSetIfChanged(ref _selectedGroup, value); }

        /// Don't allow the user to change the tag group for tags that already exist.
        public bool LockSelectedGroup { get => _lockSelectedGroup; set => this.RaiseAndSetIfChanged(ref _lockSelectedGroup, value); }

        /// Called from the View when the tag name input loses focus (because changing the selected tag group
        /// in the middle of typing is probably a little too aggressive)
        public void SetTagGroupIfTagExists()
        {
            // TODO: translate tags to some kind of dictionary/hashtable? Or call a db method..?
            // (maybe db offers "Get tag names" dictionary tagName key -> tagGroup value?)
            var lookup = TagNames.FirstOrDefault(x => x.Name == Name);
            if (lookup.Name != null)
            {
                SelectedGroup = lookup.Group;
                LockSelectedGroup = true;
            }
        }

        private void AddTagsAndClose()
        {
            SetTagGroupIfTagExists();

            Debug.Assert(SelectedGroup.Name != null, "AddTagsAndClose: SelectedGroup is not a valid tag group");

            string? tagValue = string.IsNullOrWhiteSpace(Value) ? null : Value;
            var tag = new Tag(Name, tagValue, SelectedGroup);

            CloseCommand.Execute(tag).Subscribe();
        }

        private async Task EditTagGroups()
        {
            var result = ((TagGroup? Original, TagGroup Result)?)await Interactions.ShowDialog.Handle(new EditTagGroupsViewModel());

            if (result != null)
            {
                TagGroup? original = result.Value.Original;
                TagGroup group = result.Value.Result;

                if (original.HasValue)
                {
                    int index = AvailableGroups.IndexOf(original.Value);
                    Debug.Assert(index >= 0, "EditTagGroups: Edited group was not originally in AvailableGroups");

                    AvailableGroups[index] = group;
                }
                else
                {
                    AvailableGroups.Add(group);
                }

                SelectedGroup = group;
            }
        }
    }
}
