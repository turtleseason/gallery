namespace Gallery.UI.Views
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Avalonia.Controls;
    using Avalonia.Controls.Primitives;
    using Avalonia.Markup.Xaml;
    using Avalonia.ReactiveUI;

    using Gallery.UI.ViewModels;

    using ReactiveUI;

    public partial class AddTagsView : ReactiveUserControl<AddTagsViewModel>
    {
        public AddTagsView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                AutoCompleteBox tagNameBox = this.FindControl<AutoCompleteBox>("TagName");

                tagNameBox.Events().LostFocus
                    .Subscribe(_ => ViewModel?.SetTagGroupIfTagExists())
                    .DisposeWith(disposables);

                tagNameBox.Events().DropDownClosed
                    .Subscribe(_ => ViewModel?.SetTagGroupIfTagExists())
                    .DisposeWith(disposables);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
