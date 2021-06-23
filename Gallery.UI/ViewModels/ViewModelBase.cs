namespace Gallery.UI.ViewModels
{
    using ReactiveUI;

    public class ViewModelBase : ReactiveObject, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; } = new ViewModelActivator();
    }
}
