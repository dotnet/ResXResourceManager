namespace tomenglertde.ResXManager.Translators
{
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Net.Mime;
    using System.Text;
    using System.Web;

    public static class WebClientExtensions
    {
        /// <summary>
        /// Performs a GET request on the specified client and address and returns the data.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="address">The address.</param>
        /// <returns>The returned data.</returns>
        public static string Get(this WebClient client, string address)
        {
            Contract.Requires(client != null);
            Contract.Requires(address != null);

            var data = client.DownloadData(address);

            var encoding = GetResponseEncoding(client) ?? Encoding.UTF8;

            return HttpUtility.UrlDecode(data, encoding);
        }

        private static Encoding GetResponseEncoding(WebClient client)
        {
            Contract.Requires(client != null);

            var headers = client.ResponseHeaders;
            if (headers == null)
                return Encoding.UTF8;

            var header = headers["Content-Type"];

            if (string.IsNullOrEmpty(header))
                return client.Encoding;

            var contentType = new ContentType(header);
            if (string.IsNullOrEmpty(contentType.CharSet))
                return client.Encoding;

            return Encoding.GetEncoding(contentType.CharSet);
        }
    }
}
