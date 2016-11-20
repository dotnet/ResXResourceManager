namespace tomenglertde.ResXManager.View.Behaviors
{
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    public class DataGridTryBeginEditBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.BeginningEdit += DataGrid_BeginningEdit;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            Contract.Assume(AssociatedObject != null);

            AssociatedObject.BeginningEdit -= DataGrid_BeginningEdit;
        }

        private static void DataGrid_BeginningEdit([NotNull] object sender, [NotNull] DataGridBeginningEditEventArgs e)
        {
            Contract.Requires(sender != null);
            Contract.Requires(e.Row != null);
            Contract.Requires(e.Row.Item != null);
            Contract.Requires(e.Column != null);

            var dataGridRow = e.Row;
            var entry = (ResourceTableEntry)dataGridRow.Item;
            var resourceEntity = entry.Container;

            var resourceLanguages = resourceEntity.Languages;
            if (!resourceLanguages.Any())
                return;

            var cultureKey = resourceLanguages.First()?.CultureKey;

            var languageHeader = e.Column.Header as ILanguageColumnHeader;
            if (languageHeader != null)
            {
                cultureKey = languageHeader.CultureKey;
            }

            if (!resourceEntity.CanEdit(cultureKey))
            {
                e.Cancel = true;
            }
        }
    }
}
