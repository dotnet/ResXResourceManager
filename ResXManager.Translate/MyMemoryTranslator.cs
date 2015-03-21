namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Web;

    using Newtonsoft.Json;

    using TomsToolbox.Desktop;

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

        public override bool IsLanguageSupported(CultureInfo culture)
        {
            return false;
        }

        public override void Translate(Session session)
        {
            using (var webClient = new WebClient { Encoding = Encoding.UTF8, Proxy = new WebProxy { UseDefaultCredentials = true } })
            {

                foreach (var item in session.Items)
                {
                    if (session.IsCancelled)
                        break;

                    var translationItem = item;
                    Contract.Assume(translationItem != null);

                    try
                    {
                        var result = TranslateText(webClient, translationItem.Source, Key, session.SourceLanguage, session.TargetLanguage);

                        session.Dispatcher.BeginInvoke(() =>
                        {
                            if (result.Matches != null)
                            {
                                foreach (var match in result.Matches)
                                {
                                    translationItem.Results.Add(new TranslationMatch(this, match.Translation, match.Match * match.Quality / 100.0));
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
                        session.AddMessage(ex.Message);
                        break;
                    }
                }
            }
        }

        private static Response TranslateText(WebClient webClient, string input, string key, CultureInfo sourceLanguage, CultureInfo targetLanguage)
        {
            Contract.Requires(webClient != null);
            Contract.Requires(input != null);
            Contract.Requires(sourceLanguage != null);
            Contract.Requires(targetLanguage != null);

            var url = string.Format(CultureInfo.InvariantCulture,
                "http://api.mymemory.translated.net/get?q={0}!&langpair={1}|{2}",
                HttpUtility.UrlEncode(input),
                sourceLanguage.TwoLetterISOLanguageName,
                targetLanguage.TwoLetterISOLanguageName);

            if (!string.IsNullOrEmpty(key))
                url += string.Format(CultureInfo.InvariantCulture, "&key={0}", HttpUtility.UrlEncode(key));

            var json = webClient.Get(url);

            return JsonConvert.DeserializeObject<Response>(json);
        }

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
            public double Quality
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
        }
    }
}