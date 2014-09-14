namespace tomenglertde.ResXManager.View.Behaviors
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Interactivity;
    using DataGridExtensions;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    public class ShowErrorsOnlyBehavior : Behavior<DataGrid>
    {
        private static readonly IList EmptyList = new object[0];
        private static readonly DependencyPropertyDescriptor VisibilityPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.VisibilityProperty, typeof(DataGridColumn));
        public ToggleButton ToggleButton
        {
            get { return (ToggleButton)GetValue(ToggleButtonProperty); }
            set { SetValue(ToggleButtonProperty, value); }
        }
        /// <summary>
        /// Identifies the ToggleButton dependency property
        /// </summary>
        public static readonly DependencyProperty ToggleButtonProperty =
            DependencyProperty.Register("ToggleButton", typeof(ToggleButton), typeof(ShowErrorsOnlyBehavior), new FrameworkPropertyMetadata(null, (sender, e) => ((ShowErrorsOnlyBehavior)sender).ToggleButton_Changed((ToggleButton)e.OldValue, (ToggleButton)e.NewValue)));

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

        private void ToggleButton_Changed(ToggleButton oldValue, ToggleButton newValue)
        {
            if (oldValue != null)
            {
                oldValue.Checked -= ToggleButton_StateChanged;
                oldValue.Unchecked -= ToggleButton_StateChanged;
            }

            if (newValue != null)
            {
                newValue.Checked += ToggleButton_StateChanged;
                newValue.Unchecked += ToggleButton_StateChanged;
                ToggleButton_StateChanged(newValue, EventArgs.Empty);
            }
        }

        private void ToggleButton_StateChanged(object sender, EventArgs e)
        {
            if ((sender == null) || (DataGrid == null))
                return;

            var button = (ToggleButton)sender;

            if (button.IsChecked.GetValueOrDefault())
            {
                UpdateErrorsOnlyFilter();
            }
            else
            {
                DataGrid.Items.Filter = null;
                DataGrid.SetIsAutoFilterEnabled(true);
            }
        }

        private void DataGrid_ColumnVisibilityChanged(object source, EventArgs e)
        {
            if (ToggleButton == null)
                return;

            if (ToggleButton.IsChecked.GetValueOrDefault())
            {
                this.BeginInvoke(UpdateErrorsOnlyFilter);
            }
        }

        private void UpdateErrorsOnlyFilter()
        {
            if (DataGrid == null)
                return;

            var visibleLanguageKeys = DataGrid.Columns
                .Where(column => column.Visibility == Visibility.Visible)
                .Select(column => column.Header)
                .OfType<LanguageHeader>()
                .Select(header => header.Language.ToLanguageKey())
                .ToArray();

            DataGrid.SetIsAutoFilterEnabled(false);
            DataGrid.Items.Filter = row =>
            {
                var entry = (ResourceTableEntry)row;
                var values = visibleLanguageKeys.Select(key => entry.Values[key]);
                return !entry.IsInvariant && values.Any(string.IsNullOrEmpty);
            };
        }
    }
}
