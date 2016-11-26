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

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    /// <summary>
    /// Represents a logical resource file, e.g. "Resources".
    /// A logical resource entity is linked to multiple physical resource files, one per language, e.g. "Resources.resx", "Resources.de.resx", "Resources.fr.resx".
    /// For windows store apps "de\Resources.resw", "en-us\Resources.resw" are also supported.
    /// </summary>
    public class ResourceEntity : ObservableObject, IComparable<ResourceEntity>, IComparable, IEquatable<ResourceEntity>
    {
        [NotNull]
        private readonly IDictionary<CultureKey, ResourceLanguage> _languages;
        [NotNull]
        private readonly ResourceManager _container;
        [NotNull]
        private readonly string _projectName;
        [NotNull]
        private readonly string _baseName;
        [NotNull]
        private readonly string _directoryName;
        [NotNull]
        private readonly ObservableCollection<ResourceTableEntry> _resourceTableEntries;
        [NotNull]
        private readonly ReadOnlyObservableCollection<ResourceTableEntry> _readOnlyResourceTableEntries;
        [NotNull]
        private readonly string _displayName;
        [NotNull]
        private readonly string _relativePath;
        [NotNull]
        private readonly string _sortKey;

        private ProjectFile _neutralProjectFile;

        internal ResourceEntity([NotNull] ResourceManager container, [NotNull] string projectName, [NotNull] string baseName, [NotNull] string directoryName, [NotNull] ICollection<ProjectFile> files, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(container != null);
            Contract.Requires(!string.IsNullOrEmpty(projectName));
            Contract.Requires(!string.IsNullOrEmpty(baseName));
            Contract.Requires(!string.IsNullOrEmpty(directoryName));
            Contract.Requires(files != null);
            Contract.Requires(files.Any());

            _container = container;
            _projectName = projectName;
            _baseName = baseName;
            _directoryName = directoryName;
            _languages = GetResourceLanguages(files, duplicateKeyHandling);
            _relativePath = GetRelativePath(files);
            _displayName = projectName + @" - " + _relativePath + baseName;
            _sortKey = string.Concat(@" - ", _displayName, _directoryName);
            _neutralProjectFile = files.FirstOrDefault(file => file.GetCultureKey() == CultureKey.Neutral);

            var entriesQuery = _languages.Values
                .SelectMany(language => language.ResourceKeys)
                .Distinct()
                .Select((key, index) => new ResourceTableEntry(this, key, index, _languages));

            _resourceTableEntries = new ObservableCollection<ResourceTableEntry>(entriesQuery);
            _readOnlyResourceTableEntries = new ReadOnlyObservableCollection<ResourceTableEntry>(_resourceTableEntries);

            Contract.Assume(_languages.Any());
        }

        internal bool Update([NotNull] ICollection<ProjectFile> files, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(files != null);
            Contract.Requires(files.Any());

            if (!MergeItems(_languages, GetResourceLanguages(files, duplicateKeyHandling)))
                return false; // nothing has changed, no need to continue

            _neutralProjectFile = files.FirstOrDefault(file => file.GetCultureKey() == CultureKey.Neutral);

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

                    existingEntry.Update(index, _languages);
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

            OnPropertyChanged(nameof(NeutralProjectFile));

            return true;
        }

        [NotNull]
        private static string GetRelativePath([NotNull] ICollection<ProjectFile> files)
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
        public ResourceManager Container
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceManager>() != null);
                return _container;
            }
        }

        /// <summary>
        /// Gets the containing project name of the resource entity.
        /// </summary>
        [NotNull]
        public string ProjectName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _projectName;
            }
        }

        /// <summary>
        /// Gets the base name of the resource entity.
        /// </summary>
        [NotNull]
        public string BaseName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _baseName;
            }
        }

        [NotNull]
        public string RelativePath
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _relativePath;
            }
        }

        [NotNull]
        public string UniqueName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _relativePath + _baseName;
            }
        }

        [NotNull]
        public string DisplayName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _displayName;
            }
        }

        /// <summary>
        /// Gets the directory where the physical files are located.
        /// </summary>
        [NotNull]
        public string DirectoryName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _directoryName;
            }
        }

        public ProjectFile NeutralProjectFile => _neutralProjectFile;

        public bool IsWinFormsDesignerResource => _neutralProjectFile?.IsWinFormsDesignerResource ?? false;

        /// <summary>
        /// Gets the available languages of this resource entity.
        /// </summary>
        [NotNull, ItemNotNull]
        public ICollection<ResourceLanguage> Languages
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ResourceLanguage>>() != null);
                return _languages.Values;
            }
        }

        /// <summary>
        /// Gets all the entries of this resource entity.
        /// </summary>
        [NotNull, ItemNotNull]
        public ReadOnlyObservableCollection<ResourceTableEntry> Entries
        {
            get
            {
                Contract.Ensures(Contract.Result<ReadOnlyObservableCollection<ResourceTableEntry>>() != null);
                return _readOnlyResourceTableEntries;
            }
        }

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
        public void AddLanguage([NotNull] ProjectFile file, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(file != null);

            var cultureKey = file.GetCultureKey();
            var resourceLanguage = new ResourceLanguage(this, cultureKey, file, duplicateKeyHandling);

            _languages.Add(cultureKey, resourceLanguage);
            _resourceTableEntries.ForEach(entry => entry.Refresh());

            Container.LanguageAdded(resourceLanguage.CultureKey);
        }

        public override string ToString()
        {
            return _displayName;
        }

        public bool CanEdit(CultureKey cultureKey)
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

        internal bool EqualsAll(string projectName, string baseName, string directoryName)
        {
            return string.Equals(projectName, _projectName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(baseName, _baseName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(directoryName, _directoryName, StringComparison.OrdinalIgnoreCase);
        }

        [NotNull]
        private IDictionary<CultureKey, ResourceLanguage> GetResourceLanguages([NotNull] IEnumerable<ProjectFile> files, DuplicateKeyHandling duplicateKeyHandling)
        {
            Contract.Requires(files != null);
            Contract.Requires(files.Any());
            Contract.Ensures(Contract.Result<IDictionary<CultureKey, ResourceLanguage>>() != null);
            Contract.Ensures(Contract.Result<IDictionary<CultureKey, ResourceLanguage>>().Any());

            var languageQuery =
                from file in files
                let cultureKey = file.GetCultureKey()
                orderby cultureKey
                select new ResourceLanguage(this, cultureKey, file, duplicateKeyHandling);

            var languages = languageQuery.ToDictionary(language => language.CultureKey);

            Contract.Assume(languages.Any());

            return languages;
        }

        private static bool MergeItems([NotNull] IDictionary<CultureKey, ResourceLanguage> targets, [NotNull] IDictionary<CultureKey, ResourceLanguage> sources)
        {
            Contract.Requires(targets != null);
            Contract.Requires(sources != null);

            var removedLanguages = targets.Keys
                .Except(sources.Keys)
                .ToArray();

            removedLanguages
                .ForEach(key => targets.Remove(key));

            var changedLanguages = targets.Keys
                .Where(key => !targets[key].IsContentEqual(sources[key]))
                .ToArray();

            changedLanguages
                .ForEach(key => targets[key] = sources[key]);

            var addedLanguages = sources.Keys.Except(targets.Keys)
                .ToArray();

            addedLanguages
                .ForEach(key => targets.Add(key, sources[key]));

            return removedLanguages.Any() || changedLanguages.Any() || addedLanguages.Any();
        }


        #region IComparable/IEquatable implementation

        [NotNull]
        private string SortKey
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return _sortKey;
            }
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return SortKey.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ResourceEntity);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ResourceEntity" /> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="ResourceEntity"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="ResourceEntity" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ResourceEntity other)
        {
            return Compare(this, other) == 0;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        public int CompareTo(object obj)
        {
            return Compare(this, obj as ResourceEntity);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        public int CompareTo(ResourceEntity other)
        {
            return Compare(this, other);
        }

        private static int Compare(ResourceEntity left, ResourceEntity right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (ReferenceEquals(left, null))
                return -1;
            if (ReferenceEquals(right, null))
                return 1;

            return string.Compare(left.SortKey, right.SortKey, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(ResourceEntity left, ResourceEntity right)
        {
            return Compare(left, right) == 0;
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(ResourceEntity left, ResourceEntity right)
        {
            return Compare(left, right) != 0;
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        public static bool operator >(ResourceEntity left, ResourceEntity right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        public static bool operator <(ResourceEntity left, ResourceEntity right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        public static bool operator >=(ResourceEntity left, ResourceEntity right)
        {
            return Compare(left, right) >= 0;
        }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        public static bool operator <=(ResourceEntity left, ResourceEntity right)
        {
            return Compare(left, right) <= 0;
        }

        #endregion

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_container != null);
            Contract.Invariant(_languages != null);
            Contract.Invariant(_resourceTableEntries != null);
            Contract.Invariant(_readOnlyResourceTableEntries != null);
            Contract.Invariant(!string.IsNullOrEmpty(_projectName));
            Contract.Invariant(!string.IsNullOrEmpty(_baseName));
            Contract.Invariant(!string.IsNullOrEmpty(_directoryName));
            Contract.Invariant(_displayName != null);
            Contract.Invariant(_relativePath != null);
            Contract.Invariant(_sortKey != null);
        }
    }
}