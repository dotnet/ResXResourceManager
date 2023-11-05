namespace ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    using ResXManager.Infrastructure;

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

        public bool PrefixValue
        {
            get => PrefixFieldType.IsFlagSet(PrefixFieldType.Value);
            set => PrefixFieldType = PrefixFieldType.WithFlag(PrefixFieldType.Value, value);
        }

        public bool PrefixNeutralComment
        {
            get => PrefixFieldType.IsFlagSet(PrefixFieldType.Comment);
            set => PrefixFieldType = PrefixFieldType.WithFlag(PrefixFieldType.Comment, value);
        }

        public bool PrefixTargetComment
        {
            get => PrefixFieldType.IsFlagSet(PrefixFieldType.TargetComment);
            set => PrefixFieldType = PrefixFieldType.WithFlag(PrefixFieldType.TargetComment, value);
        }

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
        [ForceGlobal]
        public string? TranslatorConfiguration { get; set; }

        [DefaultValue(true)]
        public bool AutoApplyExistingTranslations { get; set; }

        [DefaultValue("")]
        public string? CultureCountyOverrides { get; set; }
    }
}
