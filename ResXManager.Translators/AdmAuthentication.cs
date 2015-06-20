namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Web;

    internal static class AdmAuthentication
    {
        private const string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
        private const string AuthTokenPrefix = "Bearer ";

        private static readonly DataContractJsonSerializer _serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));

        public static string GetAuthToken(IWebProxy webProxy, string clientId, string clientSecret)
        {
            Contract.Requires(!string.IsNullOrEmpty(clientId));
            Contract.Requires(!string.IsNullOrEmpty(clientSecret));
            Contract.Ensures(Contract.Result<string>() != null);

            try
            {
                var request = CreateRequestDetails(clientId, clientSecret);
                var token = GetAccessToken(webProxy, request);

                return AuthTokenPrefix + token.AccessToken;
            }
            catch (WebException ex)
            {
                //using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                //{
                //    var data = reader.ReadToEnd();
                //}
                throw new InvalidOperationException("Authentication Failed", ex);
            }
        }

        private static AdmAccessToken GetAccessToken(IWebProxy webProxy, string requestDetails)
        {
            Contract.Requires(requestDetails != null);
            Contract.Ensures(Contract.Result<AdmAccessToken>() != null);

            var webRequest = CreateWebRequest(webProxy);

            using (var outputStream = webRequest.GetRequestStream())
            {
                var bytes = Encoding.UTF8.GetBytes(requestDetails);
                outputStream.Write(bytes, 0, bytes.Length);
            }

            using (var webResponse = webRequest.GetResponse())
            {
                var token = (AdmAccessToken)_serializer.ReadObject(webResponse.GetResponseStream());
                Contract.Assume(token != null);
                return token;
            }
        }

        private static string CreateRequestDetails(string clientId, string clientSecret)
        {
            var request = string.Format(CultureInfo.InvariantCulture, "grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));
            return request;
        }

        private static HttpWebRequest CreateWebRequest(IWebProxy webProxy)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(DatamarketAccessUri);
            webRequest.Proxy = webProxy;
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            return webRequest;
        }

        [DataContract]
        internal class AdmAccessToken
        {
            [DataMember(Name = "access_token")]
            public string AccessToken { get; set; }
            [DataMember(Name = "token_type")]
            public string TokenType { get; set; }
            [DataMember(Name = "expires_in")]
            public string ExpiresIn { get; set; }
            [DataMember(Name = "scope")]
            public string Scope { get; set; }
        }
    }
}
