namespace ResXManager.Scripting
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Windows.Forms;

    using tomenglertde.ResXManager.Model;

    using TomsToolbox.Desktop;
    using TomsToolbox.Desktop.Composition;

    public class Host
    {
        private readonly ICompositionHost _compositionHost = new CompositionHost();
        private readonly SourceFilesProvider _sourceFilesProvider;
        private readonly ResourceManager _resourceManager;

        public Host()
        {
            var assembly = GetType().Assembly;
            var folder = Path.GetDirectoryName(assembly.Location);
            Contract.Assume(!string.IsNullOrEmpty(folder));

            _compositionHost.AddCatalog(new DirectoryCatalog(folder, "*.dll"));

            _sourceFilesProvider = _compositionHost.GetExportedValue<SourceFilesProvider>();
            _resourceManager = _compositionHost.GetExportedValue<ResourceManager>();
        }

        public ResourceManager ResourceManager => _resourceManager;

        public void Load(string folder)
        {
            _sourceFilesProvider.Folder = folder;
            
            _resourceManager.Reload(DuplicateKeyHandling.Fail);
        }

        public void Save(StringComparison? keySortingComparison = null)
        {
            _resourceManager.Save(keySortingComparison);
        }

        public void ExportExcel(string filePath)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, null);
        }

        public void ExportExcel(string filePath, object entries)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries as IEnumerable<object>, null);
        }

        public void ExportExcel(string filePath, object entries, object languages)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries, languages, null);
        }

        public void ExportExcel(string filePath, object entries, object languages, object comments)
        {
            Contract.Requires(filePath != null);

            ExportExcel(filePath, entries, languages, comments, ExcelExportMode.SingleSheet);
        }

        public void ExportExcel(string filePath, object entries, object languages, object comments, ExcelExportMode exportMode)
        {
            Contract.Requires(filePath != null);

            var resourceScope = new ResourceScope(
                entries ?? _resourceManager.TableEntries, 
                languages ?? _resourceManager.Cultures, 
                comments ?? new object [0]);

            _resourceManager.ExportExcelFile(filePath, resourceScope, exportMode);
        }

        public void ImportExcel(string filePath)
        {
            Contract.Requires(filePath != null);

            _resourceManager.ImportExcelFile(filePath);
        }

        public string CreateSnapshot()
        {
            return _resourceManager.CreateSnapshot();
        }

        public void LoadSnapshot(string value)
        {
            _resourceManager.LoadSnapshot(value);
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
