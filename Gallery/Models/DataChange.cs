namespace Gallery.Models
{
    public enum ChangeReason { Add, Update, Remove }

    public enum ChangeEntity { File, Tag, TagGroup }

    public class DataChange
    {
        public DataChange(object item, ChangeReason reason, ChangeEntity entity, params string[] files)
        {
            Item = item;
            Reason = reason;
            EntityType = entity;
            AffectedFiles = files;
        }

        public object Item { get; }

        public ChangeReason Reason { get; }

        public ChangeEntity EntityType { get; }

        public string[] AffectedFiles { get; }
    }
}
