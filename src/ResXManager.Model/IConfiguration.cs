namespace ResXManager.Model;

using System;
using System.ComponentModel;
using System.Globalization;

public interface IConfiguration : INotifyPropertyChanged
{
    string? SolutionFolder { get; set; }

    ConfigurationScope Scope { get; }

    CodeReferenceConfiguration CodeReferences { get; }

    bool AutoCreateNewLanguageFiles { get; }

    string? FileExclusionFilter { get; }

    bool SaveFilesImmediatelyUponChange { get; }

    bool RemoveEmptyEntries { get; }

    CultureInfo NeutralResourcesLanguage { get; }

    StringComparison? EffectiveResXSortingComparison { get; }

    DuplicateKeyHandling DuplicateKeyHandling { get; }

    ResourceTableEntryRules Rules { get; }

    bool SortFileContentOnSave { get; }

    bool ConfirmAddLanguageFile { get; }

    StringComparison ResXSortingComparison { get; }

    bool PrefixTranslations { get; }

    string? TranslationPrefix { get; }

    string? EffectiveTranslationPrefix { get; }

    /// <summary>
    /// Apply translation prefix to the value
    /// </summary>
    bool PrefixValue { get; }

    /// <summary>
    /// Apply translation prefix to comment of neutral language
    /// </summary>
    bool PrefixNeutralComment { get; }

    /// <summary>
    /// Apply translation prefix to comment of target language
    /// </summary>
    bool PrefixTargetComment { get; }

    ExcelExportMode ExcelExportMode { get; }

    bool ShowPerformanceTraces { get; }

    bool EnableXlifSync { get; set; }

    string? TranslatorConfiguration { get; set; }

    bool AutoApplyExistingTranslations { get; }

    string? CultureCountyOverrides { get; set; }
}
