namespace Gallery.UI
{
    using System;
    using System.Reactive;

    using Gallery.UI.ViewModels;

    using ReactiveUI;

    public static class Interactions
    {
        /// Open a new modal dialog from the most recently opened window.
        public static readonly Interaction<DialogViewModelBase, object?> ShowDialog = new();

        /// Show an executing command's progress in a status bar in the main window.
        public static readonly Interaction<CommandStatusViewModel, Unit> ShowCommandProgress = new();

        /// Helper for Interactions.ShowCommandProgress
        public static void ReportCommandProgress(IObservable<bool> isExecuting,
                                                  IObservable<string> label,
                                                  IObservable<float?>? progress = null)
        {
            ShowCommandProgress
                .Handle(new CommandStatusViewModel(isExecuting, label, progress))
                .Subscribe();
        }
    }
}
