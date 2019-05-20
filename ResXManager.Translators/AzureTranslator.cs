namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    using JetBrains.Annotations;

    using tomenglertde.ResXManager.Infrastructure;
    using tomenglertde.ResXManager.Translators.BingServiceReference;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

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

                var token = await AzureAuthentication.GetBearerAccessTokenAsync(authenticationKey);

                var binding = new BasicHttpBinding();
                var endpointAddress = new EndpointAddress("http://api.microsofttranslator.com/V2/soap.svc");

                using (var client = new LanguageServiceClient(binding, endpointAddress))
                {
                    var innerChannel = client.InnerChannel;
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (innerChannel == null)
                        // ReSharper disable once HeuristicUnreachableCode
                        return;

                    using (new OperationContextScope(innerChannel))
                    {
                        var httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers.Add("Authorization", token);
                        var operationContext = OperationContext.Current;
                        operationContext.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                        var translateOptions = new TranslateOptions
                        {
                            ContentType = "text/plain",
                            IncludeMultipleMTAlternatives = true
                        };

                        foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
                        {
                            var cultureKey = languageGroup.Key;
                            var targetLanguage = cultureKey.Culture ?? translationSession.NeutralResourcesLanguage;

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

                                    var response = client.GetTranslationsArray("", sourceStrings, translationSession.SourceLanguage.IetfLanguageTag, targetLanguage.IetfLanguageTag, 5, translateOptions);
                                    if (response != null)
                                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed => just push out results, no need to wait.
                                        translationSession.Dispatcher.BeginInvoke(() => ReturnResults(sourceItems, response));
                                    }

                                    if (translationSession.IsCanceled)
                                        break;
                                }
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

        private void ReturnResults([NotNull][ItemNotNull] IEnumerable<ITranslationItem> items, [NotNull][ItemNotNull] IEnumerable<GetTranslationsResponse> responses)
        {
            foreach (var tuple in Enumerate.AsTuples(items, responses))
            {
                var response = tuple.Item2;
                var translationItem = tuple.Item1;
                var translations = response.Translations;
                foreach (var match in translations)
                {
                    translationItem.Results.Add(new TranslationMatch(this, match.TranslatedText, match.Rating / 5.0));
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
    }
}