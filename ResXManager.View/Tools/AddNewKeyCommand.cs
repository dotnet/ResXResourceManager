namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Visuals;

    using TomsToolbox.Wpf;

    [Export]
    internal class AddNewKeyCommand : DelegateCommand
    {
        private readonly ResourceManager _resourceManager;
        private readonly ResourceViewModel _resourceViewModel;
        private readonly ExportProvider _exportProvider;

        [ImportingConstructor]
        public AddNewKeyCommand(ResourceManager resourceManager, ResourceViewModel resourceViewModel, ExportProvider exportProvider)
        {
            Contract.Requires(resourceManager != null);
            Contract.Requires(resourceViewModel != null);
            Contract.Requires(exportProvider != null);

            _resourceManager = resourceManager;
            _resourceViewModel = resourceViewModel;
            _exportProvider = exportProvider;

            ExecuteCallback = Execute;
        }

        private void Execute()
        {
            if (_resourceViewModel.SelectedEntities.Count() != 1)
            {
                MessageBox.Show(Resources.NeedSingleEntitySelection, Resources.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var resourceFile = _resourceViewModel.SelectedEntities.Single();
            Contract.Assume(resourceFile != null);

            if (resourceFile.IsWinFormsDesignerResource)
            {
                if (MessageBox.Show(Resources.AddEntryToWinFormsResourceWarning, Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    return;
            }

            if (!resourceFile.CanEdit(null))
                return;

            var application = Application.Current;
            Contract.Assume(application != null);

            var inputBox = new InputBox(_exportProvider)
            {
                Title = Resources.Title,
                Prompt = Resources.NewKeyPrompt,
                Owner = application.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            inputBox.TextChanged += (_, args) =>
                inputBox.IsInputValid = !string.IsNullOrWhiteSpace(args.Text) && !resourceFile.Entries.Any(entry => entry.Key.Equals(args.Text, StringComparison.OrdinalIgnoreCase));

            if (inputBox.ShowDialog() != true)
                return;

            var key = inputBox.Text;
            Contract.Assume(!string.IsNullOrWhiteSpace(key));
            key = key.Trim();
            Contract.Assume(!string.IsNullOrEmpty(key));

            try
            {
                _resourceViewModel.AddNewKey(resourceFile, key);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Resources.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
            Contract.Invariant(_resourceViewModel != null);
            Contract.Invariant(_exportProvider != null);
        }
    }
}