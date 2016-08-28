namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    using tomenglertde.ResXManager.View.ColumnHeaders;

    public class LanguageColumnFilterConverter : IValueConverter
    {
        public static readonly IValueConverter Default = new LanguageColumnFilterConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collectionViewSource = new CollectionViewSource() { Source = value };
            var collectionView = collectionViewSource.View;
            if (collectionView != null)
                collectionView.Filter = Filter;

            return collectionView;
        }

        private static bool Filter(object item)
        {
            return ((DataGridColumn)item)?.Header is ILanguageColumnHeader;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
