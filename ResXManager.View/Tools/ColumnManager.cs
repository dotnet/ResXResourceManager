namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;
    using DataGridExtensions;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.Behaviors;
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Controls;
    using tomenglertde.ResXManager.View.Properties;

    public static class ColumnManager
    {
        private static readonly BitmapImage _codeReferencesImage = new BitmapImage(new Uri("/ResXManager.View;component/Assets/references.png", UriKind.RelativeOrAbsolute));

        public static void SetupColumns(this DataGrid dataGrid, IEnumerable<CultureKey> cultureKeys)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(cultureKeys != null);

            var dataGridEvents = dataGrid.GetAdditionalEvents();

            dataGridEvents.ColumnVisibilityChanged -= DataGrid_ColumnVisibilityChanged;
            dataGridEvents.ColumnVisibilityChanged += DataGrid_ColumnVisibilityChanged;

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

            var disconnectedColumns = languageColumns.Where(col => cultureKeys.All(cultureKey => !Equals(col.GetCultureKey(), cultureKey)));

            foreach (var column in disconnectedColumns)
            {
                columns.Remove(column);
            }

            var addedLanguages = cultureKeys.Where(language => languageColumns.All(col => !Equals(col.GetCultureKey(), language)));

            foreach (var language in addedLanguages)
            {
                AddLanguageColumn(columns, language);
            }
        }

        private static Image CreateCodeReferencesImage()
        {
            return new Image { Source = _codeReferencesImage, SnapsToDevicePixels = true };
        }

        private static DataGridColumn CreateCodeReferencesColumn(FrameworkElement dataGrid)
        {
            Contract.Requires(dataGrid != null);

            var elementStyle = new Style();
            elementStyle.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, new CodeReferencesToolTip()));
            elementStyle.Setters.Add(new Setter(ToolTipService.ShowDurationProperty, Int32.MaxValue));
            elementStyle.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center));

            var columnHeader = new ColumnHeader(CreateCodeReferencesImage(), ColumnType.Other)
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

        private static void DataGrid_ColumnVisibilityChanged(object sender, EventArgs e)
        {
            Contract.Requires(sender != null);

            var dataGrid = (DataGrid)sender;
            var settings = Settings.Default;

            settings.VisibleCommentColumns = UpdateColumnSettings<CommentHeader>(settings.VisibleCommentColumns, dataGrid, col => col.Visibility == Visibility.Visible);
            settings.HiddenLanguageColumns  = UpdateColumnSettings<LanguageHeader>(settings.HiddenLanguageColumns, dataGrid, col => col.Visibility != Visibility.Visible);
        }

        private static string UpdateColumnSettings<T>(string current, DataGrid dataGrid, Func<DataGridColumn, bool> includePredicate)
            where T : LanguageColumnHeaderBase
        {
            return string.Join(",", current.Split(',')
                .Concat(GetColumnKeys<T>(dataGrid, includePredicate))
                .Except(GetColumnKeys<T>(dataGrid, col => !includePredicate(col)))
                .Distinct());

        }

        private static IEnumerable<string> GetColumnKeys<T>(DataGrid dataGrid, Func<DataGridColumn, bool> predicate)
            where T : LanguageColumnHeaderBase
        {
            return dataGrid.Columns
                .Where(predicate)
                .Select(col => col.Header)
                .OfType<T>()
                .Select(hdr => hdr.CultureKey.ToString("."));
        }
    }
}
