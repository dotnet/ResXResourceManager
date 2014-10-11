namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;

    public class DataGridToSelectionScopeConverter : IValueConverter
    {
        public static readonly IValueConverter Default = new DataGridToSelectionScopeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new DataGridSelectionScope(value as DataGrid);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        class DataGridSelectionScope : IResourceScope
        {
            private readonly DataGrid _dataGrid;

            public DataGridSelectionScope(DataGrid dataGrid)
            {
                _dataGrid = dataGrid;
            }

            public IEnumerable<ResourceTableEntry> Entries
            {
                get
                {
                    if (_dataGrid == null) 
                      return Enumerable.Empty<ResourceTableEntry>() ;

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
        }
    }
}
