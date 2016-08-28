namespace tomenglertde.ResXManager.View.ColumnHeaders
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using tomenglertde.ResXManager.Infrastructure;

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

    [ContractClass(typeof(LanguageColumnHeaderContract))]
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

    [ContractClassFor(typeof(ILanguageColumnHeader))]
    internal abstract class LanguageColumnHeaderContract : ILanguageColumnHeader
    {
        ColumnType IColumnHeader.ColumnType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        CultureKey ILanguageColumnHeader.CultureKey
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureKey>() != null);

                throw new NotImplementedException();
            }
        }

        CultureInfo ILanguageColumnHeader.EffectiveCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);

                throw new NotImplementedException();
            }
        }
    }
}