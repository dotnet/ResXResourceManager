namespace ResXManager.Scripting
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
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

        public Host()
        {
            var assembly = GetType().Assembly;
            var folder = Path.GetDirectoryName(assembly.Location);
            Contract.Assume(!string.IsNullOrEmpty(folder));

            _compositionHost.AddCatalog(new DirectoryCatalog(folder, "*.dll"));

            _sourceFilesProvider = _compositionHost.GetExportedValue<SourceFilesProvider>();
            _resourceManager = _compositionHost.GetExportedValue<ResourceManager>();
            _resourceManager.BeginEditing += ResourceManager_BeginEditing;
        }

        [NotNull]
        public ResourceManager ResourceManager => _resourceManager;

        public void Load(string folder)
        {
            _sourceFilesProvider.Folder = folder;

            _resourceManager.Reload(DuplicateKeyHandling.Fail);
        }

        public void Save()
        {
            Save(null);
        }

        public void Save(StringComparison? keySortingComparison)
        {
            _resourceManager.Save(keySortingComparison);
        }

        public void ExportExcel([NotNull] string filePath)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, null);
        }

        public void ExportExcel([NotNull] string filePath, object entries)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries as IEnumerable<object>, null);
        }

        public void ExportExcel([NotNull] string filePath, object entries, object languages)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries, languages, null);
        }

        public void ExportExcel([NotNull] string filePath, object entries, object languages, object comments)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries, languages, comments, ExcelExportMode.SingleSheet);
        }

        public void ExportExcel([NotNull] string filePath, object entries, object languages, object comments, ExcelExportMode exportMode)
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
            return _resourceManager.CreateSnapshot();
        }

        public void LoadSnapshot(string value)
        {
            _resourceManager.LoadSnapshot(value);
        }

        public void Dispose()
        {
            _compositionHost.Dispose();
        }

        private void ResourceManager_BeginEditing(object sender, [NotNull] ResourceBeginEditingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.CultureKey))
            {
                e.Cancel = true;
            }
        }

        private bool CanEdit([NotNull] ResourceEntity entity, CultureKey cultureKey)
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

            entity.AddLanguage(new ProjectFile(languageFileName, rootFolder, entity.ProjectName, null), DuplicateKeyHandling.Fail);

            return true;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_compositionHost != null);
            Contract.Invariant(_sourceFilesProvider != null);
            Contract.Invariant(_resourceManager != null);
        }
    }
}
