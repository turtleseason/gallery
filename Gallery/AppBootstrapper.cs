using Gallery.Services;

using Splat;

namespace Gallery
{
    static class AppBootstrapper
    {
        /// Registers implementations for the service locator.
        /// 
        /// Trying out Splat's service locator for dependency inversion since it's already included with ReactiveUI;
        /// if it ends up being difficult to test/debug, could go the manual route (composition root) or even use a full-blown DI container?
        /// Just gotta try it and see how it works out~
        public static void RegisterDependencies()
        {
            //Locator.CurrentMutable.RegisterConstant(new SelectedFilesService(), typeof(SelectedFilesService));
            Locator.CurrentMutable.RegisterLazySingleton(() => new SelectedFilesService(), typeof(SelectedFilesService));
        }
    }
}
