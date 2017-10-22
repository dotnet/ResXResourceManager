namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Web;

    using JetBrains.Annotations;

    using Newtonsoft.Json;

    using tomenglertde.ResXManager.Infrastructure;

    using TomsToolbox.Desktop;

    [Export(typeof(ITranslator))]
    public class MyMemoryTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new Uri("http://mymemory.translated.net/doc");

        public MyMemoryTranslator()
            : base("MyMemory", "MyMemory", _uri, GetCredentials())
        {
        }

        [NotNull]
        private static IList<ICredentialItem> GetCredentials()
        {
            Contract.Ensures(Contract.Result<IList<ICredentialItem>>() != null);

            return new ICredentialItem[] { new CredentialItem("Key", "Key") };
        }

        [DataMember(Name = "Key")]
        [ContractVerification(false)]
        [CanBeNull]
        public string SerializedKey
        {
            get
            {
                return SaveCredentials ? Credentials[0]?.Value : null;
            }
            set
            {
                Credentials[0].Value = value;
            }
        }

        [ContractVerification(false)]
        [CanBeNull]
        private string Key => Credentials[0]?.Value;

        public override void Translate(ITranslationSession translationSession)
        {
            foreach (var item in translationSession.Items)
            {
                if (translationSession.IsCanceled)
                    break;

                var translationItem = item;
                Contract.Assume(translationItem != null);

                try
                {
                    var targetCulture = translationItem.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage;
                    var result = TranslateText(translationItem.Source, Key, translationSession.SourceLanguage, targetCulture);

                    translationSession.Dispatcher.BeginInvoke(() =>
                    {
                        if (result.Matches != null)
                        {
                            foreach (var match in result.Matches)
                            {
                                var translation = match.Translation;
                                if (string.IsNullOrEmpty(translation))
                                    continue;

                                translationItem.Results.Add(new TranslationMatch(this, translation, match.Match.GetValueOrDefault() * match.Quality.GetValueOrDefault() / 100.0));
                            }
                        }
                        else
                        {
                            var translation = result.ResponseData.TranslatedText;
                            if (!string.IsNullOrEmpty(translation))
                                translationItem.Results.Add(new TranslationMatch(this, translation, result.ResponseData.Match.GetValueOrDefault()));
                        }
                    });
                }
                catch (Exception ex)
                {
                    translationSession.AddMessage(DisplayName + ": " + ex.Message);
                    break;
                }
            }
        }

        [CanBeNull]
        private static Response TranslateText([NotNull] string input, [CanBeNull] string key, [NotNull] CultureInfo sourceLanguage, [NotNull] CultureInfo targetLanguage)
        {
            Contract.Requires(input != null);
            Contract.Requires(sourceLanguage != null);
            Contract.Requires(targetLanguage != null);

            var url = string.Format(CultureInfo.InvariantCulture,
                "http://api.mymemory.translated.net/get?q={0}!&langpair={1}|{2}",
                HttpUtility.UrlEncode(input, Encoding.UTF8),
                sourceLanguage.IetfLanguageTag,
                targetLanguage.IetfLanguageTag);

            if (!string.IsNullOrEmpty(key))
                url += string.Format(CultureInfo.InvariantCulture, "&key={0}", HttpUtility.UrlEncode(key));

            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Proxy = WebProxy;

            using (var webResponse = webRequest.GetResponse())
            {
                var responseStream = webResponse.GetResponseStream();
                Contract.Assume(responseStream != null);

                using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    var json = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<Response>(json);
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        private class ResponseData
        {
            [DataMember(Name = "translatedText")]
            [CanBeNull]
            public string TranslatedText
            {
                get;
                set;
            }

            [DataMember(Name = "match")]
            public double? Match
            {
                get;
                set;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        private class MatchData
        {
            [DataMember(Name = "translation")]
            [CanBeNull]
            public string Translation
            {
                get;
                set;
            }

            [DataMember(Name = "quality")]
            public double? Quality
            {
                get;
                set;
            }

            [DataMember(Name = "match")]
            public double? Match
            {
                get;
                set;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        private class Response
        {
            [DataMember(Name = "responseData")]
            [CanBeNull]
            public ResponseData ResponseData
            {
                get;
                set;
            }

            [DataMember(Name = "matches")]
            [CanBeNull]
            public MatchData[] Matches
            {
                get;
                set;
            }


        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
        }
    }
}