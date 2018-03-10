namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

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

        private string APIKey => Credentials[0].Value;

        public override void Translate(ITranslationSession translationSession)
        {
            if (string.IsNullOrEmpty(APIKey))
            {
                translationSession.AddMessage("Google Translator requires API Key.");
                return;
            }

            var replRegex = new Regex(@"&(?=[\w\d])", RegexOptions.Compiled);
            foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
            {
                if (translationSession.IsCanceled)
                    break;

                Contract.Assume(languageGroup != null);

                var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;
                if (!targetCulture.IsNeutralCulture) targetCulture = targetCulture.Parent;

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
                            parameters.AddRange(new[] { "q", replRegex.Replace(item.Source, string.Empty) });
                        parameters.AddRange(new[] {
                            "target", targetCulture.TwoLetterISOLanguageName,
                            "format", "text",
                            "source", translationSession.SourceLanguage.TwoLetterISOLanguageName,
                            "model", "base",
                            "key", APIKey });

                        // Call the Google API
                        var responseTask = GetHttpResponse("https://translation.googleapis.com/language/translate/v2", null, parameters, JsonConverter<TranslationRootObject>);

                        // Handle successful run
                        responseTask.ContinueWith(t =>
                        {
                            translationSession.Dispatcher.BeginInvoke(() =>
                            {
                                foreach (var tuple in sourceItems.Zip(t.Result.data.translations,
                                    (a, b) => new Tuple<ITranslationItem, string>(a, b.translatedText)))
                                {
                                    Contract.Assume(tuple != null);
                                    Contract.Assume(tuple.Item1 != null);
                                    Contract.Assume(tuple.Item2 != null);
                                    tuple.Item1.Results.Add(new TranslationMatch(this, tuple.Item2, 1.0));
                                }
                            });
                        }, TaskContinuationOptions.OnlyOnRanToCompletion);

                        // Handle exception in run
                        responseTask.ContinueWith(t => { translationSession.AddMessage(DisplayName + ": " + t.Exception?.InnerException?.Message); loop = false; }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                }
            }
        }

        private static async Task<T> GetHttpResponse<T>(string baseUrl, string authHeader, ICollection<string> parameters, Func<Stream, T> conv)
        {
            var url = BuildUrl(baseUrl, parameters);
            using (var c = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(authHeader))
                    c.DefaultRequestHeaders.Add("Authorization", authHeader);
                Debug.WriteLine("Google URL: " + url);
                using (var stream = await c.GetStreamAsync(url))
                    return conv(stream);
            }
        }

        private static T JsonConverter<T>(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }

        [DataContract]
        private class TranslationRootObject
        {
            [DataMember]
            public Data data { get; set; }

            [DataContract]
            public class Data
            {
                [DataMember]
                public List<Translation> translations { get; set; }

                [DataContract]
                public class Translation
                {
                    [DataMember]
                    public string translatedText { get; set; }
                }
            }
        }

        /// <summary>Builds the URL from a base, method name, and name/value paired parameters. All parameters are encoded.</summary>
        /// <param name="url">The base URL.</param>
        /// <param name="pairs">The name/value paired parameters.</param>
        /// <returns>Resulting URL.</returns>
        /// <exception cref="System.ArgumentException">There must be an even number of strings supplied for parameters.</exception>
        private static string BuildUrl(string url, ICollection<string> pairs)
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