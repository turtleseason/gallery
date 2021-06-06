namespace Gallery.Services
{
    using System;

    using Gallery.Models;

    public class DataChangedEventArgs : EventArgs
    {
        public DataChangedEventArgs(DataChange change) : base()
        {
            Change = change;
        }

        public DataChange Change { get; }
    }
}
