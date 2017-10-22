namespace tomenglertde.ResXManager.Translators
{
    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    public class TranslationMatch : ITranslationMatch
    {
        [CanBeNull]
        private readonly ITranslator _translator;
        [CanBeNull]
        private readonly string _translatedTranslatedText;
        private readonly double _rating;

        public TranslationMatch(ITranslator translator, string translatedTranslatedText, double rating)
        {
            _translator = translator;
            _translatedTranslatedText = translatedTranslatedText;
            _rating = rating;
        }

        public string TranslatedText => _translatedTranslatedText;

        public ITranslator Translator => _translator;

        public double Rating => _rating;
    }
}