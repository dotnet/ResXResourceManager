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
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Controls;
    using tomenglertde.ResXManager.View.Properties;

    public static class ColumnManager
    {
        public static void SetupColumns(this DataGrid dataGrid, IEnumerable<CultureInfo> languages)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(languages != null);

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

            var disconnectedColumns = languageColumns.Where(col => languages.All(language => !Equals(((ILanguageColumnHeader)col.Header).Language, language)));

            foreach (var column in disconnectedColumns)
            {
                columns.Remove(column);
            }

            var addedColumns = languages.Where(language => languageColumns.All(col => !Equals(((ILanguageColumnHeader)col.Header).Language, language)));

            foreach (var language in addedColumns)
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

        public static void AddLanguageColumn(this ICollection<DataGridColumn> columns, CultureInfo language)
        {
            Contract.Requires(columns != null);
            var key = language != null ? @"." + language : String.Empty;

            var isFirstCommentColumn = !columns.Any(col => col.Header is CommentHeader);

            var commentColumn = new DataGridTextColumn
            {
                Header = new CommentHeader(language),
                Binding = new Binding(@"Comments[" + key + @"]"),
                Width = 300,
                Visibility = isFirstCommentColumn ? Visibility.Visible : Visibility.Hidden
            };

            commentColumn.EnableMultilineEditing();
            commentColumn.EnableSpellchecker(language);
            columns.Add(commentColumn);

            var column = new DataGridTextColumn
            {
                Header = new LanguageHeader(language),
                Binding = new Binding(@"Values[" + key + @"]"),
                Width = 300,
            };

            column.EnableMultilineEditing();
            column.EnableSpellchecker(language);
            columns.Add(column);
        }
    }
}
