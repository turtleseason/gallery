namespace Gallery.UI.ViewModels
{
    using System;

    public class ErrorViewModel : DialogViewModelBase
    {
        public ErrorViewModel(Exception ex)
        {
            WindowTitle = "Error";
            Exception = ex.ToString();
        }

        public string Exception { get; }
    }
}
