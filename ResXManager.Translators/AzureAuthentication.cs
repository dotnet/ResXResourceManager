namespace tomenglertde.ResXManager.Translators
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net.Http;
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    internal static class AzureAuthentication
    {
        private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";

        [NotNull]
        private static readonly Uri _serviceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");

        /// <summary>
        /// Gets a token for the specified subscription.
        /// </summary>
        /// <param name="authenticationKey">Subscription secret key.</param>
        /// <returns>The encoded JWT token.</returns>
        [NotNull, ItemCanBeNull]
        private static async Task<string> GetAccessTokenAsync([CanBeNull] string authenticationKey)
        {
            Contract.Ensures(Contract.Result<Task<string>>() != null);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = _serviceUrl;
                request.Content = new StringContent(string.Empty);
                request.Headers?.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, authenticationKey);

                var response = await client.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var token = await response.Content.ReadAsStringAsync();

                return token;
            }
        }

        /// <summary>
        /// Gets a token for the specified subscription. The token is prefixed with "Bearer ".
        /// </summary>
        /// <param name="authenticationKey">Subscription secret key.</param>
        /// <returns>The encoded JWT token, prefixed with "Bearer ".</returns>
        [ItemNotNull]
        public static async Task<string> GetBearerAccessTokenAsync([CanBeNull] string authenticationKey)
        {
            return "Bearer " + await GetAccessTokenAsync(authenticationKey);
        }
    }
}
