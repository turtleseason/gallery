namespace Gallery.UI
{
    using System.Diagnostics;
    using System.Reflection;

    using Gallery.Data;
    using Gallery.Util;

    using ReactiveUI;

    using Serilog;

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
            Locator.CurrentMutable.RegisterConstant(new FileSystemUtil(), typeof(IFileSystemUtil));

            Locator.CurrentMutable.RegisterConstant(new DataService(), typeof(IDataService));

            Locator.CurrentMutable.RegisterConstant(new SelectedFilesService(), typeof(ISelectedFilesService));
        }

        /// Registers views to view models for ReactiveUI routing.
        public static void RegisterViews()
        {
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
        }

        public static void LogToFile(bool includeTraceOutput)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("log.txt",
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10000000,
                    rollOnFileSizeLimit: true)
                .CreateLogger();

            if (includeTraceOutput)
            {
                Trace.Listeners.Add(new SerilogTraceListener.SerilogTraceListener());
            }
        }
    }
}
