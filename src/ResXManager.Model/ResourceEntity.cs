namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using PropertyChanged;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    /// <summary>
    /// Represents a logical resource file, e.g. "Resources".
    /// A logical resource entity is linked to multiple physical resource files, one per language, e.g. "Resources.resx", "Resources.de.resx", "Resources.fr.resx".
    /// For windows store apps "de\Resources.resw", "en-us\Resources.resw" are also supported.
    /// </summary>
    [AddINotifyPropertyChangedInterface]
#pragma warning disable CA1036 // Override methods on comparable types => just to enable sorting in the UI
    public sealed class ResourceEntity : IComparable, IComparable<ResourceEntity>
    {
        private readonly IDictionary<CultureKey, ResourceLanguage> _languages;
        private readonly ObservableCollection<ResourceTableEntry> _resourceTableEntries;

        internal ResourceEntity(ResourceManager container, string projectName, string baseName, string directoryName, ICollection<ProjectFile> files, CultureInfo neutralResourcesLanguage, DuplicateKeyHandling duplicateKeyHandling)
        {
            Container = container;
            ProjectName = projectName;
            BaseName = baseName;
            DirectoryName = directoryName;
            _languages = GetResourceLanguages(files, neutralResourcesLanguage, duplicateKeyHandling);
            RelativePath = GetRelativePath(files);
            DisplayName = projectName + @" - " + RelativePath + baseName;

            NeutralProjectFile = files.FirstOrDefault(file => file.GetCultureKey(neutralResourcesLanguage) == CultureKey.Neutral);
            NeutralResourcesLanguage = neutralResourcesLanguage;

            var entriesQuery = _languages.Values
                .SelectMany(language => language.ResourceKeys)
                .Distinct()
                .Select((key, index) => new ResourceTableEntry(this, key, index, _languages));

            _resourceTableEntries = new ObservableCollection<ResourceTableEntry>(entriesQuery);

            Entries = new ReadOnlyObservableCollection<ResourceTableEntry>(_resourceTableEntries);
        }

        internal bool Update(ICollection<ProjectFile> files, CultureInfo neutralResourcesLanguage, DuplicateKeyHandling duplicateKeyHandling)
        {
            NeutralResourcesLanguage = neutralResourcesLanguage;

            if (!MergeItems(GetResourceLanguages(files, neutralResourcesLanguage, duplicateKeyHandling)))
                return false; // nothing has changed, no need to continue

            var neutralProjectFile = files.FirstOrDefault(file => file.GetCultureKey(neutralResourcesLanguage) == CultureKey.Neutral);

            UpdateResourceTableEntries();

            NeutralProjectFile = neutralProjectFile;

            return true;
        }

        public bool Update(ProjectFile file, [NotNullWhen(true)] out ResourceLanguage? updatedLanguage)
        {
            var duplicateKeyHandling = Container.Configuration.DuplicateKeyHandling;

            updatedLanguage = new ResourceLanguage(this, file.GetCultureKey(NeutralResourcesLanguage), file, duplicateKeyHandling);
            if (!UpdateEntry(updatedLanguage))
            {
                updatedLanguage = null;
                return false;
            }

            UpdateResourceTableEntries();
            return true;
        }

        private void UpdateResourceTableEntries()
        {
            var unmatchedTableEntries = _resourceTableEntries.ToList();

            var keys = _languages.Values
                .SelectMany(language => language.ResourceKeys)
                .Distinct()
                .ToArray();

            var index = 0;

            foreach (var key in keys)
            {
                var existingEntry = _resourceTableEntries.FirstOrDefault(entry => entry.Key == key);
                if (existingEntry != null)
                {
                    existingEntry.Update(index);
                    unmatchedTableEntries.Remove(existingEntry);
                }
                else
                {
                    _resourceTableEntries.Add(new ResourceTableEntry(this, key, index, _languages));
                }

                index += 1;
            }

            _resourceTableEntries.RemoveRange(unmatchedTableEntries);
        }

        private static string GetRelativePath(ICollection<ProjectFile> files)
        {
            var uniqueProjectName = files.Select(file => file.UniqueProjectName).FirstOrDefault();
            if (uniqueProjectName == null)
                return string.Empty;

            var relativeFilePath = files.Select(file => file.RelativeFilePath).FirstOrDefault();
            if (relativeFilePath.IsNullOrEmpty())
                return string.Empty;

            var relativeFileDirectory = Path.GetDirectoryName(relativeFilePath) + Path.DirectorySeparatorChar;

            var relativeProjectPath = Path.GetDirectoryName(uniqueProjectName);
            if (relativeProjectPath.IsNullOrEmpty())
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

        public ResourceManager Container { get; }

        /// <summary>
        /// Gets the containing project name of the resource entity.
        /// </summary>
        public string ProjectName { get; }

        /// <summary>
        /// Gets the base name of the resource entity.
        /// </summary>
        public string BaseName { get; }

        public string RelativePath { get; }

        public string UniqueName => RelativePath + BaseName;

        public string DisplayName { get; }

        /// <summary>
        /// Gets the directory where the physical files are located.
        /// </summary>
        public string DirectoryName { get; }

        public ProjectFile? NeutralProjectFile { get; private set; }

        public CultureInfo NeutralResourcesLanguage { get; private set; }

        public bool IsWinFormsDesignerResource => NeutralProjectFile?.IsWinFormsDesignerResource ?? false;

        /// <summary>
        /// Gets the available languages of this resource entity.
        /// </summary>
        public ICollection<ResourceLanguage> Languages => _languages.Values;

        /// <summary>
        /// Gets all the entries of this resource entity.
        /// </summary>
        public ReadOnlyObservableCollection<ResourceTableEntry> Entries { get; }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove(ResourceTableEntry item)
        {
            foreach (var language in _languages.Values)
            {
                language.RemoveKey(item.Key);
            }

            _resourceTableEntries.Remove(item);
        }

        /// <summary>
        /// Adds an item with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public ResourceTableEntry? Add(string key)
        {
            if (!_languages.Any() || !_languages.Values.Any())
                return null;

            var firstLanguage = _languages.Values.First();

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
        public void AddLanguage(ProjectFile file)
        {
            var cultureKey = file.GetCultureKey(NeutralResourcesLanguage);
            var resourceLanguage = new ResourceLanguage(this, cultureKey, file, Container.Configuration.DuplicateKeyHandling);

            _languages.Add(cultureKey, resourceLanguage);
            _resourceTableEntries.ForEach(entry => entry.Refresh());

            Container.OnLanguageAdded(resourceLanguage, file);
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public int CompareTo(object? obj)
        {
            return CompareTo(obj as ResourceEntity);
        }

        public int CompareTo(ResourceEntity? other)
        {
#pragma warning disable CA1310 // Specify StringComparison for correctness => only available in .NetCore api
            return DisplayName.CompareTo(other?.DisplayName);
#pragma warning restore CA1310 // Specify StringComparison for correctness
        }

        public bool CanEdit(CultureKey? cultureKey)
        {
            return Container.CanEdit(this, cultureKey);
        }

        [SuppressPropertyChangedWarnings]
        internal void OnIndexChanged(ResourceTableEntry resourceTableEntry)
        {
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
                language.MoveNode(resourceTableEntry, previousEntries);
            }
        }

        [SuppressPropertyChangedWarnings]
        public void OnItemOrderChanged(ResourceLanguage resourceLanguage)
        {
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

        internal bool EqualsAll(string? projectName, string? baseName, string? directoryName)
        {
            return string.Equals(projectName, ProjectName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(baseName, BaseName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(directoryName, DirectoryName, StringComparison.OrdinalIgnoreCase);
        }

        private IDictionary<CultureKey, ResourceLanguage> GetResourceLanguages(IEnumerable<ProjectFile> files, CultureInfo neutralResourcesLanguage, DuplicateKeyHandling duplicateKeyHandling)
        {
            var languageQuery =
                from file in files
                let cultureKey = file.GetCultureKey(neutralResourcesLanguage)
                orderby cultureKey
                select new ResourceLanguage(this, cultureKey, file, duplicateKeyHandling);

            var languages = languageQuery.ToDictionary(language => language.CultureKey);

            return languages;
        }

        private bool MergeItems(IDictionary<CultureKey, ResourceLanguage> sources)
        {
            var removedLanguages = _languages.Keys
                .Except(sources.Keys)
                .ToArray();

            removedLanguages
                .ForEach(key => _languages.Remove(key));

            var hasChanges = UpdateChangedEntries(sources.Values);

            return hasChanges || removedLanguages.Any();
        }

        private bool UpdateChangedEntries(IEnumerable<ResourceLanguage> sources)
        {
            var hasChanges = false;

            foreach (var source in sources)
            {
                if (UpdateEntry(source))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private bool UpdateEntry(ResourceLanguage source)
        {
            var cultureKey = source.CultureKey;

            if (_languages.TryGetValue(cultureKey, out var target))
            {
                if (target.IsContentEqual(source))
                    return false;

                if (IsWinFormsDesignerResource)
                {
                    foreach (var resourceKey in source.ResourceKeys)
                    {
                        source.SetComment(resourceKey, target.GetComment(resourceKey));
                    }
                }
            }

            _languages[cultureKey] = source;
            return true;
        }
    }
}