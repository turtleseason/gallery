namespace Gallery.UI.Views
{
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI.ViewModels;

    public partial class SingleFileView : ReactiveUserControl<SingleFileViewModel>
    {
        public SingleFileView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
