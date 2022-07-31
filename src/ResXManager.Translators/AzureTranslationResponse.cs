namespace ResXManager.Translators
{
    using System.Collections.Generic;

#pragma warning disable CA2227 // Collection properties should be read only => serialized DTOs!
#pragma warning disable CA1002 // Do not expose generic lists => serialized DTOs!

    public class AzureTranslationResponse
    {
        public List<Translation>? Translations { get; set; }
    }

    public class AzureDetectedLanguage
    {
        public string? Language { get; set; }

        public int Score { get; set; }
    }

    public class Translation
    {
        public string? Text { get; set; }

        public string? To { get; set; }
    }
}
