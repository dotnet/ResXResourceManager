namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Ninject;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Composition;
    using TomsToolbox.Composition.Ninject;
    using TomsToolbox.Essentials;

    public sealed class Host : IDisposable
    {
        private readonly IKernel _kernel = new StandardKernel();

        private string? _solutionFolder;

        public Host()
        {
            var assembly = GetType().Assembly;

            _kernel.BindExports(assembly,
                typeof(Infrastructure.Properties.AssemblyKey).Assembly,
                typeof(Model.Properties.AssemblyKey).Assembly);

            IExportProvider exportProvider = new ExportProvider(_kernel);

            ResourceManager = exportProvider.GetExportedValue<ResourceManager>();
            ResourceManager.BeginEditing += ResourceManager_BeginEditing;

            Configuration = exportProvider.GetExportedValue<Configuration>();
        }

        public ResourceManager ResourceManager { get; }

        public void Load(string folder, string? exclusionFilter = @"Migrations\\\d{15}")
        {
            _solutionFolder = folder;

            var sourceFilesProvider = new SourceFilesProvider(folder, exclusionFilter);

            var _ = ResourceManager.ReloadAsync(folder, sourceFilesProvider.EnumerateSourceFiles(), null).Result;
        }

        public void Save()
        {
            ResourceManager.Save();
        }

        public void ExportExcel(string filePath)
        {
            ExportExcel(filePath, null);
        }

        public void ExportExcel(string filePath, object? entries)
        {
            ExportExcel(filePath, entries as IEnumerable<object>, null);
        }

        public void ExportExcel(string filePath, object? entries, object? languages)
        {
            ExportExcel(filePath, entries, languages, null);
        }

        public void ExportExcel(string filePath, object? entries, object? languages, object? comments)
        {
            ExportExcel(filePath, entries, languages, comments, ExcelExportMode.SingleSheet);
        }

        public void ExportExcel(string filePath, object? entries, object? languages, object? comments, ExcelExportMode exportMode)
        {
            var resourceScope = new ResourceScope(
                entries ?? ResourceManager.TableEntries,
                languages ?? ResourceManager.Cultures,
                comments ?? Array.Empty<object>());

            ResourceManager.ExportExcelFile(filePath, resourceScope, exportMode);
        }

        public void ImportExcel(string filePath)
        {
            var changes = ResourceManager.ImportExcelFile(filePath);

            changes.Apply();
        }

        public string CreateSnapshot()
        {
            return ResourceManager.CreateSnapshot();
        }

        public void LoadSnapshot(string? value)
        {
            ResourceManager.LoadSnapshot(value);
        }

        public void Dispose()
        {
            _kernel.Dispose();
        }

        private void ResourceManager_BeginEditing(object? sender, ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit(ResourceEntity entity, CultureKey? cultureKey)
        {
            if (cultureKey == null)
                return true;

            var rootFolder = _solutionFolder;
            if (rootFolder.IsNullOrEmpty())
                return false;

            var language = entity.Languages.FirstOrDefault(lang => cultureKey.Equals(lang.CultureKey));

            if (language != null)
                return true;

            var culture = cultureKey.Culture;

            if (culture == null)
                return false; // no neutral culture => this should never happen.

            var neutralLanguage = entity.Languages.FirstOrDefault();
            if (neutralLanguage == null)
                return false;

            var languageFileName = neutralLanguage.ProjectFile.GetLanguageFileName(culture);

            if (!File.Exists(languageFileName))
            {
                var directoryName = Path.GetDirectoryName(languageFileName);
                if (!directoryName.IsNullOrEmpty())
                    Directory.CreateDirectory(directoryName);

                File.WriteAllText(languageFileName, Model.Properties.Resources.EmptyResxTemplate);
            }

            entity.AddLanguage(new ProjectFile(languageFileName, rootFolder, entity.ProjectName, null));

            return true;
        }

        public Configuration Configuration { get; }
    }

    [Export]
    [Export(typeof(IConfiguration))]
    [Shared]
    public class Configuration : IConfiguration
    {
        public bool IsScopeSupported { get; set; }

        public ConfigurationScope Scope { get; set; }

        public CodeReferenceConfiguration CodeReferences { get; } = new();

        public bool AutoCreateNewLanguageFiles { get; set; }

        public string? FileExclusionFilter { get; set; }

        public bool SaveFilesImmediatelyUponChange => false;

        public CultureInfo NeutralResourcesLanguage { get; set; } = new("en-US");

        public StringComparison? EffectiveResXSortingComparison { get; set; }

        public DuplicateKeyHandling DuplicateKeyHandling { get; set; }

        public ResourceTableEntryRules Rules { get; } = new();

        public bool SortFileContentOnSave { get; set; }

        public bool ConfirmAddLanguageFile { get; set; }

        public StringComparison ResXSortingComparison { get; set; }

        public bool PrefixTranslations { get; set; }

        public string? TranslationPrefix { get; set; }

        public string? EffectiveTranslationPrefix { get; set; }

        public PrefixFieldType PrefixFieldType { get; set; }

        public ExcelExportMode ExcelExportMode { get; set; }

        public bool ShowPerformanceTraces { get; set; }

        public bool EnableXlifSync { get; set; }

        public bool RemoveEmptyEntries { get; set; }

        public string? TranslatorConfiguration { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
