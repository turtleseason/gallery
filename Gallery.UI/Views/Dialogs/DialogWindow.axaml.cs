namespace Gallery.UI.Views
{
    using System;
    using System.Reactive.Disposables;
    using System.Threading.Tasks;

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
            this.WhenActivated(disposables =>
            {
                // The dialog closes itself when CloseCommand executes,
                // and the parameter from CloseCommand is passed to Close().
                ViewModel?.CloseCommand.Subscribe(Close).DisposeWith(disposables);

                Interactions.ShowDialog
                    .RegisterHandler(async interaction =>
                        interaction.SetOutput(await ShowDialog(interaction.Input)))
                    .DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task<object?> ShowDialog(DialogViewModelBase viewModel)
        {
            var dialog = new DialogWindow() { DataContext = viewModel };
            return await dialog.ShowDialog<object?>(this);
        }
    }
}
