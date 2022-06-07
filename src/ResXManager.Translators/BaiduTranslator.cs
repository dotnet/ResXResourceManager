namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using ResXManager.Infrastructure;
    using TomsToolbox.Essentials;
    using TomsToolbox.Wpf.Composition.AttributedModel;

    [DataTemplate(typeof(BaiduTranslator))]
    public class BaiduTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator)), Shared]
    public class BaiduTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new("https://fanyi-api.baidu.com/product/11");
        private static readonly IList<ICredentialItem> _credentialItems = new ICredentialItem[]
        {
            new CredentialItem("AppId", "App Id"),
            new CredentialItem("SecretKey", "Secret Key"),
            new CredentialItem("ApiUrl", "Api Url", false),
            new CredentialItem("Domain", "Domain", false),
        };

        public BaiduTranslator()
            : base("Baidu", "Baidu", _uri, _credentialItems)
        {
        }

        [DataMember(Name = "AppId")]
        public string? SerializedAppId
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        [DataMember(Name = "SecretKey")]
        public string? SerializedSecretKey
        {
            get => SaveCredentials ? Credentials[1].Value : null;
            set => Credentials[1].Value = value;
        }

        /// <summary>
        /// "https://fanyi-api.baidu.com/api/trans/vip/translate"
        /// "https://fanyi-api.baidu.com/api/trans/vip/fieldtranslate"
        /// </summary>
        [DataMember(Name = "ApiUrl")]
        public string? ApiUrl
        {
            get => Credentials[2].Value;
            set => Credentials[2].Value = value;
        }

        /// <summary>
        /// Support electronics、finance、mechanics、medicine、novel  when fieldtranslate
        /// </summary>
        [DataMember(Name = "Domain")]
        public string? Domain
        {
            get => Credentials[3].Value;
            set => Credentials[3].Value = value;
        }

        private string? AppId => Credentials[0].Value;
        private string? SecretKey => Credentials[1].Value;


        protected override async Task Translate(ITranslationSession translationSession)
        {
            if (AppId.IsNullOrEmpty())
            {
                translationSession.AddMessage("Baidu Translator requires App Id.");
                return;
            }
            if (SecretKey.IsNullOrEmpty())
            {
                translationSession.AddMessage("Baidu Translator requires Secret Key.");
                return;
            }
            foreach (var languageGroup in translationSession.Items.GroupBy(item => item.TargetCulture))
            {
                if (translationSession.IsCanceled)
                    break;

                var targetCulture = languageGroup.Key.Culture ?? translationSession.NeutralResourcesLanguage;

                using var itemsEnumerator = languageGroup.GetEnumerator();

                while (true)
                {
                    var sourceItems = itemsEnumerator.Take(numberOfItems: 1);
                    if (translationSession.IsCanceled || !sourceItems.Any())
                        break;

                    // Build  parameters
                    var parameters = new List<string?>(30);
                    Random rd = new();
                    var salt = rd.Next(100000).ToString(CultureInfo.CurrentCulture);
                    var q = sourceItems[0].Source;
                    string sign;

                    if (Domain.IsNullOrWhiteSpace())
                    {
                        sign = EncryptString(AppId + q + salt + SecretKey);
                    }
                    else
                    {
                        sign = EncryptString(AppId + q + salt + Domain + SecretKey);
                        parameters.AddRange(new[] { "domain", Domain });
                    }
                    parameters.AddRange(new[]
                    {
                        "q", q,
                        "from", translationSession.SourceLanguage.TwoLetterISOLanguageName,
                        "to", targetCulture.TwoLetterISOLanguageName,
                        "appid", AppId,
                        "salt", salt,
                        "sign", sign,
                    });

                    var apiUrl = ApiUrl;
                    if (apiUrl.IsNullOrWhiteSpace())
                    {
                        apiUrl = "https://fanyi-api.baidu.com/api/trans/vip/translate";
                    }

                    // Call the Baidu API
                    var response = await GetHttpResponse<BaiduTranslationResponse>(
                        apiUrl,
                        parameters,
                        translationSession.CancellationToken).ConfigureAwait(false);

                    if (!response.ErrorCode.IsNullOrEmpty() && !response.ErrorMsg.IsNullOrEmpty())
                    {
                        translationSession.AddMessage(response.ErrorMsg);
                        return;
                    }
                    await translationSession.MainThread.StartNew(() =>
                    {
                        if (response.TransResult == null) return;
                        foreach (var tuple in sourceItems.Zip(response.TransResult, (a, b) => new Tuple<ITranslationItem, string?>(a, b.Dst)))
                        {
                            tuple.Item1.Results.Add(new TranslationMatch(this, tuple.Item2, Ranking));
                        }
                    }).ConfigureAwait(false);
                }
            }


        }
        public static string EncryptString(string str)
        {
#pragma warning disable CA5351
            using var md5 = System.Security.Cryptography.MD5.Create();
            var byteOld = Encoding.UTF8.GetBytes(str);
            var byteNew = md5.ComputeHash(byteOld);
            StringBuilder sb = new();
            foreach (var b in byteNew)
            {
                sb.Append(value: b.ToString("x2", CultureInfo.CurrentCulture));
            }
            return sb.ToString();
#pragma warning restore CA5351
        }

        private static async Task<T> GetHttpResponse<T>(string baseUrl, ICollection<string?> parameters, CancellationToken cancellationToken)
            where T : class
        {
            var url = BuildUrl(baseUrl, parameters);

            using var httpClient = new HttpClient();
            //httpClient.Timeout = TimeSpan.FromMilliseconds(2000);
            var response = await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return JsonConverter<T>(stream) ?? throw new InvalidOperationException("Empty response.");
        }

        private static T? JsonConverter<T>(Stream stream)
            where T : class
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
        }

        [DataContract]
        private class TranslateResult
        {
            [DataMember(Name = "src")]
            public string? Src { get; set; }
            [DataMember(Name = "dst")]
            public string? Dst { get; set; }
        }

        [DataContract]
        private class BaiduTranslationResponse
        {
            [DataMember(Name = "error_code")]
            public string? ErrorCode { get; set; }
            [DataMember(Name = "error_msg")]
            public string? ErrorMsg { get; set; }
            [DataMember(Name = "from")]
            public string? From { get; set; }
            [DataMember(Name = "to")]
            public string? To { get; set; }
            [DataMember(Name = "trans_result")]
            public List<TranslateResult>? TransResult { get; set; }
        }

        /// <summary>Builds the URL from a base, method name, and name/value paired parameters. All parameters are encoded.</summary>
        /// <param name="url">The base URL.</param>
        /// <param name="pairs">The name/value paired parameters.</param>
        /// <returns>Resulting URL.</returns>
        /// <exception cref="ArgumentException">There must be an even number of strings supplied for parameters.</exception>
        private static string BuildUrl(string url, ICollection<string?> pairs)
        {
            if (pairs.Count % 2 != 0)
                throw new ArgumentException("There must be an even number of strings supplied for parameters.");

            var sb = new StringBuilder(url);
            if (pairs.Count > 0)
            {
                sb.Append('?');
                sb.Append(string.Join("&", pairs.Where((s, i) => i % 2 == 0).Zip(pairs.Where((s, i) => i % 2 == 1), Format)));
            }
            return sb.ToString();

            static string Format(string? a, string? b)
            {
                return string.Concat(WebUtility.UrlEncode(a), "=", WebUtility.UrlEncode(b));
            }
        }
    }
}
