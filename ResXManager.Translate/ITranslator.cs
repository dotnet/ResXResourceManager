namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    [ContractClass(typeof (ITranslatorContract))]
    public interface ITranslator
    {
        string Id
        {
            get;
        }

        string DisplayName
        {
            get;
        }

        Uri Uri
        {
            get;
        }

        bool IsEnabled
        {
            get;
            set;
        }

        bool SaveCredentials
        {
            get;
            set;
        }

        bool IsLanguageSupported(CultureInfo culture);

        void Translate(Session session);

        IList<ICredentialItem> Credentials
        {
            get;
        }
    }

    [ContractClassFor(typeof (ITranslator))]
    abstract class ITranslatorContract : ITranslator
    {
        string ITranslator.Id
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                throw new NotImplementedException();
            }
        }

        string ITranslator.DisplayName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                throw new NotImplementedException();
            }
        }

        Uri ITranslator.Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool ITranslator.IsEnabled
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ITranslator.SaveCredentials
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ITranslator.IsLanguageSupported(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        void ITranslator.Translate(Session session)
        {
            throw new NotImplementedException();
        }

        IList<ICredentialItem> ITranslator.Credentials
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ICredentialItem>>() != null);
                throw new NotImplementedException();
            }
        }
    }
}
