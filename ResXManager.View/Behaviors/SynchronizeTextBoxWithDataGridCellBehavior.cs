namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Interactivity;
    using System.Windows.Markup;
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Properties;

    public class SynchronizeTextBoxWithDataGridCellBehavior : Behavior<TextBox>
    {
        public DataGrid DataGrid
        {
            get { return (DataGrid)GetValue(DataGridProperty); }
            set { SetValue(DataGridProperty, value); }
        }
        /// <summary>
        /// Identifies the DataGrid dependency property
        /// </summary>
        public static readonly DependencyProperty DataGridProperty =
            DependencyProperty.Register("DataGrid", typeof(DataGrid), typeof(SynchronizeTextBoxWithDataGridCellBehavior), new FrameworkPropertyMetadata(null, (sender, e) => ((SynchronizeTextBoxWithDataGridCellBehavior)sender).DataGrid_Changed((DataGrid)e.OldValue, (DataGrid)e.NewValue)));

        private void DataGrid_Changed(DataGrid oldValue, DataGrid newValue)
        {
            if (oldValue != null)
            {
                oldValue.CurrentCellChanged -= DataGrid_CurrentCellChanged;
            }

            if (newValue != null)
            {
                newValue.CurrentCellChanged += DataGrid_CurrentCellChanged;
                DataGrid_CurrentCellChanged(newValue, EventArgs.Empty);
            }
        }

        private TextBox TextBox
        {
            get
            {
                return AssociatedObject;
            }
        }

        private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            Contract.Requires(sender != null);

            var dataGrid = (DataGrid)sender;
            var currentCell = dataGrid.CurrentCell;

            var column = currentCell.Column as DataGridBoundColumn;
            if (column == null)
                return;

            var header = column.Header as ILanguageColumnHeader;
            if (header != null)
            {
                TextBox.IsEnabled = true;
                TextBox.DataContext = currentCell.Item;

                var ieftLanguageTag = (header.CultureKey.Culture ?? Settings.Default.NeutralResourceLanguage ?? CultureInfo.InvariantCulture).IetfLanguageTag;
                TextBox.Language = XmlLanguage.GetLanguage(ieftLanguageTag);

                BindingOperations.SetBinding(TextBox, TextBox.TextProperty, column.Binding);
            }
            else
            {
                TextBox.IsEnabled = false;
                TextBox.DataContext = null;
                BindingOperations.ClearBinding(TextBox, TextBox.TextProperty);
            }
        }
    }
}
