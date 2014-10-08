namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using DataGridExtensions;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Behaviors;
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Controls;
    using tomenglertde.ResXManager.View.Properties;

    public static class ColumnManager
    {
        public static void SetupColumns(this DataGrid dataGrid, IEnumerable<CultureKey> languages)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(languages != null);

            dataGrid.RemoveHandler(ColumnVisibilityChangedEventBehavior.ColumnVisibilityChangedEvent, (RoutedEventHandler)DataGrid_ColumnVisibilityChanged);
            dataGrid.AddHandler(ColumnVisibilityChangedEventBehavior.ColumnVisibilityChangedEvent, (RoutedEventHandler)DataGrid_ColumnVisibilityChanged);

            var columns = dataGrid.Columns;

            if (columns.Count == 0)
            {
                var keyColumn = new DataGridTextColumn
                {
                    Header = new ColumnHeader(Resources.Key, ColumnType.Key),
                    Binding = new Binding(@"Key") { ValidatesOnExceptions = true },
                    Width = 200,
                    CanUserReorder = false,
                };

                columns.Add(keyColumn);

                columns.Add(CreateCodeReferencesColumn(dataGrid));
            }

            var languageColumns = columns.Skip(2).ToArray();

            var disconnectedColumns = languageColumns.Where(col => languages.All(language => !Equals(col.GetCultureKey(), language)));

            foreach (var column in disconnectedColumns)
            {
                columns.Remove(column);
            }

            var addedLanguages = languages.Where(language => languageColumns.All(col => !Equals(col.GetCultureKey(), language)));

            foreach (var language in addedLanguages)
            {
                AddLanguageColumn(columns, language);
            }
        }

        private static DataGridColumn CreateCodeReferencesColumn(FrameworkElement dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var elementStyle = new Style();
            elementStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, new CodeReferencesToolTip()));
            elementStyle.Setters.Add(new Setter(ToolTipService.ShowDurationProperty, Int32.MaxValue));
            elementStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));

            var columnHeader = new ColumnHeader(dataGrid.FindResource("CodeReferencesImage"), ColumnType.Other)
            {
                ToolTip = Resources.CodeReferencesToolTip,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var column = new DataGridTextColumn
            {
                Header = columnHeader,
                ElementStyle = elementStyle,
                Binding = new Binding(@"CodeReferences.Count"),
                CanUserReorder = false,
                CanUserResize = false,
                IsReadOnly = true,
            };

            column.SetIsFilterVisible(false);
            BindingOperations.SetBinding(column, DataGridColumn.VisibilityProperty, new Binding(@"IsFindCodeReferencesEnabled") { Source = Settings.Default, Converter = new BooleanToVisibilityConverter() });

            return column;
        }

        public static void AddLanguageColumn(this ICollection<DataGridColumn> columns, CultureKey cultureKey)
        {
            Contract.Requires(columns != null);
            Contract.Requires(cultureKey != null);
            var key = cultureKey.ToString(".");
            var settings = Settings.Default;

            var commentColumn = new DataGridTextColumn
            {
                Header = new CommentHeader(cultureKey),
                Binding = new Binding(@"Comments[" + key + @"]"),
                Width = 300,
                Visibility = settings.VisibleCommentColumns.Split(',').Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Hidden
            };

            AddLanguageColumn(columns, cultureKey, commentColumn);

            var column = new DataGridTextColumn
            {
                Header = new LanguageHeader(cultureKey),
                Binding = new Binding(@"Values[" + key + @"]"),
                Width = 300,
                Visibility = settings.HiddenLanguageColumns.Split(',').Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Hidden : Visibility.Visible
            };

            AddLanguageColumn(columns, cultureKey, column);
        }

        private static void AddLanguageColumn(ICollection<DataGridColumn> columns, CultureKey cultureKey, DataGridBoundColumn column)
        {
            Contract.Requires(columns != null);
            Contract.Requires(cultureKey != null);
            Contract.Requires(column != null);

            column.EnableMultilineEditing();
            column.EnableSpellchecker(cultureKey.Culture);
            columns.Add(column);
        }

        private static void DataGrid_ColumnVisibilityChanged(object sender, RoutedEventArgs e)
        {
            Contract.Requires(sender != null);

            var dataGrid = (DataGrid)sender;
            var settings = Settings.Default;

            settings.VisibleCommentColumns = string.Join(",", dataGrid.Columns
                .Where(col => col.Visibility == Visibility.Visible)
                .Select(col => col.Header)
                .OfType<CommentHeader>()
                .Select(hdr => hdr.CultureKey.ToString(".")));

            settings.HiddenLanguageColumns = string.Join(",", dataGrid.Columns
                .Where(col => col.Visibility != Visibility.Visible)
                .Select(col => col.Header)
                .OfType<LanguageHeader>()
                .Select(hdr => hdr.CultureKey.ToString(".")));
        }
    }
}
