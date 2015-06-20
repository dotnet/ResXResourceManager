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
    using System.Windows.Media.Imaging;

    using DataGridExtensions;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Controls;
    using tomenglertde.ResXManager.View.Converters;
    using tomenglertde.ResXManager.View.Properties;

    using TomsToolbox.Desktop;

    using BooleanToVisibilityConverter = TomsToolbox.Wpf.Converters.BooleanToVisibilityConverter;

    public static class ColumnManager
    {
        private const string NeutralCultureKeyString = ".";
        private static readonly BitmapImage _codeReferencesImage = new BitmapImage(new Uri("/ResXManager.View;component/Assets/references.png", UriKind.RelativeOrAbsolute));

        public static bool GetResourceFileExists(DependencyObject obj)
        {
            Contract.Requires(obj != null);
            return obj.GetValue<bool>(ResourceFileExistsProperty);
        }
        public static void SetResourceFileExists(DependencyObject obj, bool value)
        {
            Contract.Requires(obj != null);
            obj.SetValue(ResourceFileExistsProperty, value);
        }
        /// <summary>
        /// Identifies the ResourceFileExists dependency property
        /// </summary>
        public static readonly DependencyProperty ResourceFileExistsProperty =
            DependencyProperty.RegisterAttached("ResourceFileExists", typeof(bool), typeof(ColumnManager), new FrameworkPropertyMetadata(true));

        public static void SetupColumns(this DataGrid dataGrid, IEnumerable<CultureKey> cultureKeys)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(cultureKeys != null);

            var dataGridEvents = dataGrid.GetAdditionalEvents();
            var resourceManager = (ResourceManager)dataGrid.DataContext;
            Contract.Assume(resourceManager != null);

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

            var addedcultureKeys = cultureKeys.Where(cultureKey => languageColumns.All(col => !Equals(col.GetCultureKey(), cultureKey)));

            foreach (var cultureKey in addedcultureKeys)
            {
                Contract.Assume(cultureKey != null);
                dataGrid.AddLanguageColumn(resourceManager, cultureKey);
            }
        }

        public static void CreateNewLanguageColumn(this DataGrid dataGrid, ResourceManager resourceManager, CultureInfo culture)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(resourceManager != null);

            var cultureKey = new CultureKey(culture);

            AddLanguageColumn(dataGrid, resourceManager, cultureKey);

            var key = cultureKey.ToString(NeutralCultureKeyString);

            HiddenLanguageColumns = HiddenLanguageColumns.Where(col => !string.Equals(col, key, StringComparison.OrdinalIgnoreCase));
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
                Width = DataGridLength.SizeToHeader,
                CanUserReorder = false,
                CanUserResize = false,
                IsReadOnly = true,
            };

            column.SetIsFilterVisible(false);
            BindingOperations.SetBinding(column, DataGridColumn.VisibilityProperty, new Binding(@"IsFindCodeReferencesEnabled") { Source = Settings.Default, Converter = BooleanToVisibilityConverter.Default });

            return column;
        }

        private static void AddLanguageColumn(this DataGrid dataGrid, ResourceManager resourceManager, CultureKey cultureKey)
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(resourceManager != null);
            Contract.Requires(cultureKey != null);

            var columns = dataGrid.Columns;

            var key = cultureKey.ToString(NeutralCultureKeyString);

            var culture = cultureKey.Culture;
            var languageBinding = culture != null
                ? new Binding { Source = culture }
                : new Binding("Configuration.NeutralResourcesLanguage") { Source = resourceManager };

            languageBinding.Converter = CultureToXmlLanguageConverter.Default;
            // It's important to explicitly set the converter culture here, else we will get a binding error, because here the source for the converter culture is the target of the binding.
            languageBinding.ConverterCulture = CultureInfo.InvariantCulture;

            var flowDirectionBinding = culture != null
                ? new Binding("TextInfo.IsRightToLeft") { Source = culture }
                : new Binding("Configuration.NeutralResourcesLanguage.TextInfo.IsRightToLeft") { Source = resourceManager };

            flowDirectionBinding.Converter = IsRightToLeftToFlowDirectionConverter.Default;

            var cellStyle = new Style(typeof(DataGridCell), dataGrid.CellStyle);
            cellStyle.Setters.Add(new Setter(ResourceFileExistsProperty, new Binding(@"FileExists[" + key + @"]")));

            var commentColumn = new DataGridTextColumn
            {
                Header = new CommentHeader(resourceManager, cultureKey),
                Binding = new Binding(@"Comments[" + key + @"]"),
                MinWidth = 50,
                CellStyle = cellStyle,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Visibility = VisibleCommentColumns.Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Hidden
            };

            columns.AddLanguageColumn(commentColumn, languageBinding, flowDirectionBinding);

            var column = new DataGridTextColumn
            {
                Header = new LanguageHeader(resourceManager, cultureKey),
                Binding = new Binding(@"Values[" + key + @"]"),
                MinWidth = 50,
                CellStyle = cellStyle,
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                Visibility = HiddenLanguageColumns.Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Hidden : Visibility.Visible
            };

            columns.AddLanguageColumn(column, languageBinding, flowDirectionBinding);
        }

        private static void AddLanguageColumn(this ICollection<DataGridColumn> columns, DataGridBoundColumn column, Binding languageBinding, Binding flowDirectionBinding)
        {
            Contract.Requires(columns != null);
            Contract.Requires(languageBinding != null);
            Contract.Requires(column != null);

            column.SetElementStyle(languageBinding, flowDirectionBinding);
            column.SetEditingElementStyle(languageBinding, flowDirectionBinding);
            columns.Add(column);
        }

        private static void DataGrid_ColumnVisibilityChanged(object sender, EventArgs e)
        {
            Contract.Requires(sender != null);

            var dataGrid = (DataGrid)sender;

            VisibleCommentColumns = UpdateColumnSettings<CommentHeader>(VisibleCommentColumns, dataGrid, col => col.Visibility == Visibility.Visible);
            HiddenLanguageColumns = UpdateColumnSettings<LanguageHeader>(HiddenLanguageColumns, dataGrid, col => col.Visibility != Visibility.Visible);
        }

        private static IEnumerable<string> UpdateColumnSettings<T>(IEnumerable<string> current, DataGrid dataGrid, Func<DataGridColumn, bool> includePredicate)
            where T : LanguageColumnHeaderBase
        {
            Contract.Requires(current != null);
            Contract.Requires(dataGrid != null);
            Contract.Requires(includePredicate != null);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

            Func<DataGridColumn, bool> excludePredicate = col => !includePredicate(col);

            return current.Concat(GetColumnKeys<T>(dataGrid, includePredicate))
                .Except(GetColumnKeys<T>(dataGrid, excludePredicate))
                .Distinct();
        }

        private static IEnumerable<string> GetColumnKeys<T>(DataGrid dataGrid, Func<DataGridColumn, bool> predicate)
            where T : LanguageColumnHeaderBase
        {
            Contract.Requires(dataGrid != null);
            Contract.Requires(predicate != null);

            return dataGrid.Columns
                .Where(predicate)
                .Select(col => col.Header)
                .OfType<T>()
                .Select(hdr => hdr.CultureKey.ToString(NeutralCultureKeyString));
        }

        private static IEnumerable<string> VisibleCommentColumns
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

                return (Settings.Default.VisibleCommentColumns ?? string.Empty).Split(',');
            }
            set
            {
                Contract.Requires(value != null);

                Settings.Default.VisibleCommentColumns = string.Join(",", value);
            }
        }

        private static IEnumerable<string> HiddenLanguageColumns
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);

                return (Settings.Default.HiddenLanguageColumns ?? string.Empty).Split(',');
            }
            set
            {
                Contract.Requires(value != null);

                Settings.Default.HiddenLanguageColumns = string.Join(",", value);
            }
        }

        class IsRightToLeftToFlowDirectionConverter : IValueConverter
        {
            public static readonly IValueConverter Default = new IsRightToLeftToFlowDirectionConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return true.Equals(value) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
