namespace Gallery.UI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using DynamicData;
    using DynamicData.Aggregation;
    using DynamicData.Binding;

    using Gallery.Data;
    using Gallery.UI;
    using Gallery.Util;

    using ReactiveUI;

    using Serilog;

    using Splat;

    public class FolderListViewModel : ViewModelBase
    {
        private IDataService _dbService;
        private IFileSystemUtil _fsService;
        private ISelectedFilesService _sfService;

        private ReadOnlyObservableCollection<string> _trackedFolders;

        public FolderListViewModel(IDataService? dbService = null, IFileSystemUtil? fsService = null, ISelectedFilesService? sfService = null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemUtil>();
            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();

            IEnumerable<FolderListItemViewModel>? availableDrives = _fsService.GetAvailableDrives()
                ?.Select(driveInfo => new FolderListItemViewModel(driveInfo.Name, dbService: dbService, fsService: fsService));

            Items = availableDrives != null ? new(availableDrives) : new();
            SelectedItems = new();

            var selectedItemsObservable = SelectedItems.ToObservableChangeSet(x => x.FullPath);
            var trackedFoldersObservable = _dbService.TrackedFolders();

            var canTrack = selectedItemsObservable.Cast(x => x.FullPath)
                .Except(trackedFoldersObservable)
                .Count()
                .Select(x => x > 0);

            var canUntrack = ObservableCacheEx.And(selectedItemsObservable.Cast(x => x.FullPath), trackedFoldersObservable)
                .Count()
                .Select(x => x > 0);

            Subject<bool> isUntracking = new Subject<bool>();

            TrackCommand = ReactiveCommand.CreateFromObservable(TrackSelectedFolders,
                canTrack.CombineLatest(isUntracking, (canExecute, isExecuting) => canExecute && !isExecuting));
            UntrackCommand = ReactiveCommand.CreateFromObservable(UntrackSelectedFolders,
                canUntrack.CombineLatest(TrackCommand.IsExecuting, (canExecute, isExecuting) => canExecute && !isExecuting));

            UntrackCommand.IsExecuting.Subscribe(isUntracking);

            TrackFoldersProgress = new BehaviorSubject<float?>(null);
            TrackCommand.IsExecuting.Where(x => x)
                .Subscribe(_ => Interactions.ReportCommandProgress(
                    TrackCommand.IsExecuting,
                    Observable.Return("Tracking folders"),
                    TrackFoldersProgress));

            ShowAllTrackedCommand = ReactiveCommand.Create(ShowAllTracked);

            selectedItemsObservable.Subscribe(changes => UpdateSelectedFolders(changes));

            IDisposable disposable = trackedFoldersObservable.ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _trackedFolders)
                .Subscribe();

            LoadFirstLevelChildren();

            this.WhenActivated((disposables) =>
            {
                disposable.DisposeWith(disposables);
                TrackCommand.DisposeWith(disposables);
                UntrackCommand.DisposeWith(disposables);
            });
        }

        public FolderListViewModel() : this(null, null, null)  // for XAML designer
        { }

        public ObservableCollection<FolderListItemViewModel> Items { get; }
        public ObservableCollection<FolderListItemViewModel> SelectedItems { get; }

        public ReactiveCommand<Unit, Unit> ShowAllTrackedCommand { get; }

        public ReactiveCommand<Unit, Unit> TrackCommand { get; }
        public ReactiveCommand<Unit, Unit> UntrackCommand { get; }

        private BehaviorSubject<float?> TrackFoldersProgress { get; }

        private void UpdateSelectedFolders(IChangeSet<FolderListItemViewModel, string> changes)
        {
            string[] removedPaths = changes.Where(change => change.Reason == ChangeReason.Remove)
                .Select(change => change.Key)
                .ToArray();

            string[] addedPaths = changes.Where(change => change.Reason == ChangeReason.Add)
                .Select(change => change.Key)
                .ToArray();

            _sfService.RemoveDirectories(removedPaths);
            _sfService.AddDirectories(addedPaths);
        }

        private void ShowAllTracked()
        {
            SelectedItems.Clear();

            _sfService.ShowAllTrackedFiles();
        }

        private void LoadFirstLevelChildren()
        {
            foreach (var item in Items)
            {
                item.LoadChildren();
            }
        }

        private IObservable<Unit> TrackFolder(FolderListItemViewModel vm)
        {
            Log.Information("Tracking {Folder}", vm.Name);

            if (!_trackedFolders.Contains(vm.FullPath))
            {
                return Observable.StartAsync(() => _dbService.TrackFolder(vm.FullPath), RxApp.MainThreadScheduler);
            }
            else
            {
                Log.Information("  Skipping {Folder} - already tracked", vm.Name);
                return Observable.Return(Unit.Default);
            }
        }

        private IObservable<Unit> TrackSelectedFolders()
        {
            var items = SelectedItems.ToList();

            return items.Select((x, index) => Observable.FromAsync(async () =>
                {
                    float progress = (float)index * 100 / items.Count;
                    TrackFoldersProgress.OnNext(progress);
                    await TrackFolder(x);
                }, RxApp.MainThreadScheduler))
                .Concat();
        }

        private IObservable<Unit> UntrackSelectedFolders()
        {
            var paths = SelectedItems.Select(vm => vm.FullPath).ToArray();

            Log.Information("Untracking {Folders}", string.Join(", ", paths));

            return Observable.StartAsync(() => _dbService.UntrackFolders(paths), RxApp.MainThreadScheduler);
        }
    }
}
