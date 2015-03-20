namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;

    using TomsToolbox.Desktop;

    [DataContract]
    public abstract class TranslatorBase : ObservableObject, ITranslator
    {
        private bool _isEnabled = true;
        private bool _saveCredentials;
        private readonly string _id;
        private readonly string _displayName;
        private readonly IList<ICredentialItem> _credentials;

        protected TranslatorBase(string id, string displayName, IList<ICredentialItem> credentials)
        {
            _id = id;
            _displayName = displayName;
            _credentials = credentials ?? new ICredentialItem[0];
        }

        public string Id
        {
            get
            {
                return _id;
            }
        }

        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

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
                return _credentials;
            }
        }

        public abstract bool IsLanguageSupported(CultureInfo culture);

        public abstract void Translate(Session session);
    }
}