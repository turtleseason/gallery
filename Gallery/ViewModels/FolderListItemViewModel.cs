namespace Gallery.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using DynamicData;

    using Gallery.Services;

    using ReactiveUI;

    using Splat;

    public class FolderListItemViewModel : ViewModelBase
    {
        private IFileSystemService _fsService;
        private IDataService _dbService;

        private bool _hasLoadedChildren = false;

        private bool _isExpanded = false;

        public FolderListItemViewModel(DirectoryInfo directoryInfo, IDataService? dbService = null, IFileSystemService? fsService = null)
        {
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();
            _dbService = dbService ?? Locator.Current.GetService<IDataService>();

            DirectoryInfo = directoryInfo;
            IsExpanded = false;
            Children = new ObservableCollection<FolderListItemViewModel>();

            IsTracked = _dbService.IsTracked(FullPath);

            this.WhenAnyValue(x => x.IsExpanded)
                .Where(x => x)
                .Select(x => Unit.Default)
                .InvokeCommand(ReactiveCommand.CreateFromObservable(LoadDescendants));
        }

        public FolderListItemViewModel(string path, IDataService? dbService = null, IFileSystemService? fsService = null)
            : this(new DirectoryInfo(path), dbService, fsService)
        { }

        public DirectoryInfo DirectoryInfo { get; }

        public string Name => DirectoryInfo.Name;
        public string FullPath => DirectoryInfo.FullName;

        public ObservableCollection<FolderListItemViewModel> Children { get; }

        public IObservable<bool> IsTracked { get; }

        public bool IsExpanded { get => _isExpanded; set => this.RaiseAndSetIfChanged(ref _isExpanded, value); }

        public bool HasLoadedChildren { get => _hasLoadedChildren; set => this.RaiseAndSetIfChanged(ref _hasLoadedChildren, value); }

        public IObservable<Unit> LoadChildren()
        {
            return Observable.StartAsync(async () =>
            {
                if (!HasLoadedChildren)
                {
                    var children = await Task.Run(() =>
                        _fsService.GetDirectories(FullPath)?
                        .Select(path =>
                            new FolderListItemViewModel(path, dbService: _dbService, fsService: _fsService)));

                    if (children != null)
                    {
                        Children.AddRange(children);
                    }
                    HasLoadedChildren = true;
                }
            }, RxApp.MainThreadScheduler);
        }

        public IObservable<Unit> LoadDescendants()
        {
            return Children.ToArray().Select(x => x.LoadChildren()).Concat();
        }
    }
}