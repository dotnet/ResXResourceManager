namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Provide some additional events.
    /// </summary>
    public interface IDataGridEventsProvider
    {
        event EventHandler ColumnVisibilityChanged;
    }

    public static class DataGridEventsExtensions
    {
        public static IDataGridEventsProvider GetAdditionalEvents(this DataGrid dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var eventsProvider = dataGrid.GetValue(DataGridEventsProviderProperty) as IDataGridEventsProvider;
            if (eventsProvider != null)
                return eventsProvider;

            eventsProvider = new DataGridEventsProvider(dataGrid);
            dataGrid.SetValue(DataGridEventsProviderProperty, eventsProvider);

            return eventsProvider;
        }

        /// <summary>
        /// Identifies the DataGridEventsProvider dependency property
        /// </summary>
        public static readonly DependencyProperty DataGridEventsProviderProperty =
            DependencyProperty.RegisterAttached("DataGridEventsProvider", typeof(IDataGridEventsProvider), typeof(DataGridEventsExtensions));

        private sealed class DataGridEventsProvider : IDataGridEventsProvider
        {
            private readonly DataGrid _dataGrid;
            private static readonly IList _emptyList = new object[0];
            private static readonly DependencyPropertyDescriptor _visibilityPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.VisibilityProperty, typeof(DataGridColumn));

            public DataGridEventsProvider(DataGrid dataGrid)
            {
                Contract.Requires(dataGrid != null);

                _dataGrid = dataGrid;
                dataGrid.Columns.CollectionChanged += Columns_CollectionChanged;

                foreach (var column in dataGrid.Columns)
                {
                    _visibilityPropertyDescriptor.AddValueChanged(column, DataGridColumnVisibility_Changed);
                }
            }

            public event EventHandler ColumnVisibilityChanged;

            private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (DataGridColumn column in e.NewItems ?? _emptyList)
                        {
                            _visibilityPropertyDescriptor.AddValueChanged(column, DataGridColumnVisibility_Changed);
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        foreach (DataGridColumn column in e.OldItems ?? _emptyList)
                        {
                            _visibilityPropertyDescriptor.RemoveValueChanged(column, DataGridColumnVisibility_Changed);
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        foreach (DataGridColumn column in e.OldItems ?? _emptyList)
                        {
                            _visibilityPropertyDescriptor.RemoveValueChanged(column, DataGridColumnVisibility_Changed);
                        }
                        foreach (DataGridColumn column in e.NewItems ?? _emptyList)
                        {
                            _visibilityPropertyDescriptor.AddValueChanged(column, DataGridColumnVisibility_Changed);
                        }
                        break;
                }
            }

            private void DataGridColumnVisibility_Changed(object source, EventArgs e)
            {
                OnColumnVisibilityChanged();
            }

            private void OnColumnVisibilityChanged()
            {
                var handler = ColumnVisibilityChanged;
                if (handler != null)
                    handler(_dataGrid, EventArgs.Empty);
            }

            [ContractInvariantMethod]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_dataGrid != null);
            }

        }
    }
}
