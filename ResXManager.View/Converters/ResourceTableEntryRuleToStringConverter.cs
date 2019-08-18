namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Markup;

    using tomenglertde.ResXManager.Model;

    [ValueConversion(typeof(IResourceTableEntryRuleConfig), typeof(string))]
    public sealed class ResourceTableEntryRuleToStringConverter : MarkupExtension, IValueConverter
    {
        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IResourceTableEntryRuleConfig rule)) return value;

            Enum.TryParse(parameter?.ToString(), out ResourceTableEntryRuleToStringConverterParam param);
            return param == ResourceTableEntryRuleToStringConverterParam.Description ? rule.Description : rule.Name;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
