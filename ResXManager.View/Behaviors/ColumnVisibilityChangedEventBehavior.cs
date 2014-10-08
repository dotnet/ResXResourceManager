namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;

    public class ColumnVisibilityChangedEventBehavior : Behavior<DataGrid>
    {
        private static readonly IList EmptyList = new object[0];
        private static readonly DependencyPropertyDescriptor VisibilityPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.VisibilityProperty, typeof(DataGridColumn));

        public static readonly RoutedEvent ColumnVisibilityChangedEvent = EventManager.RegisterRoutedEvent("ColumnVisibilityChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ColumnVisibilityChangedEventBehavior));
        
        protected override void OnAttached()
        {
            base.OnAttached();
            
            DataGrid.Columns.CollectionChanged += Columns_CollectionChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            DataGrid.Columns.CollectionChanged -= Columns_CollectionChanged;
        }

        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (DataGridColumn column in e.NewItems ?? EmptyList)
                    {
                        VisibilityPropertyDescriptor.AddValueChanged(column, DataGridColumnVisibility_Changed);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (DataGridColumn column in e.OldItems ?? EmptyList)
                    {
                        VisibilityPropertyDescriptor.RemoveValueChanged(column, DataGridColumnVisibility_Changed);
                    }
                    break;
            }
        }

        private DataGrid DataGrid
        {
            get
            {
                return AssociatedObject;
            }
        }

        private void DataGridColumnVisibility_Changed(object source, EventArgs e)
        {
            var args = new RoutedEventArgs(ColumnVisibilityChangedEvent, source);
            var dataGrid = DataGrid;

            if (dataGrid != null)
                dataGrid.RaiseEvent(args);
        }
    }
}
