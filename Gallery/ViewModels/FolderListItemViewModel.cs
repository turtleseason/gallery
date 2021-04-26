using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

using DynamicData;

using Gallery.Services;

using ReactiveUI;

namespace Gallery.ViewModels
{
    public class FolderListItemViewModel : ViewModelBase
    {
        private bool _hasLoadedChildren = false;

        public FolderListItemViewModel(DirectoryInfo directoryInfo)
        {
            DirectoryInfo = directoryInfo;
            IsExpanded = false;
            Children = new ObservableCollection<FolderListItemViewModel>();

            this.WhenAnyValue(x => x.IsExpanded)
                .Where(x => x)
                .Subscribe(_ => {
                    foreach (var child in Children)
                    {
                        child.LoadChildren();
                    }
                });
        }

        public FolderListItemViewModel(string path) : this(new DirectoryInfo(path)) { }

        public DirectoryInfo DirectoryInfo { get; }

        public string Name => DirectoryInfo.Name;

        public string FullPath => DirectoryInfo.FullName;

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
        }

        public ObservableCollection<FolderListItemViewModel> Children { get; }

        public void LoadChildren()
        {
            if (!_hasLoadedChildren)
            {
                IEnumerable<string>? childDirectories = FileSystemService.GetDirectories(FullPath);
                if (childDirectories != null)
                {
                    Children.AddRange(childDirectories.Select(path => new FolderListItemViewModel(path)));
                }
                
                _hasLoadedChildren = true;
            }
        }
    }
}
