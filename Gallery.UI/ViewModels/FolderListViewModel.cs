﻿namespace Gallery.UI.ViewModels
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

            // TrackFolder & TrackSelectedFolders commands can each execute
            // if SelectedItems contains at least one item that isn't already tracked
            // and if the other command isn't currently executing.
            var canExecute = selectedItemsObservable.Cast(x => x.FullPath)
                .Except(trackedFoldersObservable)
                .Count()
                .Select(x => x > 0);

            Subject<bool> trackAllIsExecuting = new Subject<bool>();

            TrackFolderCommand = ReactiveCommand.CreateFromObservable<FolderListItemViewModel, Unit>(TrackFolder,
                canExecute.CombineLatest(trackAllIsExecuting, (canExecute, isExecuting) => canExecute && !isExecuting));
            TrackSelectedFoldersCommand = ReactiveCommand.CreateFromObservable(TrackSelectedFolders,
                canExecute.CombineLatest(TrackFolderCommand.IsExecuting, (canExecute, isExecuting) => canExecute && !isExecuting));

            TrackSelectedFoldersCommand.IsExecuting.Subscribe(trackAllIsExecuting);

            TrackFoldersProgress = new BehaviorSubject<float?>(null);

            TrackFolderCommand.IsExecuting.Where(x => x)
                .Subscribe(_ => Interactions.ReportCommandProgress(
                    TrackFolderCommand.IsExecuting,
                    Observable.Return("Tracking folder")));

            TrackSelectedFoldersCommand.IsExecuting.Where(x => x)
                .Subscribe(_ => Interactions.ReportCommandProgress(
                    TrackSelectedFoldersCommand.IsExecuting,
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
                TrackSelectedFoldersCommand.DisposeWith(disposables);
            });
        }

        public FolderListViewModel() : this(null, null, null)  // for XAML designer
        { }

        public ObservableCollection<FolderListItemViewModel> Items { get; }
        public ObservableCollection<FolderListItemViewModel> SelectedItems { get; }

        public ReactiveCommand<Unit, Unit> ShowAllTrackedCommand { get; }

        public ReactiveCommand<FolderListItemViewModel, Unit> TrackFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> TrackSelectedFoldersCommand { get; }

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
    }
}