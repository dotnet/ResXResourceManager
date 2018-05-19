namespace tomenglertde.ResXManager.View.Converters
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var source = value as CultureInfo;
            return source != null ? XmlLanguage.GetLanguage(source.IetfLanguageTag) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
