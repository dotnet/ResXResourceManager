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

    /// <summary>
    /// Represents all resources found in a folder and its's sub folders.
    /// </summary>
    public class ResourceManager : ObservableObject
    {
        private static readonly string[] SortedCultureNames = GetSortedCultureNames();
        private readonly DispatcherThrottle _selectedEntitiesChangeThrottle;

        private ObservableCollection<ResourceEntity> _resourceEntities = new ObservableCollection<ResourceEntity>();
        private ObservableCollection<ResourceEntity> _selectedEntities = new ObservableCollection<ResourceEntity>();
        private ListCollectionViewListAdapter _filteredResourceEntities;

        private ObservableCompositeCollection<ResourceTableEntry> _resourceTableEntries = new ObservableCompositeCollection<ResourceTableEntry>();
        private IList<ResourceTableEntry> _selectedTableEntries = new List<ResourceTableEntry>();

        private ResourceTableEntry _selectedEntry;
        private string _entityFilter;

        public event EventHandler<LanguageChangingEventArgs> LanguageChanging;
        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;
        public event EventHandler<ResourceBeginEditingEventArgs> BeginEditing;

        public ResourceManager()
        {
            _selectedEntitiesChangeThrottle = new DispatcherThrottle(OnSelectedEntitiesChanged);
            _filteredResourceEntities = new ListCollectionViewListAdapter(new ListCollectionView(_resourceEntities));
        }

        /// <summary>
        /// Loads all resources from the specified project files.
        /// </summary>
        /// <param name="allSourceFiles">All resource x files.</param>
        public void Load(IList<ProjectFile> allSourceFiles)
        {
            Contract.Requires(allSourceFiles != null);

            CodeReference.StopFind();

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

        public IEnumerable<ResourceTableEntry> ResourceTableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<ResourceTableEntry>>() != null);
                return _resourceTableEntries;
            }
        }

        public IEnumerable<CultureInfo> Languages
        {
            get
            {
                return ResourceEntities.SelectMany(entity => entity.Languages).Distinct().Select(lang => lang.Culture);
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
                return CultureInfo.GetCultures(CultureTypes.SpecificCultures).OrderBy(c => c.DisplayName);
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

        public ICommand CopyCommand
        {
            get
            {
                return new DelegateCommand(CanCutOrCopy, CopySelected);
            }
        }

        public ICommand CutCommand
        {
            get
            {
                return new DelegateCommand(CanCutOrCopy, CutSelected);
            }
        }

        public ICommand AddNewCommand
        {
            get
            {
                return new DelegateCommand(CanAddNew, AddNew);
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                return new DelegateCommand(CanDelete, DeleteSelected);
            }
        }

        public ICommand PasteCommand
        {
            get
            {
                return new DelegateCommand(CanPaste, Paste);
            }
        }

        public ICommand ExportExcelSelectedCommand
        {
            get
            {
                return new DelegateCommand(() => _selectedEntities.Any(), () => ExportExcel(_selectedEntities));
            }
        }

        public ICommand ExportExcelAllCommand
        {
            get
            {
                return new DelegateCommand(() => ExportExcel(null));
            }
        }

        public ICommand ImportExcelCommand
        {
            get
            {
                return new DelegateCommand(ImportExcel);
            }
        }

        public ICommand CopyKeysCommand
        {
            get
            {
                return new DelegateCommand(() => _selectedTableEntries.Any(), CopyKeys);
            }
        }

        public ICommand ToggleInvariantCommand
        {
            get
            {
                return new DelegateCommand(() => _selectedTableEntries.Any(), ToggleInvariant);
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

        public void AddNewKey(ResourceEntity entity, string key)
        {
            Contract.Requires(entity != null);
            Contract.Requires(!String.IsNullOrEmpty(key));

            if (!entity.CanEdit(null))
                return;

            var entry = entity.Add(key);

            _selectedTableEntries = new List<ResourceTableEntry> { entry };
            OnPropertyChanged(() => SelectedTableEntries);
        }

        public bool CanEdit(ResourceEntity resourceEntity, CultureInfo language)
        {
            Contract.Requires(resourceEntity != null);

            var eventHandler = BeginEditing;

            if (eventHandler == null)
                return true;

            var args = new ResourceBeginEditingEventArgs(resourceEntity, language);

            eventHandler(this, args);

            return !args.Cancel;
        }

        private bool CanAddNew()
        {
            return SelectedEntities.Count == 1;
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

        private void AddNew()
        {

            if (_selectedEntities.Count != 1)
                return;

            var entity = _selectedEntities[0];

            if (!entity.CanEdit(null))
                return;

            var entry = entity.AddNewKey();

            _selectedTableEntries = new List<ResourceTableEntry> { entry };
            OnPropertyChanged(() => SelectedTableEntries);
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

            try
            {
                switch (entity.ImportTextTable(Clipboard.GetText()))
                {
                    case ImportResult.Partial:
                        MessageBox.Show(Resources.ImportFailedPartiallyError, Resources.Title);
                        break;

                    case ImportResult.None:
                        MessageBox.Show(Resources.ImportFailedError, Resources.Title);
                        break;

                    case ImportResult.All:
                        return;

                    default:
                        throw new InvalidOperationException(@"Undefined import result.");

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Title);
            }
        }

        private void ToggleInvariant()
        {
            var items = _selectedTableEntries.ToList();

            if (!items.Any())
                return;

            var newValue = !items.First().IsInvariant;

            items.ForEach(item => item.IsInvariant = newValue);
        }

        private void ExportExcel(IEnumerable<ResourceEntity> selectedItems)
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
                this.ExportExcel(dlg.FileName, selectedItems);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ImportExcel()
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                CheckFileExists = true,
                DefaultExt = ".xlsx",
                Filter = "Excel Worksheets|*.xlsx|All Files|*.*",
                FilterIndex = 0,
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    this.ImportExcel(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void CopyKeys()
        {
            var selectedKeys = _selectedTableEntries.Select(item => item.Key);

            Clipboard.SetText(string.Join(Environment.NewLine, selectedKeys));
        }

        private void InternalLoad(IEnumerable<IGrouping<string, ProjectFile>> resourceFilesByDirectory)
        {
            Contract.Requires(resourceFilesByDirectory != null);

            var entities = GetResourceEntities(resourceFilesByDirectory)
                .OrderBy(e => e.ProjectName)
                .ThenBy(e => e.BaseName);

            _resourceEntities = new ObservableCollection<ResourceEntity>(entities);
            _filteredResourceEntities = new ListCollectionViewListAdapter(new ListCollectionView(_resourceEntities));
            if (!string.IsNullOrEmpty(_entityFilter))
                ApplyEntityFilter(_entityFilter);

            _selectedEntities.CollectionChanged -= SelectedEntities_CollectionChanged;
            _selectedEntities = new ObservableCollection<ResourceEntity>(_resourceEntities.Where(_selectedEntities.Contains));
            _selectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;

            OnPropertyChanged(() => ResourceEntities);
            OnPropertyChanged(() => FilteredResourceEntities);
            OnPropertyChanged(() => Languages);
            OnPropertyChanged(() => SelectedEntities);

            OnSelectedEntitiesChanged();
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

                        if (resourceEntity.Entries.Any())
                        {
                            resourceEntity.LanguageChanging += ResourceEntity_LanguageChanging;
                            resourceEntity.LanguageChanged += ResourceEntity_LanguageChanged;

                            yield return resourceEntity;
                        }
                    }
                }
            }
        }

        private void ResourceEntity_LanguageChanging(object sender, LanguageChangingEventArgs e)
        {
            if (!CanEdit(e.Entity, e.Language))
            {
                e.Cancel = true;
                return;
            }

            if (LanguageChanging != null)
            {
                LanguageChanging(this, e);
            }
        }

        private void ResourceEntity_LanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            if (LanguageChanged != null)
            {
                LanguageChanged(this, e);
            }
        }

        private void SelectedEntities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _selectedEntitiesChangeThrottle.Tick();
        }

        private void OnSelectedEntitiesChanged()
        {
            var selectedTableEntries = _selectedTableEntries.ToArray();

            _resourceTableEntries = new ObservableCompositeCollection<ResourceTableEntry>(_selectedEntities.Select(entity => (IList)entity.Entries).ToArray());

            _selectedTableEntries = _resourceEntities.SelectMany(entity => entity.Entries)
                .Where(item => selectedTableEntries.Contains(item, ResourceTableEntry.EqualityComparer))
                .Where(item => _resourceTableEntries.Contains(item, ResourceTableEntry.EqualityComparer))
                .ToList();

            OnPropertyChanged(() => ResourceTableEntries);
            OnPropertyChanged(() => SelectedTableEntries);
            OnPropertyChanged(() => AreAllFilesSelected);
        }

        public static bool IsValidLanguageName(string languageName)
        {
            return Array.BinarySearch(SortedCultureNames, languageName, StringComparer.OrdinalIgnoreCase) >= 0;
        }

        private static string[] GetSortedCultureNames()
        {
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            Contract.Assume(allCultures != null);
            var cultureNames = allCultures
                .SelectMany(culture => new[] { culture.IetfLanguageTag, culture.Name })
                .Distinct()
                .ToArray();

            Array.Sort(cultureNames, StringComparer.OrdinalIgnoreCase);

            return cultureNames;
        }

        private void ApplyEntityFilter(string value)
        {
            _entityFilter = value;

            if (!string.IsNullOrEmpty(_entityFilter))
            {
                try
                {
                    var regex = new Regex(_entityFilter, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    _filteredResourceEntities.Filter = item => regex.Match(item.ToString()).Success;
                    return;
                }
                catch (ArgumentException)
                {
                }
            }

            _filteredResourceEntities.Filter = null;
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
        }
    }
}
