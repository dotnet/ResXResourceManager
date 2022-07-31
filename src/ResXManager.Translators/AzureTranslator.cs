namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    [Export(typeof(ITranslator)), Shared]
    public class AzureTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new("https://www.microsoft.com/en-us/translator/");

        // Azure has a 5000-character translation limit across all Texts in a single request
        private const int MaxCharsPerApiCall = 5000;
        private const int MaxItemsPerApiCall = 100;

        public AzureTranslator()
            : base("Azure", "Azure", _uri, GetCredentials())
        {
        }

        [DataMember]
        public bool AutoDetectHtml { get; set; } = true;

        [DataMember]
        public int MaxCharactersPerMinute { get; set; } = 33300;

        protected override async Task Translate(ITranslationSession translationSession)
        {
            var authenticationKey = AuthenticationKey;

            if (authenticationKey.IsNullOrEmpty())
            {
                translationSession.AddMessage("Azure Translator requires API key.");
                return;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", authenticationKey);
                if (!Region.IsNullOrEmpty())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", Region);
                }

                var throttle = new Throttle(MaxCharactersPerMinute, translationSession.CancellationToken);

                var itemsByLanguage = translationSession.Items.GroupBy(item => item.TargetCulture);

                foreach (var languageGroup in itemsByLanguage)
                {
                    var cultureKey = languageGroup.Key;
                    var targetLanguage = cultureKey.Culture ?? translationSession.NeutralResourcesLanguage;

                    var itemsByTextType = languageGroup.GroupBy(GetTextType);

                    foreach (var textTypeGroup in itemsByTextType)
                    {
                        var textType = textTypeGroup.Key;

                        foreach (var sourceItems in SplitIntoChunks(translationSession, textTypeGroup))
                        {
                            if (!sourceItems.Any())
                                break;

                            var sourceStrings = sourceItems
                                .Select(item => item.Source)
                                .Select(RemoveKeyboardShortcutIndicators)
                                .ToList();

                            await throttle.Tick(sourceItems).ConfigureAwait(false);

                            if (translationSession.IsCanceled)
                                return;

                            var uri = new Uri($"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={translationSession.SourceLanguage.IetfLanguageTag}&to={targetLanguage.IetfLanguageTag}&textType={textType}");

                            using var content = CreateRequestContent(sourceStrings);

                            var response = await client.PostAsync(uri, content, translationSession.CancellationToken).ConfigureAwait(false);

                            response.EnsureSuccessStatusCode();

                            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                var translations = JsonConvert.DeserializeObject<List<AzureTranslationResponse>>(await reader.ReadToEndAsync().ConfigureAwait(false));
                                if (translations != null)
                                {
                                    await translationSession.MainThread.StartNew(() => ReturnResults(sourceItems, translations)).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
            }
        }

        [DataMember(Name = "AuthenticationKey")]
        public string? SerializedAuthenticationKey
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        [DataMember(Name = "Region")]
        public string? Region
        {
            get => Credentials[1].Value;
            set => Credentials[1].Value = value;
        }

        private string? AuthenticationKey => Credentials[0].Value;

        private void ReturnResults(IEnumerable<ITranslationItem> items, IEnumerable<AzureTranslationResponse> responses)
        {
            foreach (var tuple in Enumerate.AsTuples(items, responses))
            {
                var response = tuple.Item2;
                var translationItem = tuple.Item1;
                var translations = response.Translations;
                if (translations == null)
                    continue;

                foreach (var match in translations)
                {
                    translationItem.Results.Add(new TranslationMatch(this, match.Text, Ranking));
                }
            }
        }

        private static IList<ICredentialItem> GetCredentials()
        {
            return new ICredentialItem[]
            {
                new CredentialItem("AuthenticationKey", "Key"),
                new CredentialItem("Region", "Region", false)
            };
        }

        private static HttpContent CreateRequestContent(IEnumerable<string> texts)
        {
            var payload = texts.Select(text => new { Text = text }).ToArray();

            var serialized = JsonConvert.SerializeObject(payload);

            Debug.Assert(serialized != null, nameof(serialized) + " != null");

            var serializedBytes = Encoding.UTF8.GetBytes(serialized);

            var byteContent = new ByteArrayContent(serializedBytes);

            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return byteContent;
        }

        private string GetTextType(ITranslationItem item)
        {
            return AutoDetectHtml && item.Source.ContainsHtml() ? "html" : "plain";
        }

        private static IEnumerable<ICollection<ITranslationItem>> SplitIntoChunks(ITranslationSession translationSession, IEnumerable<ITranslationItem> items)
        {
            var chunk = new List<ITranslationItem>();
            var chunkTextLength = 0;

            foreach (var item in items)
            {
                var textLength = item.Source.Length;

                if (textLength > MaxCharsPerApiCall)
                {
                    translationSession.AddMessage($"Resource length exceeds Azure's {MaxCharsPerApiCall}-character limit: {item.Source.Substring(0, 20)}...");
                    continue;
                }

                if ((chunk.Count == MaxItemsPerApiCall) || ((chunkTextLength + textLength) > MaxCharsPerApiCall))
                {
                    yield return chunk;
                    chunk = new List<ITranslationItem>();
                    chunkTextLength = 0;
                }

                chunk.Add(item);
                chunkTextLength += textLength;
            }

            yield return chunk;
        }

        private class Throttle
        {
            private readonly int _maxCharactersPerMinute;
            private readonly CancellationToken _cancellationToken;
            private readonly List<Tuple<DateTime, int>> _characterCounts = new();

            public Throttle(int maxCharactersPerMinute, CancellationToken cancellationToken)
            {
                _maxCharactersPerMinute = maxCharactersPerMinute;
                _cancellationToken = cancellationToken;
            }

            public async Task Tick(ICollection<ITranslationItem> sourceItems)
            {
                var newCharacterCount = sourceItems.Sum(item => item.Source.Length);

                var threshold = DateTime.Now.Subtract(TimeSpan.FromMinutes(1));

                _characterCounts.RemoveAll(t => t.Item1 < threshold);

                var totalCharacterCount = newCharacterCount;

                for (var i = _characterCounts.Count - 1; i >= 0; i--)
                {
                    var tuple = _characterCounts[i];
                    if (totalCharacterCount + tuple.Item2 > _maxCharactersPerMinute)
                    {
                        var nextCallTime = tuple.Item1.AddMinutes(1);
                        var millisecondsToDelay = (int)Math.Ceiling((nextCallTime - DateTime.Now).TotalMilliseconds);
                        if (millisecondsToDelay > 0)
                        {
                            await Task.Delay(millisecondsToDelay, _cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }

                    totalCharacterCount += tuple.Item2;
                }

                _characterCounts.Add(new Tuple<DateTime, int>(DateTime.Now, newCharacterCount));
            }
        }
    }
}