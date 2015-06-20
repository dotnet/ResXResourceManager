namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof (TranslationItemContract))]
    public interface ITranslationItem
    {
        string Source
        {
            get;
        }

        IList<ITranslationMatch> Results
        {
            get;
        }
    }

    [ContractClassFor(typeof (ITranslationItem))]
    abstract class TranslationItemContract : ITranslationItem
    {
        string ITranslationItem.Source
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                throw new NotImplementedException();
            }
        }

        IList<ITranslationMatch> ITranslationItem.Results
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ITranslationMatch>>() != null);
                throw new NotImplementedException();
            }
        }
    }
}