namespace ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;

    public static class ExtensionMethods
    {
        public static CultureKey? GetCultureKey(this DataGridColumn column)
        {
            return (column.Header as ILanguageColumnHeader)?.CultureKey;
        }

        public static CultureInfo? GetCulture(this DataGridColumn column)
        {
            return column.GetCultureKey()?.Culture;
        }

        public static void SetEditingElementStyle(this DataGridBoundColumn column, Binding? languageBinding, Binding? flowDirectionBinding)
        {
            var textBoxStyle = new Style(typeof(TextBox), column.EditingElementStyle);
            var setters = textBoxStyle.Setters;

            setters.Add(new EventSetter(UIElement.PreviewKeyDownEvent, (KeyEventHandler)EditingElement_PreviewKeyDown));
            setters.Add(new Setter(TextBoxBase.AcceptsReturnProperty, true));

            setters.Add(new Setter(Spellcheck.IsEnabledProperty, true));
            setters.Add(new Setter(FrameworkElement.LanguageProperty, languageBinding));

            setters.Add(new Setter(FrameworkElement.FlowDirectionProperty, flowDirectionBinding));

            textBoxStyle.Seal();

            column.EditingElementStyle = textBoxStyle;
        }

        public static void SetElementStyle(this DataGridBoundColumn column, Binding? languageBinding, Binding? flowDirectionBinding)
        {
            var elementStyle = new Style(typeof(TextBlock), column.ElementStyle);
            var setters = elementStyle.Setters;

            setters.Add(new Setter(FrameworkElement.LanguageProperty, languageBinding));
            setters.Add(new Setter(FrameworkElement.FlowDirectionProperty, flowDirectionBinding));

            elementStyle.Seal();
            column.ElementStyle = elementStyle;
        }

        private static void EditingElement_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            e.Handled = true;
            var editingElement = (TextBox)sender;

            if (IsKeyDown(Key.LeftCtrl) || IsKeyDown(Key.RightCtrl))
            {
                // Ctrl+Return adds a new line
                editingElement.SelectedText = Environment.NewLine;
                editingElement.SelectionLength = 0;
                editingElement.SelectionStart += Environment.NewLine.Length;
            }
            else
            {
                // Return without Ctrl: Forward to parent, grid should move focused cell down.
                var parent = (FrameworkElement)editingElement.Parent;
                if (parent == null)
                    return;

                var args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, Key.Return)
                {
                    RoutedEvent = UIElement.KeyDownEvent
                };

                parent.RaiseEvent(args);
            }
        }

        private static bool IsKeyDown(this Key key)
        {
            return (Keyboard.GetKeyStates(key) & KeyStates.Down) != 0;
        }

        public static bool IsItemInvariant(this DataGridCellInfo info)
        {
            var col = info.Column?.Header as ILanguageColumnHeader;

            if (col?.ColumnType != ColumnType.Language)
                return false;

            if (!(info.Item is ResourceTableEntry item))
                return false;

            if (item.IsItemInvariant.GetValue(col.CultureKey))
            {
                return true;
            }

            return false;
        }

        public static bool IsOfColumnType(this DataGridCellInfo cell, params ColumnType[] columnTypes)
        {
            var cellColumnType = (cell.Column?.Header as ILanguageColumnHeader)?.ColumnType;

            return columnTypes.Any(columnType => columnType == cellColumnType);
        }

        public static IEnumerable<DataGridCellInfo> GetSelectedVisibleCells(this DataGrid dataGrid)
        {
            return dataGrid.SelectedCells.Where(cell => cell.IsValid && cell.Column?.Visibility == Visibility.Visible);
        }

        public static bool GetIsEditing(this DataGrid dataGrid)
        {
            var view = (IEditableCollectionView)dataGrid.Items;

            return view.IsEditingItem || view.IsAddingNew;
        }
    }
}