namespace ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    using ResXManager.Infrastructure;
    using ResXManager.Model.Properties;

    public enum DuplicateKeyHandling
    {
        [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Rename)]
        Rename,
        [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Fail)]
        Fail
    }

    [Flags]
    public enum PrefixFieldType
    {
        [LocalizedDisplayName(StringResourceKey.PrefixFieldTypeValue)]
        Value = 1,
        [LocalizedDisplayName(StringResourceKey.PrefixFieldTypeComment)]
        Comment = 2,
        [LocalizedDisplayName(StringResourceKey.PrefixFieldTypeBoth)]
        Both = Value | Comment
    }

    public interface IConfiguration : INotifyPropertyChanged
    {
        bool IsScopeSupported { get; }

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

        public bool PrefixTranslations { get; }

        public string? TranslationPrefix { get; }

        public string? EffectiveTranslationPrefix { get; }

        public PrefixFieldType PrefixFieldType { get; }

        public ExcelExportMode ExcelExportMode { get; }

        bool ShowPerformanceTraces { get; }

        bool EnableXlifSync { get; set; }

        string? TranslatorConfiguration { get; set; }
    }

    public abstract class Configuration : ConfigurationBase, IConfiguration
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected Configuration(ITracer tracer)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(tracer)
        {
        }

        [DefaultValue(CodeReferenceConfiguration.Default)]
        public CodeReferenceConfiguration CodeReferences { get; }

        [DefaultValue(@"Migrations\\\d{15}")]
        public string? FileExclusionFilter { get; set; }

        [DefaultValue(true)]
        public bool SaveFilesImmediatelyUponChange { get; set; }

        [DefaultValue(true)]
        public bool RemoveEmptyEntries { get; set; }

        [DefaultValue(false)]
        public bool SortFileContentOnSave { get; set; }

        [DefaultValue("en-US")]
        public CultureInfo NeutralResourcesLanguage { get; set; }

        [DefaultValue(StringComparison.OrdinalIgnoreCase)]
        public StringComparison ResXSortingComparison { get; set; }

        public StringComparison? EffectiveResXSortingComparison => SortFileContentOnSave ? ResXSortingComparison : null;

        [DefaultValue(false)]
        public bool ConfirmAddLanguageFile { get; set; }

        [DefaultValue(false)]
        public bool AutoCreateNewLanguageFiles { get; set; }

        [DefaultValue(false)]
        public bool PrefixTranslations { get; set; }

        [DefaultValue("#TODO#_")]
        public string? TranslationPrefix { get; set; }

        public string? EffectiveTranslationPrefix => PrefixTranslations ? TranslationPrefix : string.Empty;

        [DefaultValue(PrefixFieldType.Value)]
        public PrefixFieldType PrefixFieldType { get; set; }

        [DefaultValue(default(ExcelExportMode))]
        public ExcelExportMode ExcelExportMode { get; set; }

        [DefaultValue(default(DuplicateKeyHandling))]
        public DuplicateKeyHandling DuplicateKeyHandling { get; set; }

        [DefaultValue(ResourceTableEntryRules.Default)]
        public ResourceTableEntryRules Rules { get; }

        [DefaultValue(false)]
        public bool ShowPerformanceTraces { get; set; }

        [DefaultValue(false)]
        public bool EnableXlifSync { get; set; }

        [DefaultValue(null)]
        public string? TranslatorConfiguration { get; set; }
    }
}
