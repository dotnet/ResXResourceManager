namespace ResXManager.Translators;

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

using ResXManager.Infrastructure;

using TomsToolbox.Essentials;
using TomsToolbox.Wpf.Composition.AttributedModel;

[DataTemplate(typeof(GoogleTranslatorLite))]
public class GoogleTranslatorLiteConfiguration : Decorator
{
}

[Export(typeof(ITranslator)), Shared]
public class GoogleTranslatorLite : TranslatorBase
{
    private static readonly Uri _uri = new("https://translate.google.com/");

    public GoogleTranslatorLite()
        : base("GoogleLite", "Google Lite", _uri, null)
    {
    }

    protected override async Task Translate(ITranslationSession translationSession)
    {
        foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
        {
            if (translationSession.IsCanceled)
                break;

            var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;

            using var itemsEnumerator = languageGroup.GetEnumerator();
            while (true)
            {
                var sourceItems = itemsEnumerator.Take(1);
                if (translationSession.IsCanceled || !sourceItems.Any())
                    break;

                var parameters = new List<string?>(30);
                // ReSharper disable once PossibleNullReferenceException
                parameters.AddRange(new[]
                {
                    "client", "gtx",
                    "dt", "t",
                    "sl", GoogleLangCode(translationSession.SourceLanguage),
                    "tl", GoogleLangCode(targetCulture),
                    "q", RemoveKeyboardShortcutIndicators(sourceItems[0].Source)
                });

                // ReSharper disable once AssignNullToNotNullAttribute
                var response = await GetHttpResponse(
                    "https://translate.googleapis.com/translate_a/single",
                    parameters,
                    translationSession.CancellationToken).ConfigureAwait(false);

                await translationSession.MainThread.StartNew(() =>
                {
                    Tuple<ITranslationItem, Translation> tuple = new(sourceItems[0], new Translation { TranslatedText = response });
                    tuple.Item1.Results.Add(new TranslationMatch(this, tuple.Item2.TranslatedText, Ranking));
                }).ConfigureAwait(false);

            }
        }
    }

    private static string GoogleLangCode(CultureInfo cultureInfo)
    {
        var iso1 = cultureInfo.TwoLetterISOLanguageName;
        var name = cultureInfo.Name;

        if (string.Equals(iso1, "zh", StringComparison.OrdinalIgnoreCase))
            return new[] { "zh-hant", "zh-cht", "zh-hk", "zh-mo", "zh-tw" }.Contains(name, StringComparer.OrdinalIgnoreCase) ? "zh-TW" : "zh-CN";

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

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods => not available in .NET Framework
        var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (JsonNode.Parse(result) is not { } jn0 ||
            jn0.AsArray() is not { } jarr0 ||
            jarr0.FirstOrDefault() is not { } jn1 ||
            jn1.AsArray() is not { } jarr1)
        {
            return result;
        }
        var sb = new StringBuilder();
        for (var i = 0; i < jarr1.Count; i++)
        {
            if (jarr1.ElementAt(i) is not { } item_i ||
                item_i.AsArray() is not { } item_i_arr ||
                !item_i_arr.Any() ||
                item_i_arr.FirstOrDefault() is not { } item_i_0 ||
                item_i_0.ToString() is not { } text)
            {
                continue;
            }
            sb.Append(text);
        }
        return Regex.Unescape(sb.ToString());
    }

    [DataContract]
    private sealed class Translation
    {
        [DataMember(Name = "translatedText")]
        public string? TranslatedText { get; set; }
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

        var sb = new StringBuilder(url);
        if (pairs.Count > 0)
        {
            sb.Append('?');
            sb.Append(string.Join("&", pairs.Where((s, i) => i % 2 == 0).Zip(pairs.Where((s, i) => i % 2 == 1), Format)));
        }
        return sb.ToString();

        static string Format(string? a, string? b)
        {
            return string.Concat(WebUtility.UrlEncode(a), "=", WebUtility.UrlEncode(b));
        }
    }
}