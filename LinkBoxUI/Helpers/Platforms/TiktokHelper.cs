
using DomainLayer;
using DomainLayer.Models.Tiktok;
using LinkBoxUI.Context;
using LinkBoxUI.Services.Tiktok;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LinkBoxUI.Helpers.Tiktok
{
    public class TiktokHelper
    {
        private const string RequestPath = "/order/202309/orders/search";

        LinkboxDb _context = new LinkboxDb();
        //public TiktokHelper(APISetup config)
        //{
        //    _config = config ?? throw new ArgumentNullException(nameof(config));
        //}

        // private static string AppKey =>
        //     ConfigurationManager.AppSettings["TikTokAppKey"];

        //     private static string AppSecret =>
        //    ConfigurationManager.AppSettings["TikTokAppSecret"];

        //     private static string ShopCipher =>
        //   ConfigurationManager.AppSettings["TikTokShopCipher"];

        private static string BaseUrl =>
     ConfigurationManager.AppSettings["TiktokBaseUrl"];

        private static int Interval =>
       int.Parse(ConfigurationManager.AppSettings["TiktokInterval"]);


        private static readonly HttpClient client = new HttpClient();
        public  async Task<List<TikTokOrder>> GetOrders(string ApiCode, string DateFrom = null, string DateTo = null)
        {
            try
            {

                var APiConfig = _context.APISetups.FirstOrDefault(x => x.APICode == ApiCode);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string accessToken = await GetRefreshToken.RefreshAccessToken(APiConfig);

                //long createTimeGe = new DateTimeOffset(2026, 4, 23, 0, 0, 0,
                //                        TimeSpan.FromHours(8)).ToUnixTimeSeconds();//for specified date
                //long createTimeLt = new DateTimeOffset(2026, 4, 28, 23, 59, 59,
                //                        TimeSpan.FromHours(8)).ToUnixTimeSeconds();//for specified date

                bool isManual = !string.IsNullOrEmpty(DateFrom) && !string.IsNullOrEmpty(DateTo);

                long createTimeGe = 0;
                long createTimeLt = 0;
                if (isManual)
                {
                    if (!DateTime.TryParse(DateFrom, out var parsedFrom) ||
                        !DateTime.TryParse(DateTo, out var parsedTo))
                    {
                        throw new ArgumentException("Invalid date format for DateFrom or DateTo.");
                    }

                    var phTimeZone = TimeSpan.FromHours(8);

                    var from = new DateTimeOffset(
                        parsedFrom.Date,
                        phTimeZone
                    );

                    var to = new DateTimeOffset(
                        parsedTo.Date.AddDays(1).AddSeconds(-1),
                        phTimeZone
                    );

                    createTimeGe = from.ToUnixTimeSeconds();
                    createTimeLt = to.ToUnixTimeSeconds();

                    Console.WriteLine($"DateFrom: {from}");
                    Console.WriteLine($"DateTo:   {to}");
                    Console.WriteLine($"Unix GE:  {createTimeGe}");
                    Console.WriteLine($"Unix LT:  {createTimeLt}");
                }

                var allOrders = new List<TikTokOrder>();
                string pageToken = null;
                int pageCount = 0;
                const int maxPages = 200;

                while (pageCount < maxPages)
                {
                    pageCount++;
                    var timestamp = DateTimeOffset.UtcNow.AddSeconds(-Interval)
                                        .ToUnixTimeSeconds().ToString();



                    //var OrderIds = "584144026683934611,584144058386974604,583871299891398073,583912447780947333,583871189402748046,583870728345061300,583795076145710918,583798148811097170,583799398388368774";

                    var parameters = new Dictionary<string, string>
            {
                { "app_key",     APiConfig.APIKey },
                { "timestamp",   timestamp },
                { "shop_cipher", APiConfig.ShopCode },
                { "page_size",   "20" }
                //{"ids",OrderIds }
            };

                    if (!isManual)
                        parameters.Add("order_status", "AWAITING_SHIPMENT");//AWAITING_SHIPMENT

                    if (!string.IsNullOrEmpty(pageToken))
                        parameters.Add("page_token", pageToken);

                    string bodyJson = isManual
                        ? $"{{\"create_time_ge\":{createTimeGe},\"create_time_lt\":{createTimeLt},\"page_size\":20}}"
                        : "";

                    // string bodyJson = "";//for test

                    string sign = GenerateSign(APiConfig, parameters, bodyJson, "/order/202309/orders/search");
                    parameters.Add("sign", sign);

                    string query = BuildQuery(parameters);
                    string url = BaseUrl + RequestPath + "?" + query;

                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("x-tts-access-token", accessToken);

                    request.Content = new StringContent(
                        bodyJson, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();


                    var result = JsonConvert.DeserializeObject<TiktokOrderModel>(json);

                    Console.WriteLine($"[Page {pageCount}] orders={result?.data?.orders?.Count} | " +
                                      $"total={result?.data?.total_count} | " +
                                      $"next={result?.data?.next_page_token ?? "NONE"}");

                    if (result?.data?.orders == null || result.data.orders.Count == 0)
                        break;

                    allOrders.AddRange(result.data.orders);

                    string nextToken = result.data.next_page_token;

                    // No more pages
                    if (string.IsNullOrEmpty(nextToken))
                        break;

                    // Token didn't advance — prevent infinite loop
                    if (nextToken == pageToken)
                    {
                        Console.WriteLine("WARNING: token did not advance — breaking");
                        break;
                    }

                    pageToken = nextToken;
                }

                var distinct = allOrders
                    .GroupBy(o => o.id)
                    .Select(g => g.First())
                    .ToList();

                Console.WriteLine($"Raw: {allOrders.Count} | Deduped: {distinct.Count}");
                return distinct;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error - {ex.Message}");
            }
        }
    //    public async Task<string> GetColumn(string OrdNumber)
    //    {
    //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

    //        string timestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 90).ToString();
    //        var parameters = new Dictionary<string, string>
    //{
    //    { "app_key", AppKey },
    //    { "timestamp", timestamp },
    //    { "shop_cipher", ShopCipher },
    //    { "ids", OrdNumber },
    //};

    //        string sign = GenerateSign(parameters, "", "/order/202309/orders");
    //        parameters.Add("sign", sign);

    //        string query = BuildQuery(parameters);
    //        string url = BaseUrl + "/order/202309/orders" + "?" + query;

    //        var request = new HttpRequestMessage(HttpMethod.Get, url);

    //        string AccessToken = await GetRefreshToken.RefreshAccessToken();
    //        request.Headers.Add("x-tts-access-token", AccessToken);

    //        var response = await client.SendAsync(request);
    //        string responseBody = await response.Content.ReadAsStringAsync();

    //        // DO NOT use EnsureSuccessStatusCode() here — it discards the body,
    //        // and TikTok often returns HTTP 200 even on logical errors (bad sign, etc.)
    //        // so the real diagnostic info is always in responseBody, not the status code.

    //        // TEMP: log everything so you can see what's actually happening
    //        System.Diagnostics.Debug.WriteLine($"[TikTok GetColumn] Status: {(int)response.StatusCode} {response.StatusCode}");
    //        System.Diagnostics.Debug.WriteLine($"[TikTok GetColumn] URL: {url}");
    //        System.Diagnostics.Debug.WriteLine($"[TikTok GetColumn] Response: {responseBody}");

    //        if (!response.IsSuccessStatusCode)
    //        {
    //            throw new Exception($"TikTok API HTTP error {(int)response.StatusCode}: {responseBody}");
    //        }

    //        return responseBody;
    //    }
        private static string GenerateSign(APISetup Apisetup,Dictionary<string, string> parameters, string bodyJson = "", string reqpath = null)
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
            string signString = reqpath + paramBuilder.ToString();

            // Step 4: append body (empty string for GET, JSON for POST)
            signString += bodyJson;

            // Step 5: wrap with secret
            signString = Apisetup.APISecretKey + signString + Apisetup.APISecretKey;

            // Step 6: HMAC-SHA256
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Apisetup.APISecretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        private static string BuildQuery(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(p =>
                $"{p.Key}={WebUtility.UrlEncode(p.Value)}"));
        }
    }
}