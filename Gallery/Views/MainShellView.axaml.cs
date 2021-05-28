namespace Gallery.Views
{
    using System;
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.ViewModels;

    using ReactiveUI;

    public class MainShellView : ReactiveUserControl<MainShellViewModel>
    {
        public MainShellView()
        {
            InitializeComponent();
            this.WhenActivated(_ => ViewModel?.GalleryCommand.Execute().Subscribe());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
