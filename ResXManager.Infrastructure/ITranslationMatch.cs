namespace ResXManager.Infrastructure
{
    using JetBrains.Annotations;

    public interface ITranslationMatch
    {
        [CanBeNull]
        string TranslatedText { get; }

        [CanBeNull]
        ITranslator Translator { get; }

        double Rating { get; }
    }
}