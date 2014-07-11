namespace tomenglertde.ResXManager.View
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using tomenglertde.ResXManager.View.Properties;

    public class CultureToImageSourceConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param><param name="targetType">The type of the binding target property.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value);
        }

        internal static ImageSource Convert(object value)
        {
            var culture = value as CultureInfo;

            if (culture == null)
                culture = Settings.Default.NeutralResourceLanguage;

            Contract.Assume(culture != null);

            var cultureName = culture.Name;

            if (culture.IsNeutralCulture)
            {
                // Find a specific culture as we need the specific part of the name.
                // If a specific culture exists with "subtag == primary tag", use this, else fallback to the first.
                var preferredSpecificCultureName = cultureName + @"-" + cultureName.ToUpperInvariant();
                
                var allSpecificCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
                Contract.Assume(allSpecificCultures != null);

                var specificCultures = allSpecificCultures.Where(c => (c != null) && ((c.Parent.Name == cultureName) || (c.Parent.IetfLanguageTag == cultureName))).ToArray();

                culture = specificCultures.FirstOrDefault(c => c.Name.Equals(preferredSpecificCultureName, StringComparison.OrdinalIgnoreCase)) ?? specificCultures.FirstOrDefault();

                if (culture != null)
                    cultureName = culture.Name;
            }

            var cultureParts = cultureName.Split('-');
            if (cultureParts.Length != 2)
                return null;

            var key = cultureParts[1];

            var resourcePath = string.Format(CultureInfo.InvariantCulture, @"/ResXManager.View;component/Flags/{0}.gif", key);
            var imageSource = new BitmapImage(new Uri(resourcePath, UriKind.RelativeOrAbsolute));

            return imageSource;
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param><param name="targetType">The type to convert to.</param><param name="parameter">The converter parameter to use.</param><param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
