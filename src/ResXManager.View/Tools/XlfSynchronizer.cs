namespace ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using Throttle;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export(typeof(IService)), Shared]
    internal sealed class XlfSynchronizer : FileWatcher, IService
    {
        private readonly ResourceManager _resourceManager;
        private readonly ITracer _tracer;
        private readonly IConfiguration _configuration;

        private IDictionary<string, XlfDocument>? _documentsByPath;

        public XlfSynchronizer(ResourceManager resourceManager, ITracer tracer, IConfiguration configuration)
        {
            _resourceManager = resourceManager;
            _tracer = tracer;
            _configuration = configuration;

            resourceManager.SolutionFolderChanged += ResourceManager_SolutionFolderChanged;
            resourceManager.Loaded += ResourceManager_Loaded;
            resourceManager.ProjectFileSaved += ResourceManager_ProjectFileSaved;

            configuration.PropertyChanged += Configuration_PropertyChanged;
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IConfiguration.EnableXlifSync))
                return;

            Watch(_configuration.EnableXlifSync ? _resourceManager.SolutionFolder : null);
            ResourceManager_Loaded(_resourceManager, EventArgs.Empty);
        }

        public void Start()
        {
            OnFilesChanged();
            UpdateFromXlf();
        }

        protected override bool IncludeFile(string fileName)
        {
            return string.Equals(Path.GetExtension(fileName), ".xlf", StringComparison.OrdinalIgnoreCase);
        }

        [Throttled(typeof(Throttle), 500)]
        protected override void OnFilesChanged()
        {
            try
            {
                if (_resourceManager.IsLoading)
                {
                    OnFilesChanged();
                    return;
                }

                var changedFilePaths = GetChangedFiles();

                var documentsByPath = _documentsByPath;
                if (documentsByPath == null)
                    return;

                var changedFilesByOriginal = changedFilePaths
                    .Select(filePath => documentsByPath.TryGetValue(filePath, out var document) && document.IsBufferOutdated ? document.Reload() : null)
                    .ExceptNullItems()
                    .SelectMany(document => document.Files)
                    .GroupBy(file => file.Original);

                foreach (var xlfFiles in changedFilesByOriginal)
                {
                    var original = xlfFiles.Key;

                    var entity = _resourceManager.ResourceEntities.FirstOrDefault(entity => string.Equals(GetOriginal(entity), original, StringComparison.OrdinalIgnoreCase));
                    if (entity == null)
                        continue;

                    UpdateEntityFromXlf(entity, xlfFiles);
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Error reading XLIF files: {0}", ex);
            }
        }

        [Throttled(typeof(Throttle), 100)]
        private async void UpdateFromXlf()
        {
            try
            {
                if (_resourceManager.IsLoading)
                {
                    UpdateFromXlf();
                    return;
                }

                var solutionFolder = Folder;
                if (string.IsNullOrEmpty(solutionFolder))
                    return;

                _documentsByPath = await Task.Run(() =>
                {
                    var documents = Directory.EnumerateFiles(solutionFolder, "*.xlf", SearchOption.AllDirectories)
                        .Select(file => new XlfDocument(file))
                        .ToDictionary(doc => doc.FilePath, StringComparer.OrdinalIgnoreCase);

                    return documents;

                }).ConfigureAwait(true);

                var filesByOriginal = GetFilesByOriginal(_documentsByPath.Values);

                foreach (var entity in _resourceManager.ResourceEntities)
                {
                    UpdateEntityFromXlf(entity, filesByOriginal);
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Error reading XLIF files: {0}", ex);
            }
        }

        private static void UpdateEntityFromXlf(ResourceEntity entity, IDictionary<string, ICollection<XlfFile>> xlfFilesByOriginal)
        {
            var original = GetOriginal(entity);

            if (original.IsNullOrEmpty() || !xlfFilesByOriginal.TryGetValue(original, out var xlfFiles))
                return;

            UpdateEntityFromXlf(entity, xlfFiles);
        }

        private static void UpdateEntityFromXlf(ResourceEntity entity, IEnumerable<XlfFile> xlfFiles)
        {
            var entriesByKey = entity.Entries.ToDictionary(entry => entry.Key);

            foreach (var xlfFile in xlfFiles)
            {
                var targetLanguage = xlfFile.TargetLanguage;

                var xlfNodes = xlfFile.ResourceNodes;

                foreach (var node in xlfNodes)
                {
                    if (entriesByKey.TryGetValue(node.Key, out var entry))
                    {
                        // TODO: Maybe better clear the value if translation state is "new" and text equals the neutral value?
                        entry.Values[targetLanguage] = node.Text;

                        // TODO: Import comments always? Which comments to import - neutral or specific or both?
                        if (entry.Comment.IsNullOrEmpty())
                        {
                            entry.Comment = node.Comment;
                        }
                    }
                }
            }
        }

        private static void UpdateXlfFile(ResourceEntity entity, ResourceLanguage language, ResourceLanguage neutralLanguage, IDictionary<string, ICollection<XlfFile>> xlfFilesByOriginal)
        {
            var original = GetOriginal(entity);
            if (original.IsNullOrEmpty())
                return;

            if (!xlfFilesByOriginal.TryGetValue(original, out var xlfFiles))
            {
                // TODO: create new
                return;
            }

            var xlfFile = xlfFiles.FirstOrDefault(file => file.TargetLanguage == language.Culture?.Name);

            if (xlfFile == null)
            {
                // TODO: create new
                return;
            }

            xlfFile.Update(neutralLanguage, language);
        }

        private static string? GetOriginal(ResourceEntity entity)
        {
            return entity.NeutralProjectFile?.RelativeFilePath
                .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }

        private static IDictionary<string, ICollection<XlfFile>> GetFilesByOriginal(IEnumerable<XlfDocument> documents)
        {
            // ! group.Key is checked in Where clause.
            return documents
                .SelectMany(doc => doc.Files)
                .GroupBy(file => file.Original, StringComparer.OrdinalIgnoreCase)
                .Where(group => !group.Key.IsNullOrEmpty())
                .ToDictionary(group => group.Key!, group => (ICollection<XlfFile>)group.ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        private void ResourceManager_ProjectFileSaved(object? sender, ProjectFileEventArgs e)
        {
            if (!_configuration.EnableXlifSync)
                return;

            var documentsByPath = _documentsByPath;
            if (documentsByPath == null)
                return;

            var language = e.Language;
            var entity = language.Container;

            var neutralLanguage = entity.Languages.FirstOrDefault();
            if (neutralLanguage == null)
                return;

            var filesByOriginal = GetFilesByOriginal(documentsByPath.Values);

            if (language.IsNeutralLanguage)
            {
                foreach (var specificLanguage in entity.Languages.Where(l => !l.IsNeutralLanguage))
                {
                    UpdateXlfFile(entity, specificLanguage, language, filesByOriginal);
                }
            }
            else
            {
                UpdateXlfFile(entity, language, neutralLanguage, filesByOriginal);
            }
        }

        private void ResourceManager_Loaded(object? sender, EventArgs e)
        {
            if (!_configuration.EnableXlifSync)
                return;

            UpdateFromXlf();
        }

        private void ResourceManager_SolutionFolderChanged(object? sender, TextEventArgs e)
        {
            if (!_configuration.EnableXlifSync)
                return;

            Watch(e.Text);
        }
    }
}
