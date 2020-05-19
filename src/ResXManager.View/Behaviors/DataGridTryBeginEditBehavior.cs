namespace ResXManager.View.Behaviors
{
    using System.Linq;
    using System.Windows.Controls;

    using JetBrains.Annotations;

    using Microsoft.Xaml.Behaviors;

    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;

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

        private static void DataGrid_BeginningEdit([NotNull] object sender, [NotNull] DataGridBeginningEditEventArgs e)
        {
            var dataGridRow = e.Row;
            var entry = (ResourceTableEntry)dataGridRow.Item;
            var resourceEntity = entry.Container;

            var resourceLanguages = resourceEntity.Languages;
            if (!resourceLanguages.Any())
                return;

            var cultureKey = resourceLanguages.First()?.CultureKey;

            if (e.Column.Header is ILanguageColumnHeader languageHeader)
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
