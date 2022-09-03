namespace ResXManager.View.Visuals
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Threading;

    using DataGridExtensions;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;
    using ResXManager.View.Properties;
    using ResXManager.View.Tools;

    using Throttle;
    using TomsToolbox.Essentials;
    using TomsToolbox.ObservableCollections;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [Shared]
    [Export]
    [VisualCompositionExport(RegionId.Content, Sequence = 1)]
    public sealed partial class ResourceViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private readonly IConfiguration _configuration;
        private readonly ISourceFilesProvider _sourceFilesProvider;
        private readonly ITracer _tracer;
        private readonly CodeReferenceTracker _codeReferenceTracker;
        private readonly PerformanceTracer _performanceTracer;

        private CancellationTokenSource? _loadingCancellationTokenSource;

        [ImportingConstructor]
        public ResourceViewModel(ResourceManager resourceManager, IConfiguration configuration, ISourceFilesProvider sourceFilesProvider, CodeReferenceTracker codeReferenceTracker, ITracer tracer, PerformanceTracer performanceTracer)
        {
            ResourceManager = resourceManager;
            _configuration = configuration;
            _sourceFilesProvider = sourceFilesProvider;
            _codeReferenceTracker = codeReferenceTracker;
            _tracer = tracer;
            _performanceTracer = performanceTracer;

            ResourceTableEntries = SelectedEntities.ObservableSelectMany(entity => entity.Entries);
            ResourceTableEntries.CollectionChanged += (_, __) => ResourceTableEntries_CollectionChanged();

            resourceManager.LanguageChanged += ResourceManager_LanguageChanged;
        }

        internal event EventHandler<ResourceTableEntryEventArgs>? ClearFiltersRequest;

        public ResourceManager ResourceManager { get; }

        public IObservableCollection<ResourceTableEntry> ResourceTableEntries { get; }

        public ObservableCollection<ResourceEntity> SelectedEntities { get; } = new();

        public ObservableCollection<ResourceTableEntry> SelectedTableEntries { get; } = new();

        public bool IsLoading { get; private set; }

        // Do not use CollectionViewSource in XAML, has huge performance impact when there are many items in the list.
        public CollectionView GroupedResourceTableEntries
        {
            get
            {
                CollectionView collectionView = new ListCollectionView((IList)ResourceTableEntries);

                collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Container"));

                return collectionView;
            }
        }

        public string? LoadedSnapshot { get; set; }

        public static ICommand ToggleCellSelectionCommand => new DelegateCommand(() => Settings.IsCellSelectionEnabled = !Settings.IsCellSelectionEnabled);

        public ICommand CopyCommand => new DelegateCommand<DataGrid>(CanCopy, CopySelected);

        public ICommand CutCommand => new DelegateCommand<DataGrid>(CanCut, CutSelected);

        public ICommand DeleteCommand => new DelegateCommand<DataGrid>(CanDelete, DeleteSelected);

        public ICommand PasteCommand => new DelegateCommand<DataGrid>(CanPaste, Paste);

        public ICommand ExportExcelCommand => new DelegateCommand<IExportParameters>(CanExportExcel, ExportExcel);

        public ICommand ImportExcelCommand => new DelegateCommand<string>(ImportExcel);

        public ICommand ToggleInvariantCommand => new DelegateCommand(() => SelectedTableEntries.Any(), ToggleInvariant);

        public static ICommand ToggleItemInvariantCommand => new DelegateCommand<DataGrid>(CanToggleItemInvariant, ToggleItemInvariant);

        public ICommand ToggleConsistencyCheckCommand => new DelegateCommand<string>(CanToggleConsistencyCheck, ToggleConsistencyCheck);

        public ICommand ReloadCommand => new DelegateCommand(Reload);

        public ICommand SaveCommand => new DelegateCommand(() => ResourceManager.HasChanges, () => ResourceManager.Save());

        public ICommand BeginFindCodeReferencesCommand => new DelegateCommand(BeginFindCodeReferences);

        public ICommand CreateSnapshotCommand => new DelegateCommand<string>(CreateSnapshot);

        public ICommand LoadSnapshotCommand => new DelegateCommand<string>(LoadSnapshot);

        public ICommand UnloadSnapshotCommand => new DelegateCommand(() => LoadSnapshot(null));

        public ICommand SelectEntityCommand => new DelegateCommand<ResourceEntity>(entity =>
        {
            var selectedEntities = SelectedEntities;

            selectedEntities.Clear();
            selectedEntities.Add(entity);
        });

        public int ResourceTableEntryCount => ResourceTableEntries.Count;

        public void AddNewKey(ResourceEntity entity, string key)
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

        public void SelectEntry(ResourceTableEntry entry)
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

        private static Settings Settings => Settings.Default;

        private void LoadSnapshot(string? fileName)
        {
            ResourceManager.LoadSnapshot(fileName.IsNullOrEmpty() ? null : File.ReadAllText(fileName));

            LoadedSnapshot = fileName;
        }

        private void CreateSnapshot(string fileName)
        {
            var snapshot = ResourceManager.CreateSnapshot();

            File.WriteAllText(fileName, snapshot);

            LoadedSnapshot = fileName;
        }

        private bool CanCut(DataGrid? dataGrid)
        {
            return CanCopy(dataGrid) && CanDelete(dataGrid);
        }

        private void CutSelected(DataGrid? dataGrid)
        {
            CopySelected(dataGrid);
            DeleteSelected(dataGrid);
        }

        private bool CanCopy(DataGrid? dataGrid)
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

        private void CopySelected(DataGrid? dataGrid)
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

        private bool CanDelete(DataGrid? dataGrid)
        {
            if (dataGrid == null)
                return false;

            if (dataGrid.GetIsEditing())
                return false;

            if (Settings.IsCellSelectionEnabled)
                return dataGrid.GetSelectedVisibleCells().All(cellInfo => cellInfo.IsOfColumnType(ColumnType.Comment, ColumnType.Language));

            return SelectedTableEntries.Any();
        }

        private void DeleteSelected(DataGrid? dataGrid)
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

                    if (cellInfo.Item is ResourceTableEntry resourceTableEntry)
                    {
                        affectedEntries.Add(resourceTableEntry);
                    }
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

        private bool CanPaste(DataGrid? dataGrid)
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

        private void Paste(DataGrid? dataGrid)
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

        private void PasteRows(IList<IList<string>> table)
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

        private static void PasteCells(DataGrid dataGrid, IList<IList<string>> table)
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

        private static void ToggleItemInvariant(DataGrid? dataGrid)
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

        private static bool CanToggleItemInvariant(DataGrid? dataGrid)
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

        private static bool CanExportExcel(IExportParameters? param)
        {
            if (param == null)
                return true; // param will be added by converter when exporting...

            var scope = param.Scope;

            return (scope == null) || (scope.Entries.Any() && (scope.Languages.Any() || scope.Comments.Any()));
        }

        private void ExportExcel(IExportParameters? param)
        {
            var fileName = param?.FileName;
            if (fileName != null)
            {
                ResourceManager.ExportExcelFile(fileName, param.Scope, _configuration.ExcelExportMode);
            }
        }

        private void ImportExcel(string? fileName)
        {
            if (fileName.IsNullOrEmpty())
                return;

            var changes = ResourceManager.ImportExcelFile(fileName);

            changes.Apply();
        }

        private async void Reload()
        {
            try
            {
                await ReloadAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
        }

        public async Task ReloadAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Interlocked.Exchange(ref _loadingCancellationTokenSource, cancellationTokenSource)?.Cancel();

            try
            {
                IsLoading = true;

                using (_performanceTracer.Start("ResourceManager.Load"))
                {
                    var solutionFolder = _sourceFilesProvider.SolutionFolder;

                    var sourceFiles = await _sourceFilesProvider.GetSourceFilesAsync(cancellationToken).ConfigureAwait(true);

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    _codeReferenceTracker.StopFind();

                    await ResourceManager.ReloadAsync(solutionFolder, sourceFiles, cancellationToken).ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
            finally
            {
                if (Interlocked.CompareExchange(ref _loadingCancellationTokenSource, null, cancellationTokenSource) == cancellationTokenSource)
                {
                    IsLoading = false;
                }

                cancellationTokenSource.Dispose();
            }
        }

        private void BeginFindCodeReferences()
        {
            _codeReferenceTracker.BeginFind();
        }

        private void ResourceManager_LanguageChanged(object? sender, LanguageEventArgs e)
        {
            if (!_configuration.SaveFilesImmediatelyUponChange)
                return;

            var language = e.Language;

            // Defer save to avoid repeated file access
            _dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
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

        public void Dispose()
        {
            Interlocked.Exchange(ref _loadingCancellationTokenSource, null)?.Dispose();
        }
    }
}
