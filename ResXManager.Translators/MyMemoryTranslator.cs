namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Web;

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

        private static IList<ICredentialItem> GetCredentials()
        {
            Contract.Ensures(Contract.Result<IList<ICredentialItem>>() != null);

            return new ICredentialItem[] { new CredentialItem("Key", "Key") };
        }

        [DataMember]
        [ContractVerification(false)]
        public string Key
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

        public override void Translate(Session session)
        {
            foreach (var item in session.Items)
            {
                if (session.IsCanceled)
                    break;

                var translationItem = item;
                Contract.Assume(translationItem != null);

                try
                {
                    var targetCulture = translationItem.TargetCulture.Culture ?? session.NeutralResourcesLanguage;
                    var result = TranslateText(translationItem.Source, Key, session.SourceLanguage, targetCulture);

                    session.Dispatcher.BeginInvoke(() =>
                    {
                        if (result.Matches != null)
                        {
                            foreach (var match in result.Matches)
                            {
                                translationItem.Results.Add(new TranslationMatch(this, match.Translation, match.Match.GetValueOrDefault() * match.Quality.GetValueOrDefault() / 100.0));
                            }
                        }
                        else
                        {
                            translationItem.Results.Add(new TranslationMatch(this, result.ResponseData.TranslatedText, result.ResponseData.Match));
                        }
                    });
                }
                catch (Exception ex)
                {
                    session.AddMessage(DisplayName + ": " + ex.Message);
                    break;
                }
            }
        }

        private static Response TranslateText(string input, string key, CultureInfo sourceLanguage, CultureInfo targetLanguage)
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
        class ResponseData
        {
            [DataMember(Name = "translatedText")]
            public string TranslatedText
            {
                get;
                set;
            }

            [DataMember(Name = "match")]
            public double Match
            {
                get;
                set;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        class MatchData
        {
            [DataMember(Name = "translation")]
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
        class Response
        {
            [DataMember(Name = "responseData")]
            public ResponseData ResponseData
            {
                get;
                set;
            }

            [DataMember(Name = "matches")]
            public MatchData[] Matches
            {
                get;
                set;
            }


        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
        }
    }
}