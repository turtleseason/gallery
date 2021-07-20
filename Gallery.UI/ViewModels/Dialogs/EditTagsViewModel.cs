namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive;
    using System.Threading.Tasks;

    using Gallery.Data;
    using Gallery.Entities;

    using ReactiveUI;

    using Serilog;

    using Splat;

    public class EditTagsViewModel : DialogViewModelBase
    {
        private readonly IDataService _dataService;

        private readonly IEnumerable<TrackedFile> _files;

        public EditTagsViewModel(IDataService? dataService = null, params GalleryFile[] files)
        {
            _dataService = dataService ?? Locator.Current.GetService<IDataService>();

            WindowTitle = "Edit tags";

            _files = files.Where(x => x is TrackedFile).Cast<TrackedFile>();

            AllTags = new ObservableCollection<Tag>(_files.SelectMany(x => x.Tags).Distinct());
            SelectedTags = new ObservableCollection<Tag>();

            SaveAndCloseCommand = ReactiveCommand.CreateFromTask(SaveAndClose);
        }

        public EditTagsViewModel() : this(null) { }

        public ReactiveCommand<Unit, Unit> SaveAndCloseCommand { get; }

        public int FileCount => _files.Count();

        public ObservableCollection<Tag> AllTags { get; }

        public ObservableCollection<Tag> SelectedTags { get; }

        private async Task SaveAndClose()
        {
            var tags = SelectedTags.ToArray();
            var filePaths = _files.Select(x => x.FullPath).ToArray();

            Log.Information("Deleting {Tags} from {Files}", string.Join(", ", tags), string.Join(", ", filePaths));

            await _dataService.DeleteTags(tags, filePaths);

            CloseCommand.Execute(null).Subscribe();
        }
    }
}
