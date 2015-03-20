namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Globalization;

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
}
