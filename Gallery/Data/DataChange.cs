namespace Gallery.Data
{
    public enum DataChangeReason { Add, Update, Remove }

    public enum DataChangeEntity { File, Tag, TagGroup, Folder }

    public class DataChange
    {
        // Item:     The main item that changed; type is identified by DataChangeEntity
        //           (File -> TrackedFile, Folder -> string, Tag -> Tag, TagGroup -> TagGroup)
        // Original: The previous version of the item (only used for DataChangeReason.Update
        //           when the object's primary unique identifier may have changed)
        // Files:    A list of affected files, where relevant (e.g. for tag changes)
        public DataChange(object item, DataChangeReason reason, DataChangeEntity entity, object? original = null, params string[] files)
        {
            Item = item;
            Original = original;
            Reason = reason;
            EntityType = entity;
            AffectedFiles = files;
        }

        public object Item { get; }

        public object? Original { get; }

        public DataChangeReason Reason { get; }

        public DataChangeEntity EntityType { get; }

        public string[] AffectedFiles { get; }
    }
}
