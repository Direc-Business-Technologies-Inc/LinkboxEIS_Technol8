
using LinkBoxUI.Context;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LinkBoxUI.Services.Zalora
{
    public class ZaloraAccessTokenService
    {
        private static string _accessToken;
        private static DateTime _expiresAt = DateTime.MinValue;
        private static readonly object _tokenLock = new object();
        LinkboxDb _context = new LinkboxDb();

        public async Task<string> AccessToken()
        {
            if (!string.IsNullOrEmpty(_accessToken) &&
                 DateTime.UtcNow < _expiresAt.AddMinutes(-30))
            {
                return _accessToken;
            }

            var apiDetails = _context.APISetups
                .FirstOrDefault(x => x.APICode == "ZaloraAccessToken");

            if (apiDetails == null)
                throw new InvalidOperationException("Zalora API setup not found.");

            var requestParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", apiDetails.APIToken),
                    new KeyValuePair<string, string>("client_id", apiDetails.APIKey),
                    new KeyValuePair<string, string>("client_secret", apiDetails.APISecretKey)
                };

            using (var client = new HttpClient())
            using (var content = new FormUrlEncodedContent(requestParams))
            {
                var response = await client.PostAsync(apiDetails.APILoginUrl, content);

                // ✅ throw if NOT success
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Zalora token request failed. Status: {(int)response.StatusCode}. Body: {errorBody}");
                }

                var json = await response.Content.ReadAsStringAsync();

                var tokenResponse = JsonConvert
                    .DeserializeObject<ZaloraAccessTokenResponse>(json);

                if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
                    throw new InvalidOperationException("Access token is missing in Zalora response.");

                lock (_tokenLock)
                {
                    _accessToken = tokenResponse.AccessToken;
                    _expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                }

                return _accessToken;
            }
        }
    }
}