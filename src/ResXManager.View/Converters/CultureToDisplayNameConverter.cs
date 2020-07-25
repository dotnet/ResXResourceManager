namespace ResXManager.View.Converters
{
    using System;
    using System.Composition;
    using System.Globalization;
    using System.Windows.Data;

    using ResXManager.Model;

    [Export, Shared]
    public class CultureToDisplayNameConverter : IValueConverter
    {
        private readonly Configuration _configuration;

        [ImportingConstructor]
        public CultureToDisplayNameConverter(Configuration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            return Convert(value as CultureInfo);
        }

        private string? Convert(CultureInfo? culture)
        {
            if (culture == null)
            {
                culture = _configuration.NeutralResourcesLanguage;
            }

            return culture.DisplayName;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
