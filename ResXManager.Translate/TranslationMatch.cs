namespace tomenglertde.ResXManager.Translators
{
    internal class TranslationMatch : ITranslationMatch
    {
        private readonly ITranslator _translator;
        private readonly string _translatedTranslatedText;
        private readonly double _rating;

        public TranslationMatch(ITranslator translator, string translatedTranslatedText, double rating)
        {
            _translator = translator;
            _translatedTranslatedText = translatedTranslatedText;
            _rating = rating;
        }

        public string TranslatedText
        {
            get
            {
                return _translatedTranslatedText;
            }
        }

        public ITranslator Translator
        {
            get
            {
                return _translator;
            }
        }

        public double Rating
        {
            get
            {
                return _rating;
            }
        }
    }
}