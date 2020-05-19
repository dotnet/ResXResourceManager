namespace ResXManager.Infrastructure
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

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

        /// <summary>
        /// Tests whether the string contains HTML
        /// </summary>
        /// <returns>
        /// True if the contains HTML; otherwise false
        /// </returns>
        public static bool ContainsHtml(this string text)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(text);
            return !doc.DocumentNode.Descendants().All(n => n.NodeType == HtmlNodeType.Text);
        }

    }
}
