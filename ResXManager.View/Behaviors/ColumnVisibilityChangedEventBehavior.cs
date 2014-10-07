namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interactivity;
    using tomenglertde.ResXManager.View.Tools;

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
                        VisibilityPropertyDescriptor.AddValueChanged(column, DataGrid_ColumnVisibilityChanged);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (DataGridColumn column in e.OldItems ?? EmptyList)
                    {
                        VisibilityPropertyDescriptor.RemoveValueChanged(column, DataGrid_ColumnVisibilityChanged);
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

        private void DataGrid_ColumnVisibilityChanged(object source, EventArgs e)
        {
            var args = new RoutedEventArgs(ColumnVisibilityChangedEvent, source);

            DataGrid.RaiseEvent(args);
        }
    }
}
