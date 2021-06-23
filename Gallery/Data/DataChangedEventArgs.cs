namespace Gallery.Data
{
    using System;

    public class DataChangedEventArgs : EventArgs
    {
        public DataChangedEventArgs(DataChange change) : base()
        {
            Change = change;
        }

        public DataChange Change { get; }
    }
}
