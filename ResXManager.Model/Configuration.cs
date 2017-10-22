namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    public enum DuplicateKeyHandling
    {
        [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Rename)]
        Rename,
        [LocalizedDisplayName(StringResourceKey.DuplicateKeyHandling_Fail)]
        Fail
    }

    public interface IConfiguration
    {
        bool SaveFilesImmediatelyUponChange { get; }

        [NotNull]
        CultureInfo NeutralResourcesLanguage { get; }

        StringComparison? EffectiveResXSortingComparison { get; }

        DuplicateKeyHandling DuplicateKeyHandling { get; }
    }

    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Works fine with this")]
    public abstract class Configuration : ConfigurationBase, IConfiguration
    {
        protected Configuration([NotNull] ITracer tracer)
            : base(tracer)
        {
            Contract.Requires(tracer != null);
        }

        [NotNull, UsedImplicitly]
        [DefaultValue(CodeReferenceConfiguration.Default)]
        public CodeReferenceConfiguration CodeReferences { get; }

        [DefaultValue(@"Migrations\\\d{15}"), CanBeNull]
        public string FileExclusionFilter { get; set; }

        [DefaultValue(true)]
        public bool SaveFilesImmediatelyUponChange { get; set; }

        [DefaultValue(false)]
        public bool SortFileContentOnSave { get; set; }

        [DefaultValue("en-US")]
        public CultureInfo NeutralResourcesLanguage { get; set; }

        [DefaultValue(StringComparison.OrdinalIgnoreCase)]
        public StringComparison ResXSortingComparison { get; set; }

        public StringComparison? EffectiveResXSortingComparison => SortFileContentOnSave ? ResXSortingComparison : (StringComparison?)null;

        [DefaultValue(true)]
        public bool ConfirmAddLanguageFile { get; set; }

        [DefaultValue(false)]
        public bool AutoCreateNewLanguageFiles { get; set; }

        [DefaultValue(false)]
        public bool PrefixTranslations { get; set; }

        [DefaultValue("#TODO#_")]
        [CanBeNull]
        public string TranslationPrefix { get; set; }

        [CanBeNull]
        public string EffectiveTranslationPrefix => PrefixTranslations ? TranslationPrefix : string.Empty;

        [DefaultValue(default(ExcelExportMode))]
        public ExcelExportMode ExcelExportMode { get; set; }

        [DefaultValue(default(DuplicateKeyHandling))]
        public DuplicateKeyHandling DuplicateKeyHandling { get; set; }

        [DefaultValue(false)]
        public bool ShowPerformanceTraces { get; set; }
    }
}
