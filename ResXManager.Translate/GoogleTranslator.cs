namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Threading;

    public class GoogleTranslator : TranslatorBase
    {
        public GoogleTranslator()
            :base("Google", "Google", GetCredentials().ToArray())
        {
        }

        private static IEnumerable<ICredentialItem> GetCredentials()
        {
            yield return new CredentialItem("APIKey", "API Key");
        }

        public override bool IsLanguageSupported(CultureInfo culture)
        {
            return false;
        }

        public override void Translate(Dispatcher dispatcher, CultureInfo sourceLanguage, CultureInfo targetLanguage, IList<ITranslationItem> items)
        {
        }
    }
}