namespace tomenglertde.ResXManager.Translators
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    public class TranslationMatch : ITranslationMatch
    {
        public TranslationMatch([CanBeNull] ITranslator translator, [CanBeNull] string translatedText, double rating)
        {
            Translator = translator;
            TranslatedText = translatedText?.Trim().Trim('\0');
            Rating = rating;
        }

        [CanBeNull]
        public string TranslatedText { get; }

        [CanBeNull]
        public ITranslator Translator { get; }

        public double Rating { get; }

        [NotNull]
        public static readonly IEqualityComparer<TranslationMatch> TextComparer = new DelegateEqualityComparer<TranslationMatch>(m => m?.TranslatedText);
    }
}