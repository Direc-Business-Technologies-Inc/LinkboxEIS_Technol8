
using Azure.Core;
using ClosedXML.Excel;
using DataAccessLayer.Class;
using LinkBoxUI.Context;
using LinkBoxUI.Services.Zalora;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LinkBoxUI.Services.Tiktok
{
    public class TiktokFullfillmentService
    {
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        LinkboxDb _context = new LinkboxDb();
        ZaloraAccessTokenService accessTokenService = new ZaloraAccessTokenService();
        public async Task<bool> CheckFulfillment()
        {
            try
            {

                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == null && x.Platform == "Tiktok").Select(x => x.ReferenceId).ToList();
                if (order_numbers.Count > 0 && order_numbers != null)
                {
                    var orderNumbersCsv = string.Join(",", order_numbers.Select(n => $"'{n}'"));
                    var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                    var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();

                    string Query = $@"SELECT 
                                       id,
                                       custbody_ordernumber as ordernumber,
                                       custbody_status as status
                                    FROM ""transaction""
                                    WHERE type = 'SalesOrd'
                                    AND REGEXP_REPLACE(TRIM(custbody_ordernumber), '[^0-9]', '') 
                                    IN ({orderNumbersCsv}) 
                                    and custbody_status='Pending Billing';";

                    var result = await nsHelper.ListGetNetSuiteData(QueryEndpoint, Query);

                    if (result != null && result.Count > 0)
                    {
                        foreach (var res in result)
                        {

                            var platformRow = _context.PlatformTable
                                .FirstOrDefault(x => x.ReferenceId == res.ordernumber);

                            if (platformRow != null)
                            {
                                platformRow.Fulfillment = "Fulfilled";
                                platformRow.FulfilledDate = DateTime.Now;
                                _context.SaveChanges();
                            }


                            //string packageId = _context.PlatformTable.Where(x => x.ReferenceId == res.ordernumber && x.SyncStatus.Equals("Sync") && x.Platform == "Tiktok" && x.Fulfillment == null).Select(x => x.OrderNumber).FirstOrDefault();

                            ////var packageId = "1189367559748682762";

                            //var _result = await ShipPackageDropOffAsync(packageId);

                            //var json = System.Text.Json.JsonDocument.Parse(_result);
                            //int tikTokCode = json.RootElement.GetProperty("code").GetInt32();

                            //if (tikTokCode != 0)
                            //{
                            //    if (_result.ToString().IndexOf("some orders have been canceled", StringComparison.OrdinalIgnoreCase) >= 0)
                            //    {
                            //        var ids = new List<string>();
                            //        var match = Regex.Match(_result, @"OrderId:\[([^\]]+)\]");
                            //        if (match.Success)
                            //        {
                            //            ids = match.Groups[1].Value
                            //                .Split(',')
                            //                .Select(id => id.Trim())
                            //                .Where(id => !string.IsNullOrEmpty(id))
                            //                .ToList();
                            //        }

                            //        if (ids.Any())
                            //        {
                            //            var toBeUpdated = _context.PlatformTable
                            //                .Where(x => ids.Contains(x.ReferenceId))
                            //                .ToList();

                            //            if (toBeUpdated.Any())
                            //            {
                            //                foreach (var row in toBeUpdated)
                            //                {
                            //                    row.Fulfillment = "Canceled";
                            //                    row.FulfilledDate = DateTime.Now;
                            //                }
                            //                _context.SaveChanges();
                            //            }
                            //        }

                            //        continue;
                            //    }
                            //}




                            //if (_result.ToString().Contains("Success"))
                            //{
                            //    var platformRow = _context.PlatformTable
                            //    .FirstOrDefault(x => x.ReferenceId == res.ordernumber);
                            //    if (platformRow != null)
                            //    {
                            //        platformRow.Fulfillment = "Fulfilled";
                            //        platformRow.FulfilledDate = DateTime.Now;
                            //        _context.SaveChanges();
                            //    }
                            //}
                            //else
                            //{
                            //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            //    {
                            //        Task = $"Line : Code 76",
                            //        ApiUrl = "await PostFulfillment(JsonSerialize)",
                            //        Json = $"{_result}",
                            //        ErrorMsg = $"Error - {_result}",
                            //        CreateDate = DateTime.Now,
                            //    });
                            //    _context.SaveChanges();
                            //}



                        }


                    }// end of result 

                }

                return true;
            }
            catch (Exception ex)
            {

                throw;

            }
        }
        //private const string BaseUrl = "https://open-api.tiktokglobalshop.com";
        //private const string AppKey = "6j705mi7urqp9";
        //private const string AppSecret = "491ea51ce5bfdd5a7d01a3a930db6dc2c1966c7e";
        //private const string ShopCipher = "ROW__BmOPQAAAACMrBYuDcN3-zzTPR8WqTGX";//"ROW_Z7lgdAAAAAAQNNI4xxel0MueZjo2Otvd";live
        private static string AppKey => ConfigurationManager.AppSettings["TikTokAppKey"];
        private static string AppSecret => ConfigurationManager.AppSettings["TikTokAppSecret"];
        private static string ShopCipher => ConfigurationManager.AppSettings["TikTokShopCipher"];
        private static string BaseUrl => ConfigurationManager.AppSettings["TiktokBaseUrl"];

        /// <summary>
        /// Ship TikTok package using handover_method = DROP_OFF
        /// </summary>
        public static async Task<string> ShipPackageDropOffAsync(string packageId)
        {
            string requestPath = string.Format("/fulfillment/202309/packages/{0}/ship", packageId);
            string timestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 60).ToString();

            var queryParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "app_key", AppKey },
            { "shop_cipher", ShopCipher },
            { "timestamp", timestamp }
        };

            string bodyJson = "{\"handover_method\":\"DROP_OFF\"}";
            string sign = GenerateSign(requestPath, queryParams, bodyJson, AppSecret);

            // IMPORTANT:
            // Do NOT include access_token in query string for v202309.
            string requestUrl =
                BaseUrl + requestPath +
                "?app_key=" + Uri.EscapeDataString(AppKey) +
                "&timestamp=" + Uri.EscapeDataString(timestamp) +
                "&shop_cipher=" + Uri.EscapeDataString(ShopCipher) +
                "&sign=" + Uri.EscapeDataString(sign);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                string AccessToken = "";//await GetRefreshToken.RefreshAccessToken();
                request.Headers.Add("x-tts-access-token", AccessToken);
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        string.Format(
                            "TikTok Ship Package failed. Status: {0} {1}. Response: {2}",
                            (int)response.StatusCode,
                            response.StatusCode,
                            responseBody));
                }

                return responseBody;
            }
        }

        /// <summary>
        /// Generates TikTok Shop API sign using the same logic as your Postman script.
        /// </summary>
        private static string GenerateSign(
          string requestPath,
          SortedDictionary<string, string> queryParams,
          string bodyJson,
          string appSecret)
        {
            var filteredParams = queryParams
                .Where(x => !string.Equals(x.Key, "access_token", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(x.Key, "sign", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Key, StringComparer.Ordinal);

            var paramBuilder = new StringBuilder();
            foreach (var param in filteredParams)
            {
                paramBuilder.Append(param.Key);
                paramBuilder.Append(param.Value);
            }

            var signBuilder = new StringBuilder();
            signBuilder.Append(requestPath);
            signBuilder.Append(paramBuilder.ToString());

            if (!string.IsNullOrWhiteSpace(bodyJson) && bodyJson != "{}")
            {
                signBuilder.Append(bodyJson);
            }

            string signString = appSecret + signBuilder.ToString() + appSecret;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));

                // .NET Framework-safe hex conversion
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}