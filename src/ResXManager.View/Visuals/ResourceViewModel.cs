namespace ResXManager.View.Visuals
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;

    using DataGridExtensions;

    using JetBrains.Annotations;

    using Throttle;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;
    using ResXManager.View.Properties;
    using ResXManager.View.Tools;

    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.Mef;

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

        internal event EventHandler<ResourceTableEntryEventArgs> ClearFiltersRequest;

        [NotNull]
        public ResourceManager ResourceManager { get; }

        [NotNull, ItemNotNull]
        public IObservableCollection<ResourceTableEntry> ResourceTableEntries { get; }

        [NotNull, ItemNotNull]
        public ObservableCollection<ResourceEntity> SelectedEntities { get; } = new ObservableCollection<ResourceEntity>();

        [NotNull, ItemNotNull]
        public ObservableCollection<ResourceTableEntry> SelectedTableEntries { get; } = new ObservableCollection<ResourceTableEntry>();

        [NotNull]
        [ItemNotNull]
        public CollectionView GroupedResourceTableEntries
        {
            get
            {
                CollectionView collectionView = new ListCollectionView((IList)ResourceTableEntries);

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
        public ICommand CutCommand => new DelegateCommand<DataGrid>(CanCut, CutSelected);

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
        public ICommand ToggleConsistencyCheckCommand => new DelegateCommand<string>(CanToggleConsistencyCheck, ToggleConsistencyCheck);

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
            if (!entity.CanEdit(null))
                return;

            var entry = entity.Add(key);
            if (entry == null)
                return;

            ClearFiltersRequest?.Invoke(this, new ResourceTableEntryEventArgs(entry));

            ResourceManager.ReloadSnapshot();

            SelectedTableEntries.Clear();
            SelectedTableEntries.Add(entry);
        }

        public void SelectEntry([NotNull] ResourceTableEntry entry)
        {
            if (!ResourceManager.TableEntries.Contains(entry))
                return;

            var entity = entry.Container;

            ClearFiltersRequest?.Invoke(this, new ResourceTableEntryEventArgs(entry));

            if (!SelectedEntities.Contains(entity))
                SelectedEntities.Add(entity);

            SelectedTableEntries.Clear();
            SelectedTableEntries.Add(entry);
        }

        [NotNull]
        private static Settings Settings => Settings.Default;

        private void LoadSnapshot([CanBeNull] string fileName)
        {
            ResourceManager.LoadSnapshot(string.IsNullOrEmpty(fileName) ? null : File.ReadAllText(fileName));

            LoadedSnapshot = fileName;
        }

        private void CreateSnapshot([NotNull] string fileName)
        {
            var snapshot = ResourceManager.CreateSnapshot();

            File.WriteAllText(fileName, snapshot);

            LoadedSnapshot = fileName;
        }

        private bool CanCut([CanBeNull] DataGrid dataGrid)
        {
            return CanCopy(dataGrid) && CanDelete(dataGrid);
        }

        private void CutSelected([CanBeNull] DataGrid dataGrid)
        {
            CopySelected(dataGrid);
            DeleteSelected(dataGrid);
        }

        private bool CanCopy([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            if (dataGrid.GetIsEditing())
                return false;

            if (Settings.IsCellSelectionEnabled)
                return dataGrid.HasRectangularCellSelection(); // cell selection

            var entries = SelectedTableEntries;
            var totalNumberOfEntries = entries.Count;
            // Only allow if all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private void CopySelected([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return;

            if (Settings.IsCellSelectionEnabled)
            {
                dataGrid.GetCellSelection().SetClipboardData();
            }
            else
            {
                var selectedItems = SelectedTableEntries;

                selectedItems.ToTable().SetClipboardData();
            }
        }

        private bool CanDelete([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            if (dataGrid.GetIsEditing())
                return false;

            if (Settings.IsCellSelectionEnabled)
                return dataGrid.GetSelectedVisibleCells().All(cellInfo => cellInfo.IsOfColumnType(ColumnType.Comment, ColumnType.Language));

            return SelectedTableEntries.Any();
        }

        private void DeleteSelected([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return;

            if (Settings.IsCellSelectionEnabled)
            {
                var affectedEntries = new HashSet<ResourceTableEntry>();

                foreach (var cellInfo in dataGrid.GetSelectedVisibleCells().ToArray())
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

        private bool CanPaste([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            if (dataGrid.GetIsEditing())
                return false;

            if (!Clipboard.ContainsText())
                return false;

            if (Settings.IsCellSelectionEnabled)
                return dataGrid.HasRectangularCellSelection();

            return SelectedEntities.Count == 1;
        }

        private void Paste([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return;

            var table = ClipboardHelper.GetClipboardDataAsTable();
            if (table == null)
                throw new ImportException(Resources.ImportNormalizedTableExpected);

            if (Settings.IsCellSelectionEnabled)
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
            var selectedEntities = SelectedEntities.ToList();

            if (selectedEntities.Count != 1)
                return;

            var entity = selectedEntities[0];

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
            if (dataGrid.GetSelectedVisibleCells().Any(cell => (cell.Item as ResourceTableEntry)?.Container.CanEdit((cell.Column?.Header as ILanguageColumnHeader)?.CultureKey) == false))
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
                if (!item.CanEdit(item.NeutralLanguage.CultureKey))
                    return;

                item.IsInvariant = newValue;
            }
        }

        private static void ToggleItemInvariant([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return;

            var cellInfos = dataGrid.GetSelectedVisibleCells().ToArray();

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

        private static bool CanToggleItemInvariant([CanBeNull] DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            return dataGrid
                .GetSelectedVisibleCells()
                .Any(cell => (cell.Column?.Header as ILanguageColumnHeader)?.ColumnType == ColumnType.Language);
        }

        private bool CanToggleConsistencyCheck(string ruleId)
        {
            return SelectedTableEntries.Any() && _configuration.Rules.IsEnabled(ruleId);
        }

        private void ToggleConsistencyCheck(string ruleId)
        {
            var items = SelectedTableEntries.ToArray();

            if (!items.Any())
                return;

            var first = items.First();
            if (first == null)
                return;

            var newValue = !first.IsRuleEnabled[ruleId];

            foreach (var item in items)
            {
                if (!item.CanEdit(item.NeutralLanguage.CultureKey))
                    return;

                item.IsRuleEnabled[ruleId] = newValue;
            }
        }

        private static bool CanExportExcel([CanBeNull] IExportParameters param)
        {
            if (param == null)
                return true; // param will be added by converter when exporting...

            var scope = param.Scope;

            return (scope == null) || (scope.Entries.Any() && (scope.Languages.Any() || scope.Comments.Any()));
        }

        private void ExportExcel([CanBeNull] IExportParameters param)
        {
            var fileName = param?.FileName;
            if (fileName != null)
            {
                ResourceManager.ExportExcelFile(fileName, param.Scope, _configuration.ExcelExportMode);
            }
        }

        private void ImportExcel([CanBeNull] string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

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
            try
            {
                BeginFindCodeReferences(_sourceFilesProvider.SourceFiles);
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
        }

        private void BeginFindCodeReferences([NotNull, ItemNotNull] IList<ProjectFile> allSourceFiles)
        {
            _codeReferenceTracker.StopFind();

            if (Model.Properties.Settings.Default.IsFindCodeReferencesEnabled)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
                {
                    _codeReferenceTracker.BeginFind(ResourceManager, _configuration.CodeReferences, allSourceFiles, _tracer);
                });
            }
        }

        private void ResourceManager_LanguageChanged([NotNull] object sender, [NotNull] LanguageEventArgs e)
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
    }
}
