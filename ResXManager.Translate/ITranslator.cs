namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Threading;

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

        bool IsEnabled
        {
            get;
            set;
        }

        bool IsLanguageSupported(CultureInfo culture);

        void Translate(Dispatcher dispatcher, CultureInfo sourceLanguage, CultureInfo targetLanguage, IList<ITranslationItem> items);

        IList<ICredentialItem> Credentials
        {
            get;
        }
    }
}
