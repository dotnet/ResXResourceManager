namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition.Mef;

    [DataTemplate(typeof(GoogleTranslator))]
    public class GoogleTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator))]
    public class GoogleTranslator : TranslatorBase
    {
        [NotNull]
        private static readonly Uri _uri = new Uri("https://developers.google.com/translate/");
        [NotNull, ItemNotNull]
        private static readonly IList<ICredentialItem> _credentialItems = new ICredentialItem[] { new CredentialItem("APIKey", "API Key") };

        public GoogleTranslator()
            : base("Google", "Google", _uri, _credentialItems)
        {
        }

        [DataMember(Name = "ApiKey")]
        [CanBeNull]
        public string SerializedApiKey
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        [CanBeNull]
        private string ApiKey => Credentials[0].Value;

        protected override async Task Translate(ITranslationSession translationSession)
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                translationSession.AddMessage("Google Translator requires API Key.");
                return;
            }

            foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
            {
                if (translationSession.IsCanceled)
                    break;

                var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;

                using (var itemsEnumerator = languageGroup.GetEnumerator())
                {
                    while (true)
                    {
                        var sourceItems = itemsEnumerator.Take(10);
                        if (translationSession.IsCanceled || !sourceItems.Any())
                            return;

                        // Build out list of parameters
                        var parameters = new List<string>(30);
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
                            foreach (var tuple in sourceItems.Zip(response.Data.Translations,
                                (a, b) => new Tuple<ITranslationItem, string>(a, b.TranslatedText)))
                            {
                                tuple.Item1.Results.Add(new TranslationMatch(this, tuple.Item2, Ranking));
                            }
                        });

                    }
                }
            }
        }

        [NotNull]
        private static string GoogleLangCode([NotNull] CultureInfo cultureInfo)
        {
            var iso1 = cultureInfo.TwoLetterISOLanguageName;
            var name = cultureInfo.Name;

            if (string.Equals(iso1, "zh", StringComparison.OrdinalIgnoreCase))
                return new[] { "zh-hant", "zh-cht", "zh-hk", "zh-mo", "zh-tw" }.Contains(name, StringComparer.OrdinalIgnoreCase) ? "zh-TW" : "zh-CN";

            if (string.Equals(name, "haw-us", StringComparison.OrdinalIgnoreCase))
                return "haw";

            return iso1;
        }

        private static async Task<T> GetHttpResponse<T>(string baseUrl, ICollection<string> parameters, CancellationToken cancellationToken)
        {
            var url = BuildUrl(baseUrl, parameters);

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    return JsonConverter<T>(stream) ?? throw new InvalidOperationException("Empty response.");
                }
            }
        }

        [CanBeNull]
        private static T JsonConverter<T>([NotNull] Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by the data contract serializer")]
        [DataContract]
        private class Translation
        {
            [DataMember(Name = "translatedText")]
            public string TranslatedText { get; set; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by the data contract serializer")]
        [DataContract]
        private class Data
        {
            [DataMember(Name = "translations")]
            public List<Translation> Translations { get; set; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by the data contract serializer")]
        [DataContract]
        private class TranslationRootObject
        {
            [DataMember(Name = "data")]
            public Data Data { get; set; }
        }

        /// <summary>Builds the URL from a base, method name, and name/value paired parameters. All parameters are encoded.</summary>
        /// <param name="url">The base URL.</param>
        /// <param name="pairs">The name/value paired parameters.</param>
        /// <returns>Resulting URL.</returns>
        /// <exception cref="System.ArgumentException">There must be an even number of strings supplied for parameters.</exception>
        [NotNull]
        private static string BuildUrl(string url, [NotNull, ItemNotNull] ICollection<string> pairs)
        {
            if (pairs.Count % 2 != 0)
                throw new ArgumentException("There must be an even number of strings supplied for parameters.");

            var sb = new StringBuilder(url);
            if (pairs.Count > 0)
            {
                sb.Append("?");
                sb.Append(string.Join("&", pairs.Where((s, i) => i % 2 == 0).Zip(pairs.Where((s, i) => i % 2 == 1), Enc)));
            }
            return sb.ToString();

            string Enc(string a, string b) => string.Concat(System.Web.HttpUtility.UrlEncode(a), "=", System.Web.HttpUtility.UrlEncode(b));
        }
    }
}
