namespace ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    using DataGridExtensions;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;
    using ResXManager.View.Converters;
    using ResXManager.View.Properties;
    using ResXManager.View.Visuals;

    using TomsToolbox.Wpf;
    using TomsToolbox.Wpf.Composition;
    using TomsToolbox.Wpf.Converters;

    using BooleanToVisibilityConverter = TomsToolbox.Wpf.Converters.BooleanToVisibilityConverter;

    public static class ColumnManager
    {
        private const string NeutralCultureKeyString = ".";
        private static readonly BitmapImage _codeReferencesImage = new(new Uri("/ResXManager.View;component/Assets/references.png", UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Identifies the ResourceFileExists attached property
        /// </summary>
        public static readonly DependencyProperty ResourceFileExistsProperty =
            DependencyProperty.RegisterAttached("ResourceFileExists", typeof(bool), typeof(ColumnManager), new FrameworkPropertyMetadata(true));

        public static ICollection<string> GetCellAnnotations(DependencyObject element)
        {
            return (ICollection<string>)element.GetValue(CellAnnotationsProperty);
        }
        public static void SetCellAnnotations(DependencyObject element, ICollection<string> value)
        {
            element.SetValue(CellAnnotationsProperty, value);
        }
        public static readonly DependencyProperty CellAnnotationsProperty =
            DependencyProperty.RegisterAttached("CellAnnotations", typeof(ICollection<string>), typeof(ColumnManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IsCellInvariantProperty =
            DependencyProperty.RegisterAttached("IsCellInvariant", typeof(bool), typeof(ColumnManager), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, IsCellInvariant_Changed));

        public static readonly DependencyProperty TranslationStateProperty =
            DependencyProperty.RegisterAttached("TranslationState", typeof(TranslationState?), typeof(ColumnManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, TranslationState_Changed));

        public static readonly DependencyProperty SelectedCellsProperty =
            DependencyProperty.RegisterAttached("SelectedCells", typeof(IList<DataGridCellInfo>), typeof(ColumnManager));

        public static void SetupColumns(this DataGrid dataGrid, ResourceManager resourceManager, ResourceViewModel resourceViewModel, IConfiguration configuration)
        {
            var dataGridEvents = dataGrid.GetAdditionalEvents();

            dataGridEvents.ColumnVisibilityChanged -= DataGrid_ColumnVisibilityChanged;
            dataGridEvents.ColumnVisibilityChanged += DataGrid_ColumnVisibilityChanged;

            dataGrid.CurrentCellChanged -= DataGrid_CurrentCellChanged;
            dataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;

            var columns = dataGrid.Columns;

            if (columns.Count == 0)
            {
                columns.Add(CreateKeyColumn());
                columns.Add(CreateIndexColumn(resourceViewModel, configuration));
                columns.Add(CreateCodeReferencesColumn(dataGrid));
            }

            var languageColumns = columns.Skip(3).ToArray();

            IEnumerable<CultureKey> cultureKeys = resourceManager.Cultures;

            var disconnectedColumns = languageColumns.Where(col => cultureKeys.All(cultureKey => !Equals(col?.GetCultureKey(), cultureKey)));

            foreach (var column in disconnectedColumns)
            {
                columns.Remove(column);
            }

            var addedCultureKeys = cultureKeys.Where(cultureKey => languageColumns.All(col => !Equals(col?.GetCultureKey(), cultureKey)));

            foreach (var cultureKey in addedCultureKeys)
            {
                dataGrid.AddLanguageColumn(configuration, cultureKey);
            }
        }

        public static void CreateNewLanguageColumn(this DataGrid dataGrid, IConfiguration configuration, CultureInfo? culture)
        {
            var cultureKey = new CultureKey(culture);

            if (!dataGrid.Columns.Any(col => Equals(col?.GetCultureKey(), cultureKey)))
            {
                dataGrid.AddLanguageColumn(configuration, cultureKey);
            }

            var key = cultureKey.ToString(NeutralCultureKeyString);

            HiddenLanguageColumns = HiddenLanguageColumns.Where(col => !string.Equals(col, key, StringComparison.OrdinalIgnoreCase));
        }

        private static DataGridTextColumn CreateKeyColumn()
        {
            return new DataGridTextColumn
            {
                Header = new ColumnHeader(Resources.Key, ColumnType.Key),
                Binding = new Binding(nameof(ResourceTableEntry.Key)) { ValidatesOnDataErrors = true },
                Width = 200,
                CanUserReorder = false,
                SortDirection = ListSortDirection.Ascending
            };
        }

        private static DataGridTextColumn CreateIndexColumn(ResourceViewModel? resourceViewModel, IConfiguration? configuration)
        {
            var elementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right),
                    new Setter(TextBlock.PaddingProperty, new Thickness(2, 0, 2, 0)),
                    new Setter(FrameworkElement.ToolTipProperty, Resources.IndexColumnToolTip)
                }
            };

            var columnHeader = new ColumnHeader("#", ColumnType.Other)
            {
                ToolTip = Resources.IndexColumnHeaderToolTip,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var column = new DataGridTextColumn
            {
                Header = columnHeader,
                ElementStyle = elementStyle,
                Width = 26,
                Binding = new Binding(@"Index"),
                CanUserReorder = false,
            };

            column.SetIsFilterVisible(false);

            BindingOperations.SetBinding(column, DataGridColumn.VisibilityProperty, new Binding(nameof(Settings.IsIndexColumnVisible)) { Source = Settings.Default, Converter = BooleanToVisibilityConverter.Default });
            BindingOperations.SetBinding(column, DataGridColumn.IsReadOnlyProperty, new MultiBinding
            {
                Converter = LogicalMultiValueConverter.Or,
                Bindings =
                {
                    new Binding(nameof(ResourceViewModel.SelectedEntities) + ".Count") { Source = resourceViewModel, Converter = BinaryOperationConverter.Inequality, ConverterParameter = 1 },
                    new Binding(nameof(IConfiguration.SortFileContentOnSave)) { Source = configuration }
                }
            });

            return column;
        }

        private static Image CreateCodeReferencesImage()
        {
            return new Image
            {
                Source = _codeReferencesImage,
                SnapsToDevicePixels = true
            };
        }

        private static DataGridColumn CreateCodeReferencesColumn(FrameworkElement dataGrid)
        {
            var elementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center)
                },
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding(@"CodeReferences.Count"),
                        Value = null,
                        Setters =
                        {
                            new Setter(UIElement.OpacityProperty, 0.3)
                        }
                    }
                }
            };

            var columnHeader = new ColumnHeader(CreateCodeReferencesImage(), ColumnType.Other)
            {
                ToolTip = Resources.CodeReferencesToolTip,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var cellStyle = new Style(typeof(DataGridCell))
            {
                Setters =
                {
                    new Setter(ToolTipService.ShowDurationProperty, int.MaxValue),
                    new Setter(FrameworkElement.ToolTipProperty, new CodeReferencesToolTip(dataGrid.GetExportProvider()))
                },
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding(@"CodeReferences.Count") { Converter = BinaryOperationConverter.GreaterThan, ConverterParameter = 50},
                        Value = true,
                        Setters =
                        {
                            new Setter(FrameworkElement.ToolTipProperty, Resources.CodeReferencesTooManyDetailsToolTip)
                        }
                    }
                }

            };

            var column = new DataGridTextColumn
            {
                Header = columnHeader,
                CellStyle = cellStyle,
                ElementStyle = elementStyle,
                Binding = new Binding(@"CodeReferences.Count") { FallbackValue = "?" },
                Width = DataGridLength.SizeToHeader,
                CanUserReorder = false,
                CanUserResize = false,
                IsReadOnly = true,
            };

            column.SetIsFilterVisible(false);
            BindingOperations.SetBinding(column, DataGridColumn.VisibilityProperty, new Binding(@"IsFindCodeReferencesEnabled") { Source = Model.Properties.Settings.Default, Converter = BooleanToVisibilityConverter.Default });

            return column;
        }

        private static void AddLanguageColumn(this DataGrid dataGrid, IConfiguration configuration, CultureKey cultureKey)
        {
            var columns = dataGrid.Columns;
            var key = cultureKey.ToString(NeutralCultureKeyString);

            var culture = cultureKey.Culture;
            var languageBinding = culture != null
                ? new Binding { Source = culture }
                : new Binding("NeutralResourcesLanguage") { Source = configuration };

            languageBinding.Converter = CultureToXmlLanguageConverter.Default;
            // It's important to explicitly set the converter culture here, else we will get a binding error, because here the source for the converter culture is the target of the binding.
            languageBinding.ConverterCulture = CultureInfo.InvariantCulture;

            var flowDirectionBinding = culture != null
                ? new Binding("TextInfo.IsRightToLeft") { Source = culture }
                : new Binding("NeutralResourcesLanguage.TextInfo.IsRightToLeft") { Source = configuration };

            flowDirectionBinding.Converter = IsRightToLeftToFlowDirectionConverter.Default;

            var cellStyle = new Style(typeof(DataGridCell), dataGrid.CellStyle);
            cellStyle.Setters.Add(new Setter(ResourceFileExistsProperty, new Binding(@"FileExists[" + key + @"]")));

            var commentCellStyle = new Style(typeof(DataGridCell), cellStyle);
            commentCellStyle.Setters.Add(new Setter(CellAnnotationsProperty, new Binding(@"CommentAnnotations[" + key + @"]")));

            var commentColumn = new DataGridTextColumn
            {
                Header = new CommentHeader(configuration, cultureKey),
                Binding = new Binding(@"Comments[" + key + @"]"),
                MinWidth = 50,
                CellStyle = commentCellStyle,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                Visibility = VisibleCommentColumns.Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Hidden
            };

            columns.AddLanguageColumn(commentColumn, languageBinding, flowDirectionBinding);

            var textCellStyle = new Style(typeof(DataGridCell), cellStyle)
            {
                Setters =
                {
                    new Setter(CellAnnotationsProperty, new Binding(@"ValueAnnotations[" + key + @"]")),
                    new Setter(IsCellInvariantProperty, new Binding(@"IsItemInvariant[" + key + @"]")),
                    new Setter(TranslationStateProperty, new Binding(@"TranslationState["+ key + @"]"))
                }
            };

            var textElementStyle = new Style(typeof(TextBlock))
            {
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding(nameof(Settings.IsWrapLinesEnabled)) { Source = Settings.Default },
                        Value = true,
                        Setters = { new Setter(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow ) }
                    }
                }
            };

            var column = new DataGridTextColumn
            {
                Header = new LanguageHeader(configuration, cultureKey),
                Binding = new Binding(@"Values[" + key + @"]"),
                MinWidth = 120,
                CellStyle = textCellStyle,
                ElementStyle = textElementStyle,
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                Visibility = HiddenLanguageColumns.Contains(key, StringComparer.OrdinalIgnoreCase) ? Visibility.Hidden : Visibility.Visible
            };

            columns.AddLanguageColumn(column, languageBinding, flowDirectionBinding);
        }

        private static void AddLanguageColumn(this ICollection<DataGridColumn> columns, DataGridBoundColumn column, Binding languageBinding, Binding? flowDirectionBinding)
        {
            column.SetElementStyle(languageBinding, flowDirectionBinding);
            column.SetEditingElementStyle(languageBinding, flowDirectionBinding);
            columns.Add(column);
        }

        private static void DataGrid_ColumnVisibilityChanged(object? sender, EventArgs e)
        {
            if (!(sender is DataGrid dataGrid))
                return;

            VisibleCommentColumns = UpdateColumnSettings<CommentHeader>(VisibleCommentColumns, dataGrid, col => col?.Visibility == Visibility.Visible);
            HiddenLanguageColumns = UpdateColumnSettings<LanguageHeader>(HiddenLanguageColumns, dataGrid, col => col?.Visibility != Visibility.Visible);
        }

        private static IEnumerable<string> UpdateColumnSettings<T>(IEnumerable<string> current, DataGrid dataGrid, Func<DataGridColumn, bool> includePredicate)
            where T : LanguageColumnHeaderBase
        {
            bool ExcludePredicate(DataGridColumn col) => !includePredicate(col);

            return current.Concat(GetColumnKeys<T>(dataGrid, includePredicate))
                .Except(GetColumnKeys<T>(dataGrid, ExcludePredicate))
                .Distinct();
        }

        private static IEnumerable<string> GetColumnKeys<T>(DataGrid dataGrid, Func<DataGridColumn, bool> predicate)
            where T : LanguageColumnHeaderBase
        {
            return dataGrid.Columns
                .Where(predicate)
                .Select(col => col?.Header)
                .OfType<T>()
                .Select(hdr => hdr.CultureKey.ToString(NeutralCultureKeyString));
        }

        private static IEnumerable<string> VisibleCommentColumns
        {
            get => (Settings.Default.VisibleCommentColumns ?? string.Empty).Split(',');
            set => Settings.Default.VisibleCommentColumns = string.Join(",", value);
        }

        private static IEnumerable<string> HiddenLanguageColumns
        {
            get => (Settings.Default.HiddenLanguageColumns ?? string.Empty).Split(',');
            set => Settings.Default.HiddenLanguageColumns = string.Join(",", value);
        }

        private static void DataGrid_CurrentCellChanged(object? sender, EventArgs eventArgs)
        {
            if (sender is not DataGrid dataGrid)
                return;

            // postpone update, SelectedCells is updates *after* the current cell has changed.
            dataGrid.Dispatcher?.BeginInvoke(() =>
            {
                dataGrid.SetValue(SelectedCellsProperty, dataGrid.GetSelectedVisibleCells().ToArray());
            });
        }

        private static void IsCellInvariant_Changed(DependencyObject? d, DependencyPropertyChangedEventArgs e)
        {
            ForceCurrentCellUpdate(d);
        }

        private static void TranslationState_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ForceCurrentCellUpdate(d);
        }

        private static void ForceCurrentCellUpdate(DependencyObject? d)
        {
            var dataGrid = d?.TryFindAncestorOrSelf<DataGrid>();

            if (dataGrid != null)
            {
                // force an update of the selected cells property, else the value converter won't get triggered.
                DataGrid_CurrentCellChanged(dataGrid, EventArgs.Empty);
            }
        }

        private class IsRightToLeftToFlowDirectionConverter : IValueConverter
        {
            public static readonly IValueConverter Default = new IsRightToLeftToFlowDirectionConverter();

            public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
            {
                return true.Equals(value) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }

            public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}