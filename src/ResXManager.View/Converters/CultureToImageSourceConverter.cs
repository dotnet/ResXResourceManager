namespace ResXManager.View.Converters;

using System;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ResXManager.Model;
using ResXManager.View.Tools;

using TomsToolbox.Essentials;

[Export, Shared]
public class CultureToImageSourceConverter : IValueConverter
{
    private readonly IConfiguration _configuration;
    private readonly CultureCountryOverrides _cultureCountryOverrides;

    private static readonly string[] _existingFlags =
    {
        "ad", "ae", "af", "ag", "ai", "al", "am", "an", "ao", "ar", "as", "at", "au", "aw", "ax", "az", "ba", "bb", "bd", "be", "bf",
        "bg", "bh", "bi", "bj", "bm", "bn", "bo", "br", "bs", "bt", "bv", "bw", "by", "bz", "ca", "cc", "cd", "cf", "cg", "ch", "ci",
        "ck", "cl", "cm", "cn", "co", "cr", "cs", "cu", "cv", "cx", "cy", "cz", "de", "dj", "dk", "dm", "do", "dz", "ec", "ee", "eg",
        "eh", "er", "es", "et", "fi", "fj", "fk", "fm", "fo", "fr", "fy", "ga", "gb", "gd", "ge", "gf", "gh", "gi", "gl", "gm",
        "gn", "gp", "gq", "gr", "gs", "gt", "gu", "gw", "gy", "hk", "hm", "hn", "hr", "ht", "hu", "id", "ie", "il", "in", "io", "iq",
        "ir", "is", "it", "jm", "jo", "jp", "ke", "kg", "kh", "ki", "km", "kn", "kp", "kr", "kw", "ky", "kz", "la", "lb", "lc", "li",
        "lk", "lr", "ls", "lt", "lu", "lv", "ly", "ma", "mc", "md", "me", "mg", "mh", "mk", "ml", "mm", "mn", "mo", "mp", "mq", "mr",
        "ms", "mt", "mu", "mv", "mw", "mx", "my", "mz", "na", "nc", "ne", "nf", "ng", "ni", "nl", "no", "np", "nr", "nu", "nz", "om",
        "pa", "pe", "pf", "pg", "ph", "pk", "pl", "pm", "pn", "pr", "ps", "pt", "pw", "py", "qa", "re", "ro", "rs", "ru", "rw", "sa",
        "sb", "sc", "sd", "se", "sg", "sh", "si", "sj", "sk", "sl", "sm", "sn", "so", "sr", "st", "sv", "sy", "sz", "tc", "td", "tf",
        "tg", "th", "tj", "tk", "tl", "tm", "tn", "to", "tr", "tt", "tv", "tw", "tz", "ua", "ug", "um", "us", "uy", "uz", "va", "vc",
        "ve", "vg", "vi", "vn", "vu", "wf", "ws", "ye", "yt", "za", "zm", "zw"
    };

    [ImportingConstructor]
    public CultureToImageSourceConverter(IConfiguration configuration, CultureCountryOverrides cultureCountryOverrides)
    {
        _configuration = configuration;
        _cultureCountryOverrides = cultureCountryOverrides;
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

    internal ImageSource? Convert(CultureInfo? culture)
    {
        culture ??= _configuration.NeutralResourcesLanguage;

        return Convert(culture, 0);
    }

    private ImageSource? Convert(CultureInfo culture, int recursionCounter)
    {
        var cultureName = culture.Name;

        var countryOverride = _cultureCountryOverrides[culture];

        if (countryOverride != null)
        {
            culture = countryOverride;
            cultureName = culture.Name;
        }

        var cultureParts = cultureName.Split('-');
        if (!cultureParts.Any())
            return null;

        var key = cultureParts.Last();

        if (Array.BinarySearch(_existingFlags, key, StringComparer.OrdinalIgnoreCase) < 0)
        {
            var bestMatch = culture.GetDescendants()
                .Select(item => Convert(item, recursionCounter))
                .FirstOrDefault(item => item != null);

            if (bestMatch is null && recursionCounter < 3 && !culture.IsNeutralCulture)
            {
                return Convert(culture.Parent, recursionCounter + 1);
            }

            return bestMatch;
        }

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
    public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotImplementedException();
    }
}
