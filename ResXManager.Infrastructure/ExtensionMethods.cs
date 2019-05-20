namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts the culture key name to the corresponding culture. The key name is the ieft language tag with an optional '.' prefix.
        /// </summary>
        /// <param name="cultureKeyName">Key name of the culture, optionally prefixed with a '.'.</param>
        /// <returns>
        /// The culture, or <c>null</c> if the key name is empty.
        /// </returns>
        /// <exception cref="InvalidOperationException">Error parsing language:  + cultureKeyName</exception>
        [CanBeNull]
        public static CultureInfo ToCulture([CanBeNull] this string cultureKeyName)
        {
            try
            {
                cultureKeyName = cultureKeyName?.TrimStart('.');

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
        [CanBeNull]
        public static CultureKey ToCultureKey([CanBeNull] this string cultureKeyName)
        {
            try
            {
                cultureKeyName = cultureKeyName?.TrimStart('.');

                return new CultureKey(string.IsNullOrEmpty(cultureKeyName) ? null : CultureInfo.GetCultureInfo(cultureKeyName));
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

        [CanBeNull]
        public static Regex TryCreateRegex([CanBeNull] this string expression)
        {
            try
            {
                if (!string.IsNullOrEmpty(expression))
                    return new Regex(expression, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            }
            catch
            {
                // invalid expression, ignore...
            }

            return null;
        }


    }
}
