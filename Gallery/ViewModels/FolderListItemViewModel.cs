using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

using DynamicData;

using Gallery.Services;

using ReactiveUI;

using Splat;

namespace Gallery.ViewModels
{
    public class FolderListItemViewModel : ViewModelBase
    {
        IFileSystemService _fsService;
        IDatabaseService _dbService;

        bool _hasLoadedChildren = false;

        public FolderListItemViewModel(DirectoryInfo directoryInfo, IDatabaseService? dbService = null, IFileSystemService? fsService = null)
        {
            _fsService = fsService ?? Locator.Current.GetService<IFileSystemService>();
            _dbService = dbService ?? Locator.Current.GetService<IDatabaseService>();

            DirectoryInfo = directoryInfo;
            IsExpanded = false;
            Children = new ObservableCollection<FolderListItemViewModel>();

            IsTracked = _dbService.IsTracked(FullPath);

            this.WhenAnyValue(x => x.IsExpanded)
                .Where(x => x)
                .Subscribe(_ => { foreach (var child in Children) child.LoadChildren(); });
        }

        public FolderListItemViewModel(string path, IDatabaseService? dbService = null, IFileSystemService? fsService = null)
            : this(new DirectoryInfo(path), dbService, fsService)
        { }

        public DirectoryInfo DirectoryInfo { get; }

        public string Name => DirectoryInfo.Name;

        public string FullPath => DirectoryInfo.FullName;

        public ObservableCollection<FolderListItemViewModel> Children { get; }

        public IObservable<bool> IsTracked { get; init; }
        
        bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
        }

        public void LoadChildren()
        {
            if (!_hasLoadedChildren)
            {
                IEnumerable<string>? childDirectories = _fsService.GetDirectories(FullPath);
                if (childDirectories != null)
                {
                    Children.AddRange(childDirectories.Select(path =>
                        new FolderListItemViewModel(path, dbService: _dbService, fsService: _fsService)));
                }
                
                _hasLoadedChildren = true;
            }
        }
    }
}