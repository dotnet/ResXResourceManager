namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;

    using JetBrains.Annotations;

    [ContractClass(typeof (TranslatorContract))]
    public interface ITranslator : INotifyPropertyChanged
    {
        [NotNull]
        string Id
        {
            get;
        }

        [NotNull]
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

        void Translate([NotNull] ITranslationSession translationSession);

        [NotNull]
        IList<ICredentialItem> Credentials
        {
            get;
        }
    }

    [ContractClassFor(typeof (ITranslator))]
    internal abstract class TranslatorContract : ITranslator
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

        void ITranslator.Translate(ITranslationSession translationSession)
        {
            Contract.Requires(translationSession != null);
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

        public abstract event PropertyChangedEventHandler PropertyChanged;
    }
}
