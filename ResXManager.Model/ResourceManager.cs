namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Threading;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

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

        private readonly Configuration _configuration;
        private readonly CodeReferenceTracker _codeReferenceTracker;
        private readonly ITracer _tracer;
        private readonly ISourceFilesProvider _sourceFilesProvider;
        private readonly PerformanceTracer _performanceTracer;

        private readonly ObservableCollection<ResourceEntity> _resourceEntities = new ObservableCollection<ResourceEntity>();
        private readonly ObservableCollection<ResourceEntity> _selectedEntities = new ObservableCollection<ResourceEntity>();
        private readonly ICollection<ResourceTableEntry> _resourceTableEntries;
        private readonly ObservableCollection<ResourceTableEntry> _selectedTableEntries = new ObservableCollection<ResourceTableEntry>();

        private readonly ObservableCollection<CultureKey> _cultureKeys = new ObservableCollection<CultureKey>();

        private string _snapshot;

        public event EventHandler<LanguageEventArgs> LanguageSaved;
        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing;
        public event EventHandler<EventArgs> Loaded;

        [ImportingConstructor]
        private ResourceManager(Configuration configuration, CodeReferenceTracker codeReferenceTracker, ITracer tracer, ISourceFilesProvider sourceFilesProvider, PerformanceTracer performanceTracer)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(codeReferenceTracker != null);
            Contract.Requires(tracer != null);
            Contract.Requires(sourceFilesProvider != null);
            Contract.Requires(performanceTracer != null);

            _configuration = configuration;
            _codeReferenceTracker = codeReferenceTracker;
            _tracer = tracer;
            _sourceFilesProvider = sourceFilesProvider;
            _performanceTracer = performanceTracer;
            _resourceTableEntries = _selectedEntities.ObservableSelectMany(entity => entity.Entries);
        }

        /// <summary>
        /// Loads all resources from the specified project files.
        /// </summary>
        /// <param name="allSourceFiles">All resource x files.</param>
        public void Load<T>(IList<T> allSourceFiles)
            where T : ProjectFile
        {
            Contract.Requires(allSourceFiles != null);

            using (_performanceTracer.Start("ResourceManager.Load"))
            {
                _codeReferenceTracker.StopFind();

                var resourceFilesByDirectory = allSourceFiles
                    .Where(file => file.IsResourceFile())
                    .GroupBy(file => file.GetBaseDirectory());

                InternalLoad(resourceFilesByDirectory);
            }
        }

        /// <summary>
        /// Saves all modified resource files.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public void Save()
        {
            var changedResourceLanguages = _resourceEntities
                .SelectMany(entity => entity.Languages)
                .Where(lang => lang.HasChanges);

            foreach (var resourceLanguage in changedResourceLanguages)
            {
                Contract.Assume(resourceLanguage != null);
                resourceLanguage.Save();
            }
        }

        public ICollection<ResourceEntity> ResourceEntities
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<ResourceEntity>>() != null);

                return _resourceEntities;
            }
        }

        public ICollection<ResourceTableEntry> ResourceTableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<ICollection<ResourceTableEntry>>() != null);

                return _resourceTableEntries;
            }
        }

        public ObservableCollection<CultureKey> CultureKeys
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<CultureKey>>() != null);

                return _cultureKeys;
            }
        }

        public ObservableCollection<ResourceEntity> SelectedEntities
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<ResourceEntity>>() != null);

                return _selectedEntities;
            }
        }

        public IList<ResourceTableEntry> SelectedTableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ResourceTableEntry>>() != null);

                return _selectedTableEntries;
            }
        }

        public static IEnumerable<CultureInfo> SpecificCultures
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureInfo>>() != null);

                return _specificCultures;
            }
        }

        public Configuration Configuration
        {
            get
            {
                Contract.Ensures(Contract.Result<Configuration>() != null);

                return _configuration;
            }
        }

        public void AddNewKey(ResourceEntity entity, string key)
        {
            Contract.Requires(entity != null);
            Contract.Requires(!string.IsNullOrEmpty(key));

            if (!entity.CanEdit(null))
                return;

            var entry = entity.Add(key);
            if (entry == null)
                return;

            if (!string.IsNullOrEmpty(_snapshot))
                _resourceEntities.LoadSnapshot(_snapshot);

            _selectedTableEntries.Clear();
            _selectedTableEntries.Add(entry);
        }

        public void NewLanguageAdded(CultureInfo culture)
        {
            if (!_configuration.AutoCreateNewLanguageFiles)
                return;

            foreach (var resourceEntity in _resourceEntities)
            {
                Contract.Assume(resourceEntity != null);

                if (!CanEdit(resourceEntity, culture))
                    break;
            }
        }

        public void Reload()
        {
            Load(_sourceFilesProvider.SourceFiles);
        }

        public void ReloadAndBeginFindCoreReferences()
        {
            var allSourceFiles = _sourceFilesProvider.SourceFiles;

            Load(allSourceFiles);
            BeginFindCodeReferences(allSourceFiles);
        }

        public void BeginFindCoreReferences()
        {
            if (_codeReferenceTracker.IsActive)
                return;

            var allSourceFiles = _sourceFilesProvider.SourceFiles;

            BeginFindCodeReferences(allSourceFiles);
        }

        public bool CanEdit(ResourceEntity resourceEntity, CultureKey cultureKey)
        {
            Contract.Requires(resourceEntity != null);

            var eventHandler = BeginEditing;

            if (eventHandler == null)
                return true;

            var args = new ResourceBeginEditingEventArgs(resourceEntity, cultureKey);

            eventHandler(this, args);

            return !args.Cancel;
        }

        private void BeginFindCodeReferences<T>(IList<T> allSourceFiles)
            where T : ProjectFile
        {
            Contract.Requires(allSourceFiles != null);

            _codeReferenceTracker.StopFind();

            if (Settings.Default.IsFindCodeReferencesEnabled)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
                {
                    _codeReferenceTracker.BeginFind(this, _configuration.CodeReferences, allSourceFiles, _tracer);
                });
            }
        }

        private void OnLoaded()
        {
            Loaded?.Invoke(this, EventArgs.Empty);
        }

        private void OnLanguageSaved(LanguageEventArgs e)
        {
            LanguageSaved?.Invoke(this, e);
        }

        private void InternalLoad(IEnumerable<IGrouping<string, ProjectFile>> resourceFilesByDirectory)
        {
            Contract.Requires(resourceFilesByDirectory != null);

            GetResourceEntities(resourceFilesByDirectory);

            if (!string.IsNullOrEmpty(_snapshot))
                _resourceEntities.LoadSnapshot(_snapshot);

            var cultureKeys = _resourceEntities
                .SelectMany(entity => entity.Languages)
                .Distinct()
                .Select(lang => lang.CultureKey)
                .ToArray();

            _cultureKeys.SynchronizeWith(cultureKeys);

            OnLoaded();
        }

        private void GetResourceEntities(IEnumerable<IGrouping<string, ProjectFile>> fileNamesByDirectory)
        {
            Contract.Requires(fileNamesByDirectory != null);

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
                            existingEntity.Update(projectFiles);
                            unmatchedEntities.Remove(existingEntity);
                        }
                        else
                        {
                            _resourceEntities.Add(new ResourceEntity(this, projectName, baseName, directoryName, projectFiles));
                        }
                    }
                }
            }

            _resourceEntities.RemoveRange(unmatchedEntities);
        }

        internal void LanguageAdded(CultureKey cultureKey)
        {
            if (!_cultureKeys.Contains(cultureKey))
            {
                _cultureKeys.Add(cultureKey);
            }
        }

        internal void LanguageChanged(ResourceLanguage language)
        {
            // Defer save to avoid repeated file access
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (!language.HasChanges)
                        return;

                    language.Save();

                    OnLanguageSaved(new LanguageEventArgs(language));
                }
                catch (Exception ex)
                {
                    _tracer.TraceError(ex.ToString());
                    MessageBox.Show(ex.Message, Resources.Title);
                }
            });
        }

        public static bool IsValidLanguageName(string languageName)
        {
            return Array.BinarySearch(_sortedCultureNames, languageName, StringComparer.OrdinalIgnoreCase) >= 0;
        }

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

        public string CreateSnapshot()
        {
            Contract.Ensures(Contract.Result<string>() != null);

            return _snapshot = ResourceEntities.CreateSnapshot();
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceEntities != null);
            Contract.Invariant(_selectedEntities != null);
            Contract.Invariant(_selectedTableEntries != null);
            Contract.Invariant(_resourceTableEntries != null);
            Contract.Invariant(_configuration != null);
            Contract.Invariant(_cultureKeys != null);
            Contract.Invariant(_codeReferenceTracker != null);
            Contract.Invariant(_sourceFilesProvider != null);
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_performanceTracer != null);
        }
    }
}
