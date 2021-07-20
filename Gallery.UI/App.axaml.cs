namespace Gallery.UI
{
    using System;
    using System.Reactive;

    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Markup.Xaml;

    using Gallery.UI.Util;
    using Gallery.UI.ViewModels;
    using Gallery.UI.Views;

    using ReactiveUI;

    using Serilog;

    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            AppBootstrapper.RegisterDependencies();
            AppBootstrapper.RegisterViews();

            AppBootstrapper.LogToFile(includeTraceOutput: true);

            RxApp.DefaultExceptionHandler = Observer.Create<Exception>(HandleUncaughtException);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Log.Information("\n\nStarting up~\n");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleUncaughtException(ex);
            }
        }

        private void HandleUncaughtException(Exception ex)
        {
            Log.Error(ex, "Unhandled exception:");
#if DEBUG
            throw ex;
#else
            bool tryContinue = false;

            // (Show notification dialog synchronously to block program execution until user closes the dialog)
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dialog = new DialogWindow() { DataContext = new ErrorViewModel(ex) };
                tryContinue = dialog.ShowDialogSync<bool?>(desktop.MainWindow) ?? false;
            }

            if (!tryContinue)
            {
                Environment.Exit(1);
            }
#endif
        }
    }
}
