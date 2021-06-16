/// Information used by the main view to display an executing command's progress to the user.
namespace Gallery.Models
{
    using System;
    using System.Reactive.Linq;

    public class CommandProgressInfo
    {
        public CommandProgressInfo(IObservable<bool> isExecuting, IObservable<string> label, IObservable<float?>? progress = null)
        {
            IsExecuting = isExecuting;
            Label = label;
            Progress = progress ?? Observable.Return<float?>(null);
        }

        public CommandProgressInfo(IObservable<bool> isExecuting, string label, IObservable<float?>? progress = null)
            : this(isExecuting, Observable.Return(label), progress)
        { }

        // When this returns false, the command progress is considered complete and the listener will unsubscribe.
        public IObservable<bool> IsExecuting { get; }

        // The text to describe the command's progress to the user.
        public IObservable<string> Label { get; }

        // Returns the command progress as a number from 0-100.
        // If the value is null, an indeterminate progress bar is used.
        public IObservable<float?> Progress { get; }
    }
}
