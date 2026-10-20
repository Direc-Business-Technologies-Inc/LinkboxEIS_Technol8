
using DataAccessLayer.Class;
using LinkBoxUI.Context;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static LinkBoxUI.Services.Shopify.ShopifyTrackingService;

namespace LinkBoxUI.Services.Shopee
{
    public class ShopeeTrackingService
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        private static string _baseUrl => ConfigurationManager.AppSettings["Sh_baseUrl"];
        private static string _partnerId => ConfigurationManager.AppSettings["Sh_partnerId"];
        private static string _partnerKey => ConfigurationManager.AppSettings["Sh_partnerKey"];
        private static string _shopId => ConfigurationManager.AppSettings["Sh_shopId"];
        public async Task<bool> ShopeeUpdateTracking()
        {
            try
            {

                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == "Fulfilled" && x.Platform == "Shopee").Select(x => x.OrderNumber).ToList();
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
                                     AND custbody_ordernumber
                                     IN ({orderNumbersCsv})
                                    and custbody_status='Pending Billing'
                                    and entity = 213
                                    --and id = 1538485
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
                                    .FirstOrDefault(x => x.OrderNumber == res.ordernumber
                                      && x.SyncStatus == "Sync"
                                      && x.Fulfillment == "Fulfilled");

                            if (platformRecord == null) continue;

                            string NetsuiteID = res.id;
                            var track_number = await ShopeeGetTrackingNumber(platformRecord.ReferenceId);//platformRecord.ReferenceId
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

        public async Task<string> ShopeeGetTrackingNumber(string ReferenceId)
        {
            if (string.IsNullOrEmpty(ReferenceId))
                return null;
            try
            {
                try
                {
                    var tokenService = new ShopeeTokenService();
                    var token = await tokenService.CallRefreshEndpoint();
                    if(token == null || string.IsNullOrEmpty(token.AccessToken))
                        return null;
                    string accessToken = token.AccessToken;
                    using (var client = new HttpClient())
                    {
                        var path = "/api/v2/logistics/get_tracking_number";
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var sign = GenerateSignature(path, timestamp, accessToken);



                        // Parameter order matches Postman script exactly
                        var fullUrl = $"{_baseUrl}{path}" +
                                      $"?partner_id={_partnerId}" +
                                      $"&shop_id={_shopId}" +
                                      $"&access_token={accessToken}" +
                                      $"&timestamp={timestamp}" +
                                      $"&order_sn={ReferenceId}" +
                                      $"&sign={sign}";

                        var response = await client.GetStringAsync(fullUrl);
                        var result = JsonConvert.DeserializeObject<ShopeeTrackingResponse>(response);

                        string trackingNumber = string.IsNullOrWhiteSpace(result?.Response?.TrackingNumber)
                               ? string.Empty
                               : result.Response.TrackingNumber.Trim();


                        return trackingNumber ?? "";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching Shopee order detail {ReferenceId}: {ex.Message}");
                    return null;
                }
                //return  "";

            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }
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
        public class ShopeeTrackingResponse
        {
            

            [JsonProperty("response")]
            public ShopeeTrackingItem Response { get; set; }  // ✅ single object
        }

        public class ShopeeTrackingItem
        {
            [JsonProperty("tracking_number")]
            public string TrackingNumber { get; set; }

        }
    }
}