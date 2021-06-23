namespace Gallery.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            View = new MainShellViewModel();
        }

        public MainShellViewModel View { get; }
    }
}
