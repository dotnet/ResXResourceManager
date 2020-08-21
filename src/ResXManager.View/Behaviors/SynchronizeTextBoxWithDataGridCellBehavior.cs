namespace ResXManager.View.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Markup;

    using Microsoft.Xaml.Behaviors;

    using ResXManager.View.ColumnHeaders;

    public class SynchronizeTextBoxWithDataGridCellBehavior : Behavior<TextBox>
    {
        public DataGrid? DataGrid
        {
            get => (DataGrid)GetValue(DataGridProperty);
            set => SetValue(DataGridProperty, value);
        }
        /// <summary>
        /// Identifies the DataGrid dependency property
        /// </summary>
        public static readonly DependencyProperty DataGridProperty =
            DependencyProperty.Register("DataGrid", typeof(DataGrid), typeof(SynchronizeTextBoxWithDataGridCellBehavior), new FrameworkPropertyMetadata(null, (sender, e) => ((SynchronizeTextBoxWithDataGridCellBehavior)sender).DataGrid_Changed((DataGrid)e.OldValue, (DataGrid)e.NewValue)));

        private void DataGrid_Changed(DataGrid? oldValue, DataGrid? newValue)
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

        private TextBox? TextBox => AssociatedObject;

        private void DataGrid_CurrentCellChanged(object? sender, EventArgs e)
        {
            var textBox = TextBox;
            if (textBox == null)
                return;

            if (!(sender is DataGrid dataGrid))
                return;

            var currentCell = dataGrid.CurrentCell;

            if (!(currentCell.Column is DataGridBoundColumn column))
                return;

            if (column.Header is ILanguageColumnHeader header)
            {
                textBox.IsHitTestVisible = true;
                textBox.DataContext = currentCell.Item;

                var ieftLanguageTag = header.EffectiveCulture.IetfLanguageTag;
                textBox.Language = XmlLanguage.GetLanguage(ieftLanguageTag);

                BindingOperations.SetBinding(textBox, TextBox.TextProperty, column.Binding);
            }
            else
            {
                textBox.IsHitTestVisible = false;
                textBox.DataContext = null;
                BindingOperations.ClearBinding(textBox, TextBox.TextProperty);
            }
        }
    }
}
