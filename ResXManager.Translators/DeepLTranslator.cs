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

    [DataTemplate(typeof(DeepLTranslator))]
    public class DeepLTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator))]
    public class DeepLTranslator : TranslatorBase
    {
        [NotNull]
        private static readonly Uri _uri = new Uri("https://deepl.com/translator");
        [NotNull, ItemNotNull]
        private static readonly IList<ICredentialItem> _credentialItems = new ICredentialItem[] { new CredentialItem("APIKey", "API Key") };

        public DeepLTranslator()
            : base("DeepL", "DeepL", _uri, _credentialItems)
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
                translationSession.AddMessage("DeepL Translator requires API Key.");
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
                            parameters.AddRange(new[] { "text", RemoveKeyboardShortcutIndicators(item.Source) });
                        }

                        
                        parameters.AddRange(new[]
                        {
                            "target_lang", DeepLLangCode(targetCulture),
                            "source_lang", DeepLLangCode(translationSession.SourceLanguage),
                            "auth_key", ApiKey
                        });

                    

                        // Call the DeepL API
                        // ReSharper disable once AssignNullToNotNullAttribute
                        var response = await GetHttpResponse<TranslationRootObject>(
                            "https://api.deepl.com/v2/translate",
                            parameters,
                            translationSession.CancellationToken).ConfigureAwait(false);

                        await translationSession.MainThread.StartNew(() =>
                        {
                            foreach (var tuple in sourceItems.Zip(response.Translations,
                                (a, b) => new Tuple<ITranslationItem, string>(a, b.Text)))
                            {
                                tuple.Item1.Results.Add(new TranslationMatch(this, tuple.Item2, Ranking));
                            }
                        });
                    }
                }
            }
        }

        [NotNull]
        private static string DeepLLangCode([NotNull] CultureInfo cultureInfo)
        {
            var iso1 = cultureInfo.TwoLetterISOLanguageName;
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
            [DataMember(Name = "text")]
            public string Text { get; set; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by the data contract serializer")]
        [DataContract]
        private class TranslationRootObject
        {
            [DataMember(Name = "translations")]
            public List<Translation> Translations { get; set; }
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
