namespace ResXManager.View.Behaviors
{
    using System;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Markup;

    using Microsoft.Xaml.Behaviors;

    using ResXManager.View.ColumnHeaders;

    public class SynchronizeTextBoxWithDataGridCellBehavior : Behavior<TextBox>
    {
        private DataGridBoundColumn? _currentColumn;

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
                oldValue.Columns.CollectionChanged -= DataGrid_ColumnsChanged;
            }

            if (newValue != null)
            {
                newValue.CurrentCellChanged += DataGrid_CurrentCellChanged;
                newValue.Columns.CollectionChanged += DataGrid_ColumnsChanged;

                DataGrid_CurrentCellChanged(newValue, EventArgs.Empty);
            }
        }

        private void DataGrid_ColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var textBox = TextBox;
            if (textBox == null)
                return;

            var dataGrid = DataGrid;
            if (dataGrid == null)
                return;

            if (!dataGrid.Columns.Contains(_currentColumn))
            {
                ClearBinding(textBox);
            }
        }

        private TextBox? TextBox => AssociatedObject;

        private void DataGrid_CurrentCellChanged(object? sender, EventArgs e)
        {
            var textBox = TextBox;
            if (textBox == null)
                return;

            if (sender is not DataGrid dataGrid)
                return;

            var currentCell = dataGrid.CurrentCell;

            if (currentCell.Column is not DataGridBoundColumn column)
            {
                if (!dataGrid.Columns.Contains(_currentColumn))
                {
                    ClearBinding(textBox);
                }
                return;
            }

            if (column.Header is ILanguageColumnHeader header)
            {
                _currentColumn = column;

                textBox.IsHitTestVisible = true;
                textBox.DataContext = currentCell.Item;

                var ieftLanguageTag = header.EffectiveCulture.IetfLanguageTag;
                textBox.Language = XmlLanguage.GetLanguage(ieftLanguageTag);

                BindingOperations.SetBinding(textBox, TextBox.TextProperty, column.Binding);
            }
            else
            {
                ClearBinding(textBox);
            }
        }

        private void ClearBinding(TextBox textBox)
        {
            _currentColumn = null;

            textBox.IsHitTestVisible = false;
            textBox.DataContext = null;
            BindingOperations.ClearBinding(textBox, TextBox.TextProperty);
        }
    }
}
