namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using PropertyChanged;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;

    /// <summary>
    /// Represents all resources found in a folder and it's sub folders.
    /// </summary>
    [Export]
    [AddINotifyPropertyChangedInterface]
    public sealed class ResourceManager
    {
        [NotNull, ItemNotNull]
        private static readonly string[] _sortedCultureNames = GetSortedCultureNames();

        [NotNull]
        private readonly ISourceFilesProvider _sourceFilesProvider;

        private string? _snapshot;

        public event EventHandler<ResourceBeginEditingEventArgs>? BeginEditing;
        public event EventHandler<CancelEventArgs>? Reloading;
        public event EventHandler<EventArgs>? Loaded;
        public event EventHandler<LanguageEventArgs>? LanguageChanged;
        public event EventHandler<ProjectFileEventArgs>? ProjectFileSaved;

        [ImportingConstructor]
        public ResourceManager([NotNull] ISourceFilesProvider sourceFilesProvider, [NotNull] IConfiguration configuration)
        {
            Configuration = configuration;

            _sourceFilesProvider = sourceFilesProvider;
            TableEntries = ResourceEntities.ObservableSelectMany(entity => entity.Entries);
        }

        public IList<ProjectFile> AllSourceFiles { get; private set; } = Array.Empty<ProjectFile>();

        /// <summary>
        /// Loads all resources from the specified project files.
        /// </summary>
        /// <param name="allSourceFiles">All resource x files.</param>
        /// <param name="cancellationToken"></param>
        private async Task<bool> LoadAsync([NotNull] [ItemNotNull] IList<ProjectFile> allSourceFiles, CancellationToken? cancellationToken)
        {
            AllSourceFiles = allSourceFiles;

            var resourceFilesByDirectory = allSourceFiles
                .Where(file => file.IsResourceFile())
                .GroupBy(file => file.GetBaseDirectory())
                .ToList();

            return await InternalLoadAsync(resourceFilesByDirectory, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves all modified resource files.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save()
        {
            var changedResourceLanguages = ResourceEntities
                .SelectMany(entity => entity.Languages)
                .Where(lang => lang.HasChanges)
                .ToArray();

            changedResourceLanguages.ForEach(resourceLanguage => resourceLanguage.Save());
        }

        /// <summary>
        /// Gets the loaded resource entities.
        /// </summary>
        [NotNull, ItemNotNull]
        public ObservableCollection<ResourceEntity> ResourceEntities { get; } = new ObservableCollection<ResourceEntity>();

        /// <summary>
        /// Gets the table entries of all entities.
        /// </summary>
        [NotNull, ItemNotNull]
        public IObservableCollection<ResourceTableEntry> TableEntries { get; }

        /// <summary>
        /// Gets the cultures of all entities.
        /// </summary>
        [NotNull, ItemNotNull]
        public ObservableCollection<CultureKey> Cultures { get; } = new ObservableCollection<CultureKey>();

        /// <summary>
        /// Gets all system specific cultures.
        /// </summary>
        [NotNull, ItemNotNull]
        public static IEnumerable<CultureInfo> SpecificCultures { get; } = GetSpecificCultures();

        [NotNull]
        public IConfiguration Configuration { get; }

        public bool HasChanges => ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.HasChanges);

        public bool IsSaving => ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.IsSaving);

        public string? SolutionFolder => _sourceFilesProvider.SolutionFolder;

        public void ReloadSnapshot()
        {
            if (!string.IsNullOrEmpty(_snapshot))
                ResourceEntities.LoadSnapshot(_snapshot);
        }

        public async Task<bool> ReloadAsync([NotNull][ItemNotNull] IList<ProjectFile> sourceFiles, [CanBeNull] CancellationToken? cancellationToken)
        {
            var args = new CancelEventArgs();
            Reloading?.Invoke(this, args);
            if (args.Cancel)
                return false;

            return await LoadAsync(sourceFiles, cancellationToken).ConfigureAwait(false);
        }

        public bool CanEdit([NotNull] ResourceEntity resourceEntity, CultureKey? cultureKey)
        {
            var eventHandler = BeginEditing;

            if (eventHandler == null)
                return true;

            var args = new ResourceBeginEditingEventArgs(resourceEntity, cultureKey);

            eventHandler(this, args);

            return !args.Cancel;
        }

        private void OnLoaded()
        {
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        private async Task<bool> InternalLoadAsync([NotNull] [ItemNotNull] ICollection<IGrouping<string, ProjectFile>> resourceFilesByDirectory, CancellationToken? cancellationToken)
        {
            if (!await LoadEntitiesAsync(resourceFilesByDirectory, cancellationToken).ConfigureAwait(true))
                return false; // nothing has changed, no need to continue

            if (!string.IsNullOrEmpty(_snapshot))
                ResourceEntities.LoadSnapshot(_snapshot);

            var cultureKeys = ResourceEntities
                .SelectMany(entity => entity.Languages)
                .Select(lang => lang.CultureKey)
                .Distinct()
                .OrderBy(item => item.Culture?.DisplayName)
                .ToArray();

            Cultures.SynchronizeWith(cultureKeys);

            OnLoaded();

            return true;
        }

        private async Task<bool> LoadEntitiesAsync([NotNull] [ItemNotNull] ICollection<IGrouping<string, ProjectFile>> fileNamesByDirectory, CancellationToken? cancellationToken)
        {
            static string GenerateKey(string projectName, string baseName, string directoryName)
            {
                return string.Join("|", projectName, baseName, directoryName);
            }

            var unmatchedEntities = ResourceEntities.ToList();
            var existingEntities = ResourceEntities.ToDictionary(entity => GenerateKey(entity.ProjectName, entity.BaseName, entity.DirectoryName), StringComparer.OrdinalIgnoreCase);
            var newEntities = new List<ResourceEntity>();
            var entitiesToUpdate = new List<Tuple<ResourceEntity, ProjectFile[]>>();
            var duplicateKeyHandling = Configuration.DuplicateKeyHandling;

            void Load()
            {
                foreach (var directory in fileNamesByDirectory)
                {
                    var directoryName = directory.Key;
                    var filesByBaseName = directory.GroupBy(file => file.GetBaseName(), StringComparer.OrdinalIgnoreCase);

                    foreach (var files in filesByBaseName)
                    {
                        if (!files.Any())
                            continue;

                        var baseName = files.Key;
                        var filesByProject = files.GroupBy(file => file.ProjectName);

                        foreach (var item in filesByProject)
                        {
                            cancellationToken?.ThrowIfCancellationRequested();

                            var projectName = item.Key;
                            var projectFiles = item.ToArray();

                            if (projectName == null || string.IsNullOrEmpty(projectName) || !projectFiles.Any())
                                continue;

                            if (existingEntities.TryGetValue(GenerateKey(projectName, baseName, directoryName), out var existingEntity))
                            {
                                entitiesToUpdate.Add(new Tuple<ResourceEntity, ProjectFile[]>(existingEntity, projectFiles));
                                unmatchedEntities.Remove(existingEntity);
                            }
                            else
                            {
                                newEntities.Add(new ResourceEntity(this, projectName, baseName, directoryName, projectFiles, duplicateKeyHandling));
                            }
                        }
                    }
                }
            }

/*
            Load();
            await Task.Delay(1).ConfigureAwait(true); 
/*/
            await Task.Run(Load).ConfigureAwait(true);
//*/

            ResourceEntities.RemoveRange(unmatchedEntities);
            ResourceEntities.AddRange(newEntities);
            var hasChanged = entitiesToUpdate.Aggregate(false, (current, item) => current | item.Item1.Update(item.Item2, duplicateKeyHandling));

            hasChanged |= unmatchedEntities.Any() || newEntities.Any();

            return hasChanged;
        }

        internal void LanguageAdded([NotNull] CultureKey cultureKey)
        {
            if (!Cultures.Contains(cultureKey))
            {
                Cultures.Add(cultureKey);
            }
        }

        [SuppressPropertyChangedWarnings]
        internal void OnLanguageChanged([NotNull] ResourceLanguage language)
        {
            LanguageChanged?.Invoke(this, new LanguageEventArgs(language));
        }

        internal void OnProjectFileSaved([NotNull] ResourceLanguage language, [NotNull] ProjectFile projectFile)
        {
            ProjectFileSaved?.Invoke(this, new ProjectFileEventArgs(language, projectFile));
        }

        public static bool IsValidLanguageName(string? languageName)
        {
            if (string.IsNullOrEmpty(languageName))
                return false;

            return Array.BinarySearch(_sortedCultureNames, languageName, StringComparer.OrdinalIgnoreCase) >= 0;
        }

        [NotNull]
        [ItemNotNull]
        private static string[] GetSortedCultureNames()
        {
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            var pseudoLocales = new[] { "qps-ploc", "qps-ploca", "qps-plocm", "qps-Latn-x-sh" };

            var cultureNames = allCultures
                .SelectMany(culture => new[] { culture.IetfLanguageTag, culture.Name })
                .Concat(pseudoLocales)
                .Distinct()
                .ToArray();

            Array.Sort(cultureNames, StringComparer.OrdinalIgnoreCase);

            return cultureNames;
        }

        [NotNull]
        [ItemNotNull]
        private static CultureInfo[] GetSpecificCultures()
        {
            var specificCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(c => c.GetAncestors().Any())
                .OrderBy(c => c.DisplayName)
                .ToArray();

            return specificCultures;
        }

        public void LoadSnapshot(string? value)
        {
            ResourceEntities.LoadSnapshot(value);

            _snapshot = value;
        }

        [NotNull]
        public string CreateSnapshot()
        {
            return _snapshot = ResourceEntities.CreateSnapshot();
        }
    }
}
