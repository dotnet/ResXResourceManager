namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

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

        public override void Translate(Session session)
        {
        }
    }
}