namespace ResXManager.View.ColumnHeaders
{
    using System.Globalization;

    using JetBrains.Annotations;

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
        [NotNull]
        CultureKey CultureKey
        {
            get;
        }

        [NotNull]
        CultureInfo EffectiveCulture
        {
            get;
        }
    }
}