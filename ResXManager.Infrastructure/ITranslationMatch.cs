namespace tomenglertde.ResXManager.Infrastructure
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

        double Rating
        {
            get;
        }
    }
}