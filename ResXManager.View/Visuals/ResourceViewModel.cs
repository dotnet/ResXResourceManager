namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using DataGridExtensions;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [Export]
    [VisualCompositionExport("Content", Sequence = 1)]
    class ResourceViewModel : ObservableObject
    {
        private readonly ResourceManager _resourceManager;
        private string _loadedSnapshot;
        private bool _isCellSelectionEnabled;

        [ImportingConstructor]
        public ResourceViewModel(ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);

            _resourceManager = resourceManager;
        }

        public ResourceManager ResourceManager
        {
            get
            {
                Contract.Ensures(Contract.Result<ResourceManager>() != null);

                return _resourceManager;
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

        public ICommand ToggleCellSelectionCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => IsCellSelectionEnabled = !IsCellSelectionEnabled);
            }
        }

        public ICommand CopyCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<DataGrid>(CanCopy, CopySelected);
            }
        }

        public ICommand CutCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(CanCut, CutSelected);
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

                return new DelegateCommand<DataGrid>(CanPaste, Paste);
            }
        }

        public ICommand ExportExcelCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<IExportParameters>(CanExportExcel, ExportExcel);
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

        public ICommand ToggleInvariantCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => _resourceManager.SelectedTableEntries.Any(), ToggleInvariant);
            }
        }

        public ICommand ReloadCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(_resourceManager.ReloadAndBeginFindCoreReferences);
            }
        }

        public ICommand BeginFindCodeReferencesCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(_resourceManager.BeginFindCoreReferences);
            }
        }

        public ICommand CreateSnapshotCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<string>(CreateSnapshot);
            }
        }

        public ICommand LoadSnapshotCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<string>(LoadSnapshot);
            }
        }

        public ICommand UnloadSnapshotCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => LoadSnapshot(null));
            }
        }

        public ICommand SelectEntityCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand<ResourceEntity>(entity =>
                {
                    var selectedEntities = _resourceManager.SelectedEntities;

                    selectedEntities.Clear();
                    selectedEntities.Add(entity);
                });
            }
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
            return _resourceManager.SelectedTableEntries.Any();
        }

        private bool CanCut()
        {
            var entries = _resourceManager.SelectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return false;

            // Only allow is all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private bool CanCopy(DataGrid dataGrid)
        {
            if (!dataGrid.HasRectangularCellSelection())
                return false;

            var entries = _resourceManager.SelectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return true; // cell selection

            // Only allow if all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private void CutSelected()
        {
            var selectedItems = _resourceManager.SelectedTableEntries.ToList();

            var resourceFiles = selectedItems.Select(item => item.Container).Distinct();

            if (resourceFiles.Any(resourceFile => !_resourceManager.CanEdit(resourceFile, null)))
                return;

            selectedItems.ToTable().SetClipboardData();

            selectedItems.ForEach(item => item.Container.Remove(item));
        }

        private void CopySelected(DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var selectedItems = _resourceManager.SelectedTableEntries.ToArray();

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
            var selectedItems = _resourceManager.SelectedTableEntries.ToList();

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

            if (_resourceManager.SelectedEntities.Count != 1)
                return false;

            return (((dataGrid.SelectedCells == null) || !dataGrid.SelectedCells.Any())
                || dataGrid.HasRectangularCellSelection());
        }

        private void Paste(DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var selectedItems = _resourceManager.SelectedEntities.ToList();

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
            var items = _resourceManager.SelectedTableEntries.ToList();

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

            _resourceManager.ExportExcelFile(param.FileName, param.Scope);
        }

        private void ImportExcel(string fileName)
        {
            Contract.Requires(fileName != null);

            var changes = _resourceManager.ImportExcelFile(fileName);

            changes.Apply();
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Main;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
        }
    }
}
