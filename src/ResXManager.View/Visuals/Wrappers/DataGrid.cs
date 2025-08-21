namespace ResXManager.View.Visuals.Wrappers;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

public class DataGrid : System.Windows.Controls.DataGrid
{
    /// <summary>
    /// Fixes https://github.com/dotnet/ResXResourceManager/issues/430: Crash on windows scroll after an automatic translation
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new DataGridAutomationPeerWrapper(this);
    }
    
    private sealed class DataGridAutomationPeerWrapper(DataGrid owner) : ItemsControlAutomationPeer(owner), ISelectionProvider, ITableProvider
    {
        private readonly DataGridAutomationPeer _dataGridPeer = new(owner);

        int IGridProvider.RowCount => ((IGridProvider)_dataGridPeer).RowCount;

        int IGridProvider.ColumnCount => ((IGridProvider)_dataGridPeer).ColumnCount;

        bool ISelectionProvider.CanSelectMultiple => ((ISelectionProvider)_dataGridPeer).CanSelectMultiple;

        bool ISelectionProvider.IsSelectionRequired => ((ISelectionProvider)_dataGridPeer).IsSelectionRequired;

        RowOrColumnMajor ITableProvider.RowOrColumnMajor => ((ITableProvider)_dataGridPeer).RowOrColumnMajor;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.DataGrid;
        }

        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            return _dataGridPeer.GetPattern(patternInterface);
        }

        protected override ItemAutomationPeer? CreateItemAutomationPeer(object? item)
        {
            return item == null ? null : new DataGridItemAutomationPeer(item, _dataGridPeer);
        }

        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            return ((IGridProvider)_dataGridPeer).GetItem(row, column);
        }

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            return ((ISelectionProvider)_dataGridPeer).GetSelection();
        }

        IRawElementProviderSimple[] ITableProvider.GetRowHeaders()
        {
            return ((ITableProvider)_dataGridPeer).GetRowHeaders();
        }

        IRawElementProviderSimple[] ITableProvider.GetColumnHeaders()
        {
            return ((ITableProvider)_dataGridPeer).GetColumnHeaders();
        }

        public static implicit operator DataGridAutomationPeer(DataGridAutomationPeerWrapper wrapper)
        {
            return wrapper._dataGridPeer;
        }
    }
}

