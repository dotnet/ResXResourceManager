namespace tomenglertde.ResXManager.View.Tools
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    using TomsToolbox.Core;

    public static class DataGridHelper
    {
        public static bool HasRectangularCellSelection(this DataGrid dataGrid)
        {
            if (dataGrid == null)
                return false;

            var selectedCells = dataGrid.SelectedCells;
            if (selectedCells == null)
                return false;

            selectedCells = selectedCells
                .Where(c => c.Column.Visibility == Visibility.Visible)
                .ToArray();

            if (!selectedCells.Any())
                return false;

            var visibleColumnIndexes = dataGrid.Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .Select(c => c.DisplayIndex)
                .ToArray();

            var rowIndexes = selectedCells
                .Select(cell => cell.Item)
                .Distinct()
                .Select(item => dataGrid.Items.IndexOf(item))
                .ToArray();

            var columnIndexes = selectedCells
                .Select(c => c.Column.DisplayIndex)
                .Distinct()
                .Select(i => visibleColumnIndexes.IndexOf(i))
                .ToArray();

            var rows = rowIndexes.Max() - rowIndexes.Min() + 1;
            var columns = columnIndexes.Max() - columnIndexes.Min() + 1;

            return selectedCells.Count == rows * columns;
        }

        public static IList<IList<string>> GetCellSelection(this DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var selectedCells = dataGrid.SelectedCells;
            if (selectedCells == null)
                return null;

            selectedCells = selectedCells
                .Where(c => c.Column.Visibility == Visibility.Visible)
                .ToArray();

            if (!selectedCells.Any())
                return null;

            var orderedRows = selectedCells
                .GroupBy(i => i.Item)
                .OrderBy(i => dataGrid.Items.IndexOf(i.Key));

            return orderedRows.Select(GetRowContent).ToArray();
        }

        public static IList<string> GetRowContent(IGrouping<object, DataGridCellInfo> row)
        {
            Contract.Requires(row != null);
            Contract.Ensures(Contract.Result<IList<string>>() != null);

            return row
                .OrderBy(i => i.Column.DisplayIndex)
                .Select(i => i.Column.OnCopyingCellClipboardContent(i.Item))
                .Select(i => i == null ? string.Empty : i.ToString())
                .ToArray();
        }

        public static bool PasteCells(this DataGrid dataGrid, IList<IList<string>> data)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(data != null);
            Contract.Requires(Contract.ForAll(data, item => item != null));

            var numberOfRows = data.Count;
            if (data.Count < 1)
                return false;

            var firstRow = data[0];
            Contract.Assume(firstRow != null);
            Contract.Assume(Contract.ForAll(firstRow, item => item != null));

            var numberOfColumns = firstRow.Count;

            var selectedCells = dataGrid.SelectedCells;
            if (selectedCells == null)
                return false;

            selectedCells = selectedCells
                .Where(c => c.IsValid && (c.Column.Visibility == Visibility.Visible))
                .ToArray();

            if (!selectedCells.Any())
                return false;

            var selectedColumns = selectedCells
                .Select(cellInfo => cellInfo.Column)
                .Distinct()
                .ToArray();

            var selectedItems = selectedCells
                .Select(cellInfo => cellInfo.Item)
                .Distinct()
                .ToArray();

            if ((selectedColumns.Length == 1) && (selectedItems.Length == 1))
            {
                var selectedColumn = selectedColumns[0];
                selectedColumns = dataGrid.Columns
                    .Where(col => col.DisplayIndex >= selectedColumn.DisplayIndex)
                    .OrderBy(col => col.DisplayIndex)
                    .Where(col => col.Visibility == Visibility.Visible)
                    .Take(numberOfColumns)
                    .ToArray();

                var selectedItem = selectedItems[0];
                selectedItems = dataGrid.Items
                    .Cast<object>()
                    .Skip(dataGrid.Items.IndexOf(selectedItem))
                    .Take(numberOfRows)
                    .ToArray();
            }

            if ((selectedItems.Length == numberOfRows) && (selectedColumns.Length == numberOfColumns))
            {
                Enumerate.AsTuples(selectedItems, data)
                    .ForEach(row => Enumerate.AsTuples(selectedColumns, row.Item2)
                        .ForEach(column => column.Item1.OnPastingCellClipboardContent(row.Item1, column.Item2)));

                return true;
            }

            if ((data.Count == 1) && (firstRow.Count == 1))
            {
                selectedItems.ForEach(row => selectedColumns
                    .ForEach(column => column.OnPastingCellClipboardContent(row, firstRow[0])));

                return true;
            }

            return false;
        }
    }
}
