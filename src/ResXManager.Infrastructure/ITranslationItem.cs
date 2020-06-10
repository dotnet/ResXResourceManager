namespace ResXManager.Infrastructure
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    public interface ITranslationItem
    {
        [NotNull]
        string Source { get; }

        [NotNull]
        [ItemNotNull]
        IList<ITranslationMatch> Results { get; }

        [NotNull]
        CultureKey TargetCulture { get; }

        string? Translation { get; }

        bool Apply(string? prefix);
    }
}