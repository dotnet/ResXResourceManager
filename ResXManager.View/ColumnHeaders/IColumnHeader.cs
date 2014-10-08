namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System.Diagnostics.Contracts;
    using tomenglertde.ResXManager.Model;

    public enum ColumnType
    {
        Other,
        Key,
        Comment,
        Language
    }

    public interface IColumnHeader
    {
        ColumnType ColumnType { get; }
    }

    [ContractClass(typeof (LanguageColumnHeaderContract))]
    public interface ILanguageColumnHeader : IColumnHeader
    {
        CultureKey CultureKey { get; }
    }

    [ContractClassFor(typeof (ILanguageColumnHeader))]
    abstract class LanguageColumnHeaderContract : ILanguageColumnHeader
    {
        ColumnType IColumnHeader.ColumnType
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        CultureKey ILanguageColumnHeader.CultureKey
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureKey>() != null);

                throw new System.NotImplementedException();
            }
        }
    }
}