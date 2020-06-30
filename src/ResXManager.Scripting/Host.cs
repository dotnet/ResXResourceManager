namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Composition.Hosting;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using TomsToolbox.Composition;
    using TomsToolbox.Composition.Mef2;

    public sealed class Host : IDisposable
    {
        private CompositionHost? _container;
        private readonly SourceFilesProvider _sourceFilesProvider;

        public Host()
        {
            var assembly = GetType().Assembly;

            var configuration = new ContainerConfiguration()
                .WithAssembly(assembly)
                .WithAssembly(typeof(Infrastructure.Properties.AssemblyKey).Assembly)
                .WithAssembly(typeof(Model.Properties.AssemblyKey).Assembly);

            _container = configuration.CreateContainer();

            ExportProvider = new ExportProviderAdapter(_container);

            _sourceFilesProvider = ExportProvider.GetExportedValue<SourceFilesProvider>();

            ResourceManager = ExportProvider.GetExportedValue<ResourceManager>();
            ResourceManager.BeginEditing += ResourceManager_BeginEditing;

            Configuration = ExportProvider.GetExportedValue<Configuration>();
        }

        [Export(typeof(IExportProvider))]
        public IExportProvider? ExportProvider { get; }


        [NotNull]
        public ResourceManager ResourceManager { get; }

        public void Load(string? folder, string? exclusionFilter = @"Migrations\\\d{15}")
        {
            _sourceFilesProvider.SolutionFolder = folder;
            _sourceFilesProvider.ExclusionFilter = exclusionFilter;

            var loaded = ResourceManager.ReloadAsync(_sourceFilesProvider.EnumerateSourceFiles(), null).Result;
        }

        public void Save()
        {
            ResourceManager.Save();
        }

        public void ExportExcel([NotNull] string filePath)
        {
            ExportExcel(filePath, null);
        }

        public void ExportExcel([NotNull] string filePath, object? entries)
        {
            ExportExcel(filePath, entries as IEnumerable<object>, null);
        }

        public void ExportExcel([NotNull] string filePath, object? entries, object? languages)
        {
            ExportExcel(filePath, entries, languages, null);
        }

        public void ExportExcel([NotNull] string filePath, object? entries, object? languages, object? comments)
        {
            ExportExcel(filePath, entries, languages, comments, ExcelExportMode.SingleSheet);
        }

        public void ExportExcel([NotNull] string filePath, object? entries, object? languages, object? comments, ExcelExportMode exportMode)
        {
            var resourceScope = new ResourceScope(
                entries ?? ResourceManager.TableEntries,
                languages ?? ResourceManager.Cultures,
                comments ?? Array.Empty<object>());

            ResourceManager.ExportExcelFile(filePath, resourceScope, exportMode);
        }

        public void ImportExcel([NotNull] string filePath)
        {
            var changes = ResourceManager.ImportExcelFile(filePath);

            changes.Apply();
        }

        [NotNull]
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
            _container?.Dispose();
        }

        private void ResourceManager_BeginEditing([NotNull] object sender, [NotNull] ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit([NotNull] ResourceEntity entity, CultureKey? cultureKey)
        {
            if (cultureKey == null)
                return true;

            var rootFolder = _sourceFilesProvider.SolutionFolder;
            if (rootFolder == null || string.IsNullOrEmpty(rootFolder))
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
                if (!string.IsNullOrEmpty(directoryName))
                    Directory.CreateDirectory(directoryName);

                File.WriteAllText(languageFileName, Model.Properties.Resources.EmptyResxTemplate);
            }

            entity.AddLanguage(new ProjectFile(languageFileName, rootFolder, entity.ProjectName, null));

            return true;
        }

        [NotNull]
        public Configuration Configuration { get; }
    }

    [Export(typeof(IConfiguration))]
    [Export(typeof(Configuration))]
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
