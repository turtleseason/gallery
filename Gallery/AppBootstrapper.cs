namespace Gallery
{
    using System.Reflection;

    using Gallery.Services;
    using Gallery.ViewModels;
    using Gallery.Views;

    using ReactiveUI;

    using Splat;

    internal static class AppBootstrapper
    {
        /// Registers implementations for the service locator.
        ///
        /// Trying out Splat's service locator for dependency inversion since it's already included with ReactiveUI;
        /// if it ends up being difficult to test/debug, could go the manual route (composition root) or even use a full-blown DI container?
        /// Just gotta try it and see how it works out~
        public static void RegisterDependencies()
        {
            Locator.CurrentMutable.RegisterConstant(new FileSystemService(), typeof(IFileSystemService));

            Locator.CurrentMutable.RegisterConstant(new DataService(), typeof(IDataService));

            Locator.CurrentMutable.RegisterConstant(new SelectedFilesService(), typeof(ISelectedFilesService));
        }

        /// Registers views to view models for ReactiveUI routing.
        public static void RegisterViews()
        {
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());

            ////Locator.CurrentMutable.Register(() => new SearchView(), typeof(IViewFor<SearchViewModel>));
            ////Locator.CurrentMutable.Register(() => new GalleryView(), typeof(IViewFor<GalleryViewModel>));
        }
    }
}