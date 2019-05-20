namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

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

        [CanBeNull]
        private string ApiKey => Credentials[0].Value;

        public override async void Translate(ITranslationSession translationSession)
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
                    var loop = true;
                    while (loop)
                    {
                        var sourceItems = itemsEnumerator.Take(10);
                        if (translationSession.IsCanceled || !sourceItems.Any())
                            break;

                        // Build out list of parameters
                        var parameters = new List<string>(30);
                        foreach (var item in sourceItems)
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            parameters.AddRange(new[] { "q", RemoveKeyboardShortcutIndicators(item.Source) });
                        }

                        parameters.AddRange(new[] {
                            "target", GoogleLangCode(targetCulture),
                            "format", "text",
                            "source", GoogleLangCode(translationSession.SourceLanguage),
                            "model", "base",
                            "key", ApiKey });

                        try
                        {

                            // Call the Google API
                            // ReSharper disable once AssignNullToNotNullAttribute
                            var response = await GetHttpResponse("https://translation.googleapis.com/language/translate/v2", null, parameters, JsonConverter<TranslationRootObject>).ConfigureAwait(false);

                            await translationSession.Dispatcher.BeginInvoke(() =>
                            {
                                foreach (var tuple in sourceItems.Zip(response.Data.Translations,
                                    (a, b) => new Tuple<ITranslationItem, string>(a, b.TranslatedText)))
                                {
                                    tuple.Item1.Results.Add(new TranslationMatch(this, tuple.Item2, 1.0));
                                }
                            });

                        }
                        catch (Exception ex)
                        {
                            translationSession.AddMessage(DisplayName + ": " + ex.InnerException?.Message);
                            loop = false;
                        }
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
                return new[] {"zh-hant", "zh-cht", "zh-hk", "zh-mo", "zh-tw"}.Contains(name, StringComparer.OrdinalIgnoreCase) ? "zh-TW" : "zh-CN";

            if (string.Equals(name, "haw-us", StringComparison.OrdinalIgnoreCase))
                return "haw";

            return iso1;
        }

        private static async Task<T> GetHttpResponse<T>(string baseUrl, string authHeader, [NotNull] ICollection<string> parameters, Func<Stream, T> conv)
        {
            var url = BuildUrl(baseUrl, parameters);
            using (var c = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    c.DefaultRequestHeaders.Add("Authorization", authHeader);
                }

                Debug.WriteLine("Google URL: " + url);
                using (var stream = await c.GetStreamAsync(new Uri(url)).ConfigureAwait(false))
                {
                    return conv(stream);
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
            [DataMember(Name="data")]
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