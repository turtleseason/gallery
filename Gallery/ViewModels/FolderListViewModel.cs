using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

using DynamicData;
using DynamicData.Binding;

using Gallery.Services;

using Splat;

namespace Gallery.ViewModels
{
    public class FolderListViewModel : ViewModelBase
    {
        SelectedFilesService _sfService;

        public FolderListViewModel(SelectedFilesService? sfService=null)
        {
            _sfService = sfService ?? Locator.Current.GetService<SelectedFilesService>();

            IEnumerable<FolderListItemViewModel>? availableDrives = FileSystemService.GetAvailableDrives()
                ?.Select(driveInfo => new FolderListItemViewModel(driveInfo.Name));
            
            Items = availableDrives != null ? new(availableDrives) : new();
            SelectedItems = new();

            // Todo: design a better way of updating SFService lol
            SelectedItems.ToObservableChangeSet(x => x)
            // ToObservableChangeSet "is only recommended for simple queries which act only on the UI thread
            // as ObservableCollection is not thread safe."
                .Subscribe(changes =>
                {
                    foreach (Change<FolderListItemViewModel, FolderListItemViewModel> change in changes)
                    {
                        if (change.Reason == ChangeReason.Add)
                        {
                            _sfService.AddDirectory(change.Key.FullPath);
                        }
                        else if (change.Reason == ChangeReason.Remove) {
                            _sfService.RemoveDirectory(change.Key.FullPath);
                        }
                    }
                }
            );

            LoadFirstLevelChildren();
        }

        public ObservableCollection<FolderListItemViewModel> Items { get; }

        public ObservableCollection<FolderListItemViewModel> SelectedItems { get; }

        void LoadFirstLevelChildren()
        {
            foreach (var item in Items)
            {
                item.LoadChildren();
            }
        }
    }
}
