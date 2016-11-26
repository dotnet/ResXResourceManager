namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
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

    using JetBrains.Annotations;

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
        [NotNull]
        private readonly DispatcherThrottle _resourceTableEntiyCountUpdateThrottle;
        [NotNull]
        private readonly ResourceManager _resourceManager;
        [NotNull]
        private readonly Configuration _configuration;
        [NotNull]
        private readonly ISourceFilesProvider _sourceFilesProvider;
        [NotNull]
        private readonly ITracer _tracer;
        [NotNull]
        private readonly CodeReferenceTracker _codeReferenceTracker;
        [NotNull]
        private readonly DispatcherThrottle _restartFindCodeReferencesThrottle;
        [NotNull, ItemNotNull]
        private readonly ObservableCollection<ResourceEntity> _selectedEntities = new ObservableCollection<ResourceEntity>();
        [NotNull, ItemNotNull]
        private readonly IObservableCollection<ResourceTableEntry> _resourceTableEntries;
        [NotNull, ItemNotNull]
        private readonly ObservableCollection<ResourceTableEntry> _selectedTableEntries = new ObservableCollection<ResourceTableEntry>();
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;

        private string _loadedSnapshot;
        private bool _isCellSelectionEnabled;

        [ImportingConstructor]
        public ResourceViewModel([NotNull] ResourceManager resourceManager, [NotNull] Configuration configuration, [NotNull] ISourceFilesProvider sourceFilesProvider, [NotNull] CodeReferenceTracker codeReferenceTracker, [NotNull] ITracer tracer, [NotNull] PerformanceTracer performanceTracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);
            Contract.Requires(sourceFilesProvider != null);
            Contract.Requires(codeReferenceTracker != null);
            Contract.Requires(tracer != null);
            Contract.Requires(performanceTracer != null);

            _resourceManager = resourceManager;
            _configuration = configuration;
            _sourceFilesProvider = sourceFilesProvider;
            _codeReferenceTracker = codeReferenceTracker;
            _tracer = tracer;
            _performanceTracer = performanceTracer;

            _resourceTableEntiyCountUpdateThrottle = new DispatcherThrottle(() => OnPropertyChanged(nameof(ResourceTableEntryCount)));

            _resourceTableEntries = _selectedEntities.ObservableSelectMany(entity => entity.Entries);
            _resourceTableEntries.CollectionChanged += (_, __) => _resourceTableEntiyCountUpdateThrottle.Tick();

            _restartFindCodeReferencesThrottle = new DispatcherThrottle(DispatcherPriority.ContextIdle, () => BeginFindCodeReferences(sourceFilesProvider.SourceFiles));

            resourceManager.TableEntries.CollectionChanged += (_, __) => _restartFindCodeReferencesThrottle.Tick();

            resourceManager.LanguageChanged += ResourceManager_LanguageChanged;
        }

        [NotNull]
        public ResourceManager ResourceManager
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceManager>() != null);

                return _resourceManager;
            }
        }

        [NotNull]
        public IObservableCollection<ResourceTableEntry> ResourceTableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<IObservableCollection<ResourceTableEntry>>() != null);

                return _resourceTableEntries;
            }
        }

        [NotNull]
        public ObservableCollection<ResourceEntity> SelectedEntities
        {
            get
            {
                Contract.Ensures(Contract.Result<ObservableCollection<ResourceEntity>>() != null);

                return _selectedEntities;
            }
        }

        [NotNull]
        public IList<ResourceTableEntry> SelectedTableEntries
        {
            get
            {
                Contract.Ensures(Contract.Result<IList<ResourceTableEntry>>() != null);

                return _selectedTableEntries;
            }
        }

        [NotNull]
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

        [NotNull]
        public ICommand ToggleCellSelectionCommand => new DelegateCommand(() => IsCellSelectionEnabled = !IsCellSelectionEnabled);

        [NotNull]
        public ICommand CopyCommand => new DelegateCommand<DataGrid>(CanCopy, CopySelected);

        [NotNull]
        public ICommand CutCommand => new DelegateCommand(CanCut, CutSelected);

        [NotNull]
        public ICommand DeleteCommand => new DelegateCommand(CanDelete, DeleteSelected);

        [NotNull]
        public ICommand PasteCommand => new DelegateCommand<DataGrid>(CanPaste, Paste);

        [NotNull]
        public ICommand ExportExcelCommand => new DelegateCommand<IExportParameters>(CanExportExcel, ExportExcel);

        [NotNull]
        public ICommand ImportExcelCommand => new DelegateCommand<string>(ImportExcel);

        [NotNull]
        public ICommand ToggleInvariantCommand => new DelegateCommand(() => _selectedTableEntries.Any(), ToggleInvariant);

        [NotNull]
        public ICommand ReloadCommand => new DelegateCommand(() => Reload(true));

        [NotNull]
        public ICommand SaveCommand => new DelegateCommand(() => _resourceManager.HasChanges, () => _resourceManager.Save(_configuration.EffectiveResXSortingComparison));

        [NotNull]
        public ICommand BeginFindCodeReferencesCommand => new DelegateCommand(BeginFindCodeReferences);

        [NotNull]
        public ICommand CreateSnapshotCommand => new DelegateCommand<string>(CreateSnapshot);

        [NotNull]
        public ICommand LoadSnapshotCommand => new DelegateCommand<string>(LoadSnapshot);

        [NotNull]
        public ICommand UnloadSnapshotCommand => new DelegateCommand(() => LoadSnapshot(null));

        [NotNull]
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

        public void AddNewKey([NotNull] ResourceEntity entity, [NotNull] string key)
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

        private void CreateSnapshot([NotNull] string fileName)
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

        private void CopySelected([NotNull] DataGrid dataGrid)
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

        private void Paste([NotNull] DataGrid dataGrid)
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

        private void ExportExcel([NotNull] IExportParameters param)
        {
            Contract.Requires(param != null);
            Contract.Requires(param.FileName != null);

            _resourceManager.ExportExcelFile(param.FileName, param.Scope, _configuration.ExcelExportMode);
        }

        private void ImportExcel([NotNull] string fileName)
        {
            Contract.Requires(fileName != null);

            var changes = _resourceManager.ImportExcelFile(fileName);

            changes.Apply();
        }

        public void Reload()
        {
            Reload(false);
        }

        public void Reload(bool forceFindCodeReferences)
        {
            try
            {
                using (_performanceTracer.Start("ResourceManager.Load"))
                {
                    var sourceFiles = _sourceFilesProvider.SourceFiles;

                    _codeReferenceTracker.StopFind();

                    if (_resourceManager.Reload(sourceFiles, _configuration.DuplicateKeyHandling) || forceFindCodeReferences)
                    {
                        _restartFindCodeReferencesThrottle.Tick();
                    }

                    _configuration.Reload();
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
        }

        private void BeginFindCodeReferences()
        {
            BeginFindCodeReferences(_sourceFilesProvider.SourceFiles);
        }

        private void BeginFindCodeReferences([NotNull] IList<ProjectFile> allSourceFiles)
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

        private void ResourceManager_LanguageChanged(object sender, LanguageEventArgs e)
        {
            if (!_configuration.SaveFilesImmediatelyUponChange)
                return;

            var language = e.Language;

            // Defer save to avoid repeated file access
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (!language.HasChanges)
                        return;

                    language.Save(_configuration.EffectiveResXSortingComparison);
                }
                catch (Exception ex)
                {
                    _tracer.TraceError(ex.ToString());
                    MessageBox.Show(ex.Message, Resources.Title);
                }
            });
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Main;
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
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
            Contract.Invariant(_performanceTracer != null);
            Contract.Invariant(_restartFindCodeReferencesThrottle != null);
            Contract.Invariant(_resourceTableEntiyCountUpdateThrottle != null);
        }
    }
}
