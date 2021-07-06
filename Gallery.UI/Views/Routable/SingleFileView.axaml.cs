namespace Gallery.UI.Views
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI.ViewModels;

    using ReactiveUI;

    public partial class SingleFileView : ReactiveUserControl<SingleFileViewModel>
    {
        public SingleFileView()
        {
            InitializeComponent();
            this.WhenActivated(_ => { });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
