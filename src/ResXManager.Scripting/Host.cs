namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
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
        private readonly SourceFilesProvider _sourceFilesProvider;

        public Host()
        {
            var assembly = GetType().Assembly;

            _kernel.BindExports(assembly,
                typeof(Infrastructure.Properties.AssemblyKey).Assembly,
                typeof(Model.Properties.AssemblyKey).Assembly);

            IExportProvider exportProvider = new ExportProvider(_kernel);

            _sourceFilesProvider = exportProvider.GetExportedValue<SourceFilesProvider>();

            ResourceManager = exportProvider.GetExportedValue<ResourceManager>();
            ResourceManager.BeginEditing += ResourceManager_BeginEditing;

            Configuration = exportProvider.GetExportedValue<Configuration>();
        }

        public ResourceManager ResourceManager { get; }

        public void Load(string? folder, string? exclusionFilter = @"Migrations\\\d{15}")
        {
            _sourceFilesProvider.SolutionFolder = folder;
            _sourceFilesProvider.ExclusionFilter = exclusionFilter;

            var _ = ResourceManager.ReloadAsync(_sourceFilesProvider.EnumerateSourceFiles(), null).Result;
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
            _kernel?.Dispose();
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

            var rootFolder = _sourceFilesProvider.SolutionFolder;
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
        public bool SaveFilesImmediatelyUponChange => false;

        public CultureInfo NeutralResourcesLanguage { get; set; } = new CultureInfo("en-US");

        public StringComparison? EffectiveResXSortingComparison { get; set; }

        public DuplicateKeyHandling DuplicateKeyHandling { get; set; }

        public ResourceTableEntryRules Rules { get; } = new ResourceTableEntryRules();

        public bool RemoveEmptyEntries { get; set; }
    }
}
