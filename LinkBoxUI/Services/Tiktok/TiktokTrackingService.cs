
using DataAccessLayer.Class;
using DomainLayer.Models.Tiktok;
using LinkBoxUI.Context;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
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
using System.Net.Http;
using static LinkBoxUI.Services.Shopify.ShopifyTrackingService;

namespace LinkBoxUI.Services.Tiktok
{
    public class TiktokTrackingService
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        private const string RequestPath = "/order/202507/orders";
        private static string AppKey =>
            ConfigurationManager.AppSettings["TikTokAppKey"];

        private static string AppSecret =>
       ConfigurationManager.AppSettings["TikTokAppSecret"];

        private static string ShopCipher =>
      ConfigurationManager.AppSettings["TikTokShopCipher"];

        private static string BaseUrl =>
    ConfigurationManager.AppSettings["TiktokBaseUrl"];

        private static readonly HttpClient client = new HttpClient();
        public async Task<bool> TiktokUpdateTracking()
        {

            try
            {

                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == "Fulfilled" && x.Platform == "Tiktok").Select(x => x.ReferenceId).ToList();
                if (order_numbers.Count > 0 && order_numbers != null)
                {
                    var orderNumbersCsv = string.Join(",", order_numbers.Select(n => $"'{n}'"));

                    var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                    var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();

                    string Query = $@"SELECT 
                                       id,
                                       custbody_ordernumber as ordernumber,
                                       custbody_status as status
                                    FROM transaction
                                    WHERE type = 'SalesOrd'
                                    AND REGEXP_REPLACE(TRIM(custbody_ordernumber), '[^0-9]', '') 
                                    IN ({orderNumbersCsv}) 
                                    --and custbody_status='Pending Billing'
                                    and entity = 214
                                    and id = 1538817
								AND (
										custbody_trackingcode IS  NULL
										OR TRIM(custbody_trackingcode) = ''
									)";

                    //next time the status is not Pending Billing

                    var result = await NShelper.ListGetNetSuiteData(QueryEndpoint, Query);

                    if (result != null && result.Count > 0)
                    {
                        foreach (var res in result)
                        {
                            var platformRecord = _context.PlatformTable
                                    .FirstOrDefault(x => x.ReferenceId == res.ordernumber
                                      && x.SyncStatus == "Sync"
                                      && x.Fulfillment == "Fulfilled");

                            if (platformRecord == null) continue;

                            string NetsuiteID = res.id;
                            var track_number = await TiktokGetTrackingNumber(platformRecord.ReferenceId);//platformRecord.ReferenceId
                            if (!string.IsNullOrEmpty(track_number))
                            {
                                var payload = new
                                {
                                    custbody_trackingcode = track_number
                                };

                                string payloadJson = JsonConvert.SerializeObject(payload);
                                string postingResult = await NShelper.PostDataToNetSuite(PostDataEndpoint, payloadJson, "salesorder", "PATCH", NetsuiteID);

                                if (postingResult.Contains("successfully posted"))
                                {
                                    platformRecord.Fulfillment = $"{platformRecord.Fulfillment},{track_number}";
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<string> TiktokGetTrackingNumber(string referenceId)
        {
            if (string.IsNullOrEmpty(referenceId))
                return null;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string accessToken = "";//await GetRefreshToken.RefreshAccessToken();

                var timestamp = DateTimeOffset.UtcNow.AddSeconds(-60)
                                              .ToUnixTimeSeconds().ToString();

                var parameters = new Dictionary<string, string>
        {
            { "app_key",     AppKey },
            { "timestamp",   timestamp },
            { "shop_cipher", ShopCipher },
            { "page_size",   "20" },
            { "ids",         referenceId }
        };

                string sign = GenerateSign(parameters, "");
                parameters.Add("sign", sign);

                string query = BuildQuery(parameters);
                string url = BaseUrl + RequestPath + "?" + query;

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-tts-access-token", accessToken);
                // no Content — GET request

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<TiktokOrderResponse>(json);

                string trackingNumber = string.Join(",",
                    result?.Data?.Orders
                        .SelectMany(o => o.LineItems)
                        .Where(x => !string.IsNullOrWhiteSpace(x?.TrackingNumber))
                        .Select(x => x.TrackingNumber.Trim())
                        .Distinct()
                    ?? Enumerable.Empty<string>()
                );

                return trackingNumber;
            }
            catch (Exception ex)
            {
                throw new Exception($"TiktokGetTrackingNumber Error - {ex.Message}");
            }
        }
        private string GenerateSign(Dictionary<string, string> parameters, string bodyJson = "")
        {
            // Step 1: remove sign/access_token and sort
            var sorted = parameters
                .Where(p => p.Key != "sign" && p.Key != "access_token")
                .OrderBy(p => p.Key);

            // Step 2: concatenate key+value
            StringBuilder paramBuilder = new StringBuilder();
            foreach (var p in sorted)
                paramBuilder.Append(p.Key).Append(p.Value);

            // Step 3: prepend API path
            string signString = RequestPath + paramBuilder.ToString();

            // Step 4: append body (empty string for GET, JSON for POST)
            signString += bodyJson;

            // Step 5: wrap with secret
            signString = AppSecret + signString + AppSecret;

            // Step 6: HMAC-SHA256
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private string BuildQuery(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(p =>
                $"{p.Key}={WebUtility.UrlEncode(p.Value)}"));
        }

        public class TiktokOrderResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("data")]
            public TiktokOrderData Data { get; set; }
        }

        public class TiktokOrderData
        {
            [JsonProperty("orders")]
            public List<TiktokOrder> Orders { get; set; }
        }

        public class TiktokOrder
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("line_items")]
            public List<TiktokLineItem> LineItems { get; set; }
        }

        public class TiktokLineItem
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("tracking_number")]
            public string TrackingNumber { get; set; }
        }
    }
}