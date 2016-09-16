namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Runtime.Serialization;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    [ContractClass(typeof(TranslatorBaseContract))]
    [DataContract]
    public abstract class TranslatorBase : ObservableObject, ITranslator
    {
        protected static readonly IWebProxy WebProxy;

        private readonly string _id;
        private readonly string _displayName;
        private readonly IList<ICredentialItem> _credentials;
        private readonly Uri _uri;

        private bool _isEnabled = true;
        private bool _saveCredentials;

        static TranslatorBase()
        {
            try
            {
                WebProxy = WebRequest.DefaultWebProxy ?? new WebProxy();
                WebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            catch
            {
                // ignored
            }
        }

        protected TranslatorBase(string id, string displayName, Uri uri, IList<ICredentialItem> credentials)
        {
            Contract.Requires(id != null);
            Contract.Requires(displayName != null);

            _id = id;
            _displayName = displayName;
            _uri = uri;
            _credentials = credentials ?? new ICredentialItem[0];
        }

        public string Id => _id;

        public string DisplayName => _displayName;

        public Uri Uri => _uri;

        [DataMember]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                SetProperty(ref _isEnabled, value, () => IsEnabled);
            }
        }

        [DataMember]
        public bool SaveCredentials
        {
            get
            {
                return _saveCredentials;
            }
            set
            {
                SetProperty(ref _saveCredentials, value, () => SaveCredentials);
            }
        }

        public virtual IList<ICredentialItem> Credentials
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ICredentialItem>>() != null);
                return _credentials;
            }
        }

        public abstract void Translate(ITranslationSession translationSession);

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_id != null);
            Contract.Invariant(_displayName != null);
            Contract.Invariant(_credentials != null);
        }
    }

    [ContractClassFor(typeof(TranslatorBase))]
    internal abstract class TranslatorBaseContract : TranslatorBase
    {
        protected TranslatorBaseContract(string id, string displayName, Uri uri, IList<ICredentialItem> credentials)
            : base(id, displayName, uri, credentials)
        {
        }

        public override void Translate(ITranslationSession translationSession)
        {
            Contract.Requires(translationSession != null);
            throw new NotImplementedException();
        }
    }
}