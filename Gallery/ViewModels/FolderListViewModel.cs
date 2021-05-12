using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;

using Gallery.Services;

using ReactiveUI;

using Splat;

namespace Gallery.ViewModels
{
    public class FolderListViewModel : ViewModelBase, IActivatableViewModel
    {
        IDatabaseService _dbService;
        IFileSystemService _fsService;
        ISelectedFilesService _sfService;

        ReadOnlyObservableCollection<string> _trackedFolders;

        public FolderListViewModel(IDatabaseService? dbService = null, IFileSystemService ? fsService=null, ISelectedFilesService? sfService=null)
        {
            _dbService = dbService ?? Locator.Current.GetService<IDatabaseService>();
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();
            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();

            IEnumerable<FolderListItemViewModel>? availableDrives = _fsService.GetAvailableDrives()
                ?.Select(driveInfo => new FolderListItemViewModel(driveInfo.Name, dbService: dbService, fsService: fsService));
            
            Items = availableDrives != null ? new(availableDrives) : new();
            SelectedItems = new();

            var selectedItemsObservable = SelectedItems.ToObservableChangeSet(x => x.FullPath);
            var trackedFoldersObservable = _dbService.TrackedFolders();  // is it OK to share this observable?

            // CanExecute if SelectedItems contains at least one item that isn't already tracked
            var canExecute = selectedItemsObservable.Cast(x => x.FullPath)
                .Except(trackedFoldersObservable)
                .Count()
                .Select(x => x > 0);

            TrackSelectedFoldersCommand = ReactiveCommand.Create(TrackSelectedFolders, canExecute);
            TrackFolderCommand = ReactiveCommand.Create((FolderListItemViewModel vm) => TrackFolder(vm));
            
            selectedItemsObservable.Subscribe(changes => UpdateSelectedFolders(changes));

            IDisposable subscription = trackedFoldersObservable.ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _trackedFolders)
                .Subscribe();

            LoadFirstLevelChildren();

            this.WhenActivated((CompositeDisposable disposables) => subscription.DisposeWith(disposables));
        }

        public FolderListViewModel() : this(null, null, null)  // for XAML designer
        { }


        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        
        public ObservableCollection<FolderListItemViewModel> Items { get; }
        public ObservableCollection<FolderListItemViewModel> SelectedItems { get; }

        public ReactiveCommand<FolderListItemViewModel, Unit> TrackFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> TrackSelectedFoldersCommand { get; }

        void UpdateSelectedFolders(IChangeSet<FolderListItemViewModel, string> changes)
        {
            foreach (var change in changes)
            {
                if (change.Reason == ChangeReason.Add)
                {
                    _sfService.AddDirectory(change.Key);
                }
                else if (change.Reason == ChangeReason.Remove)
                {
                    _sfService.RemoveDirectory(change.Key);
                }
            }
        }

        void LoadFirstLevelChildren()
        {
            foreach (var item in Items)
            {
                item.LoadChildren();
            }
        }

        void TrackFolder(FolderListItemViewModel vm)
        {
            Debug.WriteLine($"Tracking {vm.Name}");
            
            if (!_trackedFolders.Contains(vm.FullPath))
            {
                _dbService.TrackFolder(vm.FullPath);
            } 
            else
            {
                Debug.WriteLine("  Already tracked - skipping");
            }
        }

        void TrackSelectedFolders()
        {
            foreach (FolderListItemViewModel vm in SelectedItems)
            {
                TrackFolder(vm);
            }
        }
    }
}
