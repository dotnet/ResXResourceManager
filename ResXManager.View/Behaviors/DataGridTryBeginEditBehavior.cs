namespace tomenglertde.ResXManager.View.Behaviors
{
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Interactivity;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    public class DataGridTryBeginEditBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.BeginningEdit += DataGrid_BeginningEdit;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.BeginningEdit -= DataGrid_BeginningEdit;
        }

        private static void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            Contract.Requires(sender != null);
            Contract.Requires(e.Row != null);
            Contract.Requires(e.Row.Item != null);
            Contract.Requires(e.Column != null);

            var dataGridRow = e.Row;
            var entry = (ResourceTableEntry)dataGridRow.Item;
            var resourceEntity = entry.Owner;

            var language = resourceEntity.Languages.First().Culture;

            var languageHeader = e.Column.Header as ILanguageColumnHeader;
            if (languageHeader != null)
            {
                language = languageHeader.Language;
            }

            if (!resourceEntity.CanEdit(language))
            {
                e.Cancel = true;
            }
        }
    }
}
