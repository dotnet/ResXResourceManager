namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    using tomenglertde.ResXManager.Translators.BingServiceReference;

    using TomsToolbox.Core;
    using TomsToolbox.Desktop;

    public class BingTranslator : TranslatorBase
    {
        public BingTranslator()
            : base("Bing", "Bing", GetCredentials().ToArray())
        {
        }

        public override bool IsLanguageSupported(CultureInfo culture)
        {
            return false;
        }

        public override void Translate(Session session)
        {
            try
            {
                var clientId = ClientId;
                var clientSecret = ClientSecret;

                var token = AdmAuthentication.GetAuthToken(clientId, clientSecret);

                var binding = new BasicHttpBinding();
                var endpointAddress = new EndpointAddress("http://api.microsofttranslator.com/V2/soap.svc");

                using (var client = new LanguageServiceClient(binding, endpointAddress))
                {
                    using (new OperationContextScope(client.InnerChannel))
                    {
                        var httpRequestProperty = new HttpRequestMessageProperty();
                        httpRequestProperty.Headers.Add("Authorization", token);
                        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                        using (var itemsEnumerator = session.Items.GetEnumerator())
                        {
                            while (true)
                            {
                                var sourceItems = itemsEnumerator.Take(10);
                                var sourceStrings = sourceItems.Select(item => item.Source).ToArray();

                                if (!sourceStrings.Any())
                                    break;

                                var response = client.GetTranslationsArray("", sourceStrings, session.SourceLanguage.TwoLetterISOLanguageName, session.TargetLanguage.TwoLetterISOLanguageName, 5,
                                    new TranslateOptions()
                                    {
                                        ContentType = "text/plain",
                                        IncludeMultipleMTAlternatives = true
                                    });

                                session.Dispatcher.BeginInvoke(() => ReturnResults(sourceItems, response));

                                if (session.IsCancelled)
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                session.AddMessage("Bing translator reported a problem: " + ex);
            }
        }

        [DataMember]
        public string ClientSecret
        {
            get
            {
                return SaveCredentials ? Credentials[1].Value : null;
            }
            set
            {
                Credentials[1].Value = value;
            }
        }

        [DataMember]
        public string ClientId
        {
            get
            {
                return SaveCredentials ? Credentials[0].Value : null;
            }
            set
            {
                Credentials[0].Value = value;
            }
        }

        private void ReturnResults(IEnumerable<ITranslationItem> items, IEnumerable<GetTranslationsResponse> responses)
        {
            foreach (var tuple in Enumerate.AsTuples(items, responses))
            {
                var response = tuple.Item2;
                var translationItem = tuple.Item1;

                foreach (var match in response.Translations)
                {
                    translationItem.Results.Add(new TranslationMatch(this, match.TranslatedText, match.Rating));
                }
            }
        }

        private static IEnumerable<ICredentialItem> GetCredentials()
        {
            yield return new CredentialItem("ClientId", "Client ID");
            yield return new CredentialItem("ClientSecret", "Client Secret");
        }
    }
}