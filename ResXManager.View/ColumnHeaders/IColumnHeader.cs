namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using tomenglertde.ResXManager.Model;

    public interface IColumnHeader
    {
        ColumnType ColumnType { get; }
    }

    public interface ILanguageColumnHeader : IColumnHeader
    {
        CultureKey CultureKey { get; }
    }

    public enum ColumnType
    {
        Other,
        Key,
        Comment,
        Language
    }
}