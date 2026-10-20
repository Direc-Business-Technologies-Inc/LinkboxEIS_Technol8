using DataAccessLayer.Class;
using LinkBoxUI.Context;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LinkBoxUI.Services.Shopee
{
    public class ShopeeFullfillmentService
    {
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        LinkboxDb _context = new LinkboxDb();
        ShopeeTokenService tokenService = new ShopeeTokenService();

        // ✅ Correct Sandbox Base URL
        private static string _baseUrl =>
          ConfigurationManager.AppSettings["Sh_baseUrl"];
        private static string _partnerId =>
            ConfigurationManager.AppSettings["Sh_partnerId"];
        private static string _partnerKey =>
            ConfigurationManager.AppSettings["Sh_partnerKey"];
        private static string _shopId =>
            ConfigurationManager.AppSettings["Sh_shopId"];

        public async Task<bool> CheckFulfillment()
        {
            try
            {
                // ← Get orders from PlatformTable that are Synced but not yet fulfilled
                var order_numbers = _context.PlatformTable
                    .Where(x => x.SyncStatus == "Sync"
                             && x.Fulfillment == null
                             && x.Platform == "Shopee")
                    .Select(x => x.OrderNumber)
                    .ToList();

                if (order_numbers == null || order_numbers.Count == 0)
                    return true;

                var orderNumbersCsv = string.Join(",", order_numbers.Select(n => $"'{n}'"));

                var QueryEndpoint = _context.APISetups
                    .Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint"))
                    .Select(x => x.APIURL)
                    .FirstOrDefault();

                // ← Query NetSuite for order status
                string Query = $@"SELECT 
                                    id,
                                    custbody_ordernumber as ordernumber,
                                    custbody_status as status
                                FROM ""transaction""
                                WHERE type = 'SalesOrd'
                                AND custbody_ordernumber
                                IN ({orderNumbersCsv})
                                AND custbody_status = 'Pending_Billing'";

                var result = await nsHelper.ListGetNetSuiteData(QueryEndpoint, Query);

                if (result == null || result.Count == 0)
                    return true;

                foreach (var res in result)
                {
                    var platformRow = _context.PlatformTable
                        .FirstOrDefault(x => x.OrderNumber == res.ordernumber
                                          && x.SyncStatus == "Sync"
                                          && x.Fulfillment == null
                                          && x.Platform == "Shopee");

                    if (platformRow == null) continue;

                    platformRow.Fulfillment = "Fulfilled";
                    platformRow.FulfilledDate = DateTime.Now;
                    _context.SaveChanges();

                    // ← ReferenceId here should be the Shopee order_sn

                    //var isPosted = await PostFulfillment(platformRow.ReferenceId);

                    //if (isPosted.IsSuccess || isPosted.Body.ToString().Contains("package_already_shipped") || isPosted.Body.ToString().Contains("ship_order_not_ready_to_ship"))
                    //{
                    //    platformRow.Fulfillment = isPosted.Body.ToString().Contains("ship_order_not_ready_to_ship") ? "Canceled" : "Fulfilled";
                    //    platformRow.FulfilledDate = DateTime.Now;
                    //    _context.SaveChanges();
                    //}
                    //else
                    //{
                    //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                    //    {
                    //        Task = "ShopeeFullfillment",
                    //        ApiUrl = "PostFulfillment",
                    //        Json = platformRow.ReferenceId,
                    //        ErrorMsg = $"Error {isPosted.StatusCode}: {isPosted.ReasonPhrase} - {isPosted.Body}",
                    //        CreateDate = DateTime.Now,
                    //    });
                    //    _context.SaveChanges();
                    //}
                }

                return true;
            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "ShopeeFullfillment",
                    ErrorMsg = ex.Message,
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
                throw;
            }
        }

        // -------------------------------------------------------
        // POST fulfillment to Shopee
        // POST /api/v2/logistics/ship_order
        // -------------------------------------------------------
        public async Task<ShopeeApiResult> PostFulfillment(string order_sn)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();

                // ✅ HARDCODED for debugging - bypass _baseUrl entirely
                //var baseUrl = "https://partner.test-stable.shopeemobile.com";
                var apiPath = "/api/v2/logistics/ship_order";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();


                //var sign = GenerateSignature(apiPath, timestamp, token.AccessToken);
                var sign = GenerateSignature(apiPath, timestamp, token.AccessToken);
                var fullUrl = $"{_baseUrl}{apiPath}"
                         + $"?partner_id={token.PartnerId}"
                         + $"&timestamp={timestamp}"
                         + $"&access_token={token.AccessToken}"  // ✅ raw token, NO EscapeDataString
                         + $"&shop_id={token.ShopId}"
                         + $"&sign={sign}"; ;

              
                var payload = JsonConvert.SerializeObject(new
                {
                    order_sn = order_sn,
                    dropoff = new
                    {
                        address_id = 11658
                    }
                });

                using (var client = new HttpClient())
                {
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(fullUrl, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    //Console.WriteLine($"RESPONSE  : {responseBody}");

                    var shopeeResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    bool isSuccess = response.IsSuccessStatusCode
                                     && (string)shopeeResponse?.error == "";

                    return new ShopeeApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = isSuccess,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"PostFulfillment error: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // ✅ Shopee HMAC-SHA256 — secret is PartnerKey, NOT PartnerId
        // -------------------------------------------------------
        private string GenerateSignature(string path, long timestamp, string accessToken)
        {
            var raw = $"{_partnerId}{path}{timestamp}{accessToken}{_shopId}";
            var keyBytes = Encoding.UTF8.GetBytes(_partnerKey);
            var msgBytes = Encoding.UTF8.GetBytes(raw);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }

    // -------------------------------------------------------
    // Models
    // -------------------------------------------------------
    public class ShopeeApiResult
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ReasonPhrase { get; set; }
        public string Body { get; set; }
        public string PartnerKey { get; set; }  // ← ADD THIS   
    }
}