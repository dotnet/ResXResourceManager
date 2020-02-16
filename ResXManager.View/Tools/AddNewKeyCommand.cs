namespace ResXManager.View.Tools
{
    using System;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Windows;

    using JetBrains.Annotations;

    using ResXManager.View.Properties;
    using ResXManager.View.Visuals;

    using TomsToolbox.Composition;
    using TomsToolbox.Wpf;

    [Export]
    internal class AddNewKeyCommand : DelegateCommand<DependencyObject>
    {
        [NotNull]
        private readonly ResourceViewModel _resourceViewModel;
        [NotNull]
        private readonly IExportProvider _exportProvider;

        [ImportingConstructor]
        public AddNewKeyCommand([NotNull] ResourceViewModel resourceViewModel, [NotNull] IExportProvider exportProvider)
        {
            _resourceViewModel = resourceViewModel;
            _exportProvider = exportProvider;

            ExecuteCallback = InternalExecute;
        }

        private void InternalExecute([CanBeNull] DependencyObject parameter)
        {
            if (_resourceViewModel.SelectedEntities.Count() != 1)
            {
                MessageBox.Show(Resources.NeedSingleEntitySelection, Resources.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var resourceFile = _resourceViewModel.SelectedEntities.Single();

            if (resourceFile.IsWinFormsDesignerResource)
            {
                if (MessageBox.Show(Resources.AddEntryToWinFormsResourceWarning, Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    return;
            }

            if (!resourceFile.CanEdit(null))
                return;

            var application = Application.Current;

            var owner = parameter != null ? Window.GetWindow(parameter) : application.MainWindow;

            var inputBox = new InputBox(_exportProvider)
            {
                Title = Resources.Title,
                Prompt = Resources.NewKeyPrompt,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            inputBox.TextChanged += (_, args) =>
                inputBox.IsInputValid = !string.IsNullOrWhiteSpace(args?.Text) 
                                        && !resourceFile.Entries.Any(entry => entry.Key.Equals(args.Text, StringComparison.OrdinalIgnoreCase))
                                        && !args.Text.Equals(resourceFile.BaseName, StringComparison.OrdinalIgnoreCase);

            if (inputBox.ShowDialog() != true)
                return;

            var key = inputBox.Text;
            // ReSharper disable once PossibleNullReferenceException
            key = key.Trim();

            try
            {
                _resourceViewModel.AddNewKey(resourceFile, key);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Resources.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}