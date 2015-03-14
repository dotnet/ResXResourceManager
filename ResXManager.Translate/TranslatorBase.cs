namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Threading;

    using TomsToolbox.Desktop;

    public abstract class TranslatorBase : ObservableObject, ITranslator
    {
        private bool _isEnabled = true;
        private readonly string _id;
        private readonly string _displayName;
        private readonly ICollection<ICredentialItem> _credentials;

        protected TranslatorBase(string id, string displayName, ICollection<ICredentialItem> credentials)
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

        public virtual IList<ICredentialItem> Credentials
        {
            get
            {
                return _credentials;
            }
        }

        public abstract bool IsLanguageSupported(CultureInfo culture);

        public abstract void Translate(Dispatcher dispatcher, CultureInfo sourceLanguage, CultureInfo targetLanguage, IList<ITranslationItem> items);
    }
}