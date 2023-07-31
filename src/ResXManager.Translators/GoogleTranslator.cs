namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [DataTemplate(typeof(GoogleTranslator))]
    public class GoogleTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator)), Shared]
    public class GoogleTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new("https://developers.google.com/translate/");
        private static readonly IList<ICredentialItem> _credentialItems = new ICredentialItem[] { new CredentialItem("APIKey", "API Key") };

        public GoogleTranslator()
            : base("Google", "Google", _uri, _credentialItems)
        {
        }

        [DataMember(Name = "ApiKey")]
        public string? SerializedApiKey
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        private string? ApiKey => Credentials[0].Value;

        protected override async Task Translate(ITranslationSession translationSession)
        {
            if (ApiKey.IsNullOrEmpty())
            {
                translationSession.AddMessage("Google Translator requires API Key.");
                return;
            }

            foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
            {
                if (translationSession.IsCanceled)
                    break;

                var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;

                using var itemsEnumerator = languageGroup.GetEnumerator();
                while (true)
                {
                    var sourceItems = itemsEnumerator.Take(10);
                    if (translationSession.IsCanceled || !sourceItems.Any())
                        break;

                    // Build out list of parameters
                    var parameters = new List<string?>(30);
                    foreach (var item in sourceItems)
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        parameters.AddRange(new[] { "q", RemoveKeyboardShortcutIndicators(item.Source) });
                    }

                    parameters.AddRange(new[]
                    {
                        "target", GoogleLangCode(targetCulture),
                        "format", "text",
                        "source", GoogleLangCode(translationSession.SourceLanguage),
                        "model", "nmt",
                        "key", ApiKey
                    });

                    // Call the Google API
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var response = await GetHttpResponse<TranslationRootObject>(
                        "https://translation.googleapis.com/language/translate/v2",
                        parameters,
                        translationSession.CancellationToken).ConfigureAwait(false);

                    await translationSession.MainThread.StartNew(() =>
                    {
                        foreach (var tuple in sourceItems.Zip(response.Data?.Translations ?? Array.Empty<Translation>(), (a, b) => new Tuple<ITranslationItem, Translation>(a, b)))
                        {
                            tuple.Item1.Results.Add(new TranslationMatch(this, tuple.Item2.TranslatedText, Ranking));
                        }
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

        private static async Task<T> GetHttpResponse<T>(string baseUrl, ICollection<string?> parameters, CancellationToken cancellationToken)
            where T : class
        {
            var url = BuildUrl(baseUrl, parameters);

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

#pragma warning disable CA2016 // Forward the 'CancellationToken' parameter to methods => not available in NetFramework
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return JsonConverter<T>(stream) ?? throw new InvalidOperationException("Empty response.");
        }

        private static T? JsonConverter<T>(Stream stream)
            where T : class
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }

        [DataContract]
        private sealed class Translation
        {
            [DataMember(Name = "translatedText")]
            public string? TranslatedText { get; set; }
        }

        [DataContract]
        private sealed class Data
        {
            [DataMember(Name = "translations")]
            public Translation[]? Translations { get; set; }
        }

        [DataContract]
        private sealed class TranslationRootObject
        {
            [DataMember(Name = "data")]
            public Data? Data { get; set; }
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
}
