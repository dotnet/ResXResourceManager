namespace tomenglertde.ResXManager.View.Visuals
{
    using System;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using tomenglertde.ResXManager.Model;

    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class ConfigurationEditor
    {
        public ConfigurationEditor()
        {
            InitializeComponent();
        }

        private void CodeReferencesConfiguration_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            CodeReferencesConfiguration_EndEditing(e.EditAction);
        }

        private void CodeReferencesConfiguration_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            CodeReferencesConfiguration_EndEditing(e.EditAction);
        }

        private void CodeReferencesConfiguration_EndEditing(DataGridEditAction editAction)
        {
            if (editAction != DataGridEditAction.Commit)
                return;

            var viewModel = (ResourceManager)DataContext;
            if (viewModel == null)
                return;

            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)viewModel.Configuration.PersistCodeReferences);
        }
    }
}
