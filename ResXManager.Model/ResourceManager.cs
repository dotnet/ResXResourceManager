namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Data;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;

    /// <summary>
    /// Represents all resources found in a folder and its's sub folders.
    /// </summary>
    [Export]
    public class ResourceManager : ObservableObject
    {
        private static readonly string[] _sortedCultureNames = GetSortedCultureNames();
        private static readonly CultureInfo[] _specificCultures = GetSpecificCultures();

        private readonly DispatcherThrottle _selectedEntitiesChangeThrottle;
        private readonly Configuration _configuration;
        private readonly CodeReferenceTracker _codeReferenceTracker;
        private readonly ITracer _tracer;

        private ObservableCollection<ResourceEntity> _resourceEntities = new ObservableCollection<ResourceEntity>();
        private ObservableCollection<ResourceEntity> _selectedEntities = new ObservableCollection<ResourceEntity>();
        private ObservableCollection<CultureKey> _cultureKeys = new ObservableCollection<CultureKey>();
        private ListCollectionViewListAdapter<ResourceEntity> _filteredResourceEntities;

        private ObservableCompositeCollection<ResourceTableEntry> _resourceTableEntries = new ObservableCompositeCollection<ResourceTableEntry>();
        private ObservableCollection<ResourceTableEntry> _selectedTableEntries = new ObservableCollection<ResourceTableEntry>();

        private string _entityFilter;
        private string _snapshot;

        public event EventHandler<LanguageEventArgs> LanguageSaved;
        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing;
        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<EventArgs> ReloadRequested;
        public event EventHandler<EventArgs> SelectedEntitiesChanged;

        [ImportingConstructor]
        private ResourceManager(Configuration configuration, CodeReferenceTracker codeReferenceTracker, ITracer tracer)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(codeReferenceTracker != null);
            Contract.Requires(tracer != null);

            _configuration = configuration;
            _codeReferenceTracker = codeReferenceTracker;
            _tracer = tracer;
            _selectedEntitiesChangeThrottle = new DispatcherThrottle(OnSelectedEntitiesChanged);
            _filteredResourceEntities = new ListCollectionViewListAdapter<ResourceEntity>(new ListCollectionView(_resourceEntities));
        }

        /// <summary>
        /// Loads all resources from the specified project files.
        /// </summary>
        /// <param name="allSourceFiles">All resource x files.</param>
        public void Load<T>(IList<T> allSourceFiles)
            where T : ProjectFile
        {
            Contract.Requires(allSourceFiles != null);

            _codeReferenceTracker.StopFind();

            var resourceFilesByDirectory = allSourceFiles
                .Where(file => file.IsResourceFile())
                .GroupBy(file => file.GetBaseDirectory());

            InternalLoad(resourceFilesByDirectory);
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
                Contract.Ensures(Contract.Result<IEnumerable<ResourceEntity>>() != null);

                return _resourceEntities;
            }
        }

        public IList FilteredResourceEntities
        {
            get
            {
                Contract.Ensures(Contract.Result<IList>() != null);

                return _filteredResourceEntities;
            }
        }

        public ICollection<ResourceTableEntry> ResourceTableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ResourceTableEntry>>() != null);

                return _resourceTableEntries;
            }
        }

        public ICollection<CultureKey> CultureKeys
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<CultureKey>>() != null);

                return _cultureKeys;
            }
        }

        public IList<ResourceEntity> SelectedEntities
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ResourceEntity>>() != null);

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

        public string EntityFilter
        {
            get
            {
                return _entityFilter;
            }
            set
            {
                if (_entityFilter == value)
                    return;

                ApplyEntityFilter(value);

                OnPropertyChanged(() => EntityFilter);
                OnPropertyChanged(() => AreAllFilesSelected);
            }
        }

        public bool? AreAllFilesSelected
        {
            get
            {
                if (_selectedEntities.Count == 0)
                    return false;

                if (_selectedEntities.Count == _filteredResourceEntities.Count)
                    return true;

                return null;
            }
            set
            {
                if (value == AreAllFilesSelected)
                    return;

                var selected = (value == true) ? _filteredResourceEntities.Cast<ResourceEntity>() : Enumerable.Empty<ResourceEntity>();

                _selectedEntities.CollectionChanged -= SelectedEntities_CollectionChanged;
                _selectedEntities = new ObservableCollection<ResourceEntity>(selected);
                _selectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;

                _selectedEntitiesChangeThrottle.Tick();
                OnPropertyChanged(() => SelectedEntities);
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

            _selectedTableEntries = new ObservableCollection<ResourceTableEntry> { entry };
            OnPropertyChanged(() => SelectedTableEntries);
        }

        public void LanguageAdded(CultureInfo culture)
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
            OnReloadRequested();
        }

        public bool CanEdit(ResourceEntity resourceEntity, CultureInfo culture)
        {
            Contract.Requires(resourceEntity != null);

            var eventHandler = BeginEditing;

            if (eventHandler == null)
                return true;

            var args = new ResourceBeginEditingEventArgs(resourceEntity, culture);

            eventHandler(this, args);

            return !args.Cancel;
        }

        private void OnLoaded()
        {
            var handler = Loaded;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnLanguageSaved(LanguageEventArgs e)
        {
            var handler = LanguageSaved;
            if (handler != null)
                handler(this, e);
        }

        private void OnReloadRequested()
        {
            var handler = ReloadRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void InternalLoad(IEnumerable<IGrouping<string, ProjectFile>> resourceFilesByDirectory)
        {
            Contract.Requires(resourceFilesByDirectory != null);

            var entities = GetResourceEntities(resourceFilesByDirectory)
                .OrderBy(e => e.ProjectName)
                .ThenBy(e => e.BaseName);

            var reload = _resourceEntities.Any();

            _resourceEntities = new ObservableCollection<ResourceEntity>(entities);

            if (!string.IsNullOrEmpty(_snapshot))
                _resourceEntities.LoadSnapshot(_snapshot);

            _filteredResourceEntities = new ListCollectionViewListAdapter<ResourceEntity>(new ListCollectionView(_resourceEntities));
            if (!string.IsNullOrEmpty(_entityFilter))
                ApplyEntityFilter(_entityFilter);

            _selectedEntities.CollectionChanged -= SelectedEntities_CollectionChanged;
            _selectedEntities = new ObservableCollection<ResourceEntity>(reload ? _resourceEntities.Where(_selectedEntities.Contains) : _resourceEntities);
            _selectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;

            var cultureKeys = _resourceEntities.SelectMany(entity => entity.Languages).Distinct().Select(lang => lang.CultureKey);
            _cultureKeys = new ObservableCollection<CultureKey>(cultureKeys);

            OnPropertyChanged(() => ResourceEntities);
            OnPropertyChanged(() => FilteredResourceEntities);
            OnPropertyChanged(() => SelectedEntities);
            OnPropertyChanged(() => CultureKeys);

            OnSelectedEntitiesChanged();
            OnLoaded();
        }

        private IEnumerable<ResourceEntity> GetResourceEntities(IEnumerable<IGrouping<string, ProjectFile>> fileNamesByDirectory)
        {
            Contract.Requires(fileNamesByDirectory != null);

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

                    foreach (var projectFiles in filesByProject)
                    {
                        if (projectFiles == null)
                            continue;

                        var projectName = projectFiles.Key;

                        if (string.IsNullOrEmpty(projectName))
                            continue;

                        var resourceEntity = new ResourceEntity(this, projectName, baseName, directoryName, files.ToArray());

                        resourceEntity.LanguageChanging += ResourceEntity_LanguageChanging;
                        resourceEntity.LanguageChanged += ResourceEntity_LanguageChanged;
                        resourceEntity.LanguageAdded += ResourceEntity_LanguageAdded;

                        yield return resourceEntity;
                    }
                }
            }
        }

        private void ResourceEntity_LanguageAdded(object sender, LanguageChangedEventArgs e)
        {
            var cultureKey = e.Language.CultureKey;

            if (!_cultureKeys.Contains(cultureKey))
            {
                _cultureKeys.Add(cultureKey);
            }
        }

        private void ResourceEntity_LanguageChanging(object sender, LanguageChangingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.Culture))
            {
                e.Cancel = true;
            }
        }

        private void ResourceEntity_LanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            // Defer save to avoid repeated file access
            Dispatcher.BeginInvoke(new Action(
                delegate
                {
                    try
                    {
                        if (!e.Language.HasChanges)
                            return;

                        e.Language.Save();

                        OnLanguageSaved(e);
                    }
                    catch (Exception ex)
                    {
                        _tracer.TraceError(ex.ToString());
                        MessageBox.Show(ex.Message, Resources.Title);
                    }
                }));
        }

        private void SelectedEntities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _selectedEntitiesChangeThrottle.Tick();
        }

        private void OnSelectedEntitiesChanged()
        {
            var selectedTableEntries = _selectedTableEntries.ToArray();

            _resourceTableEntries = new ObservableCompositeCollection<ResourceTableEntry>(_selectedEntities.Select(entity => entity.Entries).ToArray());

            _selectedTableEntries = new ObservableCollection<ResourceTableEntry>(_resourceEntities.SelectMany(entity => entity.Entries)
                .Where(item => selectedTableEntries.Contains(item, ResourceTableEntry.EqualityComparer))
                .Where(item => _resourceTableEntries.Contains(item, ResourceTableEntry.EqualityComparer)));

            OnPropertyChanged(() => ResourceTableEntries);
            OnPropertyChanged(() => SelectedTableEntries);
            OnPropertyChanged(() => AreAllFilesSelected);

            var eventHandler = SelectedEntitiesChanged;
            if (eventHandler != null)
                eventHandler(this, EventArgs.Empty);
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

        private void ApplyEntityFilter(string value)
        {
            _entityFilter = value;

            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var regex = new Regex(value.Trim(), RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    _filteredResourceEntities.CollectionView.Filter = item => regex.Match(item.ToString()).Success;
                    return;
                }
                catch (ArgumentException)
                {
                }
            }

            _filteredResourceEntities.CollectionView.Filter = null;
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
            Contract.Invariant(_filteredResourceEntities != null);
            Contract.Invariant(FilteredResourceEntities != null);
            Contract.Invariant(_selectedTableEntries != null);
            Contract.Invariant(_resourceTableEntries != null);
            Contract.Invariant(_selectedEntitiesChangeThrottle != null);
            Contract.Invariant(_configuration != null);
            Contract.Invariant(_cultureKeys != null);
        }
    }
}
