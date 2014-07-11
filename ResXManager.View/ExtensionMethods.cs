namespace tomenglertde.ResXManager.View
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Media;
    using tomenglertde.ResXManager.View.Properties;

    internal static class ExtensionMethods
    {
        public static void EnableMultilineEditing(this DataGridBoundColumn column)
        {
            Contract.Requires(column != null);

            var textBoxStyle = new Style(typeof(TextBox), column.EditingElementStyle);
            textBoxStyle.Setters.Add(new EventSetter(UIElement.PreviewKeyDownEvent, (KeyEventHandler)EditingElement_PreviewKeyDown));
            textBoxStyle.Setters.Add(new Setter(TextBoxBase.AcceptsReturnProperty, true));
            textBoxStyle.Seal();
            column.EditingElementStyle = textBoxStyle;
        }

        public static void EnableSpellChecker(this DataGridBoundColumn column, CultureInfo language)
        {
            Contract.Requires(column != null);

            var textBoxStyle = new Style(typeof(TextBox), column.EditingElementStyle);
            textBoxStyle.Setters.Add(new Setter(SpellCheck.IsEnabledProperty, true));
            var ieftLanguageTag = (language ?? Settings.Default.NeutralResourceLanguage ?? CultureInfo.InvariantCulture).IetfLanguageTag;
            textBoxStyle.Setters.Add(new Setter(FrameworkElement.LanguageProperty, XmlLanguage.GetLanguage(ieftLanguageTag)));
            textBoxStyle.Seal();
            column.EditingElementStyle = textBoxStyle;
        }

        private static void EditingElement_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Contract.Requires(sender != null);

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
                var args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, Key.Return)
                {
                    RoutedEvent = UIElement.KeyDownEvent
                };

                parent.RaiseEvent(args);
            }
        }

        public static bool IsChildOfEditingElement(this DependencyObject element)
        {
            while (element != null)
            {
                if (element is TextBox)
                    return true;

                if (element is DataGrid)
                    return false;

                element = LogicalTreeHelper.GetParent(element);
            }

            return false;
        }

        public static bool IsKeyDown(this Key key)
        {
            return (Keyboard.GetKeyStates(key) & KeyStates.Down) != 0;
        }

        public static IEnumerable<T> VisualDescendants<T>(this DependencyObject item) where T : DependencyObject
        {
            Contract.Requires(item != null);

            var numberOfChildren = VisualTreeHelper.GetChildrenCount(item);
            for (var i = 0; i < numberOfChildren; i++)
            {
                var child = VisualTreeHelper.GetChild(item, i);
                if (child == null)
                    continue;

                var c = child as T;

                if (c != null)
                {
                    yield return c;
                }

                foreach (var x in child.VisualDescendants<T>())
                {
                    yield return x;
                }
            }
        }

        public static IEnumerable<T> VisualDescendantsAndSelf<T>(this T item) where T : DependencyObject
        {
            Contract.Requires(item != null);

            yield return item;

            foreach (var x in item.VisualDescendants<T>())
            {
                yield return x;
            }
        }
    }
}