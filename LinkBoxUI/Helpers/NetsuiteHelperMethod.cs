
using LinkBoxUI.Context;
using LinkBoxUI.Services.Shopify;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LinkBoxUI.Helpers
{

    public class NetsuiteHelperMethod
    {
        LinkboxDb _context = new LinkboxDb();
        public async Task<dynamic> GetImageDetails(string FileId)
        {
            // ===== NetSuite Credentials =====
            string accountId = ConfigurationManager.AppSettings["accountId"];
            string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string tokenId = ConfigurationManager.AppSettings["tokenId"];
            string tokenSecret = ConfigurationManager.AppSettings["tokenSecret"];

            // ===== Base URL (NO query params here) =====
            string baseUrl = $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

            // ===== Query Parameters (MUST be signed) =====
            // -------------------------------
            // Query parameters (SIGNED)
            // -------------------------------
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("script", "671");
            queryParams.Add("deploy", "1");
            queryParams.Add("fileId", FileId);


            // -------------------------------
            // OAuth parameters
            // -------------------------------
            Dictionary<string, string> oauthParams = new Dictionary<string, string>();
            oauthParams.Add("oauth_consumer_key", consumerKey);
            oauthParams.Add("oauth_token", tokenId);
            oauthParams.Add("oauth_nonce", Guid.NewGuid().ToString("N"));
            oauthParams.Add("oauth_timestamp",
                ((int)(DateTime.UtcNow -
                new DateTime(1970, 1, 1)).TotalSeconds).ToString());
            oauthParams.Add("oauth_signature_method", "HMAC-SHA256");
            oauthParams.Add("oauth_version", "1.0");

            // -------------------------------
            // Combine params for signing
            // -------------------------------
            Dictionary<string, string> allParams = new Dictionary<string, string>();
            foreach (var kv in queryParams) allParams.Add(kv.Key, kv.Value);
            foreach (var kv in oauthParams) allParams.Add(kv.Key, kv.Value);

            // -------------------------------
            // Normalize parameters
            // -------------------------------
            List<string> paramList = new List<string>();
            foreach (var kv in allParams)
            {
                paramList.Add(
                    UrlEncode(kv.Key) + "=" + UrlEncode(kv.Value)
                );
            }
            paramList.Sort();
            string normalizedParams = string.Join("&", paramList.ToArray());

            // -------------------------------
            // Signature base string
            // -------------------------------
            string signatureBase =
                "GET&" +
                UrlEncode(baseUrl) + "&" +
                UrlEncode(normalizedParams);

            string signingKey =
                UrlEncode(consumerSecret) + "&" +
                UrlEncode(tokenSecret);

            string signature;
            using (HMACSHA256 hmac =
                new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
            {
                signature = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))
                );
            }

            // -------------------------------
            // Authorization header
            // -------------------------------
            StringBuilder authHeader = new StringBuilder();
            authHeader.Append("OAuth realm=\"");
            authHeader.Append(accountId);
            authHeader.Append("\",");

            foreach (var kv in oauthParams)
            {
                authHeader.Append(kv.Key);
                authHeader.Append("=\"");
                authHeader.Append(UrlEncode(kv.Value));
                authHeader.Append("\",");
            }

            authHeader.Append("oauth_signature=\"");
            authHeader.Append(UrlEncode(signature));
            authHeader.Append("\"");

            // -------------------------------
            // Request URL
            // -------------------------------
            StringBuilder requestUrl = new StringBuilder();
            requestUrl.Append(baseUrl);
            requestUrl.Append("?");

            bool first = true;
            foreach (var kv in queryParams)
            {
                if (!first) requestUrl.Append("&");
                first = false;
                requestUrl.Append(kv.Key);
                requestUrl.Append("=");
                requestUrl.Append(UrlEncode(kv.Value));
            }

            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, requestUrl.ToString()))
            {
                request.Headers.Add("Authorization", authHeader.ToString());
                request.Headers.Add("Accept", "application/json");

                HttpResponseMessage response = await client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception(body);


                var obj = JsonConvert.DeserializeObject<ImageDetailsResponse>(body);

                if (obj == null || obj.success != true)
                    return "";

                return obj;
            }


        }
        public async Task<string> PostImagetoShopify(string ProductId, string Base64, string ImageName)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;

                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{ProductId}/images.json";

                var payload = new
                {
                    image = new
                    {
                        attachment = Base64,
                        alt = ImageName,
                    }
                };
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var json = System.Text.Json.JsonSerializer.Serialize(payload);

                    using (var content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    ))
                    {
                        var response = await client.PostAsync(url, content);
                        var responseBody = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();

                            return $"Shopify error {response.StatusCode}: {errorBody}";

                        }
                        return responseBody;

                    }

                }
            }
            catch (Exception ex)
            {
                return $"error - {ex.Message.ToString()}";
            }


        }
        public async Task<ShopifyApiResult> DeleteShopifyImage(
    string productId,
    string imageId)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;

                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{productId}/images/{imageId}.json";


                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var response = await client.DeleteAsync(url);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new ShopifyApiResult
                        {
                            StatusCode = 404,
                            IsSuccess = true,
                            ReasonPhrase = "Image already deleted",
                            Body = responseBody
                        };
                    }

                    return new ShopifyApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody
                    };


                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        private static string UrlEncode(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty)
                      .Replace("!", "%21")
                      .Replace("*", "%2A")
                      .Replace("'", "%27")
                      .Replace("(", "%28")
                      .Replace(")", "%29");
        }
        public bool Save_SystemLogs(string orderId, string apiUrl, string json, string errorMsg, string module)
        {
            var existing = _context.SystemLogs
                .FirstOrDefault(x => x.Table == orderId);

            if (existing != null)
            {
                if (existing.ErrorMsg != errorMsg)
                {
                    // Update — overwrite with latest attempt result
                    existing.Table = orderId;
                    existing.ApiUrl = apiUrl;
                    existing.Json = json;
                    existing.ErrorMsg = errorMsg;
                    existing.Database = "0";         // add this column if not present
                    existing.CreateDate = DateTime.Now;    // add this column if not present
                }

            }
            else
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Table = orderId,              // add this column if not present
                    Task = $"{module} Sales Order Posting",
                    ApiUrl = apiUrl,
                    Json = json,
                    ErrorMsg = errorMsg,
                    Module = module,
                    CreateDate = DateTime.Now,
                });
            }

            _context.SaveChanges();
            return true;
        }

    }
}