
using DataAccessLayer.Class;
using LinkBoxUI.Context;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
namespace LinkBoxUI.Services.Lazada
{
    public class LazadaTrackingService
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();

        private readonly string _baseUrl = "https://api.lazada.com.ph/rest";
        public async Task<bool> LazadaUpdateTracking()
        {

            try
            {
                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == "Fulfilled" && x.Platform == "Lazada").Select(x => x.ReferenceId).ToList();
                if (order_numbers.Count > 0 && order_numbers != null)
                {
                    var orderNumbersCsv = string.Join(",", order_numbers.Select(n => $"'{n}'"));

                    var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                    var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();

                    string Query = $@"SELECT
	                                    id,
	                                    custbody_ordernumber AS ordernumber,
	                                    custbody_status AS status
                                    FROM
	                                    transaction
                                    WHERE
	                                    type = 'SalesOrd' 
                                        AND REGEXP_REPLACE(TRIM(custbody_ordernumber), '[^0-9]', '')  
                                        IN ({orderNumbersCsv}) 
                                        and custbody_status='Pending Billing'
	                                    AND entity = 211
	                                    --AND id = 1538225
	                                    AND ( custbody_trackingcode IS NULL
	                                    OR TRIM(custbody_trackingcode) = '' )";

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
                            var track_number = await LazadaGetTrackingNumber(platformRecord.ReferenceId);//platformRecord.ReferenceId
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

    
        private async Task<string> LazadaGetTrackingNumber(string orderId)
        {
            try
            {
                var tokenService = new LazadaTokenService();
                var token = await tokenService.CallRefreshEndpoint();

                if (token == null) return null;
                using (var client = new HttpClient())
                {
                    var apiPath = "/order/items/get";
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                    //var tokenService = new LazadaTokenService();
                    //var token = await tokenService.CallRefreshEndpoint();

                    var paramDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken  },
                    { "app_key",      token.AppKey },
                    { "order_id",     orderId.ToString() },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                    var stringToSign = apiPath;
                    foreach (var kvp in paramDict)
                        stringToSign += kvp.Key + kvp.Value;

                    var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                    var queryString = string.Join("&",
                        paramDict.Select(kvp =>
                            $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                    var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";
                    var response = await client.GetStringAsync(fullUrl);
                     var result = JsonConvert.DeserializeObject<LazadaTrackingResponse>(response);


                    string trackingNumber = string.Join(",",
                           result?.Data?
                               .Where(x => !string.IsNullOrWhiteSpace(x?.TrackingCode))
                               .Select(x => x.TrackingCode.Trim())
                               .Distinct()
                           ?? Enumerable.Empty<string>());
                  

                    return trackingNumber ?? "";
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching items for order {orderId}: {ex.Message}", ex);
                //Console.WriteLine($"Error fetching items for order {orderId}: {ex.Message}");
                //return new List<LazadaOrderItemModel>();
            }
        }
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
        public class LazadaTrackingResponse
        {
            [JsonProperty("data")]
            public List<LazadaTrackingItem> Data { get; set; }
        }

        public class LazadaTrackingItem
        {
            [JsonProperty("tracking_code")]
            public string TrackingCode { get; set; }
        }
    }
}