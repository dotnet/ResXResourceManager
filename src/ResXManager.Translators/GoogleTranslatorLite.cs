namespace ResXManager.Translators;

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

using ResXManager.Infrastructure;

using TomsToolbox.Wpf.Composition.AttributedModel;

[DataTemplate(typeof(GoogleTranslatorLite))]
public class GoogleTranslatorLiteConfiguration : Decorator
{
}

[Export(typeof(ITranslator)), Shared]
public class GoogleTranslatorLite() : TranslatorBase("GoogleLite", "Google Lite", _uri, null)
{
    private static readonly Uri _uri = new("https://translate.google.com/");

    protected override async Task Translate(ITranslationSession translationSession)
    {
        foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
        {
            if (translationSession.IsCanceled)
                break;

            var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;

            foreach (var sourceItem in languageGroup)
            {
                if (translationSession.IsCanceled)
                    break;

                var parameters = new List<string?>(30);
                parameters.AddRange(
                [
                    "client", "gtx",
                    "dt", "t",
                    "sl", GoogleLangCode(translationSession.SourceLanguage),
                    "tl", GoogleLangCode(targetCulture),
                    "q", RemoveKeyboardShortcutIndicators(sourceItem.Source)
                ]);

                var response = await GetHttpResponse("https://translate.googleapis.com/translate_a/single", parameters, translationSession.CancellationToken).ConfigureAwait(false);

                await translationSession.MainThread.StartNew(() => { sourceItem.Results.Add(new TranslationMatch(this, response, Ranking)); }).ConfigureAwait(false);
            }
        }
    }

    private static string GoogleLangCode(CultureInfo cultureInfo)
    {
        var iso1 = cultureInfo.TwoLetterISOLanguageName;
        var name = cultureInfo.Name;

        string[] twCultures = ["zh-hant", "zh-cht", "zh-hk", "zh-mo", "zh-tw"];
        if (string.Equals(iso1, "zh", StringComparison.OrdinalIgnoreCase))
            return twCultures.Contains(name, StringComparer.OrdinalIgnoreCase) ? "zh-TW" : "zh-CN";

        if (string.Equals(name, "haw-us", StringComparison.OrdinalIgnoreCase))
            return "haw";

        return iso1;
    }

    private static async Task<string> GetHttpResponse(string baseUrl, ICollection<string?> parameters, CancellationToken cancellationToken)
    {
        var url = BuildUrl(baseUrl, parameters);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return ParseResponse(result);
    }

    public static string ParseResponse(string result)
    {
        var node = JsonNode.Parse(result);

        if ((node is JsonArray level1) && (level1.FirstOrDefault() is JsonArray level2))
        {
            return string.Concat(level2.OfType<JsonArray>().Select(item => item.FirstOrDefault()));
        }

        return string.Empty;
    }

    /// <summary>Builds the URL from a base, method name, and name/value paired parameters. All parameters are encoded.</summary>
    /// <param name="url">The base URL.</param>
    /// <param name="pairs">The name/value paired parameters.</param>
    /// <returns>Resulting URL.</returns>
    /// <exception cref="ArgumentException">There must be an even number of strings supplied for parameters.</exception>
    private static string BuildUrl(string url, ICollection<string?> pairs)
    {
        if (pairs.Count % 2 != 0)
            throw new ArgumentException("There must be an even number of strings supplied for parameters.");

        if (pairs.Count <= 0) 
            return string.Empty;

        var sb = new StringBuilder(url);
        sb.Append('?');
        sb.Append(string.Join("&", pairs.Where((s, i) => i % 2 == 0).Zip(pairs.Where((s, i) => i % 2 == 1), Format)));
        return sb.ToString();

        static string Format(string? a, string? b)
        {
            return string.Concat(WebUtility.UrlEncode(a), "=", WebUtility.UrlEncode(b));
        }
    }
}
