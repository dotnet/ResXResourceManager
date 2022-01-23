namespace ResXManager.Infrastructure;

using System.Collections.Generic;

public interface ITranslationItem
{
    string Source { get; }

    IList<ITranslationMatch> Results { get; }

    CultureKey TargetCulture { get; }

    string? Translation { get; }

    bool Apply(string? valuePrefix, string? commentPrefix);
}