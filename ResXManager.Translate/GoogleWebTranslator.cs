namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Mime;
    using System.Text;

    using TomsToolbox.Desktop;

    public class GoogleWebTranslator : TranslatorBase
    {
        public GoogleWebTranslator()
            : base("GoogleWeb", "Google (Web)", null)
        {
        }

        public override bool IsLanguageSupported(CultureInfo culture)
        {
            return false;
        }

        public override void Translate(Session session)
        {
            foreach (var translationItem in session.Items)
            {
                var item = translationItem;
                var text = TranslateText(item.Source, session.SourceLanguage, session.TargetLanguage);
                session.Dispatcher.BeginInvoke(() => item.Results.Add(new TranslationMatch(this, text, 3)));
            }
        }

        public static string TranslateText(string input, CultureInfo sourceLanguage, CultureInfo targetLanguage)
        {
            var url = String.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}|{2}", 
                Uri.EscapeDataString(input), 
                sourceLanguage.TwoLetterISOLanguageName, 
                targetLanguage.TwoLetterISOLanguageName);

            var webClient = new WebClient { Encoding = Encoding.UTF8, Proxy = new WebProxy { UseDefaultCredentials = true } };

            var result = DownloadStringUsingResponseEncoding(webClient, url);
            result = result.Substring(result.IndexOf("<span title=\"") + "<span title=\"".Length);
            result = result.Substring(result.IndexOf(">") + 1);
            result = result.Substring(0, result.IndexOf("</span>"));

            return result.Trim();
        }

        private static string DownloadStringUsingResponseEncoding(WebClient client, string address)
        {
            return DecodeStringUsingResponseEncoding(client, client.DownloadData(address));
        }

        private static string DecodeStringUsingResponseEncoding(WebClient client, byte[] data)
        {
            var contentType = GetResponseContentType(client);

            var encoding = (contentType == null) || string.IsNullOrEmpty(contentType.CharSet)
                ? client.Encoding
                : Encoding.GetEncoding(contentType.CharSet);

            return encoding.GetString(data);
        }

        private static ContentType GetResponseContentType(WebClient client)
        {
            var headers = client.ResponseHeaders;
            if (headers == null)
                return null;

            var header = headers["Content-Type"];

            return !string.IsNullOrEmpty(header) ? new ContentType(header) : null;
        }
    }
}