namespace Gallery.Data
{
    public enum DataChangeReason { Add, Update, Remove }

    public enum DataChangeEntity { File, Tag, TagGroup, Folder }

    public class DataChange
    {
        public DataChange(object item, DataChangeReason reason, DataChangeEntity entity, params string[] files)
        {
            Item = item;
            Reason = reason;
            EntityType = entity;
            AffectedFiles = files;
        }

        public object Item { get; }

        public DataChangeReason Reason { get; }

        public DataChangeEntity EntityType { get; }

        public string[] AffectedFiles { get; }
    }
}
