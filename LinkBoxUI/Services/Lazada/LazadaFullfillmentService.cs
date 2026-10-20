using DataAccessLayer.Class;
using LinkBoxUI.Context;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace LinkBoxUI.Services.Lazada
{
    public class LazadaFullfillmentService
    {
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        LinkboxDb _context = new LinkboxDb();
        LazadaTokenService tokenService = new LazadaTokenService();


        private readonly string _baseUrl = "https://api.lazada.com.ph/rest";

        // -------------------------------------------------------
        // Check and process fulfillment for Lazada orders
        // -------------------------------------------------------
        public async Task<bool> CheckFulfillment()
        {
            try
            {
                //await PostFulfillment("");
                // ← Get orders from PlatformTable that are Synced but not yet fulfilled
                var order_numbers = _context.PlatformTable
                    .Where(x => x.SyncStatus == "Sync"
                             && x.Fulfillment == null
                             && x.Platform == "Lazada")
                    .Select(x => x.ReferenceId)
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
                                AND REGEXP_REPLACE(TRIM(custbody_ordernumber), '[^0-9]', '') 
                                IN ({orderNumbersCsv})
                                AND custbody_status = 'Pending_Billing'";

                var result = await nsHelper.ListGetNetSuiteData(QueryEndpoint, Query);

                if (result == null || result.Count == 0)
                    return true;

                foreach (var res in result)
                {
                    var platformRow = _context.PlatformTable
                        .FirstOrDefault(x => x.ReferenceId == res.ordernumber
                                          && x.SyncStatus == "Sync"
                                          && x.Fulfillment == null
                                          && x.Platform == "Lazada");

                    if (platformRow == null) continue;

                    // ← Post fulfillment to Lazada
                    var isPosted = await PostFulfillment(platformRow.OrderNumber); //platformRow.OrderNumber

                    if (isPosted.IsSuccess && isPosted.ErrorCode == "0")
                    {
                        platformRow.Fulfillment = "Fulfilled";
                        platformRow.FulfilledDate = DateTime.Now;
                        _context.SaveChanges();
                    }
                    else
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                        {
                            Task = "LazadaFullfillment",
                            ApiUrl = "PostFulfillment",
                            Json = platformRow.ReferenceId,
                            ErrorMsg = $"Error {isPosted.StatusCode}: {isPosted.ReasonPhrase} - {isPosted.Body}",
                            CreateDate = DateTime.Now,
                        });
                        _context.SaveChanges();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "LazadaFullfillment",
                    ErrorMsg = ex.Message,
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
                throw;
            }
        }

        // -------------------------------------------------------
        // OLD
        // POST fulfillment to Lazada
        // POST /order/package/rts
        // -------------------------------------------------------
        //public async Task<LazadaApiResult> PostFulfillment(string package_id)
        //{
        //    try
        //    {
        //        //package_id = "FP039612951333907";
        //        var token = await tokenService.CallRefreshEndpoint();
        //        var apiPath = "/order/package/rts";
        //        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        //        // ← Build JSON value for form body first
        //        var jsonValue = JsonConvert.SerializeObject(new
        //        {
        //            packages = new[]
        //            {
        //        new { package_id = package_id }
        //    }
        //        });

        //        // ← For signature — includes ALL params including readyToShipReq
        //        var signDict = new SortedDictionary<string, string>
        //{
        //    { "access_token",   token.AccessToken },
        //    { "app_key",        token.AppKey },
        //    { "readyToShipReq", jsonValue },
        //    { "sign_method",    "sha256" },
        //    { "timestamp",      timestamp }
        //};

        //        // ← Generate signature
        //        var stringToSign = apiPath;
        //        foreach (var kvp in signDict)
        //            stringToSign += kvp.Key + kvp.Value;

        //        var sign = GenerateSignature(stringToSign, token.AppSecretKey);

        //        // ← For URL — excludes readyToShipReq, sign appended at end
        //        var urlDict = new SortedDictionary<string, string>
        //{
        //    { "access_token", token.AccessToken },
        //    { "app_key",      token.AppKey },
        //    { "sign_method",  "sha256" },
        //    { "timestamp",    timestamp }
        //};

        //        var queryString = string.Join("&",
        //            urlDict.Select(kvp =>
        //                $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        //        var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

        //        Console.WriteLine($"Fulfillment URL: {fullUrl}");
        //        Console.WriteLine($"Payload: {jsonValue}");

        //        using (var client = new HttpClient())
        //        {
        //            var formData = new List<KeyValuePair<string, string>>
        //    {
        //        new KeyValuePair<string, string>("readyToShipReq", jsonValue)
        //    };

        //            var content = new FormUrlEncodedContent(formData);
        //            var response = await client.PostAsync(fullUrl, content);
        //            var responseBody = await response.Content.ReadAsStringAsync();

        //            Console.WriteLine($"Fulfillment response: {responseBody}");

        //            return new LazadaApiResult
        //            {
        //                StatusCode = (int)response.StatusCode,
        //                IsSuccess = response.IsSuccessStatusCode,
        //                ReasonPhrase = response.ReasonPhrase,
        //                Body = responseBody
        //            };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"PostFulfillment error: {ex.Message}");
        //    }
        //}
        public async Task<LazadaApiResult> PostFulfillment(string orderid)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();
                var apiPath = "/order/fulfill/pack";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var orderItems = await GetOrderItemsAsync(orderid, token);

                // Build the pack request payload as a proper C# object
                var packRequestPayload = new
                {
                    pack_order_list = new[]
                    {
                new
                {
                    order_id = long.Parse(orderid),
                    order_item_list = orderItems.Select(i => i.order_item_id).ToList(),
                }
            },
                    delivery_type = "dropship",
                    shipping_allocate_type = "TFS"
                };

                var packReq = JsonConvert.SerializeObject(packRequestPayload);

                // Signature includes packReq as a parameter
                var signDict = new SortedDictionary<string, string>
        {
            { "access_token", token.AccessToken },
            { "app_key",      token.AppKey },
            { "packReq",      packReq },          // fixed typo: was "packReqpackReq"
            { "sign_method",  "sha256" },
            { "timestamp",    timestamp }
        };

                var stringToSign = apiPath;
                foreach (var kvp in signDict)
                    stringToSign += kvp.Key + kvp.Value;

                var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                // URL query string excludes packReq body param
                var urlDict = new SortedDictionary<string, string>
        {
            { "access_token", token.AccessToken },
            { "app_key",      token.AppKey },
            { "sign_method",  "sha256" },
            { "timestamp",    timestamp }
        };

                var queryString = string.Join("&",
                    urlDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

                Console.WriteLine($"Fulfillment URL: {fullUrl}");
                Console.WriteLine($"Payload: {packReq}");

                using (var client = new HttpClient())
                {

                    var formData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("packReq", packReq)  // was "jsonValue" (undefined)
        };

                    var content = new FormUrlEncodedContent(formData);
                    var response = await client.PostAsync(fullUrl, content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var parsed = JObject.Parse(responseBody);
                    var code = parsed["code"]?.ToString();
                    Console.WriteLine($"Fulfillment response: {responseBody}");

                    return new LazadaApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        ErrorCode = code,
                        Body = responseBody
                    };

                }
            }
            catch (Exception ex)
            {
                throw new Exception($"PostFulfillment error: {ex.Message}", ex); // preserve stack trace
            }
        }
        private async Task<List<LazadaOrderItemModel>> GetOrderItemsAsync(string orderId, LazadaTokenModel token)
        {
            try
            {
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
                    var result = JsonConvert.DeserializeObject<LazadaItemResponse>(response);

                    return result?.data ?? new List<LazadaOrderItemModel>();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching items for order {orderId}: {ex.Message}");
                return new List<LazadaOrderItemModel>();
            }
        }

        // -------------------------------------------------------
        // Generate HMAC-SHA256 signature
        // -------------------------------------------------------
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

    // -------------------------------------------------------
    // Models
    // -------------------------------------------------------
    public class LazadaApiResult
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ReasonPhrase { get; set; }
        public string Body { get; set; }
       public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}