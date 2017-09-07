namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Data;

    using tomenglertde.ResXManager.Model;
    using tomenglertde.ResXManager.View.ColumnHeaders;
    using tomenglertde.ResXManager.View.Tools;

    public class IsCellSelectionInvariantConverter : IValueConverter
    {
        public static readonly IsCellSelectionInvariantConverter Default = new IsCellSelectionInvariantConverter();

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value as IEnumerable<DataGridCellInfo>);
        }

        private object Convert(IEnumerable<DataGridCellInfo> cellInfos)
        {
            return cellInfos.IsAnyItemInvariant();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
