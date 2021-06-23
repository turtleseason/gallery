namespace Gallery.UI.Views
{
    using System.Reactive.Disposables;
    using System.Threading.Tasks;

    using Avalonia;
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI;
    using Gallery.UI.ViewModels;

    using ReactiveUI;

    public class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            this.WhenActivated(disposables =>
            {
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
