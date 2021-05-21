namespace Gallery
{
    using Gallery.ViewModels;

    using ReactiveUI;

    public static class Interactions
    {
        public static readonly Interaction<DialogViewModelBase, object?> ShowDialog = new();
    }
}
