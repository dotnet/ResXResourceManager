namespace ResXManager.View.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Markup;

    using JetBrains.Annotations;

    public class CultureToXmlLanguageConverter : IValueConverter
    {
        [NotNull]
        public static readonly IValueConverter Default = new CultureToXmlLanguageConverter();

        [CanBeNull]
        public object Convert([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            return value is CultureInfo source ? XmlLanguage.GetLanguage(source.IetfLanguageTag) : null;
        }

        [CanBeNull]
        public object ConvertBack([CanBeNull] object value, [CanBeNull] Type targetType, [CanBeNull] object parameter, [CanBeNull] CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
