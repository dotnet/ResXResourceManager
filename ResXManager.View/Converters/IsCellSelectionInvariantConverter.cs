namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Data;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.View.Tools;

    public sealed class IsCellSelectionInvariantConverter : IValueConverter
    {
        [NotNull]
        public static readonly IsCellSelectionInvariantConverter Default = new IsCellSelectionInvariantConverter();

        [CanBeNull]
        object IValueConverter.Convert([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            return (value as IEnumerable<DataGridCellInfo>)?.Any(item => item.IsItemInvariant());
        }

        [CanBeNull]
        object IValueConverter.ConvertBack([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
