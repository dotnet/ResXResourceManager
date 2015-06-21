namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;
    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    [VisualCompositionExport("Content", Sequence = 1)]
    class ResourceViewModel : ObservableObject, IComposablePart
    {
        private readonly ResourceManager _resourceManager;

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
                return _resourceManager;
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

        public ICommand CopyKeysCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);

                return new DelegateCommand(() => _resourceManager.SelectedTableEntries.Any(), CopyKeys);
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

                return new DelegateCommand(_resourceManager.Reload);
            }
        }

        private bool CanDelete()
        {
            return _resourceManager.SelectedTableEntries.Any();
        }

        private bool CanCutOrCopy()
        {
            var entries = _resourceManager.SelectedTableEntries;

            var totalNumberOfEntries = entries.Count;
            if (totalNumberOfEntries == 0)
                return false;

            // Only allow is all keys are different.
            var numberOfDistinctEntries = entries.Select(e => e.Key).Distinct().Count();

            return numberOfDistinctEntries == totalNumberOfEntries;
        }

        private bool CanPaste()
        {
            return _resourceManager.SelectedEntities.Count == 1;
        }

        private void CutSelected()
        {
            var selectedItems = _resourceManager.SelectedTableEntries.ToList();

            var resourceFiles = selectedItems.Select(item => item.Owner).Distinct();

            if (resourceFiles.Any(resourceFile => !_resourceManager.CanEdit(resourceFile, null)))
                return;

            Clipboard.SetText(selectedItems.ToTextTable());

            selectedItems.ForEach(item => item.Owner.Remove(item));
        }

        private void CopySelected()
        {
            var selectedItems = _resourceManager.SelectedTableEntries.ToList();

            var entries = selectedItems.Cast<ResourceTableEntry>().ToArray();
            Clipboard.SetText(entries.ToTextTable());
        }

        public void DeleteSelected()
        {
            var selectedItems = _resourceManager.SelectedTableEntries.ToList();

            if (selectedItems.Count == 0)
                return;

            var resourceFiles = selectedItems.Select(item => item.Owner).Distinct();

            if (resourceFiles.Any(resourceFile => !_resourceManager.CanEdit(resourceFile, null)))
                return;

            selectedItems.ForEach(item => item.Owner.Remove(item));
        }

        private void Paste()
        {
            var selectedItems = _resourceManager.SelectedEntities.ToList();

            if (selectedItems.Count != 1)
                return;

            var entity = selectedItems[0];

            Contract.Assume(entity != null);

            if (!_resourceManager.CanEdit(entity, null))
                return;

            entity.ImportTextTable(Clipboard.GetText());
        }

        private void ToggleInvariant()
        {
            var items = _resourceManager.SelectedTableEntries.ToList();

            if (!items.Any())
                return;

            var newValue = !items.First().IsInvariant;

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

            _resourceManager.ImportExcelFile(fileName);
        }

        private void CopyKeys()
        {
            var selectedKeys = _resourceManager.SelectedTableEntries.Select(item => item.Key);

            Clipboard.SetText(string.Join(Environment.NewLine, selectedKeys));
        }

        public override string ToString()
        {
            return Resources.ShellTabHeader_Main;
        }
    }
}
