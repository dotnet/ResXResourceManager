namespace ResXManager.View.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;
    using ResXManager.Model;
    using ResXManager.View.ColumnHeaders;

    public class DataGridToSelectionScopeConverter : IValueConverter
    {
        [NotNull]
        public static readonly IValueConverter Default = new DataGridToSelectionScopeConverter();

        [NotNull]
        public object Convert([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            return new DataGridSelectionScope(value as DataGrid);
        }

        [CanBeNull]
        public object ConvertBack([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private class DataGridSelectionScope : IResourceScope, IExportParameters
        {
            [CanBeNull]
            private readonly DataGrid _dataGrid;

            public DataGridSelectionScope([CanBeNull] DataGrid dataGrid)
            {
                _dataGrid = dataGrid;
            }

            public IEnumerable<ResourceTableEntry> Entries
            {
                get
                {
                    if (_dataGrid == null)
                        return Enumerable.Empty<ResourceTableEntry>();

                    return _dataGrid.SelectedItems.Cast<ResourceTableEntry>();
                }
            }

            public IEnumerable<CultureKey> Languages
            {
                get
                {
                    if (_dataGrid == null)
                        return Enumerable.Empty<CultureKey>();

                    return _dataGrid.Columns
                        .Where(col => col.Visibility == Visibility.Visible)
                        .Select(col => col.Header)
                        .OfType<LanguageHeader>()
                        .Select(hdr => hdr.CultureKey);
                }
            }

            public IEnumerable<CultureKey> Comments
            {
                get
                {
                    if (_dataGrid == null)
                        return Enumerable.Empty<CultureKey>();

                    return _dataGrid.Columns
                        .Where(col => col.Visibility == Visibility.Visible)
                        .Select(col => col.Header)
                        .OfType<CommentHeader>()
                        .Select(hdr => hdr.CultureKey);
                }
            }

            [NotNull]
            public IResourceScope Scope => this;

            [CanBeNull]
            public string FileName => null;
        }
    }
}
