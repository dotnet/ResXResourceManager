namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using PropertyChanged;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
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

        [CanBeNull]
        private string _snapshot;

        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing;
        public event EventHandler<CancelEventArgs> Reloading;
        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<LanguageEventArgs> LanguageChanged;
        public event EventHandler<ProjectFileEventArgs> ProjectFileSaved;

        [ImportingConstructor]
        private ResourceManager([NotNull] ISourceFilesProvider sourceFilesProvider, [NotNull] IConfiguration configuration)
        {
            Configuration = configuration;

            _sourceFilesProvider = sourceFilesProvider;
            TableEntries = ResourceEntities.ObservableSelectMany(entity => entity.Entries);
        }

        /// <summary>
        /// Loads all resources from the specified project files.
        /// </summary>
        /// <param name="allSourceFiles">All resource x files.</param>
        private bool Load([NotNull, ItemNotNull] IList<ProjectFile> allSourceFiles)
        {
            var resourceFilesByDirectory = allSourceFiles
                .Where(file => file.IsResourceFile())
                .GroupBy(file => file.GetBaseDirectory());

            return InternalLoad(resourceFilesByDirectory);
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

        public void ReloadSnapshot()
        {
            if (!string.IsNullOrEmpty(_snapshot))
                ResourceEntities.LoadSnapshot(_snapshot);
        }

        public bool Reload()
        {
            return Reload(_sourceFilesProvider.SourceFiles);
        }

        public bool Reload([NotNull][ItemNotNull] IList<ProjectFile> sourceFiles)
        {
            var args = new CancelEventArgs();
            Reloading?.Invoke(this, args);
            if (args.Cancel)
                return false;

            return Load(sourceFiles);
        }

        public bool CanEdit([NotNull] ResourceEntity resourceEntity, [CanBeNull] CultureKey cultureKey)
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

        private bool InternalLoad([NotNull][ItemNotNull] IEnumerable<IGrouping<string, ProjectFile>> resourceFilesByDirectory)
        {
            if (!LoadEntities(resourceFilesByDirectory))
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

        private bool LoadEntities([NotNull][ItemNotNull] IEnumerable<IGrouping<string, ProjectFile>> fileNamesByDirectory)
        {
            var hasChanged = false;

            var unmatchedEntities = ResourceEntities.ToList();

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
                        var projectName = item.Key;
                        var projectFiles = item.ToArray();

                        if (string.IsNullOrEmpty(projectName) || !projectFiles.Any())
                            continue;

                        var existingEntity = ResourceEntities.FirstOrDefault(entity => entity.EqualsAll(projectName, baseName, directoryName));

                        if (existingEntity != null)
                        {
                            if (existingEntity.Update(projectFiles))
                                hasChanged = true;

                            unmatchedEntities.Remove(existingEntity);
                        }
                        else
                        {
                            ResourceEntities.Add(new ResourceEntity(this, projectName, baseName, directoryName, projectFiles));
                            hasChanged = true;
                        }
                    }
                }
            }

            ResourceEntities.RemoveRange(unmatchedEntities);

            hasChanged |= unmatchedEntities.Any();

            return hasChanged;
        }

        internal void LanguageAdded([NotNull] CultureKey cultureKey)
        {
            if (!Cultures.Contains(cultureKey))
            {
                Cultures.Add(cultureKey);
            }
        }

        internal void OnLanguageChanged([NotNull] ResourceLanguage language)
        {
            LanguageChanged?.Invoke(this, new LanguageEventArgs(language));
        }

        internal void OnProjectFileSaved([NotNull] ResourceLanguage language, [NotNull] ProjectFile projectFile)
        {
            ProjectFileSaved?.Invoke(this, new ProjectFileEventArgs(language, projectFile));
        }

        public static bool IsValidLanguageName([CanBeNull] string languageName)
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

        public void LoadSnapshot([CanBeNull] string value)
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
