namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    [DataContract]
    public abstract class TranslatorBase : ObservableObject, ITranslator
    {
        protected static readonly IWebProxy WebProxy;

        [NotNull]
        private readonly IList<ICredentialItem> _credentials;

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

        protected TranslatorBase([NotNull] string id, [NotNull] string displayName, Uri uri, IList<ICredentialItem> credentials)
        {
            Contract.Requires(id != null);
            Contract.Requires(displayName != null);

            Id = id;
            DisplayName = displayName;
            Uri = uri;
            _credentials = credentials ?? new ICredentialItem[0];
        }

        public string Id { get; }

        public string DisplayName { get; }

        public Uri Uri { get; }

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

        public IList<ICredentialItem> Credentials => _credentials;

        public abstract void Translate(ITranslationSession translationSession);

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_credentials != null);
        }
    }
}