namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;

    using DataGridExtensions;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [Export]
    [VisualCompositionExport(RegionId.Content, Sequence = 1)]
    public class ResourceViewModel : ObservableObject
    {
        private readonly DispatcherThrottle _resourceTableEntiyCountUpdateThrottle;
        private readonly ResourceManager _resourceManager;
        private readonly Configuration _configuration;
        private readonly ISourceFilesProvider _sourceFilesProvider;
        private readonly ITracer _tracer;
        private readonly CodeReferenceTracker _codeReferenceTracker;
        private readonly DispatcherThrottle _restartFindCodeReferencesThrottle;
        private readonly ObservableCollection<ResourceEntity> _selectedEntities = new ObservableCollection<ResourceEntity>();
        private readonly IObservableCollection<ResourceTableEntry> _resourceTableEntries;
        private readonly ObservableCollection<ResourceTableEntry> _selectedTableEntries = new ObservableCollection<ResourceTableEntry>();

        private string _loadedSnapshot;
        private bool _isCellSelectionEnabled;

        [ImportingConstructor]
        public ResourceViewModel(ResourceManager resourceManager, Configuration configuration, ISourceFilesProvider sourceFilesProvider, CodeReferenceTracker codeReferenceTracker, ITracer tracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);
            Contract.Requires(sourceFilesProvider != null);
            Contract.Requires(codeReferenceTracker != null);
            Contract.Requires(tracer != null);

            _resourceManager = resourceManager;
            _configuration = configuration;
            _sourceFilesProvider = sourceFilesProvider;
            _codeReferenceTracker = codeReferenceTracker;
            _tracer = tracer;

            _resourceTableEntiyCountUpdateThrottle = new DispatcherThrottle(() => OnPropertyChanged(nameof(ResourceTableEntryCount)));

            _resourceTableEntries = _selectedEntities.ObservableSelectMany(entity => entity.Entries);
            _resourceTableEntries.CollectionChanged += (_, __) => _resourceTableEntiyCountUpdateThrottle.Tick();

            _restartFindCodeReferencesThrottle = new DispatcherThrottle(DispatcherPriority.ContextIdle, () => BeginFindCodeReferences(sourceFilesProvider.SourceFiles));

            resourceManager.TableEntries.CollectionChanged += (_, __) => _restartFindCodeReferencesThrottle.Tick();
        }

        public ResourceManager ResourceManager
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceManager>() != null);

                return _resourceManager;
            }
        }

        public IObservableCollection<ResourceTableEntry> ResourceTableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<ResourceTableEntry>>() != null);

                return _resourceTableEntries;
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

        public CollectionView GroupedResourceTableEntries
        {
            get
            {
                CollectionView collectionView = new ListCollectionView((IList)ResourceTableEntries);

                // ReSharper disable once PossibleNullReferenceException
                collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Container"));

                return collectionView;
            }
        }

        public string LoadedSnapshot
        {
            get
            {
                return _loadedSnapshot;
            }
            set
            {
                SetProperty(ref _loadedSnapshot, value, () => LoadedSnapshot);
            }
        }

        public bool IsCellSelectionEnabled
        {
            get
            {
                return _isCellSelectionEnabled;
            }
            set
            {
                SetProperty(ref _isCellSelectionEnabled, value, () => IsCellSelectionEnabled);
            }
        }

        public ICommand ToggleCellSelectionCommand => new DelegateCommand(() => IsCellSelectionEnabled = !IsCellSelectionEnabled);

        public ICommand CopyCommand => new DelegateCommand<DataGrid>(CanCopy, CopySelected);

        public ICommand CutCommand => new DelegateCommand(CanCut, CutSelected);

        public ICommand DeleteCommand => new DelegateCommand(CanDelete, DeleteSelected);

        public ICommand PasteCommand => new DelegateCommand<DataGrid>(CanPaste, Paste);

        public ICommand ExportExcelCommand => new DelegateCommand<IExportParameters>(CanExportExcel, ExportExcel);

        public ICommand ImportExcelCommand => new DelegateCommand<string>(ImportExcel);

        public ICommand ToggleInvariantCommand => new DelegateCommand(() => _selectedTableEntries.Any(), ToggleInvariant);

        public ICommand ReloadCommand => new DelegateCommand(Reload);

        public ICommand SaveCommand => new DelegateCommand(() => _resourceManager.HasChanges, () => _resourceManager.Save());

        public ICommand BeginFindCodeReferencesCommand => new DelegateCommand(BeginFindCodeReferences);

        public ICommand CreateSnapshotCommand => new DelegateCommand<string>(CreateSnapshot);

        public ICommand LoadSnapshotCommand => new DelegateCommand<string>(LoadSnapshot);

        public ICommand UnloadSnapshotCommand => new DelegateCommand(() => LoadSnapshot(null));

        public ICommand SelectEntityCommand
        {
            get
            {
                return new DelegateCommand<ResourceEntity>(entity =>
                {
                    var selectedEntities = _selectedEntities;

                    selectedEntities.Clear();
                    selectedEntities.Add(entity);
                });
            }
        }

        public int ResourceTableEntryCount => _resourceTableEntries.Count;

        public void AddNewKey(ResourceEntity entity, string key)
        {
            Contract.Requires(entity != null);
            Contract.Requires(!string.IsNullOrEmpty(key));

            if (!entity.CanEdit(null))
                return;

            var entry = entity.Add(key);
            if (entry == null)
                return;

            _resourceManager.ReloadSnapshot();

            _selectedTableEntries.Clear();
            _selectedTableEntries.Add(entry);
        }

        private void LoadSnapshot(string fileName)
        {
            _resourceManager.LoadSnapshot(string.IsNullOrEmpty(fileName) ? null : File.ReadAllText(fileName));

            LoadedSnapshot = fileName;
        }

        private void CreateSnapshot(string fileName)
        {
            var snapshot = _resourceManager.CreateSnapshot();

            File.WriteAllText(fileName, snapshot);

            LoadedSnapshot = fileName;
        }

        private bool CanDelete()
        {
            return _selectedTableEntries.Any();
        }

        private bool CanCut()
        {
            var entries = _selectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return false;

            // Only allow is all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private bool CanCopy(DataGrid dataGrid)
        {
            var entries = _selectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return dataGrid.HasRectangularCellSelection(); // cell selection

            // Only allow if all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private void CutSelected()
        {
            var selectedItems = _selectedTableEntries.ToList();

            var resourceFiles = selectedItems.Select(item => item.Container).Distinct();

            if (resourceFiles.Any(resourceFile => !_resourceManager.CanEdit(resourceFile, null)))
                return;

            selectedItems.ToTable().SetClipboardData();

            selectedItems.ForEach(item => item.Container.Remove(item));
        }

        private void CopySelected(DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var selectedItems = _selectedTableEntries.ToArray();

            if (selectedItems.Length == 0)
            {
                dataGrid.GetCellSelection().SetClipboardData();
            }
            else
            {
                selectedItems.ToTable().SetClipboardData();
            }
        }

        public void DeleteSelected()
        {
            var selectedItems = _selectedTableEntries.ToList();

            if (selectedItems.Count == 0)
                return;

            var resourceFiles = selectedItems.Select(item => item.Container).Distinct();

            if (resourceFiles.Any(resourceFile => !_resourceManager.CanEdit(resourceFile, null)))
                return;

            selectedItems.ForEach(item => item.Container.Remove(item));
        }

        private bool CanPaste(DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            if (!Clipboard.ContainsText())
                return false;

            if (_selectedEntities.Count != 1)
                return false;

            return (dataGrid.SelectedCells?.Any() != true) || dataGrid.HasRectangularCellSelection();
        }

        private void Paste(DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var selectedItems = _selectedEntities.ToList();

            if (selectedItems.Count != 1)
                return;

            var entity = selectedItems[0];

            Contract.Assume(entity != null);

            if (!_resourceManager.CanEdit(entity, null))
                return;

            var table = ClipboardHelper.GetClipboardDataAsTable();
            if (table == null)
                throw new ImportException(Resources.ImportNormalizedTableExpected);

            try
            {
                if (table.HasValidTableHeaderRow())
                {
                    entity.ImportTable(table);
                }
                else
                {
                    if (!dataGrid.PasteCells(table))
                        throw new ImportException(Resources.PasteSelectionSizeMismatch);
                }
            }
            catch (ImportException ex)
            {
                throw new ImportException(Resources.PasteFailed + " " + ex.Message);
            }
        }

        private void ToggleInvariant()
        {
            var items = _selectedTableEntries.ToList();

            if (!items.Any())
                return;

            var first = items.First();
            if (first == null)
                return;

            var newValue = !first.IsInvariant;

            items.ForEach(item => item.IsInvariant = newValue);
        }

        private static bool CanExportExcel(IExportParameters param)
        {
            if (param == null)
                return true;

            var scope = param.Scope;

            return (scope == null) || (scope.Entries.Any() && (scope.Languages.Any() || scope.Comments.Any()));
        }

        private void ExportExcel(IExportParameters param)
        {
            Contract.Requires(param != null);
            Contract.Requires(param.FileName != null);

            _resourceManager.ExportExcelFile(param.FileName, param.Scope, _configuration.ExcelExportMode);
        }

        private void ImportExcel(string fileName)
        {
            Contract.Requires(fileName != null);

            var changes = _resourceManager.ImportExcelFile(fileName);

            changes.Apply();
        }

        public void Reload()
        {
            var sourceFiles = _sourceFilesProvider.SourceFiles;

            _codeReferenceTracker.StopFind();

            _resourceManager.Reload(sourceFiles);

            BeginFindCodeReferences(sourceFiles);
        }

        private void BeginFindCodeReferences()
        {
            BeginFindCodeReferences(_sourceFilesProvider.SourceFiles);
        }

        private void BeginFindCodeReferences(IList<ProjectFile> allSourceFiles)
        {
            Contract.Requires(allSourceFiles != null);

            _codeReferenceTracker.StopFind();

            if (Model.Properties.Settings.Default.IsFindCodeReferencesEnabled)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
                {
                    _codeReferenceTracker.BeginFind(_resourceManager, _configuration.CodeReferences, allSourceFiles, _tracer);
                });
            }
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Main;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_configuration != null);
            Contract.Invariant(_sourceFilesProvider != null);
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_codeReferenceTracker != null);
            Contract.Invariant(_selectedEntities != null);
            Contract.Invariant(_resourceTableEntries != null);
            Contract.Invariant(_selectedTableEntries != null);
        }
    }
}
