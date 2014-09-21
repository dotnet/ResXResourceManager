namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
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
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi", Justification = "Use the same term as in System.Windows.Controls.Primitives.MultiSelector")]
    public static class MultiSelectorExtensions
    {
        private static readonly IList EmptyObjectArray = new object[0];

        /// <summary>
        /// Gets the value of the <see cref="SelectionBindingProperty"/> property.
        /// </summary>
        /// <param name="obj">The object to attach to.</param>
        /// <returns>The current selection.</returns>
        public static IList GetSelectionBinding(this Selector obj)
        {
            Contract.Requires(obj != null);
            return (IList)obj.GetValue(SelectionBindingProperty);
        }
        /// <summary>
        /// Sets the value of the <see cref="SelectionBindingProperty"/> property.
        /// </summary>
        /// <param name="obj">The object to attach to.</param>
        /// <param name="value">The new selection.</param>
        public static void SetSelectionBinding(this Selector obj, IList value)
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
        /// <para/>
        /// <code><![CDATA[
        /// <ListBox ItemsSource="{Binding Path=Items}" core:MultiSelectorExtensions.SelectionBinding="{Binding Path=SelectedItems}"/>
        /// ]]></code>
        /// </example>
        public static readonly DependencyProperty SelectionBindingProperty =
            DependencyProperty.RegisterAttached("SelectionBinding", typeof(IList), typeof(MultiSelectorExtensions), new FrameworkPropertyMetadata(EmptyObjectArray, SelectionBinding_Changed));

        private static readonly DependencyProperty SelectionSynchronizerProperty =
            DependencyProperty.RegisterAttached("SelectionSynchronizer", typeof(SelectionSynchronizer), typeof(MultiSelectorExtensions));

        private static void SelectionBinding_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Contract.Requires(d != null);

            // The selector is the target of the binding, and the ViewModel property is the source.
            var synchronizer = (SelectionSynchronizer)d.GetValue(SelectionSynchronizerProperty);

            if (synchronizer != null)
            {
                if (synchronizer.IsUpdating)
                    return;

                synchronizer.Dispose();
            }

            d.SetValue(SelectionSynchronizerProperty, new SelectionSynchronizer((Selector)d, (IList)e.NewValue));
        }

        private static void CommitEdit(this Selector selector)
        {
            var dataGrid = selector as DataGrid;
            if (dataGrid != null)
            {
                dataGrid.CommitEdit();
            }
        }

        private static IList GetSelectedItems(this Selector selector)
        {
            Contract.Requires(selector != null);
            Contract.Ensures(Contract.Result<IList>() != null);

            var selectedItems = (IList)((dynamic)selector).SelectedItems;
            Contract.Assume(selectedItems != null);
            return selectedItems;
        }

        private static void ScrollIntoView(this Selector selector, object selectedItem)
        {
            Contract.Requires(selector != null);

            ((dynamic)selector).ScrollIntoView(selectedItem);
        }

        private static void BeginSetFocus(this ItemsControl selector, object selectedItem)
        {
            Contract.Requires(selector != null);

            selector.BeginInvoke(() =>
            {
                var container = selector.ItemContainerGenerator.ContainerFromItem(selectedItem) as FrameworkElement;
                if (container == null)
                    return;

                var child = container.VisualDescendantsAndSelf<UIElement>().FirstOrDefault(item => item.Focusable);
                if (child != null)
                {
                    child.Focus();
                }
            });
        }

        private static void ClearSourceSelection(this Selector selector)
        {
            Contract.Requires(selector != null);

            var sourceSelection = selector.GetSelectionBinding();

            if (sourceSelection.IsFixedSize || sourceSelection.IsReadOnly)
            {
                selector.SetSelectionBinding(EmptyObjectArray);
            }
            else
            {
                sourceSelection.Clear();
            }
        }

        private static bool All(this IEnumerable items, Func<object, bool> condition)
        {
            Contract.Requires(items != null);
            Contract.Requires(condition != null);

            return Enumerable.All(items.Cast<object>(), condition);
        }

        private static void SynchronizeWithSource(this Selector selector, IList sourceSelection)
        {
            Contract.Requires(selector != null);
            Contract.Requires(sourceSelection != null);

            var selectedItems = selector.GetSelectedItems();

            if ((selectedItems.Count == sourceSelection.Count) && sourceSelection.All(selectedItems.Contains))
                return;

            selector.CommitEdit();

            // Clear the selection.
            selector.SelectedIndex = -1;

            if (sourceSelection.Count == 1)
            {
                selector.SelectSingleItem(sourceSelection);
            }
            else
            {
                selector.AddItemsToSelection(sourceSelection);
            }
        }

        private static void AddItemsToSelection(this Selector selector, IList itemsToSelect)
        {
            Contract.Requires(selector != null);
            Contract.Requires(itemsToSelect != null);

            var isSourceInvalid = false;
            var selectedItems = selector.GetSelectedItems();

            foreach (var item in itemsToSelect)
            {
                if (selector.Items.Contains(item))
                {
                    selectedItems.Add(item);
                }
                else
                {
                    // The item is not present, e.g. because of filtering, and can't be selected at this time.
                    if (itemsToSelect.IsFixedSize || itemsToSelect.IsReadOnly)
                    {
                        isSourceInvalid = true;
                    }
                    else
                    {
                        itemsToSelect.Remove(item);
                    }
                }
            }

            if (isSourceInvalid)
            {
                selector.SetSelectionBinding(ArrayList.Adapter(selector.GetSelectedItems()));
            }
        }

        private static void SelectSingleItem(this Selector selector, IList sourceSelection)
        {
            Contract.Requires(selector != null);
            Contract.Requires(sourceSelection != null);
            Contract.Requires(sourceSelection.Count == 1);

            // Special handling, maybe listbox is in single selection mode where we can't call selectedItems.Add().
            var selectedItem = sourceSelection[0];

            // The item is not present, e.g. because of filtering, and can't be selected at this time.
            if (!selector.Items.Contains(selectedItem))
            {
                selector.ClearSourceSelection();
            }
            else
            {
                selector.SelectedItem = selectedItem;
                selector.ScrollIntoView(selectedItem);

                if (selector.IsKeyboardFocusWithin)
                {
                    selector.BeginSetFocus(selectedItem);
                }
            }
        }

        private sealed class SelectionSynchronizer : IDisposable
        {
            private readonly Selector _selector;
            private readonly INotifyCollectionChanged _observableSourceSelection;

            public SelectionSynchronizer(Selector selector, IList sourceSelection)
            {
                Contract.Requires(selector != null);
                Contract.Requires(sourceSelection != null);

                _selector = selector;
                selector.SynchronizeWithSource(sourceSelection);

                selector.SelectionChanged += Selector_SelectionChanged;

                if (sourceSelection.IsFixedSize || sourceSelection.IsReadOnly)
                    return;

                _observableSourceSelection = sourceSelection as INotifyCollectionChanged;

                if (_observableSourceSelection != null)
                {
                    _observableSourceSelection.CollectionChanged += SourceSelection_CollectionChanged;
                }
            }

            internal bool IsUpdating
            {
                get;
                private set;
            }

            private void SourceSelection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (IsUpdating)
                    return;

                IsUpdating = true;

                try
                {
                    var selectedItems = _selector.GetSelectedItems();

                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Reset:
                            _selector.SynchronizeWithSource((IList)sender);
                            break;

                        case NotifyCollectionChangedAction.Add:
                        case NotifyCollectionChangedAction.Remove:
                        case NotifyCollectionChangedAction.Replace:
                            selectedItems.RemoveRange(e.OldItems ?? EmptyObjectArray);
                            _selector.AddItemsToSelection(e.NewItems ?? EmptyObjectArray);
                            break;
                    }
                }
                finally
                {
                    IsUpdating = false;
                }
            }

            private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (IsUpdating)
                    return;

                IsUpdating = true;

                try
                {
                    var sourceSelection = _selector.GetSelectionBinding();

                    if (sourceSelection.IsFixedSize || sourceSelection.IsReadOnly)
                    {
                        _selector.SetSelectionBinding(ArrayList.Adapter(_selector.GetSelectedItems()));
                    }
                    else
                    {
                        sourceSelection.RemoveRange(e.RemovedItems ?? EmptyObjectArray);
                        sourceSelection.AddRange(e.AddedItems ?? EmptyObjectArray);
                    }
                }
                finally
                {
                    IsUpdating = false;
                }
            }

            public void Dispose()
            {
                _selector.SelectionChanged -= Selector_SelectionChanged;

                if (_observableSourceSelection != null)
                {
                    _observableSourceSelection.CollectionChanged -= SourceSelection_CollectionChanged;
                }
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_selector != null);
            }
        }
    }
}