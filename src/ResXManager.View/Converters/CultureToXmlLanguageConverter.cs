namespace ResXManager.View.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Markup;

    public class CultureToXmlLanguageConverter : IValueConverter
    {
        public static readonly IValueConverter Default = new CultureToXmlLanguageConverter();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return value is CultureInfo source ? XmlLanguage.GetLanguage(source.IetfLanguageTag) : null;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
