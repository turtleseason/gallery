using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData;
using DynamicData.Binding;

using Gallery.Models;
using Gallery.Services;

using ReactiveUI;

using Splat;

namespace Gallery.ViewModels
{
    public class GalleryViewModel : ViewModelBase, IActivatableViewModel
    {
        ISelectedFilesService _sfService;

        public GalleryViewModel(ISelectedFilesService? sfService=null)
        {
            _sfService = sfService ?? Locator.Current.GetService<ISelectedFilesService>();

            IDisposable subscription = _sfService.Connect()
                .Sort(SortExpressionComparer<GalleryFile>.Ascending(file => file.FullPath))  // todo: sort in SFS (so it applies everywhere)
                .ObserveOn(RxApp.MainThreadScheduler)  // not sure if necessary?
                .Bind(out _items)
                .Subscribe();

            this.WhenActivated((CompositeDisposable disposables) => subscription.DisposeWith(disposables));
        }

        // Need to declare parameterless constructor explicitly for the XAML designer preview to work
        public GalleryViewModel() : this(null)
        { }

        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        
        ReadOnlyObservableCollection<GalleryFile> _items;
        public ReadOnlyObservableCollection<GalleryFile> Items => _items;
    }
}