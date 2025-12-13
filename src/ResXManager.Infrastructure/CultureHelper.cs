namespace ResXManager.Infrastructure;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using TomsToolbox.Essentials;

public static class CultureHelper
{
    [return: NotNullIfNotNull("languageName")]
    public static CultureInfo? CreateCultureInfo(string? languageName)
    {
        if (languageName is null)
            return null;

        return int.TryParse(languageName, out var lcid) ? new CultureInfo(lcid) : new CultureInfo(languageName);
    }

    public static bool IsValidCultureName(string? languageName)
    {
        try
        {
            if (languageName.IsNullOrEmpty())
                return false;

            // pseudo-locales:
            if (languageName.StartsWith("qps-", StringComparison.Ordinal))
                return true;

            // #376: support Custom dialect resource
            var culture = CreateCultureInfo(languageName);
            while (!culture.IsNeutralCulture)
            {
                culture = culture.Parent;
            }

            return WellKnownNeutralCultures.Contains(culture.Name);
        }
        catch
        {
            return false;
        }
    }

    private static class WellKnownNeutralCultures
    {
        private static readonly string[] _sortedNeutralCultureNames = GetSortedNeutralCultureNames();

        public static bool Contains(string cultureName)
        {
            return Array.BinarySearch(_sortedNeutralCultureNames, cultureName, StringComparer.OrdinalIgnoreCase) >= 0;
        }

        private static string[] GetSortedNeutralCultureNames()
        {
            var allCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

            var cultureNames = allCultures.Select(culture => culture.IetfLanguageTag)
                .Concat(allCultures.Select(culture => culture.Name))
                .Distinct()
                .ToArray();

            Array.Sort(cultureNames, StringComparer.OrdinalIgnoreCase);

            return cultureNames;
        }
    }

    /// <summary>
    /// Gets all system specific cultures.
    /// </summary>
    public static IEnumerable<CultureInfo> SpecificCultures => WellKnownSpecificCultures.Value;

    private static class WellKnownSpecificCultures
    {
        public static readonly CultureInfo[] Value = GetSpecificCultures();

        private static CultureInfo[] GetSpecificCultures()
        {
            var specificCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(c => c.GetAncestors().Any())
                .OrderBy(c => c.DisplayName)
                .ToArray();

            return specificCultures;
        }
    }

}
