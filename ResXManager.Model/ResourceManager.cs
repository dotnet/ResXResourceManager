namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
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
    /// Represents all resources found in a folder and its's sub folders.
    /// </summary>
    [Export]
    [AddINotifyPropertyChangedInterface]
    public sealed class ResourceManager
    {
        private static readonly string[] _sortedCultureNames = GetSortedCultureNames();

        [NotNull]
        private readonly ISourceFilesProvider _sourceFilesProvider;

        private string _snapshot;

        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing;
        public event EventHandler<CancelEventArgs> Reloading;
        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<LanguageEventArgs> LanguageChanged;
        public event EventHandler<ProjectFileEventArgs> ProjectFileSaved;

        [ImportingConstructor]
        private ResourceManager([NotNull] ISourceFilesProvider sourceFilesProvider)
        {
            Contract.Requires(sourceFilesProvider != null);

            _sourceFilesProvider = sourceFilesProvider;
            TableEntries = ResourceEntities.ObservableSelectMany(entity => entity.Entries);
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
            var changedResourceLanguages = ResourceEntities
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
        public ObservableCollection<ResourceEntity> ResourceEntities { get; } = new ObservableCollection<ResourceEntity>();

        /// <summary>
        /// Gets the table entries of all entities.
        /// </summary>
        [NotNull]
        public IObservableCollection<ResourceTableEntry> TableEntries { get; }

        /// <summary>
        /// Gets the cultures of all entities.
        /// </summary>
        [NotNull]
        public ObservableCollection<CultureKey> Cultures { get; } = new ObservableCollection<CultureKey>();

        /// <summary>
        /// Gets all system specific cultures.
        /// </summary>
        [NotNull, ItemNotNull]
        public static IEnumerable<CultureInfo> SpecificCultures { get; } = GetSpecificCultures();

        public bool HasChanges => ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.HasChanges);

        public bool IsSaving => ResourceEntities.SelectMany(entity => entity.Languages).Any(lang => lang.IsSaving);

        public void ReloadSnapshot()
        {
            if (!string.IsNullOrEmpty(_snapshot))
                ResourceEntities.LoadSnapshot(_snapshot);
        }

        public bool Reload(DuplicateKeyHandling duplicateKeyHandling)
        {
            return Reload(_sourceFilesProvider.SourceFiles, duplicateKeyHandling);
        }

        public bool Reload([NotNull] IList<ProjectFile> sourceFiles, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(sourceFiles != null);

            var args = new CancelEventArgs();
            Reloading?.Invoke(this, args);
            if (args.Cancel)
                return false;

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

        private bool LoadEntities([NotNull] IEnumerable<IGrouping<string, ProjectFile>> fileNamesByDirectory, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(fileNamesByDirectory != null);

            var hasChanged = false;

            var unmatchedEntities = ResourceEntities.ToList();

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

                        var existingEntity = ResourceEntities.FirstOrDefault(entity => entity.EqualsAll(projectName, baseName, directoryName));

                        if (existingEntity != null)
                        {
                            if (existingEntity.Update(projectFiles, duplicateKeyHandling))
                                hasChanged = true;

                            unmatchedEntities.Remove(existingEntity);
                        }
                        else
                        {
                            ResourceEntities.Add(new ResourceEntity(this, projectName, baseName, directoryName, projectFiles, duplicateKeyHandling));
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
            Contract.Requires(cultureKey != null);

            if (!Cultures.Contains(cultureKey))
            {
                Cultures.Add(cultureKey);
            }
        }

        internal void OnLanguageChanged([NotNull] ResourceLanguage language)
        {
            Contract.Requires(language != null);

            LanguageChanged?.Invoke(this, new LanguageEventArgs(language));
        }

        internal void OnProjectFileSaved([NotNull] ResourceLanguage language, [NotNull] ProjectFile projectFile)
        {
            Contract.Requires(projectFile != null);

            ProjectFileSaved?.Invoke(this, new ProjectFileEventArgs(language, projectFile));
        }

        public static bool IsValidLanguageName([NotNull] string languageName)
        {
            Contract.Requires(languageName != null);

            return Array.BinarySearch(_sortedCultureNames, languageName, StringComparer.OrdinalIgnoreCase) >= 0;
        }

        [NotNull]
        private static string[] GetSortedCultureNames()
        {
            Contract.Ensures(Contract.Result<string[]>() != null);

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
            Contract.Ensures(Contract.Result<CultureInfo[]>() != null);

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
            Contract.Invariant(ResourceEntities != null);
            Contract.Invariant(Cultures != null);
            Contract.Invariant(_sourceFilesProvider != null);
            Contract.Invariant(TableEntries != null);
        }
    }
}
