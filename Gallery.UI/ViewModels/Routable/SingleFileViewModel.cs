namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using Avalonia.Media.Imaging;

    using DynamicData;
    using DynamicData.Binding;

    using Gallery.Data;
    using Gallery.Entities;
    using Gallery.UI;
    using Gallery.Util;

    using ReactiveUI;

    using Splat;

    public class SingleFileViewModel : ViewModelBase, IRoutableViewModel
    {
        private readonly IDataService _dataService;
        private readonly ReadOnlyObservableCollection<GalleryFile> _selectedFiles;

        private readonly GalleryFile _file;

        private Bitmap? _image;
        private string _description = string.Empty;
        private string _editedDescription = string.Empty;
        private bool _isEditing = false;

        public SingleFileViewModel(IScreen screen, GalleryFile file, ISelectedFilesService? sfService = null, IDataService? dataService = null)
        {
            HostScreen = screen;
            _file = file;
            _dataService = dataService ?? Locator.Current.GetService<IDataService>();

            sfService ??= Locator.Current.GetService<ISelectedFilesService>();
            _selectedFiles = sfService.SelectedFiles();

            Description = (_file as TrackedFile)?.Description ?? string.Empty;
            EditedDescription = Description;

            if (file is TrackedFile trackedFile)
            {
                Tags = new ObservableCollection<Tag>(trackedFile.Tags);
            }

            var canExecute = _selectedFiles.ToObservableChangeSet(x => x.FullPath)
                .Watch(_file.FullPath)
                .Where(change => change.Reason is DynamicData.ChangeReason.Add or DynamicData.ChangeReason.Remove)
                .Select(x => x.Reason == DynamicData.ChangeReason.Add && x.Current.Equals(_file));

            AddTagCommand = ReactiveCommand.CreateFromTask(AddTag);

            PreviousFileCommand = ReactiveCommand.CreateFromObservable(() => NavigateToFile(-1), canExecute);
            NextFileCommand = ReactiveCommand.CreateFromObservable(() => NavigateToFile(1), canExecute);

            Observable.FromAsync(() => ImageUtil.LoadBitmap(_file.FullPath), RxApp.MainThreadScheduler)
                .Subscribe(bitmap => Image = bitmap, error => RxApp.DefaultExceptionHandler.OnNext(error));
        }

        public string? UrlPathSegment => _file.Name;

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit, Unit> AddTagCommand { get; }

        public ReactiveCommand<Unit, Unit> PreviousFileCommand { get; }
        public ReactiveCommand<Unit, Unit> NextFileCommand { get; }

        public bool IsTracked => _file is TrackedFile;

        public string Description { get => _description; set => this.RaiseAndSetIfChanged(ref _description, value); }

        public ObservableCollection<Tag>? Tags { get; }

        public Bitmap? Image { get => _image; set => this.RaiseAndSetIfChanged(ref _image, value); }

        public bool IsEditing { get => _isEditing; set => this.RaiseAndSetIfChanged(ref _isEditing, value); }

        public string EditedDescription { get => _editedDescription; set => this.RaiseAndSetIfChanged(ref _editedDescription, value); }

        public void ToggleEdit()
        {
            IsEditing = !IsEditing;
        }

        public void ResetTextBox()
        {
            EditedDescription = Description;
        }

        public void SaveDescription()
        {
            if (_file is TrackedFile)
            {
                _dataService.UpdateDescription(EditedDescription, _file.FullPath);
                Description = EditedDescription;
            }

            IsEditing = false;
        }

        public IObservable<Unit> NavigateToFile(int offset)
        {
            int index = _selectedFiles.IndexOf(_file);
            if (index < 0 || _selectedFiles.Count == 1)
            {
                return Observable.Return(Unit.Default);
            }

            int nextIndex = (index + offset + _selectedFiles.Count) % _selectedFiles.Count;

            return ((MainShellViewModel)HostScreen).FileViewCommand
                .Execute(_selectedFiles[nextIndex])
                .Select(_ => Unit.Default);
        }

        private async Task AddTag()
        {
            var tag = (Tag?)await Interactions.ShowDialog.Handle(new AddTagsViewModel(_dataService));

            if (tag != null)
            {
                await _dataService.AddTag(tag.Value, _file.FullPath);
                // Todo: return the tag from AddTag & use it when updating (to make sure the group is accurate)
                Tags?.Add(tag.Value);
            }
        }
    }
}
