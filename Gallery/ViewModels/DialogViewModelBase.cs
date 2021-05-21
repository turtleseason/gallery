namespace Gallery.ViewModels
{
    using ReactiveUI;

    public class DialogViewModelBase : ViewModelBase
    {
        public DialogViewModelBase()
        {
            CloseCommand = ReactiveCommand.Create<object?, object?>(result => result);
        }

        public string WindowTitle { get; protected set; } = string.Empty;

        /// A dialog can invoke this command to close itself; the command parameter passed to CloseCommand
        /// will be the value returned from the dialog interaction.
        public ReactiveCommand<object?, object?> CloseCommand { get; }
    }
}
