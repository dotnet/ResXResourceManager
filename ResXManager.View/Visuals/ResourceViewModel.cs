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
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Tools;

    using Throttle;

    using TomsToolbox.Desktop;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [Export]
    [VisualCompositionExport(RegionId.Content, Sequence = 1)]
    public class ResourceViewModel : ObservableObject
    {
        [NotNull]
        private readonly Configuration _configuration;
        [NotNull]
        private readonly ISourceFilesProvider _sourceFilesProvider;
        [NotNull]
        private readonly ITracer _tracer;
        [NotNull]
        private readonly CodeReferenceTracker _codeReferenceTracker;
        [NotNull]
        private readonly PerformanceTracer _performanceTracer;

        [ImportingConstructor]
        public ResourceViewModel([NotNull] ResourceManager resourceManager, [NotNull] Configuration configuration, [NotNull] ISourceFilesProvider sourceFilesProvider, [NotNull] CodeReferenceTracker codeReferenceTracker, [NotNull] ITracer tracer, [NotNull] PerformanceTracer performanceTracer)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(configuration != null);
            Contract.Requires(sourceFilesProvider != null);
            Contract.Requires(codeReferenceTracker != null);
            Contract.Requires(tracer != null);
            Contract.Requires(performanceTracer != null);

            ResourceManager = resourceManager;
            _configuration = configuration;
            _sourceFilesProvider = sourceFilesProvider;
            _codeReferenceTracker = codeReferenceTracker;
            _tracer = tracer;
            _performanceTracer = performanceTracer;

            ResourceTableEntries = SelectedEntities.ObservableSelectMany(entity => entity.Entries);
            ResourceTableEntries.CollectionChanged += (_, __) => ResourceTableEntries_CollectionChanged();

            resourceManager.TableEntries.CollectionChanged += (_, __) => BeginFindCodeReferences();
            resourceManager.LanguageChanged += ResourceManager_LanguageChanged;
        }

        [NotNull]
        public ResourceManager ResourceManager { get; }

        [NotNull, ItemNotNull]
        public IObservableCollection<ResourceTableEntry> ResourceTableEntries { get; }

        [NotNull, ItemNotNull]
        public ObservableCollection<ResourceEntity> SelectedEntities { get; } = new ObservableCollection<ResourceEntity>();

        [NotNull, ItemNotNull]
        public ObservableCollection<ResourceTableEntry> SelectedTableEntries { get; } = new ObservableCollection<ResourceTableEntry>();

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

        [CanBeNull]
        public string LoadedSnapshot { get; set; }

        [NotNull]
        public static ICommand ToggleCellSelectionCommand => new DelegateCommand(() => Settings.IsCellSelectionEnabled = !Settings.IsCellSelectionEnabled);

        [NotNull]
        public ICommand CopyCommand => new DelegateCommand<DataGrid>(CanCopy, CopySelected);

        [NotNull]
        public ICommand CutCommand => new DelegateCommand(CanCut, CutSelected);

        [NotNull]
        public ICommand DeleteCommand => new DelegateCommand<DataGrid>(CanDelete, DeleteSelected);

        [NotNull]
        public ICommand PasteCommand => new DelegateCommand<DataGrid>(CanPaste, Paste);

        [NotNull]
        public ICommand ExportExcelCommand => new DelegateCommand<IExportParameters>(CanExportExcel, ExportExcel);

        [NotNull]
        public ICommand ImportExcelCommand => new DelegateCommand<string>(ImportExcel);

        [NotNull]
        public ICommand ToggleInvariantCommand => new DelegateCommand(() => SelectedTableEntries.Any(), ToggleInvariant);

        [NotNull]
        public static ICommand ToggleItemInvariantCommand => new DelegateCommand<DataGrid>(CanToggleItemInvariant, ToggleItemInvariant);

        [NotNull]
        public ICommand ReloadCommand => new DelegateCommand(ForceReload);

        [NotNull]
        public ICommand SaveCommand => new DelegateCommand(() => ResourceManager.HasChanges, () => ResourceManager.Save());

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
                    var selectedEntities = SelectedEntities;

                    selectedEntities.Clear();
                    selectedEntities.Add(entity);
                });
            }
        }

        public int ResourceTableEntryCount => ResourceTableEntries.Count;

        public void AddNewKey([NotNull] ResourceEntity entity, [NotNull] string key)
        {
            Contract.Requires(entity != null);
            Contract.Requires(!string.IsNullOrEmpty(key));

            if (!entity.CanEdit(null))
                return;

            var entry = entity.Add(key);
            if (entry == null)
                return;

            ResourceManager.ReloadSnapshot();

            SelectedTableEntries.Clear();
            SelectedTableEntries.Add(entry);
        }

        [NotNull]
        // ReSharper disable once AssignNullToNotNullAttribute
        private static Settings Settings => Settings.Default;

        private void LoadSnapshot(string fileName)
        {
            ResourceManager.LoadSnapshot(string.IsNullOrEmpty(fileName) ? null : File.ReadAllText(fileName));

            LoadedSnapshot = fileName;
        }

        private void CreateSnapshot([NotNull] string fileName)
        {
            Contract.Requires(fileName != null);

            var snapshot = ResourceManager.CreateSnapshot();

            File.WriteAllText(fileName, snapshot);

            LoadedSnapshot = fileName;
        }

        private bool CanDelete(DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            return (dataGrid.SelectionUnit == DataGridSelectionUnit.CellOrRowHeader) && dataGrid.SelectedCells.Any(cellInfo => cellInfo.IsOfColumnType(ColumnType.Comment, ColumnType.Language))
                || SelectedTableEntries.Any();
        }

        private bool CanCut()
        {
            var entries = SelectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return false;

            // Only allow is all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private bool CanCopy(DataGrid dataGrid)
        {
            var entries = SelectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return dataGrid.HasRectangularCellSelection(); // cell selection

            // Only allow if all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private void CutSelected()
        {
            var selectedItems = SelectedTableEntries.ToList();

            var resourceFiles = selectedItems.Select(item => item.Container).Distinct();

            if (resourceFiles.Any(resourceFile => !ResourceManager.CanEdit(resourceFile, null)))
                return;

            selectedItems.ToTable().SetClipboardData();

            selectedItems.ForEach(item => item.Container.Remove(item));
        }

        private void CopySelected([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var selectedItems = SelectedTableEntries.ToArray();

            if (selectedItems.Length == 0)
            {
                dataGrid.GetCellSelection().SetClipboardData();
            }
            else
            {
                selectedItems.ToTable().SetClipboardData();
            }
        }

        private void DeleteSelected([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            if (dataGrid.SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
            {
                var affectedEntries = new HashSet<ResourceTableEntry>();

                foreach (var cellInfo in dataGrid.SelectedCells)
                {
                    if (!cellInfo.IsOfColumnType(ColumnType.Comment, ColumnType.Language))
                        continue;

                    cellInfo.Column?.OnPastingCellClipboardContent(cellInfo.Item, string.Empty);

                    affectedEntries.Add(cellInfo.Item as ResourceTableEntry);
                }

                dataGrid.CommitEdit();
                dataGrid.CommitEdit();

                foreach (var entry in affectedEntries)
                {
                    entry?.Refresh();
                }
            }
            else
            {
                var selectedItems = SelectedTableEntries.ToList();

                if (selectedItems.Count == 0)
                    return;

                var resourceFiles = selectedItems.Select(item => item.Container).Distinct();

                if (resourceFiles.Any(resourceFile => !ResourceManager.CanEdit(resourceFile, null)))
                    return;

                selectedItems.ForEach(item => item.Container.Remove(item));
            }
        }

        private bool CanPaste(DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            if (!Clipboard.ContainsText())
                return false;

            if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                return dataGrid.HasRectangularCellSelection();

            return SelectedEntities.Count == 1;
        }

        private void Paste([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var table = ClipboardHelper.GetClipboardDataAsTable();
            if (table == null)
                throw new ImportException(Resources.ImportNormalizedTableExpected);

            if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
            {
                PasteCells(dataGrid, table);
            }
            else
            {
                PasteRows(table);
            }
        }

        private void PasteRows([NotNull, ItemNotNull] IList<IList<string>> table)
        {
            Contract.Requires(table != null);

            var selectedEntities = SelectedEntities.ToList();

            if (selectedEntities.Count != 1)
                return;

            var entity = selectedEntities[0];

            Contract.Assume(entity != null);

            if (!ResourceManager.CanEdit(entity, null))
                return;

            try
            {
                if (table.HasValidTableHeaderRow())
                {
                    entity.ImportTable(table);
                }
                else
                {
                    throw new ImportException(Resources.PasteSelectionSizeMismatch);
                }
            }
            catch (ImportException ex)
            {
                throw new ImportException(Resources.PasteFailed + " " + ex.Message);
            }
        }

        private static void PasteCells([NotNull] DataGrid dataGrid, [NotNull, ItemNotNull] IList<IList<string>> table)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(table != null);

            if (dataGrid.SelectedCells.Any(cell => (cell.Item as ResourceTableEntry)?.Container.CanEdit((cell.Column?.Header as ILanguageColumnHeader)?.CultureKey) == false))
                return;

            if (!dataGrid.PasteCells(table))
                throw new ImportException(Resources.PasteSelectionSizeMismatch);
        }

        private void ToggleInvariant()
        {
            var items = SelectedTableEntries.ToArray();

            if (!items.Any())
                return;

            var first = items.First();
            if (first == null)
                return;

            var newValue = !first.IsInvariant;

            foreach (var item in items)
            {
                Contract.Assume(item != null);

                if (!item.CanEdit(item.NeutralLanguage.CultureKey))
                    return;

                item.IsInvariant = newValue;
            }
        }

        private static void ToggleItemInvariant([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var cellInfos = dataGrid.SelectedCells;
            if (cellInfos == null)
                return;

            var isInvariant = !cellInfos.Any(item => item.IsItemInvariant());

            foreach (var info in cellInfos)
            {
                var col = info.Column?.Header as ILanguageColumnHeader;

                if (col?.ColumnType != ColumnType.Language)
                    continue;

                var item = info.Item as ResourceTableEntry;

                if (item?.CanEdit(col.CultureKey) != true)
                    return;

                item.IsItemInvariant.SetValue(col.CultureKey, isInvariant);
            }
        }

        private static bool CanToggleItemInvariant([NotNull] DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            return dataGrid.SelectedCells?.Any(cell => (cell.Column?.Header as ILanguageColumnHeader)?.ColumnType == ColumnType.Language) ?? false;
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

            var fileName = param.FileName;
            if (fileName != null)
            {
                ResourceManager.ExportExcelFile(fileName, param.Scope, _configuration.ExcelExportMode);
            }
        }

        private void ImportExcel([NotNull] string fileName)
        {
            Contract.Requires(fileName != null);

            var changes = ResourceManager.ImportExcelFile(fileName);

            changes.Apply();
        }

        private void ForceReload()
        {
            _sourceFilesProvider.Invalidate();
            Reload(true);
        }

        public void Reload()
        {
            Reload(false);
        }

        private void Reload(bool forceFindCodeReferences)
        {
            try
            {
                using (_performanceTracer.Start("ResourceManager.Load"))
                {
                    var sourceFiles = _sourceFilesProvider.SourceFiles;

                    _codeReferenceTracker.StopFind();

                    if (ResourceManager.Reload(sourceFiles) || forceFindCodeReferences)
                    {
                        BeginFindCodeReferences();
                    }

                    _configuration.Reload();
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
        }

        [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.ContextIdle)]
        private void BeginFindCodeReferences()
        {
            BeginFindCodeReferences(_sourceFilesProvider.SourceFiles);
        }

        private void BeginFindCodeReferences([NotNull, ItemNotNull] IList<ProjectFile> allSourceFiles)
        {
            Contract.Requires(allSourceFiles != null);

            _codeReferenceTracker.StopFind();

            if (Model.Properties.Settings.Default.IsFindCodeReferencesEnabled)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
                {
                    _codeReferenceTracker.BeginFind(ResourceManager, _configuration.CodeReferences, allSourceFiles, _tracer);
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

                    language.Save();
                }
                catch (Exception ex)
                {
                    _tracer.TraceError(ex.ToString());
                    MessageBox.Show(ex.Message, Resources.Title);
                }
            });
        }

        [Throttled(typeof(DispatcherThrottle))]
        private void ResourceTableEntries_CollectionChanged()
        {
            OnPropertyChanged(nameof(ResourceTableEntryCount));
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
            Contract.Invariant(ResourceManager != null);
            Contract.Invariant(_configuration != null);
            Contract.Invariant(_sourceFilesProvider != null);
            Contract.Invariant(_tracer != null);
            Contract.Invariant(_codeReferenceTracker != null);
            Contract.Invariant(SelectedEntities != null);
            Contract.Invariant(ResourceTableEntries != null);
            Contract.Invariant(SelectedTableEntries != null);
            Contract.Invariant(_performanceTracer != null);
        }
    }
}
