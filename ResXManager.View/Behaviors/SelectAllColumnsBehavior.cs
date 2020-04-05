namespace ResXManager.View.Behaviors
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;

    using JetBrains.Annotations;

    using Microsoft.Xaml.Behaviors;

    using ResXManager.View.ColumnHeaders;

    public class SelectAllColumnsBehavior : Behavior<CheckBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Checked += CheckBox_Checked;
            AssociatedObject.Unchecked += CheckBox_Unchecked;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Checked -= CheckBox_Checked;
            AssociatedObject.Unchecked -= CheckBox_Unchecked;

            var listBox = ListBox;
            if (listBox != null)
                listBox.SelectionChanged -= ListBox_SelectionChanged;
        }

        public ListBox ListBox
        {
            get => (ListBox)GetValue(ListBoxProperty);
            set => SetValue(ListBoxProperty, value);
        }
        public static readonly DependencyProperty ListBoxProperty =
            DependencyProperty.Register("ListBox", typeof(ListBox), typeof(SelectAllColumnsBehavior), new FrameworkPropertyMetadata(default, (sender, e) => ((SelectAllColumnsBehavior)sender).ListBox_Changed((ListBox)e.OldValue, (ListBox)e.NewValue)));

        public ColumnType ColumnType
        {
            get => (ColumnType)GetValue(ColumnTypeProperty);
            set => SetValue(ColumnTypeProperty, value);
        }
        public static readonly DependencyProperty ColumnTypeProperty =
            DependencyProperty.Register("ColumnType", typeof(ColumnType), typeof(SelectAllColumnsBehavior), new FrameworkPropertyMetadata(ColumnType.Language));

        private void ListBox_Changed([CanBeNull] ListBox oldValue, [CanBeNull] ListBox newValue)
        {
            if (oldValue != null)
                oldValue.SelectionChanged -= ListBox_SelectionChanged;
            if (newValue != null)
                newValue.SelectionChanged += ListBox_SelectionChanged;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;

            var items = GetEffectiveColumns(listBox.Items, ColumnType);

            var visibleItemsCount = items.Count(item => item.Visibility == Visibility.Visible);

            if (visibleItemsCount == 0)
            {
                AssociatedObject.IsChecked = false;
            }
            else if (visibleItemsCount == items.Count)
            {
                AssociatedObject.IsChecked = true;
            }
            else
            {
                AssociatedObject.IsChecked = null;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var listBox = ListBox;
            if (listBox == null)
                return;

            var itemsToUnselect = GetEffectiveColumns(listBox.SelectedItems, ColumnType);

            foreach (var item in itemsToUnselect)
            {
                listBox.SelectedItems.Remove(item);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var listBox = ListBox;
            if (listBox == null)
                return;

            var columnType = ColumnType;

            var selectedItems = GetEffectiveColumns(listBox.SelectedItems, columnType);
            var allItems = GetEffectiveColumns(listBox.Items, columnType);
            var itemsToSelect = allItems.Except(selectedItems);

            foreach (var item in itemsToSelect)
            {
                listBox.SelectedItems.Add(item);
            }
        }

        private static IList<DataGridColumn> GetEffectiveColumns(IList columns, ColumnType columnType)
        {
            return columns.OfType<DataGridColumn>()
                .Where(item => (item.Header as ILanguageColumnHeader)?.ColumnType == columnType)
                .ToList();
        }
    }
}
