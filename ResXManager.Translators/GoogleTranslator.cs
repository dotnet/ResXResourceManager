namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    public class GoogleTranslator : TranslatorBase
    {
        [NotNull]
        private static readonly Uri _uri = new Uri("Todo");
        [NotNull, ItemNotNull]
        private static readonly IList<ICredentialItem> _credentialItems = new ICredentialItem[] { new CredentialItem("APIKey", "API Key") };

        public GoogleTranslator()
            : base("Google", "Google", _uri, _credentialItems)
        {
        }

        public override void Translate(ITranslationSession translationSession)
        {
        }
    }
}