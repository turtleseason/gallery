namespace Gallery
{
    using System;
    using System.Reactive;

    using Gallery.Models;
    using Gallery.ViewModels;

    using ReactiveUI;

    public static class Interactions
    {
        public static readonly Interaction<DialogViewModelBase, object?> ShowDialog = new();

        public static readonly Interaction<CommandProgressInfo, Unit> ShowCommandProgress = new();

        /// Helper for Interactions.ShowCommandProgress
        public static void ReportCommandProgress(IObservable<bool> isExecuting,
                                                  IObservable<string> label,
                                                  IObservable<float?>? progress = null)
        {
            ShowCommandProgress
                .Handle(new CommandProgressInfo(isExecuting, label, progress))
                .Subscribe();
        }
    }
}
