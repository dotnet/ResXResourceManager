namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;

    using JetBrains.Annotations;

    using PropertyChanged;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;

    /// <summary>
    /// Represents a logical resource file, e.g. "Resources".
    /// A logical resource entity is linked to multiple physical resource files, one per language, e.g. "Resources.resx", "Resources.de.resx", "Resources.fr.resx".
    /// For windows store apps "de\Resources.resw", "en-us\Resources.resw" are also supported.
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public sealed class ResourceEntity
    {
        [NotNull]
        private readonly IDictionary<CultureKey, ResourceLanguage> _languages;
        [NotNull, ItemNotNull]
        private readonly ObservableCollection<ResourceTableEntry> _resourceTableEntries;

        internal ResourceEntity([NotNull] ResourceManager container, [NotNull] string projectName, [NotNull] string baseName, [NotNull] string directoryName, [NotNull][ItemNotNull] ICollection<ProjectFile> files)
        {
            Contract.Requires(container != null);
            Contract.Requires(!string.IsNullOrEmpty(projectName));
            Contract.Requires(!string.IsNullOrEmpty(baseName));
            Contract.Requires(!string.IsNullOrEmpty(directoryName));
            Contract.Requires(files != null);
            Contract.Requires(files.Any());

            Container = container;
            ProjectName = projectName;
            BaseName = baseName;
            DirectoryName = directoryName;
            _languages = GetResourceLanguages(files);
            RelativePath = GetRelativePath(files);
            DisplayName = projectName + @" - " + RelativePath + baseName;
            NeutralProjectFile = files.FirstOrDefault(file => file.GetCultureKey() == CultureKey.Neutral);

            var entriesQuery = _languages.Values
                .SelectMany(language => language.ResourceKeys)
                .Distinct()
                .Select((key, index) => new ResourceTableEntry(this, key, index, _languages));

            _resourceTableEntries = new ObservableCollection<ResourceTableEntry>(entriesQuery);

            Entries = new ReadOnlyObservableCollection<ResourceTableEntry>(_resourceTableEntries);

            Contract.Assume(_languages.Any());
        }

        internal bool Update([NotNull, ItemNotNull] ICollection<ProjectFile> files)
        {
            Contract.Requires(files != null);
            Contract.Requires(files.Any());

            if (!MergeItems(_languages, GetResourceLanguages(files)))
                return false; // nothing has changed, no need to continue

            var neutralProjectFile = files.FirstOrDefault(file => file.GetCultureKey() == CultureKey.Neutral);

            var unmatchedTableEntries = _resourceTableEntries.ToList();

            var keys = _languages.Values
                .SelectMany(language => language.ResourceKeys)
                .Distinct()
                .ToArray();

            var index = 0;

            foreach (var key in keys)
            {
                Contract.Assume(!string.IsNullOrEmpty(key));
                var existingEntry = _resourceTableEntries.FirstOrDefault(entry => entry.Key == key);
                if (existingEntry != null)
                {
                    Contract.Assume(_languages.Any());

                    existingEntry.Update(index);
                    unmatchedTableEntries.Remove(existingEntry);
                }
                else
                {
                    Contract.Assume(_languages.Any());

                    _resourceTableEntries.Add(new ResourceTableEntry(this, key, index, _languages));
                }

                index += 1;
            }

            _resourceTableEntries.RemoveRange(unmatchedTableEntries);

            NeutralProjectFile = neutralProjectFile;

            return true;
        }

        [NotNull]
        private static string GetRelativePath([NotNull][ItemNotNull] ICollection<ProjectFile> files)
        {
            Contract.Requires(files != null);
            Contract.Ensures(Contract.Result<string>() != null);

            var uniqueProjectName = files.Select(file => file.UniqueProjectName).FirstOrDefault();
            if (uniqueProjectName == null)
                return string.Empty;

            var relativeFilePath = files.Select(file => file.RelativeFilePath).FirstOrDefault();
            if (string.IsNullOrEmpty(relativeFilePath))
                return string.Empty;

            var relativeFileDirectory = Path.GetDirectoryName(relativeFilePath) + Path.DirectorySeparatorChar;

            var relativeProjectPath = Path.GetDirectoryName(uniqueProjectName);
            if (string.IsNullOrEmpty(relativeProjectPath))
            {
                return relativeFileDirectory;
            }

            var relativeProjectDirectory = Path.GetDirectoryName(uniqueProjectName) + Path.DirectorySeparatorChar;
            if ((relativeFileDirectory.Length > relativeProjectDirectory.Length)
                && relativeFileDirectory.StartsWith(relativeProjectDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return relativeFileDirectory.Substring(relativeProjectDirectory.Length);
            }

            return string.Empty;
        }

        [NotNull]
        public ResourceManager Container { get; }

        /// <summary>
        /// Gets the containing project name of the resource entity.
        /// </summary>
        [NotNull]
        public string ProjectName { get; }

        /// <summary>
        /// Gets the base name of the resource entity.
        /// </summary>
        [NotNull]
        public string BaseName { get; }

        [NotNull]
        public string RelativePath { get; }

        [NotNull]
        public string UniqueName => RelativePath + BaseName;

        [NotNull]
        public string DisplayName { get; }

        /// <summary>
        /// Gets the directory where the physical files are located.
        /// </summary>
        [NotNull]
        public string DirectoryName { get; }

        [CanBeNull]
        public ProjectFile NeutralProjectFile { get; private set; }

        public bool IsWinFormsDesignerResource => NeutralProjectFile?.IsWinFormsDesignerResource ?? false;

        /// <summary>
        /// Gets the available languages of this resource entity.
        /// </summary>
        [NotNull, ItemNotNull]
        public ICollection<ResourceLanguage> Languages => _languages.Values;

        /// <summary>
        /// Gets all the entries of this resource entity.
        /// </summary>
        [NotNull, ItemNotNull]
        public ReadOnlyObservableCollection<ResourceTableEntry> Entries { get; }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove([NotNull] ResourceTableEntry item)
        {
            Contract.Requires(item != null);

            foreach (var language in _languages.Values)
            {
                Contract.Assume(language != null);
                language.RemoveKey(item.Key);
            }

            _resourceTableEntries.Remove(item);
        }

        /// <summary>
        /// Adds an item with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        [CanBeNull]
        public ResourceTableEntry Add([NotNull] string key)
        {
            Contract.Requires(!string.IsNullOrEmpty(key));

            if (!_languages.Any() || !_languages.Values.Any())
                return null;

            var firstLanguage = _languages.Values.First();
            Contract.Assume(firstLanguage != null);

            firstLanguage.ForceValue(key, string.Empty); // force an entry in the neutral language resource file.
            var index = Math.Floor(_resourceTableEntries.Select(entry => entry.Index).DefaultIfEmpty().Max()) + 1;
            var resourceTableEntry = new ResourceTableEntry(this, key, index, _languages);
            _resourceTableEntries.Add(resourceTableEntry);

            return resourceTableEntry;
        }

        /// <summary>
        /// Adds the language represented by the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="duplicateKeyHandling">How to handle duplicate keys.</param>
        public void AddLanguage([NotNull] ProjectFile file)
        {
            Contract.Requires(file != null);

            var cultureKey = file.GetCultureKey();
            var resourceLanguage = new ResourceLanguage(this, cultureKey, file);

            _languages.Add(cultureKey, resourceLanguage);
            _resourceTableEntries.ForEach(entry => entry.Refresh());

            Container.LanguageAdded(resourceLanguage.CultureKey);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public bool CanEdit([CanBeNull] CultureKey cultureKey)
        {
            return Container.CanEdit(this, cultureKey);
        }

        internal void OnIndexChanged([NotNull] ResourceTableEntry resourceTableEntry)
        {
            Contract.Requires(resourceTableEntry != null);

            var previousEntries = _resourceTableEntries
                .Where(entry => entry.Index < resourceTableEntry.Index)
                .Reverse()
                .ToArray();

            if (!previousEntries.Any())
                return;

            if (!_languages.Values.All(l => l.CanEdit()))
                return;

            foreach (var language in _languages.Values)
            {
                Contract.Assume(language != null);

                language.MoveNode(resourceTableEntry, previousEntries);
            }
        }

        public void OnItemOrderChanged([NotNull] ResourceLanguage resourceLanguage)
        {
            Contract.Requires(resourceLanguage != null);

            if (resourceLanguage.CultureKey != CultureKey.Neutral)
                return;

            var index = 0;

            var entries = _resourceTableEntries.ToDictionary(entry => entry.Key);

            foreach (var key in resourceLanguage.ResourceKeys)
            {
                if (entries.TryGetValue(key, out var value))
                {
                    value.UpdateIndex(index++);
                }
            }
        }

        internal bool EqualsAll([CanBeNull] string projectName, [CanBeNull] string baseName, [CanBeNull] string directoryName)
        {
            return string.Equals(projectName, ProjectName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(baseName, BaseName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(directoryName, DirectoryName, StringComparison.OrdinalIgnoreCase);
        }

        [NotNull]
        private IDictionary<CultureKey, ResourceLanguage> GetResourceLanguages([NotNull][ItemNotNull] IEnumerable<ProjectFile> files)
        {
            Contract.Requires(files != null);
            Contract.Requires(files.Any());
            Contract.Ensures(Contract.Result<IDictionary<CultureKey, ResourceLanguage>>() != null);
            Contract.Ensures(Contract.Result<IDictionary<CultureKey, ResourceLanguage>>().Any());

            var languageQuery =
                from file in files
                let cultureKey = file.GetCultureKey()
                orderby cultureKey
                select new ResourceLanguage(this, cultureKey, file);

            var languages = languageQuery.ToDictionary(language => language.CultureKey);

            Contract.Assume(languages.Any());

            return languages;
        }

        private bool MergeItems([NotNull] IDictionary<CultureKey, ResourceLanguage> targets, [NotNull] IDictionary<CultureKey, ResourceLanguage> sources)
        {
            Contract.Requires(targets != null);
            Contract.Requires(sources != null);

            var removedLanguages = targets.Keys
                .Except(sources.Keys)
                .ToArray();

            removedLanguages
                .ForEach(key => targets.Remove(key));

            var hasChanges = UpdateChangedEntries(targets, sources);

            var addedLanguages = sources.Keys.Except(targets.Keys)
                .ToArray();

            addedLanguages
                .ForEach(key => targets.Add(key, sources[key]));

            return removedLanguages.Any() || hasChanges || addedLanguages.Any();
        }

        private bool UpdateChangedEntries([NotNull] IDictionary<CultureKey, ResourceLanguage> targets, [NotNull] IDictionary<CultureKey, ResourceLanguage> sources)
        {
            Contract.Requires(targets != null);
            Contract.Requires(sources != null);

            var hasChanges = false;

            foreach (var targetItem in targets.ToArray())
            {
                var cultureKey = targetItem.Key;
                var target = targetItem.Value;

                var source = sources[cultureKey];

                if (target.IsContentEqual(source))
                    continue;

                if (IsWinFormsDesignerResource)
                {
                    foreach (var resourceKey in source.ResourceKeys)
                    {
                        source.SetComment(resourceKey, target.GetComment(resourceKey));
                    }
                }

                targets[cultureKey] = source;
                hasChanges = true;
            }

            return hasChanges;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(Container != null);
            Contract.Invariant(_languages != null);
            Contract.Invariant(_resourceTableEntries != null);
            Contract.Invariant(Entries != null);
            Contract.Invariant(!string.IsNullOrEmpty(ProjectName));
            Contract.Invariant(!string.IsNullOrEmpty(BaseName));
            Contract.Invariant(!string.IsNullOrEmpty(DirectoryName));
            Contract.Invariant(DisplayName != null);
            Contract.Invariant(RelativePath != null);
        }
    }
}