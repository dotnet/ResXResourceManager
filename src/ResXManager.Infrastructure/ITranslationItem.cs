namespace ResXManager.Infrastructure
{
    using System.Collections.Generic;
    using System.Globalization;

    public interface ITranslationItem
    {
        string Source { get; }

        public IList<(CultureInfo Culture, string Text, string? Comment)> GetAllItems(CultureInfo neutralCulture);

        IList<ITranslationMatch> Results { get; }

        CultureKey TargetCulture { get; }

        string? Translation { get; }

        bool UpdateTranslation(string? prefix);

        bool UpdateComment(string? prefix, bool useNeutralLanguage, bool useTargetLanguage);
    }
}