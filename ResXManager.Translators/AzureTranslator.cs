namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export(typeof(ITranslator))]
    public class AzureTranslator : TranslatorBase
    {
        [NotNull]
        private static readonly Uri _uri = new Uri("https://www.microsoft.com/en-us/translator/");

        // Azure has a 5000-character translation limit across all Texts in a single request
        private const int MaxCharsPerApiCall = 5000;
        private const int MaxItemsPerApiCall = 100;

        public AzureTranslator()
            : base("Azure", "Azure", _uri, GetCredentials())
        {
            MaxCharactersPerMinute = 33300;
        }

        [DataMember]
        public bool AutoDetectHtml { get; set; }

        [DataMember]
        public int MaxCharactersPerMinute { get; set; }

        public override async void Translate(ITranslationSession translationSession)
        {
            try
            {
                var authenticationKey = AuthenticationKey;

                if (string.IsNullOrEmpty(authenticationKey))
                {
                    translationSession.AddMessage("Azure Translator requires subscription secret.");
                    return;
                }

                var token = await AzureAuthentication.GetBearerAccessTokenAsync(authenticationKey).ConfigureAwait(false);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", token);

                    var characterCountsThisMinute = new List<Tuple<DateTime, int>>();

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

                                await Throttle(characterCountsThisMinute, sourceItems);

                                var uri = new Uri($"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={translationSession.SourceLanguage.IetfLanguageTag}&to={targetLanguage.IetfLanguageTag}&textType={textType}");

                                var response = await client.PostAsync(uri, CreateRequestContent(sourceStrings)).ConfigureAwait(false);

                                if (response.IsSuccessStatusCode)
                                {
                                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                                    {
                                        var translations = JsonConvert.DeserializeObject<List<AzureTranslationResponse>>(reader.ReadToEnd());
                                        if (translations != null)
                                        {
#pragma warning disable CS4014 // Because this call is not awaited ... => just push out results, no need to wait.
                                            translationSession.Dispatcher.BeginInvoke(() => ReturnResults(sourceItems, translations));
                                        }
                                    }
                                }
                                else
                                {
                                    var errorMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                    translationSession.AddMessage("Azure translator reported a problem: " + errorMessage);
                                }

                                if (translationSession.IsCanceled)
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                translationSession.AddMessage("Azure translator reported a problem: " + ex);
            }
        }

        [DataMember(Name = "AuthenticationKey")]
        [CanBeNull]
        public string SerializedAuthenticationKey
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        [CanBeNull]
        private string AuthenticationKey => Credentials[0].Value;

        private void ReturnResults([NotNull][ItemNotNull] IEnumerable<ITranslationItem> items, [NotNull][ItemNotNull] IEnumerable<AzureTranslationResponse> responses)
        {
            foreach (var tuple in Enumerate.AsTuples(items, responses))
            {
                var response = tuple.Item2;
                var translationItem = tuple.Item1;
                var translations = response.Translations;
                foreach (var match in translations)
                {
                    translationItem.Results.Add(new TranslationMatch(this, match.Text, 1.0));
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        private static IList<ICredentialItem> GetCredentials()
        {
            return new ICredentialItem[]
            {
                new CredentialItem("AuthenticationKey", "Key")
            };
        }

        private HttpContent CreateRequestContent(IEnumerable<string> texts)
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

        [ItemNotNull]
        private IEnumerable<ICollection<ITranslationItem>> SplitIntoChunks(ITranslationSession translationSession, IEnumerable<ITranslationItem> items)
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

        private async Task Throttle(List<Tuple<DateTime, int>> characterCountsThisMinute,
            ICollection<ITranslationItem> sourceItems)
        {
            var newCharacterCount = sourceItems.Sum(item => item.Source.Length);

            var threshold = DateTime.Now.Subtract(TimeSpan.FromMinutes(1));

            characterCountsThisMinute.RemoveAll(t => t.Item1 < threshold);
            var totalCharacterCount = newCharacterCount;

            for (var i = characterCountsThisMinute.Count - 1; i >= 0; i--)
            {
                var tuple = characterCountsThisMinute[i];
                if (totalCharacterCount + tuple.Item2 > MaxCharactersPerMinute)
                {
                    DateTime nextCallTime = tuple.Item1.AddMinutes(1);
                    var millisecondsToDelay = (int) Math.Ceiling((nextCallTime - DateTime.Now).TotalMilliseconds);
                    if (millisecondsToDelay > 0)
                    {
                        await Task.Delay(millisecondsToDelay);
                    }
                    break;
                }

                totalCharacterCount += tuple.Item2;
            }

            characterCountsThisMinute.Add(new Tuple<DateTime, int>(DateTime.Now, newCharacterCount));
        }
    }
}