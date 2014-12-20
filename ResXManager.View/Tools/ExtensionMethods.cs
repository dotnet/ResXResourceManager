namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    public static class ExtensionMethods
    {
        public static CultureKey GetCultureKey(this DataGridColumn column)
        {
            Contract.Requires(column != null);

            return (column.Header as ILanguageColumnHeader).Maybe().Return(l => l.CultureKey);
        }

        public static CultureInfo GetCulture(this DataGridColumn column)
        {
            Contract.Requires(column != null);

            return column.GetCultureKey().Maybe().Return(c => c.Culture);
        }

        public static void EnableMultilineEditing(this DataGridBoundColumn column)
        {
            Contract.Requires(column != null);

            var textBoxStyle = new Style(typeof(TextBox), column.EditingElementStyle);
            textBoxStyle.Setters.Add(new EventSetter(UIElement.PreviewKeyDownEvent, (KeyEventHandler)EditingElement_PreviewKeyDown));
            textBoxStyle.Setters.Add(new Setter(TextBoxBase.AcceptsReturnProperty, true));
            textBoxStyle.Seal();
            column.EditingElementStyle = textBoxStyle;
        }

        public static void EnableSpellchecker(this DataGridBoundColumn column, Binding languageBinding)
        {
            Contract.Requires(column != null);

            var textBoxStyle = new Style(typeof(TextBox), column.EditingElementStyle);
            textBoxStyle.Setters.Add(new Setter(SpellCheck.IsEnabledProperty, true));
            textBoxStyle.Setters.Add(new Setter(FrameworkElement.LanguageProperty, languageBinding));
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
                if (parent == null)
                    return;

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
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

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

        public static IEnumerable<T> VisualDescendantsAndSelf<T>(this DependencyObject item) where T : DependencyObject
        {
            Contract.Requires(item != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            var target = item as T;
            if (target != null)
                yield return target;

            foreach (var x in item.VisualDescendants<T>())
            {
                yield return x;
            }
        }

        /// <summary>
        /// Returns an enumeration of elements that contain this element, and the ancestors of this element.
        /// </summary>
        /// <param name="self">The starting element.</param>
        /// <returns>The ancestor list.</returns>
        public static IEnumerable<DependencyObject> AncestorsAndSelf(this DependencyObject self)
        {
            Contract.Requires(self != null);
            Contract.Ensures(Contract.Result<IEnumerable<DependencyObject>>() != null);

            while (self != null)
            {
                yield return self;
                self = LogicalTreeHelper.GetParent(self) ?? VisualTreeHelper.GetParent(self);
            }
        }

        /// <summary>
        /// Returns an enumeration of the ancestor elements of this element.
        /// </summary>
        /// <param name="self">The starting element.</param>
        /// <returns>The ancestor list.</returns>
        public static IEnumerable<DependencyObject> Ancestors(this DependencyObject self)
        {
            Contract.Requires(self != null);
            Contract.Ensures(Contract.Result<IEnumerable<DependencyObject>>() != null);

            return self.AncestorsAndSelf().Skip(1);
        }

        /// <summary>
        /// Returns the first element in the ancestor list that implements the type of the type parameter.
        /// </summary>
        /// <typeparam name="T">The type of element to return.</typeparam>
        /// <param name="self">The starting element.</param>
        /// <returns>The first element matching the criteria, or null if no element was found.</returns>
        public static T TryFindAncestorOrSelf<T>(this DependencyObject self) where T : DependencyObject
        {
            Contract.Requires(self != null);

            return self.AncestorsAndSelf().OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Returns the first element in the ancestor list that implements the type of the type parameter.
        /// </summary>
        /// <typeparam name="T">The type of element to return.</typeparam>
        /// <param name="self">The starting element.</param>
        /// <param name="match">The predicate to match.</param>
        /// <returns>The first element matching the criteria, or null if no element was found.</returns>
        public static T TryFindAncestorOrSelf<T>(this DependencyObject self, Func<T, bool> match) where T : DependencyObject
        {
            Contract.Requires(self != null);
            Contract.Requires(match != null);

            return self.AncestorsAndSelf().OfType<T>().FirstOrDefault(match);
        }

        /// <summary>
        /// Returns the first element in the ancestor list that implements the type of the type parameter.
        /// </summary>
        /// <typeparam name="T">The type of element to return.</typeparam>
        /// <param name="self">The starting element.</param>
        /// <returns>The first element matching the criteria, or null if no element was found.</returns>
        public static T TryFindAncestor<T>(this DependencyObject self) where T : DependencyObject
        {
            Contract.Requires(self != null);

            return self.Ancestors().OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Returns the first element in the ancestor list that implements the type of the type parameter.
        /// </summary>
        /// <typeparam name="T">The type of element to return.</typeparam>
        /// <param name="self">The starting element.</param>
        /// <param name="match">The predicate to match.</param>
        /// <returns>The first element matching the criteria, or null if no element was found.</returns>
        public static T TryFindAncestor<T>(this DependencyObject self, Func<T, bool> match) where T : DependencyObject
        {
            Contract.Requires(self != null);
            Contract.Requires(match != null);

            return self.Ancestors().OfType<T>().FirstOrDefault(match);
        }


    }
}