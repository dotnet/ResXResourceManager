namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    using JetBrains.Annotations;

    public class CultureToImageConverter : IValueConverter
    {
        [NotNull]
        public static readonly IValueConverter Default = new CultureToImageConverter();

        [NotNull]
        public object Convert([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            var imageSource = CultureToImageSourceConverter.Convert(value as CultureInfo);

            return new Image { Source = imageSource };
        }

        [CanBeNull]
        public object ConvertBack([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
