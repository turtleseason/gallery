/// Create, rename, and change the color of TagGroups.
///
/// The result passed to CloseCommand is either null or a tuple of type (TagGroup? Original, TagGroup Result) -
/// Original is null when creating a new TagGroup, otherwise it contains the original values of the group that was edited
/// (as a way to identify which group changed in case of a rename).

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

    public class EditTagGroupsViewModel : DialogViewModelBase
    {
        private static readonly Regex _hexColorRegex = new("^#[a-fA-F0-9]{6}$");

        private readonly IDataService _dataService;

        private TagGroup _selectedGroup;
        private bool _editGroup = false;
        private string _groupName = "New Group";
        private string _groupColor = TagGroup.DefaultGroupColor;

        public EditTagGroupsViewModel(IDataService? dataService = null)
        {
            WindowTitle = "Edit tag groups";

            _dataService = dataService ?? Locator.Current.GetService<IDataService>();

            TagGroups = new ObservableCollection<TagGroup>(
                _dataService.GetAllTagGroups().Where(x => x.Name != TagGroup.DefaultGroupName));
            SelectedGroup = TagGroups.FirstOrDefault();
            CanEdit = TagGroups.Count > 0;

            this.WhenAnyValue(x => x.SelectedGroup, x => x.EditGroup, (group, _) => group)
                .Where(_ => EditGroup)
                .Subscribe(x =>
            {
                Name = x.Name;
                Color = x.Color;
            });

            IsNameValid = this.WhenAnyValue(x => x.Name, name => !string.IsNullOrWhiteSpace(name));
            IsNameUnique = this.WhenAnyValue(x => x.Name, x => x.EditGroup,
                (name, editing) => (editing && name == SelectedGroup.Name)
                                || (name != TagGroup.DefaultGroupName && !TagGroups.Any(x => x.Name == name)));
            IsColorValid = this.WhenAnyValue(x => x.Color, color => _hexColorRegex.IsMatch(color));

            LastValidColor = this.WhenAnyValue(x => x.Color).Where(color => _hexColorRegex.IsMatch(color));

            var canSave = Observable.CombineLatest(IsNameValid, IsNameUnique, IsColorValid, (x, y, z) => x && y && z);
            SaveCommand = ReactiveCommand.Create(AddOrEditGroup, canSave);
        }

        public EditTagGroupsViewModel() : this(null) { }

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public ObservableCollection<TagGroup> TagGroups { get; }  // Use ObservableCache for faster name lookups?
        public TagGroup SelectedGroup { get => _selectedGroup; set => this.RaiseAndSetIfChanged(ref _selectedGroup, value); }

        public bool EditGroup { get => _editGroup; set => this.RaiseAndSetIfChanged(ref _editGroup, value); }

        public string Name { get => _groupName; set => this.RaiseAndSetIfChanged(ref _groupName, value); }
        public string Color { get => _groupColor; set => this.RaiseAndSetIfChanged(ref _groupColor, value); }

        public bool CanEdit { get; }

        public IObservable<bool> IsNameValid { get; }
        public IObservable<bool> IsNameUnique { get; }
        public IObservable<bool> IsColorValid { get; }

        public IObservable<string> LastValidColor { get; }

        private void AddOrEditGroup()
        {
            var group = new TagGroup(Name, Color);

            (TagGroup? Original, TagGroup Result) result = (null, group);

            if (EditGroup)
            {
                result.Original = SelectedGroup;
                _dataService.UpdateTagGroup(SelectedGroup, group);
            }
            else
            {
                _dataService.CreateTagGroup(group);
            }

            CloseCommand.Execute(result).Subscribe();
        }
    }
}
