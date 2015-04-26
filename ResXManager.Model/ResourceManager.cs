namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;

    using Microsoft.Win32;

    using tomenglertde.ResXManager.Model.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;

    /// <summary>
    /// Represents all resources found in a folder and its's sub folders.
    /// </summary>
    public class ResourceManager : ObservableObject
    {
        private static readonly string[] _sortedCultureNames = GetSortedCultureNames();
        private static readonly CultureInfo[] _specificCultures = GetSpecificCultures();

        private readonly DispatcherThrottle _selectedEntitiesChangeThrottle;
        private readonly Translations _translations;

        private Configuration _configuration = new Configuration();

        private ObservableCollection<ResourceEntity> _resourceEntities = new ObservableCollection<ResourceEntity>();
        private ObservableCollection<ResourceEntity> _selectedEntities = new ObservableCollection<ResourceEntity>();
        private ObservableCollection<CultureKey> _cultureKeys = new ObservableCollection<CultureKey>();
        private ListCollectionViewListAdapter<ResourceEntity> _filteredResourceEntities;

        private ObservableCompositeCollection<ResourceTableEntry> _resourceTableEntries = new ObservableCompositeCollection<ResourceTableEntry>();
        private IList<ResourceTableEntry> _selectedTableEntries = new List<ResourceTableEntry>();

        private ResourceTableEntry _selectedEntry;
        private string _entityFilter;

        public event EventHandler<LanguageEventArgs> LanguageSaved;
        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing;
        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<EventArgs> ReloadRequested;

        public ResourceManager()
        {
            _selectedEntitiesChangeThrottle = new DispatcherThrottle(OnSelectedEntitiesChanged);
            _filteredResourceEntities = new ListCollectionViewListAdapter<ResourceEntity>(new ListCollectionView(_resourceEntities));
            _translations = new Translations(this);
        }

        /// <summary>
        /// Loads all resources from the specified project files.
        /// </summary>
        /// <param name="allSourceFiles">All resource x files.</param>
        /// <param name="configuration"></param>
        public void Load<T>(IList<T> allSourceFiles, Configuration configuration)
            where T : ProjectFile
        {
            Contract.Requires(allSourceFiles != null);
            Contract.Requires(configuration != null);

            CodeReference.StopFind();

            Configuration = configuration;

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

        public IEnumerable<ResourceEntity> ResourceEntities
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

        public ResourceTableEntry SelectedEntry
        {
            get
            {
                return _selectedEntry;
            }
            set
            {
                if (_selectedEntry == value)
                    return;

                _selectedEntry = value;
                OnPropertyChanged(() => SelectedEntry);
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

        public Translations Translations
        {
            get
            {
                return _translations;
            }
        }

        public ICommand CopyCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(CanCutOrCopy, CopySelected);
            }
        }

        public ICommand CutCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(CanCutOrCopy, CutSelected);
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(CanDelete, DeleteSelected);
            }
        }

        public ICommand PasteCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(CanPaste, Paste);
            }
        }

        public ICommand ExportExcelSelectedCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<IResourceScope>(
                    scope => scope.Entries.Any() && (scope.Languages.Any() || scope.Comments.Any()),
                    ExportExcel);
            }
        }

        public ICommand ExportExcelAllCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => ExportExcel(null));
            }
        }

        public ICommand ImportExcelCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<string>(ImportExcel);
            }
        }

        public ICommand CopyKeysCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => _selectedTableEntries.Any(), CopyKeys);
            }
        }

        public ICommand ToggleInvariantCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => _selectedTableEntries.Any(), ToggleInvariant);
            }
        }

        public ICommand ReloadCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(OnReloadRequested);
            }
        }

        public ICommand SortNodesByKeyCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(SortNodesByKey);
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
            set
            {
                if (Equals(value, _configuration))
                    return;
                _configuration = value;
                OnPropertyChanged(() => Configuration);
            }
        }

        public void AddNewKey(ResourceEntity entity, string key)
        {
            Contract.Requires(entity != null);
            Contract.Requires(!String.IsNullOrEmpty(key));

            if (!entity.CanEdit(null))
                return;

            var entry = entity.Add(key);
            if (entry == null)
                return;

            _selectedTableEntries = new List<ResourceTableEntry> { entry };
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

        private bool CanEdit(ResourceEntity resourceEntity, CultureInfo culture)
        {
            Contract.Requires(resourceEntity != null);

            var eventHandler = BeginEditing;

            if (eventHandler == null)
                return true;

            var args = new ResourceBeginEditingEventArgs(resourceEntity, culture);

            eventHandler(this, args);

            return !args.Cancel;
        }

        private bool CanDelete()
        {
            return SelectedTableEntries.Any();
        }

        private bool CanCutOrCopy()
        {
            var entries = SelectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return false;

            // Only allow is all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private bool CanPaste()
        {
            return _selectedEntities.Count == 1;
        }

        private void CutSelected()
        {
            var selectedItems = _selectedTableEntries.ToList();

            var resourceFiles = selectedItems.Select(item => item.Owner).Distinct();

            if (resourceFiles.Any(resourceFile => !CanEdit(resourceFile, null)))
                return;

            Clipboard.SetText(selectedItems.ToTextTable());

            selectedItems.ForEach(item => item.Owner.Remove(item));
        }

        private void CopySelected()
        {
            var selectedItems = _selectedTableEntries.ToList();

            var entries = selectedItems.Cast<ResourceTableEntry>().ToArray();
            Clipboard.SetText(entries.ToTextTable());
        }

        public void DeleteSelected()
        {
            var selectedItems = _selectedTableEntries.ToList();

            if (selectedItems.Count == 0)
                return;

            var resourceFiles = selectedItems.Select(item => item.Owner).Distinct();

            if (resourceFiles.Any(resourceFile => !CanEdit(resourceFile, null)))
                return;

            selectedItems.ForEach(item => item.Owner.Remove(item));
        }

        private void Paste()
        {
            var selectedItems = _selectedEntities.ToList();

            if (selectedItems.Count != 1)
                return;

            var entity = selectedItems[0];

            Contract.Assume(entity != null);

            if (!CanEdit(entity, null))
                return;

            entity.ImportTextTable(Clipboard.GetText());
        }

        private void ToggleInvariant()
        {
            var items = _selectedTableEntries.ToList();

            if (!items.Any())
                return;

            var newValue = !items.First().IsInvariant;

            items.ForEach(item => item.IsInvariant = newValue);
        }

        private void ExportExcel(IResourceScope scope)
        {
            var dlg = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".xlsx",
                Filter = "Excel Worksheets|*.xlsx|All Files|*.*",
                FilterIndex = 0
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                this.ExportExcelFile(dlg.FileName, scope);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ImportExcel(string fileName)
        {
            Contract.Requires(fileName != null);

            this.ImportExcelFile(fileName);
        }

        private void CopyKeys()
        {
            var selectedKeys = _selectedTableEntries.Select(item => item.Key);

            Clipboard.SetText(string.Join(Environment.NewLine, selectedKeys));
        }

        private void SortNodesByKey()
        {
            foreach (var language in _resourceEntities.SelectMany(entity => entity.Languages))
            {
                Contract.Assume(language != null);
                language.SortNodesByKey();
            }
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

            _resourceEntities = new ObservableCollection<ResourceEntity>(entities);
            _filteredResourceEntities = new ListCollectionViewListAdapter<ResourceEntity>(new ListCollectionView(_resourceEntities));
            if (!string.IsNullOrEmpty(_entityFilter))
                ApplyEntityFilter(_entityFilter);

            _selectedEntities.CollectionChanged -= SelectedEntities_CollectionChanged;
            _selectedEntities = new ObservableCollection<ResourceEntity>(_resourceEntities.Where(_selectedEntities.Contains));
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

        void ResourceEntity_LanguageAdded(object sender, LanguageChangedEventArgs e)
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
                    var regex = new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    _filteredResourceEntities.CollectionView.Filter = item => regex.Match(item.ToString()).Success;
                    return;
                }
                catch (ArgumentException)
                {
                }
            }

            _filteredResourceEntities.CollectionView.Filter = null;
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
