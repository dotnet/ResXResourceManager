namespace ResXManager.Infrastructure
{
    using System.Collections.Generic;

    public interface ITranslationItem
    {
        string Source { get; }

        IList<ITranslationMatch> Results { get; }

        CultureKey TargetCulture { get; }

        string? Translation { get; }

        bool UpdateTranslation(string? prefix);

        bool UpdateComment(string? prefix, bool useNeutralLanguage, bool useTargetLanguage);
    }
}