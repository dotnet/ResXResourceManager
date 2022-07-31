namespace ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ResXManager.Infrastructure;
    using ResXManager.Model;

    using Throttle;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export, Export(typeof(IService)), Shared]
    public sealed class XlfSynchronizer : FileWatcher, IService
    {
        private static readonly HashSet<string> _supportedExtension = new(new[] { ".xlf", ".xliff" }, StringComparer.OrdinalIgnoreCase);

        private readonly ResourceManager _resourceManager;
        private readonly ITracer _tracer;
        private readonly IConfiguration _configuration;

        private IDictionary<string, XlfDocument> _documentsByPath = new Dictionary<string, XlfDocument>();

        private int _isUpdateFromXlfRunning;
        private int _isUpdateFromResXRunning;

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

                await UpdateFromXlfAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _tracer.TraceError("Error reading XLIF files: {0}", ex);
            }
        }

        public async Task UpdateFromXlfAsync()
        {
            if (_isUpdateFromResXRunning > 0)
                return;

            Interlocked.Increment(ref _isUpdateFromXlfRunning);

            try
            {
                var solutionFolder = Folder;
                if (string.IsNullOrEmpty(solutionFolder))
                    return;

                _documentsByPath = await Task.Run(() =>
                {
                    var documents = new DirectoryInfo(solutionFolder).EnumerateSourceFiles()
                        .Where(file => _supportedExtension.Contains(file.Extension))
                        .Select(file => new XlfDocument(file.FullName))
                        .ToDictionary(doc => doc.FilePath, StringComparer.OrdinalIgnoreCase);

                    return documents;
                }).ConfigureAwait(true);

                var filesByOriginal = GetFilesByOriginal(_documentsByPath.Values);

                foreach (var entity in _resourceManager.ResourceEntities)
                {
                    UpdateEntityFromXlf(entity, filesByOriginal);
                }
            }
            finally
            {
                Interlocked.Increment(ref _isUpdateFromXlfRunning);
            }
        }

        private void UpdateEntityFromXlf(ResourceEntity entity, IDictionary<string, ICollection<XlfFile>> xlfFilesByOriginal)
        {
            var original = GetOriginal(entity);

            if (original.IsNullOrEmpty() || !xlfFilesByOriginal.TryGetValue(original, out var xlfFiles))
                return;

            UpdateEntityFromXlf(entity, xlfFiles);
        }

        private void UpdateEntityFromXlf(ResourceEntity entity, IEnumerable<XlfFile> xlfFiles)
        {
            if (_isUpdateFromResXRunning > 0)
                return;

            Interlocked.Increment(ref _isUpdateFromXlfRunning);

            try
            {
                var entriesByKey = entity.Entries.ToDictionary(entry => entry.Key);

                foreach (var xlfFile in xlfFiles)
                {
                    var targetLanguage = xlfFile.TargetLanguage;
                    var targetCultureKey = CultureKey.Parse(targetLanguage);

                    var xlfNodes = xlfFile.ResourceNodes;

                    foreach (var node in xlfNodes)
                    {
                        if (!entriesByKey.TryGetValue(node.Key, out var entry))
                            continue;

                        if (!entry.CanEdit(targetCultureKey))
                            continue;

                        entry.Values[targetLanguage] = node.Text;
                        entry.SetCommentText(targetCultureKey, node.Comment);

                        /* TODO: Enable configurable translation state tracking
                            var resxState = entry.TranslationState[targetLanguage];
                            var xlfState = node.TranslationState;
    
                            if (resxState == xlfState)
                                continue;
    
                            entry.TranslationState[targetLanguage] = xlfState;
                        */
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _isUpdateFromXlfRunning);
            }
        }

        private bool UpdateXlfFile(ResourceEntity entity, CultureKey language, IDictionary<string, ICollection<XlfFile>> xlfFilesByOriginal)
        {
            var neutralProjectFile = entity.NeutralProjectFile;
            if (neutralProjectFile == null)
                return false;

            var original = GetOriginal(neutralProjectFile);

            var targetCulture = language.Culture;
            if (targetCulture == null)
                return false;

            XlfFile? xlfFile;

            if (!xlfFilesByOriginal.TryGetValue(original, out var xlfFiles) || (xlfFile = xlfFiles?.FirstOrDefault(file => file.TargetLanguage == targetCulture.Name)) == null)
            {
                var documentsByPath = _documentsByPath;
                var uniqueProjectName = neutralProjectFile.UniqueProjectName;
                var directoryName = Path.GetDirectoryName(uniqueProjectName);
                var solutionFolder = entity.Container.SolutionFolder;

                if (uniqueProjectName.IsNullOrEmpty() || directoryName.IsNullOrEmpty() || solutionFolder.IsNullOrEmpty())
                    return false;

                var fileName = Path.ChangeExtension(Path.GetFileName(uniqueProjectName), targetCulture.Name + ".xlf");

                var directory = xlfFiles?.FirstOrDefault()?.Document.Directory;
                directory ??= Path.Combine(solutionFolder, directoryName, "MultilingualResources");

                var filePath = Path.Combine(directory, fileName);

                if (!documentsByPath.TryGetValue(filePath, out var document))
                {
                    Directory.CreateDirectory(directory);
                    document = new XlfDocument(filePath);
                    documentsByPath.Add(filePath, document);
                }

                var neutralResourcesLanguage = entity.NeutralResourcesLanguage;

                xlfFiles ??= Array.Empty<XlfFile>();
                xlfFile = document.Files.FirstOrDefault(file => file.TargetLanguage == targetCulture.Name);

                if (xlfFile == null)
                {
                    xlfFile = document.AddFile(original, neutralResourcesLanguage.Name, targetCulture.Name);
                    xlfFilesByOriginal[original] = xlfFiles.Append(xlfFile).ToArray();
                }

                document.Save();
            }

            return xlfFile.Update(entity, language);
        }

        private static string? GetOriginal(ResourceEntity entity)
        {
            return GetOriginal(entity.NeutralProjectFile);
        }

        [return: NotNullIfNotNull("projectFile")]
        private static string? GetOriginal(ProjectFile? projectFile)
        {
            return projectFile?.RelativeFilePath
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

            if (_isUpdateFromXlfRunning > 0)
                return;

            Interlocked.Increment(ref _isUpdateFromResXRunning);

            try
            {
                var documentsByPath = _documentsByPath;
                var filesByOriginal = GetFilesByOriginal(documentsByPath.Values);

                var language = e.Language;
                var entity = language.Container;

                var neutralLanguage = entity.Languages.FirstOrDefault();
                if (neutralLanguage == null)
                    return;

                if (language.IsNeutralLanguage)
                {
                    foreach (var specificLanguage in entity.Languages.Where(l => !l.IsNeutralLanguage))
                    {
                        UpdateXlfFile(entity, specificLanguage.CultureKey, filesByOriginal);
                    }
                }
                else
                {
                    UpdateXlfFile(entity, language.CultureKey, filesByOriginal);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _isUpdateFromResXRunning);
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
