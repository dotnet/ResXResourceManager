namespace tomenglertde.ResXManager.View
{
    using System.Collections;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using tomenglertde.ResXManager.Model;

    /// <summary>
    /// Extensions for multi selectors like ListBox or DataGrid:
    /// <list type="bullet">
    /// <item>Support binding operations with SelectedItems property.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// SelectionBinding:
    /// <para/>
    /// Since there is no common interface for ListBox and DataGrid, the SelectionBinding is implemented via reflection/dynamics, so it will
    /// work on any FrameworkElement that has the SelectedItems, SelectedItem and SelectedItemIndex properties and the SelectionChanged event.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi", Justification = "Use the same term as in System.Windows.Controls.Primitives.MultiSelector")]
    public static class MultiSelectorExtensions
    {
        // Simple recursion blocking. Change events should appear only on the UI thread, so a static bool will do the job.
        private static bool _selectionBindingIsUpdatingTarget;

        /// <summary>
        /// Gets the value of the <see cref="SelectionBindingProperty"/> property.
        /// </summary>
        /// <param name="obj">The object to attach to.</param>
        /// <returns>The current selection.</returns>
        public static IList GetSelectionBinding(DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return (IList)obj.GetValue(SelectionBindingProperty);
        }
        /// <summary>
        /// Sets the value of the <see cref="SelectionBindingProperty"/> property.
        /// </summary>
        /// <param name="obj">The object to attach to.</param>
        /// <param name="value">The new selection.</param>
        public static void SetSelectionBinding(DependencyObject obj, IList value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(SelectionBindingProperty, value);
        }
        /// <summary>
        /// Identifies the SelectionBinding dependency property.
        /// <para/>
        /// Attach this property to a ListBox or DataGrid to bind the selectors SelectedItems property to the view models SelectedItems property.
        /// </summary>
        /// <example>
        /// If your view model has two properties "AnyList Items { get; }" and "IList SelectedItems { get; set; }" your XAML looks like this:
        /// <code><![CDATA[
        /// <ListBox ItemsSource="{Binding Path=Items}" core:MultiSelectorExtensions.SelectionBinding="{Binding Path=SelectedItems}"/>
        /// ]]></code>
        /// </example>
        public static readonly DependencyProperty SelectionBindingProperty =
            DependencyProperty.RegisterAttached("SelectionBinding", typeof(IList), typeof(MultiSelectorExtensions), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, SelectionBinding_Changed));

        [ContractVerification(false)] // Contracts get confused by dynamic variables.
        private static void SelectionBinding_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The selector is the target of the binding, and the ViewModel property is the source.

            var selector = d as Selector;
            if (selector == null)
                return;

            _selectionBindingIsUpdatingTarget = true;

            try
            {
                // Simply remove and add again, so we don't need to track if we have already attached the event.
                selector.SelectionChanged -= selector_SelectionChanged;
                selector.SelectionChanged += selector_SelectionChanged;

                // Updating this direction is a rare case, usually happens only once.
                // Use a very simple approach to update the target - just clear the list and then add all selected again.
                // Set SelectedIndex to clear the content; maybe listbox is in single selection mode, this will work always.
                selector.SelectedIndex = -1;

                var bindingSource = (IList)e.NewValue;

                if (bindingSource == null)
                    return;

                var bindingTarget = (dynamic)selector;

                var dataGrid = selector as DataGrid;
                if (dataGrid != null)
                {
                    dataGrid.CommitEdit();
                }

                if (bindingSource.Count == 1)
                {
                    var selectedItem = bindingSource[0];

                    if (!selector.Items.Contains(selectedItem))
                    {
                        // The item is not present, e.g. because of filtering, and can't be selected at this time.
                        bindingSource.Clear();
                        return;
                    }

                    selector.SelectedItem = selectedItem;

                    bindingTarget.ScrollIntoView(selectedItem);

                    if (selector.IsKeyboardFocusWithin)
                    {
                        selector.BeginInvoke(() =>
                        {
                            var container = selector.ItemContainerGenerator.ContainerFromItem(selectedItem) as FrameworkElement;
                            if (container == null)
                                return;

                            var child = container.VisualDescendantsAndSelf().FirstOrDefault(item => item.Focusable);
                            if (child != null)
                            {
                                child.Focus();
                            }
                        });
                    }
                }
                else
                {
                    var selectedItems = (IList)bindingTarget.SelectedItems;

                    foreach (var item in bindingSource)
                    {
                        if (selector.Items.Contains(item))
                        {
                            selectedItems.Add(item);
                        }
                        else
                        {
                            // The item is not present, e.g. because of filtering, and can't be selected at this time.
                            bindingSource.Remove(item);
                        }
                    }
                }
            }
            finally
            {
                _selectionBindingIsUpdatingTarget = false;
            }
        }

        private static void selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectionBindingIsUpdatingTarget)
                return;

            var selector = sender as DependencyObject;

            if (selector == null)
                return;

            var itemList = selector.GetValue(SelectionBindingProperty) as IList;

            if (itemList == null)
                return;

            if (e.RemovedItems != null)
            {
                itemList.RemoveRange(e.RemovedItems);
            }

            if (e.AddedItems != null)
            {
                itemList.AddRange(e.AddedItems);
            }
        }
    }
}