namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Properties;
    using tomenglertde.ResXManager.View.Visuals;

    using TomsToolbox.Wpf;

    [Export]
    public class AddNewKeyCommand : DelegateCommand
    {
        private readonly ResourceManager _resourceManager;

        [ImportingConstructor]
        public AddNewKeyCommand(ResourceManager resourceManager)
        {
            Contract.Requires(resourceManager != null);

            _resourceManager = resourceManager;

            CanExecuteCallback = CanExecute;
            ExecuteCallback = Execute;
        }

        private bool CanExecute()
        {
            if (_resourceManager.SelectedEntities.Count != 1)
                return false;

            return _resourceManager.SelectedEntities.Single()?.NeutralProjectFile?.IsWinFormsDesignerResource != true;
        }

        private void Execute()
        {
            if (_resourceManager.SelectedEntities.Count != 1)
                return;

            var resourceFile = _resourceManager.SelectedEntities.Single();

            if (!resourceFile.CanEdit(null))
                return;

            var application = Application.Current;
            Contract.Assume(application != null);

            var inputBox = new InputBox
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
                _resourceManager.AddNewKey(resourceFile, key);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Resources.Title);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_resourceManager != null);
        }
    }
}