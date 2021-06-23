namespace Gallery.UI.Views
{
    using System;
    using System.Reactive.Disposables;

    using Avalonia;
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI.ViewModels;

    using ReactiveUI;

    public partial class DialogWindow : ReactiveWindow<DialogViewModelBase>
    {
        public DialogWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            // The dialog closes itself when CloseCommand executes, and the parameter from CloseCommand is passed to Close().
            this.WhenActivated(disposables => ViewModel?.CloseCommand.Subscribe(Close).DisposeWith(disposables));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
