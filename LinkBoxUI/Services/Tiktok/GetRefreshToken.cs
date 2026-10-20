using Antlr.Runtime;
using DomainLayer;
using LinkBoxUI.Context;
using LinkBoxUI.Services.Lazada;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static DomainLayer.ViewModels.SyncViewModel;

namespace LinkBoxUI.Services.Tiktok
{
    public class GetRefreshToken
    {
        //public static string AccessToken { get; set; }
        public static DateTime TokenExpireTime { get; set; }

        //public static string AppKey =>
        //    ConfigurationManager.AppSettings["TikTokAppKey"];

        //public static string AppSecret =>
        //    ConfigurationManager.AppSettings["TikTokAppSecret"];

        //public static string RefreshToken =>
        //    ConfigurationManager.AppSettings["TikTokRefreshToken"];

        public static async Task<string> RefreshAccessToken(DomainLayer.APISetup config)
        {
            // Return cached token if still valid
            if (!string.IsNullOrEmpty(config.AccessToken)
                     && config.AccessTokenExpiry > DateTime.UtcNow.AddDays(1))
            {
                return config.AccessToken;
            }

            var requestParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("app_key", config.APIKey),
                new KeyValuePair<string, string>("app_secret", config.APISecretKey),
                new KeyValuePair<string, string>("refresh_token", config.RefreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            };

            var queryString = string.Join("&",
                requestParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

            var url = $"https://auth.tiktok-shops.com/api/v2/token/refresh?{queryString}";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(json);

                if ((int)data["code"] == 0)
                {
                    //config.AccessToken = data["data"]["access_token"].ToString();

                    long expireUnix = (long)data["data"]["access_token_expire_in"];

                    TokenExpireTime = DateTimeOffset
                        .FromUnixTimeSeconds(expireUnix)
                        .UtcDateTime;

                    using (var context = new LinkboxDb())
                    {
                        var tracked = await context.APISetups.FindAsync(config.APIId);
                        if (tracked == null)
                            throw new InvalidOperationException($"ApiSetup {config.APIId} not found.");

                        tracked.AccessToken = data["data"]["access_token"].ToString();
                        tracked.AccessTokenExpiry = TokenExpireTime;
                        tracked.UpdateDate = DateTime.UtcNow;

                        await context.SaveChangesAsync();
                    }

                    return data["data"]["access_token"].ToString();

                }




                throw new Exception("Token refresh failed: " + json);
            }
        }

        public async Task<TikTokTokenModel> _GenerateAccessToken(string appKey, string appSecret, string AuthCode)
        {
            try
            {
                using (var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                })
                using (var client = new HttpClient(handler))
                {

                    const string baseUrl = "https://auth.tiktok-shops.com";
                    string apiPath = "/api/v2/token/get"; // API path only
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                    var paramDict = new SortedDictionary<string, string>
                        {
                            { "app_key", appKey },
                            { "app_secret", appSecret },
                            { "grant_type", "authorized_code" },
                            { "auth_code", AuthCode }

                        };

                    var stringToSign = apiPath;
                    foreach (var kvp in paramDict)
                        stringToSign += kvp.Key + kvp.Value;

                    var queryString = string.Join("&",
                        paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    var fullUrl = $"{baseUrl}{apiPath}?{queryString}";

                    var response = await client.GetAsync(fullUrl);
                    var body = await response.Content.ReadAsStringAsync();


                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(
                            $"Tiktok request to {apiPath} failed. Status: {(int)response.StatusCode}. Body: {body}");

                    TokenApiResponse parsed;

                    try
                    {
                        parsed = System.Text.Json.JsonSerializer.Deserialize<TokenApiResponse>(body);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to parse Lazada response for {apiPath}. Body: {body}", ex);
                    }

                    if (parsed is null)
                        throw new InvalidOperationException(
                            $"Tiktok returned an error code '{parsed?.Code}'. Body: {body}");

                    var now = DateTime.UtcNow;

                    return new TikTokTokenModel
                    {
                        AccessToken = parsed.Data?.AccessToken ?? "",
                        RefreshToken = parsed.Data?.RefreshToken ?? "",
                        AccessTokenExpiry = DateTimeOffset.FromUnixTimeSeconds(parsed.Data?.AccessTokenExpireIn ?? 0).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        RefreshTokenExpiry = DateTimeOffset.FromUnixTimeSeconds(parsed.Data?.RefreshTokenExpireIn ?? 0).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Message = parsed.Message?.ToString() ?? "",
                        Code = parsed.Code.ToString() ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating access token: {ex.Message}");
            }
        }

    }
    internal class TokenApiResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public TokenData Data { get; set; }
    }

    internal class TokenData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("access_token_expire_in")]
        public long AccessTokenExpireIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("refresh_token_expire_in")]
        public long RefreshTokenExpireIn { get; set; }
    }

    // ---- Domain model: what your service returns to the rest of the app. ----
    public class TikTokTokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string AccessTokenExpiry { get; set; }
        public string RefreshTokenExpiry { get; set; }
        public string UpdatedDate { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
    }

}