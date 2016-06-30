namespace tomenglertde.ResXManager.Translators
{
    using System;

    public class GoogleTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new Uri("Todo");

        public GoogleTranslator()
            : base("Google", "Google", _uri, new[] { new CredentialItem("APIKey", "API Key") })
        {
        }

        public override void Translate(Session session)
        {
        }
    }
}