namespace ResXManager.View.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    using JetBrains.Annotations;

    using ResXManager.View.ColumnHeaders;

    public class LanguageColumnFilterConverter : IValueConverter
    {
        [NotNull]
        public static readonly IValueConverter Default = new LanguageColumnFilterConverter();

        [CanBeNull]
        public object Convert([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            var collectionViewSource = new CollectionViewSource { Source = value };
            var collectionView = collectionViewSource.View;
            if (collectionView != null)
                collectionView.Filter = Filter;

            return collectionView;
        }

        private static bool Filter([CanBeNull] object item)
        {
            return ((DataGridColumn)item)?.Header is ILanguageColumnHeader;
        }

        [CanBeNull]
        public object ConvertBack([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
