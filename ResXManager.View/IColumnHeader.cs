namespace tomenglertde.ResXManager.View
{
    using System.Globalization;

    public interface IColumnHeader
    {
        ColumnType ColumnType { get; }
    }

    public interface ILanguageColumnHeader : IColumnHeader
    {
        CultureInfo Language { get; }
    }

    public enum ColumnType
    {
        Other,
        Key,
        Comment,
        Language
    }
}