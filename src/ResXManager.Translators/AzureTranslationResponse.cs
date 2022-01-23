namespace ResXManager.Translators;

using System.Collections.Generic;

#pragma warning disable CA2227 // Collection properties should be read only => serialized DTOs!

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
