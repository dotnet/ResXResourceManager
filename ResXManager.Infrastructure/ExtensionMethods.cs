namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.Globalization;

    using TomsToolbox.Core;

    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts the culture key name to the corresponding culture. The key name is the ieft language tag with an optional '.' prefix.
        /// </summary>
        /// <param name="cultureKeyName">Key name of the culture, optionally prefixed with a '.'.</param>
        /// <returns>
        /// The culture, or <c>null</c> if the key name is empty.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Error parsing language:  + cultureKeyName</exception>
        public static CultureInfo ToCulture(this string cultureKeyName)
        {
            try
            {
                cultureKeyName = cultureKeyName.Maybe().Return(c => c.TrimStart('.'));

                return string.IsNullOrEmpty(cultureKeyName) ? null : CultureInfo.GetCultureInfo(cultureKeyName);
            }
            catch (ArgumentException)
            {
            }

            throw new InvalidOperationException("Error parsing language: " + cultureKeyName);
        }

        /// <summary>
        /// Converts the culture key name to the corresponding culture. The key name is the ieft language tag with an optional '.' prefix.
        /// </summary>
        /// <param name="cultureKeyName">Key name of the culture, optionally prefixed with a '.'.</param>
        /// <returns>
        /// The cultureKey, or <c>null</c> if the culture is invalid.
        /// </returns>
        public static CultureKey ToCultureKey(this string cultureKeyName)
        {
            try
            {
                cultureKeyName = cultureKeyName.Maybe().Return(c => c.TrimStart('.'));

                return new CultureKey(string.IsNullOrEmpty(cultureKeyName) ? null : CultureInfo.GetCultureInfo(cultureKeyName));
            }
            catch (ArgumentException)
            {
            }

            return null;
        }
    }
}
