namespace tomenglertde.ResXManager.View.Visuals
{
    using System.ComponentModel.Composition.Hosting;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Input;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;

    /// <summary>
    /// Interaction logic for MoveToResourceDialog.xaml
    /// </summary>
    public partial class ConfirmationDialog
    {
        public ConfirmationDialog(ExportProvider exportProvider)
        {
            this.SetExportProvider(exportProvider);

            InitializeComponent();

            Resources.MergedDictionaries.Add(DataTemplateManager.CreateDynamicDataTemplates(exportProvider));
        }

        public ICommand CommitCommand
        {
            get
            {
                Contract.Ensures(Contract.Result<ICommand>() != null);
                return new DelegateCommand(CanCommit, Commit);
            }
        }

        private void Commit()
        {
            DialogResult = true;
        }

        private bool CanCommit()
        {
            return !this.VisualDescendants().Any(Validation.GetHasError);
        }
    }
}
