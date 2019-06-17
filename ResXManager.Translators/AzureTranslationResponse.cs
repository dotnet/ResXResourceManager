using System.Collections.Generic;

namespace tomenglertde.ResXManager.Translators
{
    public class AzureTranslationResponse
    {
        public List<Translation> Translations { get; set; }
    }

    public class AzureDetectedLanguage
    {
        public string Language { get; set; }

        public int Score { get; set; }
    }

    public class Translation
    {
        public string Text { get; set; }

        public string To { get; set; }
    }
}
