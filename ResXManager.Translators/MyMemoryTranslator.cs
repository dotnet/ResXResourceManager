namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Windows.Controls;

    using JetBrains.Annotations;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.Mef;

    [DataTemplate(typeof(MyMemoryTranslator))]
    public class MyMemoryTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator))]
    public class MyMemoryTranslator : TranslatorBase
    {
        [NotNull]
        private static readonly Uri _uri = new Uri("http://mymemory.translated.net/doc");

        public MyMemoryTranslator()
            : base("MyMemory", "MyMemory", _uri, GetCredentials())
        {
        }

        [NotNull]
        [ItemNotNull]
        private static IList<ICredentialItem> GetCredentials()
        {
            return new ICredentialItem[] { new CredentialItem("Key", "Key") };
        }

        [DataMember(Name = "Key")]
        [CanBeNull]
        public string SerializedKey
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        [CanBeNull]
        private string Key => Credentials[0].Value;

        protected override async Task Translate(ITranslationSession translationSession)
        {
            using (var client = new HttpClient())
            {

                foreach (var item in translationSession.Items)
                {
                    if (translationSession.IsCanceled)
                        break;

                    var translationItem = item;

                    var targetCulture = translationItem.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage;
                    var result = await TranslateTextAsync(client, translationItem.Source, Key, translationSession.SourceLanguage, targetCulture, translationSession.CancellationToken);

                    await translationSession.MainThread.StartNew(() =>
                    {
                        if (result?.Matches != null)
                        {
                            foreach (var match in result.Matches)
                            {
                                var translation = match.Translation;
                                if (string.IsNullOrEmpty(translation))
                                    continue;

                                translationItem.Results.Add(new TranslationMatch(this, translation, Ranking * match.Match.GetValueOrDefault() * match.Quality.GetValueOrDefault() / 100.0));
                            }
                        }
                        else
                        {
                            var translation = result?.ResponseData?.TranslatedText;
                            if (!string.IsNullOrEmpty(translation))
                                translationItem.Results.Add(new TranslationMatch(this, translation, Ranking * result.ResponseData.Match.GetValueOrDefault()));
                        }
                    });
                }
            }
        }

        [NotNull, ItemCanBeNull]
        private static async Task<Response> TranslateTextAsync(HttpClient client, [NotNull] string input, [CanBeNull] string key, [NotNull] CultureInfo sourceLanguage, [NotNull] CultureInfo targetLanguage, CancellationToken cancellationToken)
        {
            var rawInput = RemoveKeyboardShortcutIndicators(input);

            var url = string.Format(CultureInfo.InvariantCulture,
                "http://api.mymemory.translated.net/get?q={0}!&langpair={1}|{2}",
                HttpUtility.UrlEncode(rawInput, Encoding.UTF8),
                sourceLanguage, targetLanguage);

            if (!string.IsNullOrEmpty(key))
                url += string.Format(CultureInfo.InvariantCulture, "&key={0}", HttpUtility.UrlEncode(key));

            var response = await client.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();

            using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Response>(json);
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
            [ItemNotNull]
            public MatchData[] Matches
            {
                get;
                set;
            }
        }
    }
}