﻿namespace Gallery.ViewModels
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

    using Gallery.Models;
    using Gallery.Services;

    using ReactiveUI;

    using Splat;

    public class SingleFileViewModel : ViewModelBase, IRoutableViewModel
    {
        private readonly IDataService _dataService;
        private readonly ReadOnlyObservableCollection<GalleryFile> _selectedFiles;

        private readonly GalleryFile _file;

        private Bitmap? _image;

        public SingleFileViewModel(IScreen screen, GalleryFile file, ISelectedFilesService? sfService = null, IDataService? dataService = null)
        {
            HostScreen = screen;
            _file = file;
            _dataService = dataService ?? Locator.Current.GetService<IDataService>();

            sfService ??= Locator.Current.GetService<ISelectedFilesService>();
            _selectedFiles = sfService.SelectedFiles();

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
                .Subscribe(bitmap => Image = bitmap);
        }

        public string? UrlPathSegment => _file.Name;

        public IScreen HostScreen { get; }

        public ReactiveCommand<Unit, Unit> AddTagCommand { get; }

        public ReactiveCommand<Unit, Unit> PreviousFileCommand { get; }
        public ReactiveCommand<Unit, Unit> NextFileCommand { get; }

        public string Description => (_file as TrackedFile)?.Description ?? string.Empty;

        public ObservableCollection<Tag>? Tags { get; }

        public Bitmap? Image { get => _image; set => this.RaiseAndSetIfChanged(ref _image, value); }

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
            Tag? tag = (Tag?)await Interactions.ShowDialog.Handle(new AddTagsViewModel(_dataService));

            if (tag != null)
            {
                await _dataService.AddTag(tag.Value, _file.FullPath);
                // Todo: return the tag from AddTag & use it when updating (to make sure the group is accurate)
                Tags?.Add(tag.Value);
            }
        }
    }
}
