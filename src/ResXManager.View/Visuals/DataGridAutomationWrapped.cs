using System;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;

namespace ResXManager.View.Visuals;

public class DataGridAutomationWrapped : DataGrid
{
    /// <summary>
    /// Turn off UI Automation
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new DataGridAutomationPeerWrapper(this);
    }
}

public sealed class DataGridAutomationPeerWrapper : ItemsControlAutomationPeer,
        IGridProvider, ISelectionProvider, ITableProvider
{
    private readonly DataGridAutomationPeer dataGridPeer;

    int IGridProvider.RowCount
    {
        get
        {
            var peerIGridProvider = dataGridPeer as IGridProvider;
            return peerIGridProvider.RowCount;
        }
    }

    int IGridProvider.ColumnCount
    {
        get
        {
            var peerIGridProvider = dataGridPeer as IGridProvider;
            return peerIGridProvider.ColumnCount;
        }
    }

    bool ISelectionProvider.CanSelectMultiple
    {
        get
        {
            var peerISelectionProvider = dataGridPeer as ISelectionProvider;
            return peerISelectionProvider.CanSelectMultiple;
        }
    }

    bool ISelectionProvider.IsSelectionRequired
    {
        get
        {
            var peerISelectionProvider = dataGridPeer as ISelectionProvider;
            return peerISelectionProvider.IsSelectionRequired;
        }
    }

    RowOrColumnMajor ITableProvider.RowOrColumnMajor
    {
        get
        {
            var peerITableProvider = dataGridPeer as ITableProvider;
            return peerITableProvider.RowOrColumnMajor;
        }
    }

    public DataGridAutomationPeerWrapper(DataGrid owner)
            : base(owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }
        dataGridPeer = new DataGridAutomationPeer(owner);
    }

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
        return dataGridPeer.GetPattern(patternInterface);
    }

    protected override ItemAutomationPeer? CreateItemAutomationPeer(object item)
    {
        if (item == null) return null;
        return new DataGridItemAutomationPeer(item, dataGridPeer);
    }

    IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
    {
        var peerIGridProvider = dataGridPeer as IGridProvider;
        return peerIGridProvider.GetItem(row, column);
    }

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        var peerISelectionProvidere = dataGridPeer as ISelectionProvider;
        return peerISelectionProvidere.GetSelection();
    }

    IRawElementProviderSimple[] ITableProvider.GetRowHeaders()
    {
        var peerITableProvider = dataGridPeer as ITableProvider;
        return peerITableProvider.GetRowHeaders();
    }

    IRawElementProviderSimple[] ITableProvider.GetColumnHeaders()
    {
        var peerITableProvider = dataGridPeer as ITableProvider;
        return peerITableProvider.GetColumnHeaders();
    }

    public static implicit operator DataGridAutomationPeer(DataGridAutomationPeerWrapper wrapper)
    {
        return wrapper.dataGridPeer;
    }
}
