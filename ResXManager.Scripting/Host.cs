namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.Model.Properties;

    using TomsToolbox.Desktop.Composition;

    public sealed class Host : IDisposable
    {
        [NotNull]
        private readonly ICompositionHost _compositionHost = new CompositionHost();
        [NotNull]
        private readonly SourceFilesProvider _sourceFilesProvider;

        public Host()
        {
            var assembly = GetType().Assembly;
            var folder = Path.GetDirectoryName(assembly.Location);

            // ReSharper disable once AssignNullToNotNullAttribute
            _compositionHost.AddCatalog(new DirectoryCatalog(folder, "*.dll"));

            _sourceFilesProvider = _compositionHost.GetExportedValue<SourceFilesProvider>();
            ResourceManager = _compositionHost.GetExportedValue<ResourceManager>();
            ResourceManager.BeginEditing += ResourceManager_BeginEditing;

            Configuration = _compositionHost.GetExportedValue<Configuration>();
        }

        [NotNull]
        public ResourceManager ResourceManager { get; }

        public void Load([CanBeNull] string folder, [CanBeNull] string exclusionFilter = @"Migrations\\\d{15}")
        {
            _sourceFilesProvider.Folder = folder;
            _sourceFilesProvider.ExclusionFilter = exclusionFilter;

            ResourceManager.Reload();
        }

        public void Save()
        {
            ResourceManager.Save();
        }

        public void ExportExcel([NotNull] string filePath)
        {
            ExportExcel(filePath, null);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries)
        {
            ExportExcel(filePath, entries as IEnumerable<object>, null);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries, [CanBeNull] object languages)
        {
            ExportExcel(filePath, entries, languages, null);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries, [CanBeNull] object languages, [CanBeNull] object comments)
        {
            ExportExcel(filePath, entries, languages, comments, ExcelExportMode.SingleSheet);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries, [CanBeNull] object languages, [CanBeNull] object comments, ExcelExportMode exportMode)
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

        public void LoadSnapshot([CanBeNull] string value)
        {
            ResourceManager.LoadSnapshot(value);
        }

        public void Dispose()
        {
            _compositionHost.Dispose();
        }

        private void ResourceManager_BeginEditing([NotNull] object sender, [NotNull] ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit([NotNull] ResourceEntity entity, [CanBeNull] CultureKey cultureKey)
        {
            if (cultureKey == null)
                return true;

            var rootFolder = _sourceFilesProvider.Folder;
            if (string.IsNullOrEmpty(rootFolder))
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

                File.WriteAllText(languageFileName, Resources.EmptyResxTemplate);
            }

            entity.AddLanguage(new ProjectFile(languageFileName, rootFolder, entity.ProjectName, null));

            return true;
        }

        [NotNull]
        public Configuration Configuration { get; }
    }

    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    [Export(typeof(IConfiguration))]
    [Export(typeof(Configuration))]
    public class Configuration : IConfiguration
    {
        public bool SaveFilesImmediatelyUponChange => false;

        public CultureInfo NeutralResourcesLanguage { get; set; } = new CultureInfo("en-US");

        public StringComparison? EffectiveResXSortingComparison { get; set; }

        public DuplicateKeyHandling DuplicateKeyHandling { get; set; }

        public bool RemoveEmptyEntries { get; set; }
    }
}
