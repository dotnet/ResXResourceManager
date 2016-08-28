namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;

    public class GoogleTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new Uri("Todo");
        private static readonly IList<ICredentialItem> _credentialItems = new ICredentialItem[] { new CredentialItem("APIKey", "API Key") };

        public GoogleTranslator()
            : base("Google", "Google", _uri, _credentialItems)
        {
        }

        public override void Translate(Session session)
        {
        }
    }
}