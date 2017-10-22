namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
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
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly Configuration _configuration;

        public Host()
        {
            var assembly = GetType().Assembly;
            var folder = Path.GetDirectoryName(assembly.Location);
            Contract.Assume(!string.IsNullOrEmpty(folder));

            _compositionHost.AddCatalog(new DirectoryCatalog(folder, "*.dll"));

            _sourceFilesProvider = _compositionHost.GetExportedValue<SourceFilesProvider>();
            _resourceManager = _compositionHost.GetExportedValue<ResourceManager>();
            _resourceManager.BeginEditing += ResourceManager_BeginEditing;

            _configuration = _compositionHost.GetExportedValue<Configuration>();
        }

        [NotNull]
        public ResourceManager ResourceManager => _resourceManager;

        public void Load([CanBeNull] string folder, [CanBeNull] string exclusionFilter = @"Migrations\\\d{15}")
        {
            _sourceFilesProvider.Folder = folder;
            _sourceFilesProvider.ExclusionFilter = exclusionFilter;

            _resourceManager.Reload();
        }

        public void Save()
        {
            _resourceManager.Save();
        }

        public void ExportExcel([NotNull] string filePath)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, null);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries as IEnumerable<object>, null);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries, [CanBeNull] object languages)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries, languages, null);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries, [CanBeNull] object languages, [CanBeNull] object comments)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries, languages, comments, ExcelExportMode.SingleSheet);
        }

        public void ExportExcel([NotNull] string filePath, [CanBeNull] object entries, [CanBeNull] object languages, [CanBeNull] object comments, ExcelExportMode exportMode)
        {
            Contract.Requires(filePath != null);

            var resourceScope = new ResourceScope(
                entries ?? _resourceManager.TableEntries,
                languages ?? _resourceManager.Cultures,
                comments ?? new object[0]);

            _resourceManager.ExportExcelFile(filePath, resourceScope, exportMode);
        }

        public void ImportExcel([NotNull] string filePath)
        {
            Contract.Requires(filePath != null);

            var changes = _resourceManager.ImportExcelFile(filePath);

            changes.Apply();
        }

        [NotNull]
        public string CreateSnapshot()
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return _resourceManager.CreateSnapshot();
        }

        public void LoadSnapshot([CanBeNull] string value)
        {
            _resourceManager.LoadSnapshot(value);
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
            Contract.Requires(entity != null);

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
        public Configuration Configuration => _configuration;

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
            Contract.Invariant(_sourceFilesProvider != null);
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
        }
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
    }
}
