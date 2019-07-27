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

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf;

    [Export(typeof(ITranslator))]
    public class AzureTranslator : TranslatorBase
    {
        [NotNull]
        private static readonly Uri _uri = new Uri("https://www.microsoft.com/en-us/translator/");

        public AzureTranslator()
            : base("Azure", "Azure", _uri, GetCredentials())
        {
        }

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

                    foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
                    {
                        var cultureKey = languageGroup.Key;
                        var targetLanguage = cultureKey.Culture ?? translationSession.NeutralResourcesLanguage;
                        var uri = new Uri($"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={translationSession.SourceLanguage.IetfLanguageTag}&to={targetLanguage.IetfLanguageTag}");

                        using (var itemsEnumerator = languageGroup.GetEnumerator())
                        {
                            while (true)
                            {
                                var sourceItems = itemsEnumerator.Take(10);
                                var sourceStrings = sourceItems
                                    // ReSharper disable once PossibleNullReferenceException
                                    .Select(item => item.Source)
                                    .Select(RemoveKeyboardShortcutIndicators)
                                    .ToArray();

                                if (!sourceStrings.Any())
                                    break;

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
                    translationItem.Results.Add(new TranslationMatch(this, match.Text, 5.0));
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
    }
}