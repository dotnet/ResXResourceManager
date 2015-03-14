namespace tomenglertde.ResXManager.Translators
{
    public interface ITranslationMatch
    {
        string TranslatedText
        {
            get;
        }

        ITranslator Translator
        {
            get;
        }

        int Rating
        {
            get;
        }
    }
}