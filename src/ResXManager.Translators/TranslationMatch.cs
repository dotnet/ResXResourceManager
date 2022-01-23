namespace ResXManager.Translators;

using System.Collections.Generic;

using ResXManager.Infrastructure;

using TomsToolbox.Essentials;

public class TranslationMatch : ITranslationMatch
{
    public TranslationMatch(ITranslator? translator, string? translatedText, double rating)
    {
        Translator = translator;
        TranslatedText = translatedText?.Trim().Trim('\0');
        Rating = rating;
    }

    public string? TranslatedText { get; }

    public ITranslator? Translator { get; }

    public double Rating { get; }

    public static readonly IEqualityComparer<TranslationMatch> TextComparer = new DelegateEqualityComparer<TranslationMatch>(m => m?.TranslatedText);
}