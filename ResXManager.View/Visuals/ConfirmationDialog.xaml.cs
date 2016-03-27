namespace tomenglertde.ResXManager.VSIX.Visuals
{
    using System.ComponentModel.Composition.Hosting;
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

        public ICommand CommitCommand => new DelegateCommand(CanCommit, Commit);

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
