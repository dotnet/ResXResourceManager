namespace ResXManager.View.ColumnHeaders;

using System.Globalization;

using ResXManager.Infrastructure;

public enum ColumnType
{
    Other,
    Key,
    Comment,
    Language
}

public interface IColumnHeader
{
    ColumnType ColumnType
    {
        get;
    }
}

public interface ILanguageColumnHeader : IColumnHeader
{
    CultureKey CultureKey
    {
        get;
    }

    CultureInfo EffectiveCulture
    {
        get;
    }
}