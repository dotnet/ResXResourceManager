namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;

    /// <summary>
    /// Represents all resources found in a folder and its's sub folders.
    /// </summary>
    [Export]
    public class ResourceManager : ObservableObject
    {
        private static readonly string[] _sortedCultureNames = GetSortedCultureNames();
        private static readonly CultureInfo[] _specificCultures = GetSpecificCultures();

        [NotNull]
        private readonly ISourceFilesProvider _sourceFilesProvider;

        [NotNull]
        private readonly ObservableCollection<ResourceEntity> _resourceEntities = new ObservableCollection<ResourceEntity>();
        [NotNull]
        private readonly IObservableCollection<ResourceTableEntry> _tableEntries;

        [NotNull]
        private readonly ObservableCollection<CultureKey> _cultureKeys = new ObservableCollection<CultureKey>();

        private string _snapshot;

        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing;
        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<LanguageEventArgs> LanguageChanged;

        [ImportingConstructor]
        private ResourceManager([NotNull] ISourceFilesProvider sourceFilesProvider)
        {
            Contract.Requires(sourceFilesProvider != null);

            _sourceFilesProvider = sourceFilesProvider;
            _tableEntries = _resourceEntities.ObservableSelectMany(entity => entity.Entries);
        }

        /// <summary>
        /// Loads all resources from the specified project files.
        /// </summary>
        /// <param name="allSourceFiles">All resource x files.</param>
        /// <param name="duplicateKeyHandling">The duplicate key handling mode.</param>
        private bool Load([NotNull] IList<ProjectFile> allSourceFiles, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(allSourceFiles != null);

            var resourceFilesByDirectory = allSourceFiles
                .Where(file => file.IsResourceFile())
                .GroupBy(file => file.GetBaseDirectory());

            return InternalLoad(resourceFilesByDirectory, duplicateKeyHandling);
        }

        /// <summary>
        /// Saves all modified resource files.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save(StringComparison? fileContentSorting)
        {
            var changedResourceLanguages = _resourceEntities
                .SelectMany(entity => entity.Languages)
                .Where(lang => lang.HasChanges)
                .ToArray();

            changedResourceLanguages.ForEach(resourceLanguage => resourceLanguage.Save(fileContentSorting));
        }

        /// <summary>
        /// Gets the loaded resource entities.
        /// </summary>
        [ItemNotNull]
        [NotNull]
        public ICollection<ResourceEntity> ResourceEntities
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<ResourceEntity>>() != null);

                return _resourceEntities;
            }
        }

        /// <summary>
        /// Gets the table entries of all entities.
        /// </summary>
        [NotNull]
        public IObservableCollection<ResourceTableEntry> TableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<ResourceTableEntry>>() != null);

                return _tableEntries;
            }
        }

        /// <summary>
        /// Gets the cultures of all entities.
        /// </summary>
        [NotNull]
        public ObservableCollection<CultureKey> Cultures
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<CultureKey>>() != null);

                return _cultureKeys;
            }
        }

        /// <summary>
        /// Gets all system specific cultures.
        /// </summary>
        [NotNull]
        public static IEnumerable<CultureInfo> SpecificCultures
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureInfo>>() != null);

                return _specificCultures;
            }
        }

        public bool HasChanges
        {
            get
            {
                return _resourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.HasChanges);
            }
        }

        public void ReloadSnapshot()
        {
            if (!string.IsNullOrEmpty(_snapshot))
                _resourceEntities.LoadSnapshot(_snapshot);
        }

        public bool Reload(DuplicateKeyHandling duplicateKeyHandling)
        {
            return Reload(_sourceFilesProvider.SourceFiles, duplicateKeyHandling);
        }

        public bool Reload([NotNull] IList<ProjectFile> sourceFiles, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(sourceFiles != null);

            return Load(sourceFiles, duplicateKeyHandling);
        }

        public bool CanEdit([NotNull] ResourceEntity resourceEntity, CultureKey cultureKey)
        {
            Contract.Requires(resourceEntity != null);

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

        private bool InternalLoad([NotNull] IEnumerable<IGrouping<string, ProjectFile>> resourceFilesByDirectory, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(resourceFilesByDirectory != null);

            if (!LoadEntities(resourceFilesByDirectory, duplicateKeyHandling))
                return false; // nothing has changed, no need to continue

            if (!string.IsNullOrEmpty(_snapshot))
                _resourceEntities.LoadSnapshot(_snapshot);

            var cultureKeys = _resourceEntities
                .SelectMany(entity => entity.Languages)
                .Select(lang => lang.CultureKey)
                .Distinct()
                .OrderBy(item => item.Culture?.DisplayName)
                .ToArray();

            _cultureKeys.SynchronizeWith(cultureKeys);

            OnLoaded();

            return true;
        }

        private bool LoadEntities([NotNull] IEnumerable<IGrouping<string, ProjectFile>> fileNamesByDirectory, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(fileNamesByDirectory != null);

            var hasChanged = false;

            var unmatchedEntities = _resourceEntities.ToList();

            foreach (var directory in fileNamesByDirectory)
            {
                Contract.Assume(directory != null);

                var directoryName = directory.Key;
                Contract.Assume(!string.IsNullOrEmpty(directoryName));

                var filesByBaseName = directory.GroupBy(file => file.GetBaseName());

                foreach (var files in filesByBaseName)
                {
                    if ((files == null) || !files.Any())
                        continue;

                    var baseName = files.Key;
                    Contract.Assume(!string.IsNullOrEmpty(baseName));

                    var filesByProject = files.GroupBy(file => file.ProjectName);

                    foreach (var item in filesByProject)
                    {
                        Contract.Assume(item != null);

                        var projectName = item.Key;
                        var projectFiles = item.ToArray();

                        if (string.IsNullOrEmpty(projectName) || !projectFiles.Any())
                            continue;

                        var existingEntity = _resourceEntities.FirstOrDefault(entity => entity.EqualsAll(projectName, baseName, directoryName));

                        if (existingEntity != null)
                        {
                            if (existingEntity.Update(projectFiles, duplicateKeyHandling))
                                hasChanged = true;

                            unmatchedEntities.Remove(existingEntity);
                        }
                        else
                        {
                            _resourceEntities.Add(new ResourceEntity(this, projectName, baseName, directoryName, projectFiles, duplicateKeyHandling));
                            hasChanged = true;
                        }
                    }
                }
            }

            _resourceEntities.RemoveRange(unmatchedEntities);

            hasChanged |= unmatchedEntities.Any();

            return hasChanged;
        }

        internal void LanguageAdded([NotNull] CultureKey cultureKey)
        {
            if (!_cultureKeys.Contains(cultureKey))
            {
                _cultureKeys.Add(cultureKey);
            }
        }

        internal void OnLanguageChanged([NotNull] ResourceLanguage language)
        {
            Contract.Requires(language != null);

            LanguageChanged?.Invoke(this, new LanguageEventArgs(language));
        }

        public static bool IsValidLanguageName([NotNull] string languageName)
        {
            return Array.BinarySearch(_sortedCultureNames, languageName, StringComparer.OrdinalIgnoreCase) >= 0;
        }

        [NotNull]
        private static string[] GetSortedCultureNames()
        {
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            var cultureNames = allCultures
                .SelectMany(culture => new[] { culture.IetfLanguageTag, culture.Name })
                .Distinct()
                .ToArray();

            Array.Sort(cultureNames, StringComparer.OrdinalIgnoreCase);

            return cultureNames;
        }

        [NotNull]
        private static CultureInfo[] GetSpecificCultures()
        {
            var specificCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(c => c.GetAncestors().Any())
                .OrderBy(c => c.DisplayName)
                .ToArray();

            return specificCultures;
        }

        public void LoadSnapshot(string value)
        {
            ResourceEntities.LoadSnapshot(value);

            _snapshot = value;
        }

        [NotNull]
        public string CreateSnapshot()
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return _snapshot = ResourceEntities.CreateSnapshot();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceEntities != null);
            Contract.Invariant(_cultureKeys != null);
            Contract.Invariant(_sourceFilesProvider != null);
            Contract.Invariant(_tableEntries != null);
        }
    }
}
