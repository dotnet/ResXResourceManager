namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows.Controls;
    using System.Windows.Data;

    using JetBrains.Annotations;

    public class CultureToImageConverter : IValueConverter
    {
        public static readonly IValueConverter Default = new CultureToImageConverter();

        [NotNull]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Contract.Ensures(Contract.Result<object>() != null);
            var imageSource = CultureToImageSourceConverter.Convert(value as CultureInfo);

            return new Image { Source = imageSource };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
