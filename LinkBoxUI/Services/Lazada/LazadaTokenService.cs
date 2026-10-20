using Antlr.Runtime;
using DomainLayer;
using DomainLayer.Models;
using LinkBoxUI.Context;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static DomainLayer.ViewModels.PostingViewModel;
using static DomainLayer.ViewModels.SyncViewModel;

namespace LinkBoxUI.Services.Lazada
{


    public class LazadaTokenService
    {
        private readonly string _tokenFilePath;
        private static string _Url => ConfigurationManager.AppSettings["Sh_Url"];
        public async Task<LazadaTokenModel> CallRefreshEndpoint()
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        (message, cert, chain, errors) => true;

                    using (var client = new HttpClient(handler))
                    {
                        var url = $"{_Url}/api/auth/lazada/refresh";
                        var response = await client.GetAsync(url);
                        var result = await response.Content.ReadAsStringAsync();

                        // ← Deserialize to LazadaRefreshResponseModel first
                        var refreshResponse = JsonConvert.DeserializeObject<LazadaRefreshResponseModel>(result);

                        if (refreshResponse == null)
                            throw new Exception("Failed to deserialize refresh response!");

                        // ← Convert to LazadaTokenModel and return
                        var tokenModel = new LazadaTokenModel
                        {
                            AccessToken = refreshResponse.access_token,
                            RefreshToken = refreshResponse.refresh_token,
                            AccessTokenExpiry = refreshResponse.access_token_expiry.ToString(),
                            RefreshTokenExpiry = refreshResponse.refresh_token_expiry.ToString(),
                            UpdatedDate = refreshResponse.updatedate,
                            AppKey = refreshResponse.app_key,
                            AppSecretKey = refreshResponse.secret_key

                        };

                        return tokenModel;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calling refresh endpoint: {ex.Message}");
            }
        }

        public async Task<LazadaTokenModel> _GenerateAccessToken(string appKey,string appSecret,string AuthCode)
        {
            try
            {
                using (var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                })
                using (var client = new HttpClient(handler))
                {

                    const string baseUrl = "https://api.lazada.com.ph/rest";
                    string apiPath = "/auth/token/create"; // API path only
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                    var paramDict = new SortedDictionary<string, string>
                        {
                            { "app_key", appKey },
                            { "sign_method", "sha256" },
                            { "timestamp", timestamp },
                            { "code", AuthCode }

                        };

                    var stringToSign = apiPath;
                    foreach (var kvp in paramDict)
                        stringToSign += kvp.Key + kvp.Value;

                    var sign = GenerateSignature(stringToSign, appSecret);

                    var queryString = string.Join("&",
                        paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    var fullUrl = $"{baseUrl}{apiPath}?{queryString}&sign={sign}";

                    var response = await client.GetAsync(fullUrl);
                    var body = await response.Content.ReadAsStringAsync();


                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(
                            $"Lazada request to {apiPath} failed. Status: {(int)response.StatusCode}. Body: {body}");
                    LazadaTokenResponse parsed;

                    try
                    {
                        parsed = System.Text.Json.JsonSerializer.Deserialize<LazadaTokenResponse>(body);
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to parse Lazada response for {apiPath}. Body: {body}", ex);
                    }

                    //if (parsed is null || parsed.Code != "0")
                    //    throw new InvalidOperationException(
                    //        $"Lazada returned an error code '{parsed?.Code}'. Body: {body}");

                    var now = DateTime.UtcNow;

                    return new LazadaTokenModel
                    {
                        Code = parsed?.Code ?? "",
                        AccessToken = parsed.AccessToken ?? "",
                        RefreshToken = parsed.RefreshToken ?? "",
                        AccessTokenExpiry = now.AddSeconds(parsed.ExpiresIn).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        RefreshTokenExpiry = now.AddSeconds(parsed.RefreshExpiresIn).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Message = parsed.message?.ToString() ?? "",
                        UpdatedDate = now,
                    };
                }
            }catch(Exception ex)
            {
                throw new Exception($"Error generating access token: {ex.Message}");
            }
        }

        public async Task<LazadaTokenModel> _GenerateRefreshTokenAsync(DomainLayer.APISetup apisetup)
        {
                if (!string.IsNullOrEmpty(apisetup.AccessToken) && apisetup.AccessTokenExpiry > DateTime.UtcNow.AddDays(1))
                {
                return new LazadaTokenModel
                {
                    Code = "0",
                    AccessToken = apisetup.AccessToken ?? "",
                    RefreshToken = apisetup.RefreshToken ?? "",
                    AppKey = apisetup.APIKey,
                    AppSecretKey = apisetup.APISecretKey
                };
            }

            using (var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // TODO: remove, see earlier note
            })
            using (var client = new HttpClient(handler))
            {
                const string baseUrl = "https://api.lazada.com.ph/rest";
                const string apiPath = "/auth/token/refresh";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var paramDict = new SortedDictionary<string, string>
        {
            { "app_key", apisetup.APIKey },
            { "sign_method", "sha256" },
            { "timestamp", timestamp },
            { "refresh_token", apisetup.RefreshToken }
        };

                var stringToSign = apiPath + string.Concat(paramDict.Select(kvp => kvp.Key + kvp.Value));
                var sign = GenerateSignature(stringToSign, apisetup.APISecretKey);
                var queryString = string.Join("&", paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var fullUrl = $"{baseUrl}{apiPath}?{queryString}&sign={sign}";

                var response = await client.GetAsync(fullUrl);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Lazada request to {apiPath} failed. Status: {(int)response.StatusCode}. Body: {body}");

                LazadaTokenResponse parsed;
                try
                {
                    parsed = System.Text.Json.JsonSerializer.Deserialize<LazadaTokenResponse>(body);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Failed to parse Lazada response for {apiPath}. Body: {body}", ex);
                }

                if (parsed is null || string.IsNullOrEmpty(parsed.AccessToken))
                    throw new InvalidOperationException($"Lazada refresh failed. Code: {parsed?.Code}, Message: {parsed?.message}. Body: {body}");

                var now = DateTime.UtcNow;

                using (var context = new LinkboxDb())
                {
                    var tracked = await context.APISetups.FindAsync(apisetup.APIId);
                    if (tracked == null)
                        throw new InvalidOperationException($"ApiSetup {apisetup.APIId} not found.");

                    tracked.AccessToken = parsed.AccessToken;
                    tracked.RefreshToken = parsed.RefreshToken ?? tracked.RefreshToken; // keep old if Lazada didn't rotate it
                    tracked.AccessTokenExpiry = now.AddSeconds(parsed.ExpiresIn);
                    tracked.UpdateDate = now;

                    await context.SaveChangesAsync();
                }

                return new LazadaTokenModel
                {
                    Code = parsed.Code ?? "0",
                    AccessToken = parsed.AccessToken,
                    RefreshToken = parsed.RefreshToken ?? "",
                    AccessTokenExpiry = now.AddSeconds(parsed.ExpiresIn).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    RefreshTokenExpiry = now.AddSeconds(parsed.RefreshExpiresIn).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    Message = parsed.message?.ToString() ?? "",
                    UpdatedDate = now,
                    AppKey = apisetup.APIKey,
                    AppSecretKey = apisetup.APISecretKey
                };
            }
        }

        //public async Task<LazadaTokenModel> _GenerateRefreshToken(APISetup apisetup)
        //{
        //    try
        //    {
        //        if(apisetup.AccessTokenExpiry > DateTime.UtcNow.AddDays(1))
        //        {
        //            return new LazadaTokenModel
        //            {
        //                Code = "0",
        //                AccessToken = apisetup.AccessToken ?? "",
        //                RefreshToken = apisetup.RefreshToken ?? "",
        //                AppKey = apisetup.APIKey,
        //                AppSecretKey = apisetup.APISecretKey
        //            };
        //        }
        //        using (var handler = new HttpClientHandler
        //        {
        //            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        //        })
        //        using (var client = new HttpClient(handler))
        //        {
        //            using (var context = new LinkboxDb())
        //            {
        //                const string baseUrl = "https://api.lazada.com.ph/rest";
        //            string apiPath = "/auth/token/refresh"; // API path only
        //            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        //            var paramDict = new SortedDictionary<string, string>
        //                {
        //                    { "app_key", apisetup.APIKey },
        //                    { "sign_method", "sha256" },
        //                    { "timestamp", timestamp },
        //                    { "refresh_token", apisetup.RefreshToken }  

        //                };

        //            var stringToSign = apiPath;
        //            foreach (var kvp in paramDict)
        //                stringToSign += kvp.Key + kvp.Value;

        //            var sign = GenerateSignature(stringToSign, apisetup.APISecretKey);

        //            var queryString = string.Join("&",
        //                paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        //            var fullUrl = $"{baseUrl}{apiPath}?{queryString}&sign={sign}";

        //            var response = await client.GetAsync(fullUrl);
        //            var body = await response.Content.ReadAsStringAsync();


        //            if (!response.IsSuccessStatusCode)
        //                throw new HttpRequestException(
        //                    $"Lazada request to {apiPath} failed. Status: {(int)response.StatusCode}. Body: {body}");
        //            LazadaTokenResponse parsed;

        //            try
        //            {
        //                parsed = System.Text.Json.JsonSerializer.Deserialize<LazadaTokenResponse>(body);
        //            }
        //            catch (JsonException ex)
        //            {
        //                throw new InvalidOperationException(
        //                    $"Failed to parse Lazada response for {apiPath}. Body: {body}", ex);
        //            }

        //            //if (parsed is null || parsed.Code != "0")
        //            //    throw new InvalidOperationException(
        //            //        $"Lazada returned an error code '{parsed?.Code}'. Body: {body}");


        //                var _apisetup = await context.APISetups.FindAsync(apisetup.APIId);
        //                if (_apisetup == null)
        //                    throw new InvalidOperationException($"ApiSetup {_apisetup} not found.");

        //                //var parsed = await _GenerateAccessToken(appKey, appSecret, authCode);


        //                var now = DateTime.UtcNow;
        //                _apisetup.AccessToken = parsed.AccessToken;
        //                _apisetup.AccessTokenExpiry = now.AddSeconds(parsed.ExpiresIn);
        //                _apisetup.UpdateDate = now;

        //                await context.SaveChangesAsync();

        //            //var now = DateTime.UtcNow;

        //            //apisetup.AccessToken = parsed.AccessToken ?? "";
        //            //apisetup.AccessTokenExpiry = now.AddSeconds(parsed.ExpiresIn);//.ToString("yyyy-MM-dd HH:mm:ss.fff");
        //            //apisetup.UpdateDate = now;

        //            return new LazadaTokenModel
        //            {
        //                Code = parsed?.Code ?? "-1",
        //                AccessToken = parsed.AccessToken ?? "",
        //                RefreshToken = parsed.RefreshToken ?? "",
        //                AccessTokenExpiry = now.AddSeconds(parsed.ExpiresIn).ToString("yyyy-MM-dd HH:mm:ss.fff"),
        //                RefreshTokenExpiry = now.AddSeconds(parsed.RefreshExpiresIn).ToString("yyyy-MM-dd HH:mm:ss.fff"),
        //                Message = parsed.message?.ToString() ?? "",
        //                UpdatedDate = now,
        //                AppKey = apisetup.APIKey,
        //                AppSecretKey = apisetup.APISecretKey
        //            };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error generating access token: {ex.Message}");
        //    }
        //}

        private string GenerateSignature(string stringToSign, string appSecret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(appSecret);
            var msgBytes = Encoding.UTF8.GetBytes(stringToSign);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }

    }

    internal class LazadaTokenResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("refresh_expires_in")]
        public long RefreshExpiresIn { get; set; }
        public string message { get; set; }
    }

    // -------------------------------------------------------
    // Token Model
    // -------------------------------------------------------
    public class LazadaTokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string AccessTokenExpiry { get; set; }
        public string RefreshTokenExpiry { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string AppKey { get; set; }
        public string AppSecretKey { get; set; }
        public string Code { get; set; }

        public string Message { get; set; }
    }

    public class LazadaRefreshResponseModel
    {
        public string status { get; set; }
        public string message { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public DateTime access_token_expiry { get; set; }
        public DateTime refresh_token_expiry { get; set; }
        public DateTime updatedate { get; set; }
        public string app_key { get; set; }
        public string secret_key { get; set; }

    }
    // -------------------------------------------------------
    // Refresh Response Model
    // -------------------------------------------------------
    public class LazadaRefreshResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string expires_in { get; set; }
        public string refresh_expires_in { get; set; }
        public string country { get; set; }
    }
}