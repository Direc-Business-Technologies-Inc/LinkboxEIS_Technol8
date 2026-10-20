
using DomainLayer;
using LinkBoxUI.Services.Lazada;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinkBoxUI.Helpers.Platforms
{
    public class LazadaHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _baseUrl = "https://api.lazada.com.ph/rest";

        #region
        //public static async Task<List<LazadaOrderModel>> GetLazadaOrdersAsync(APISetup apisetup,string DateFrom = null, string DateTo = null)
        //{
        //    var tokenService = new LazadaTokenService();
        //    var token = await tokenService._GenerateRefreshToken(apisetup);
        //    var allOrders = new List<LazadaOrderModel>();
        //    const int limit = 20;
        //    int offset = 0;
        //    int totalCount = int.MaxValue;

        //    while (offset < totalCount)
        //    {
        //        var page = await FetchLazadaOrderPageAsync(token, limit, offset, DateFrom, DateTo);

        //        if (offset == 0)
        //            totalCount = page.countTotal;

        //        if (page.orders == null || page.orders.Count == 0)
        //            break;

        //        var semaphore = new SemaphoreSlim(5);
        //        var itemTasks = page.orders.Select(async order =>
        //        {
        //            await semaphore.WaitAsync();
        //            try
        //            {
        //                //order.order_id.ToString()
        //                order.items = await GetOrderItemsAsync("1120438919911713", token);
        //                order.package_id = order.items?.FirstOrDefault()?.package_id ?? "";
        //                return order;
        //            }
        //            finally
        //            {
        //                semaphore.Release();
        //            }
        //        });


        //        var fetched = await Task.WhenAll(itemTasks);
        //        allOrders.AddRange(fetched.Where(o => o != null));

        //        offset += limit;
        //    }

        //    return allOrders;
        //}

        //private static async Task<List<LazadaOrderItemModel>> GetOrderItemsAsync(string orderId, LazadaTokenModel token)
        //{
        //    try
        //    {
        //        using (var client = new HttpClient())
        //        {
        //            var apiPath = "/order/items/get";
        //            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        //            //var tokenService = new LazadaTokenService();
        //            //var token = await tokenService.CallRefreshEndpoint();

        //            var paramDict = new SortedDictionary<string, string>
        //        {
        //            { "access_token", token.AccessToken  },
        //            { "app_key",      token.AppKey },
        //            { "order_id",     orderId.ToString() },
        //            { "sign_method",  "sha256" },
        //            { "timestamp",    timestamp }
        //        };

        //            var stringToSign = apiPath;
        //            foreach (var kvp in paramDict)
        //                stringToSign += kvp.Key + kvp.Value;

        //            var sign = GenerateSignature(stringToSign, token.AppSecretKey);

        //            var queryString = string.Join("&",
        //                paramDict.Select(kvp =>
        //                    $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        //            var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";
        //            var response = await client.GetStringAsync(fullUrl);
        //            var result = JsonConvert.DeserializeObject<LazadaItemResponse>(response);

        //            return result?.data ?? new List<LazadaOrderItemModel>();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error fetching items for order {orderId}: {ex.Message}");
        //        return new List<LazadaOrderItemModel>();
        //    }
        //}

        //private static async Task<LazadaOrderData> FetchLazadaOrderPageAsync(
        //    LazadaTokenModel token, int limit, int offset, string DateFrom = null, string DateTo = null)
        //{
        //    var apiPath = "/orders/get";
        //    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();


        //    bool isManual = !string.IsNullOrEmpty(DateFrom) && !string.IsNullOrEmpty(DateTo);
        //    //TUNAY

        //    var paramDict = new SortedDictionary<string, string>
        //    {
        //        { "access_token",  token.AccessToken },
        //        { "app_key",       token.AppKey },
        //        { "sign_method",   "sha256" },
        //        { "timestamp",     timestamp },
        //        { "limit",         limit.ToString() },
        //        { "offset",        offset.ToString() },
        //    };

        //    if (isManual)
        //    {
        //        if (!DateTime.TryParse(DateFrom, out var parsedFrom) ||
        //            !DateTime.TryParse(DateTo, out var parsedTo))
        //        {
        //            throw new ArgumentException("Invalid date format for DateFrom or DateTo.");
        //        }

        //        var createdAfter = new DateTimeOffset(DateTime.SpecifyKind(parsedFrom.Date, DateTimeKind.Utc))
        //                                .ToString("yyyy-MM-ddTHH:mm:sszzz");
        //        var createdBefore = new DateTimeOffset(DateTime.SpecifyKind(parsedTo.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc))
        //                                .ToString("yyyy-MM-ddTHH:mm:sszzz");

        //        paramDict.Add("created_after", createdAfter);
        //        paramDict.Add("created_before", createdBefore);
        //    }
        //    else
        //    {
        //        var created_after = DateTimeOffset.UtcNow
        //            .ToOffset(TimeSpan.FromHours(8))
        //            .AddHours(-5)
        //            .ToString("yyyy-MM-ddTHH:mm:sszzz");

        //        paramDict.Add("update_after", created_after);
        //        paramDict.Add("status", "ready_to_ship");// //shipped //packed
        //    }


        //    var stringToSign = apiPath;
        //    foreach (var kvp in paramDict)
        //        stringToSign += kvp.Key + kvp.Value;

        //    var sign = GenerateSignature(stringToSign, token.AppSecretKey);
        //    var queryString = string.Join("&",
        //        paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        //    var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

        //    var response = await _httpClient.GetStringAsync(fullUrl);
        //    var result = JsonConvert.DeserializeObject<LazadaOrderResponse>(response);

        //    if (result?.data == null)
        //        throw new InvalidOperationException($"Unexpected Lazada response: {response}");

        //    return result.data;
        //}
        #endregion

        public static async Task<List<LazadaOrderModel>> GetLazadaOrdersAsync(APISetup apisetup, string DateFrom = null, string DateTo = null)
        {
            var tokenService = new LazadaTokenService();
            var token = await tokenService._GenerateRefreshTokenAsync(apisetup);
            var allOrders = new List<LazadaOrderModel>();
            const int limit = 20;
            int offset = 0;
            int totalCount = int.MaxValue;

            using (var semaphore = new SemaphoreSlim(5))
            {
                while (offset < totalCount)
                {
                    var page = await FetchLazadaOrderPageAsync(token, limit, offset, DateFrom, DateTo);

                    if (offset == 0)
                        totalCount = page.countTotal;

                    if (page.orders == null || page.orders.Count == 0)
                        break;

                    var itemTasks = page.orders.Select(async order =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            order.items = await GetOrderItemsAsync(order.order_id.ToString(), token);
                            return SplitByPackage(order);
                        }
                        catch (Exception ex)
                        {
                            // Surface the failure instead of silently dropping the order.
                            // Replace with your actual logger.
                            Console.Error.WriteLine($"Failed to process order {order.order_id}: {ex.Message}");
                            return Enumerable.Empty<LazadaOrderModel>();
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    var fetchedGroups = await Task.WhenAll(itemTasks);
                    allOrders.AddRange(fetchedGroups.SelectMany(g => g));

                    offset += limit;
                }
            }

            return allOrders;
        }

        private static IEnumerable<LazadaOrderModel> SplitByPackage(LazadaOrderModel order)
        {
            if (order.items == null || order.items.Count == 0)
            {
                order.package_id = "";
                return new[] { order };
            }

            return order.items
                .GroupBy(i => i.package_id ?? "")
                .Select(group =>
                {
                    var clone = order.Clone();
                    clone.package_id = group.Key;
                    clone.items = group.ToList();
                    return clone;
                });
        }
      

        private static async Task<List<LazadaOrderItemModel>> GetOrderItemsAsync(string orderId, LazadaTokenModel token)
        {
            // NOTE: no try/catch here anymore — let failures bubble up to the caller
            // in GetLazadaOrdersAsync, which logs and skips just that order instead
            // of silently returning an empty item list that looks like a valid "no items" order.
            var apiPath = "/order/items/get";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var paramDict = new SortedDictionary<string, string>
            {
                { "access_token", token.AccessToken },
                { "app_key",      token.AppKey },
                { "order_id",     orderId },
                { "sign_method",  "sha256" },
                { "timestamp",    timestamp }
            };

            var stringToSign = apiPath;
            foreach (var kvp in paramDict)
                stringToSign += kvp.Key + kvp.Value;

            var sign = GenerateSignature(stringToSign, token.AppSecretKey);

            var queryString = string.Join("&",
                paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

            var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

            // Reuse the static _httpClient instead of creating a new one per call —
            // avoids socket exhaustion under concurrent SemaphoreSlim(5) calls.
            var response = await _httpClient.GetStringAsync(fullUrl);
            var result = JsonConvert.DeserializeObject<LazadaItemResponse>(response);

            return result?.data ?? new List<LazadaOrderItemModel>();
        }

        private static async Task<LazadaOrderData> FetchLazadaOrderPageAsync(
            LazadaTokenModel token, int limit, int offset, string DateFrom = null, string DateTo = null)
            {
                var apiPath = "/orders/get";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                bool isManual = !string.IsNullOrEmpty(DateFrom) && !string.IsNullOrEmpty(DateTo);

                var paramDict = new SortedDictionary<string, string>
                {
                    { "access_token",  token.AccessToken },
                    { "app_key",       token.AppKey },
                    { "sign_method",   "sha256" },
                    { "timestamp",     timestamp },
                    { "limit",         limit.ToString() },
                    { "offset",        offset.ToString() },
                };

            if (isManual)
            {
                if (!DateTime.TryParse(DateFrom, out var parsedFrom) ||
                    !DateTime.TryParse(DateTo, out var parsedTo))
                {
                    throw new ArgumentException("Invalid date format for DateFrom or DateTo.");
                }

                //var createdAfter = new DateTimeOffset(DateTime.SpecifyKind(parsedFrom.Date, DateTimeKind.Utc))
                //                        .ToString("yyyy-MM-ddTHH:mm:sszzz");
                //var createdBefore = new DateTimeOffset(DateTime.SpecifyKind(parsedTo.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc))
                //                        .ToString("yyyy-MM-ddTHH:mm:sszzz");

                var phOffset = TimeSpan.FromHours(8);

                var createdAfter = new DateTimeOffset(parsedFrom.Date, phOffset)
                                        .ToString("yyyy-MM-ddTHH:mm:sszzz");

                var createdBefore = new DateTimeOffset(parsedTo.Date.AddDays(1).AddSeconds(-1), phOffset)
                                        .ToString("yyyy-MM-ddTHH:mm:sszzz");

                paramDict.Add("created_after", createdAfter);
                paramDict.Add("created_before", createdBefore);

                // CONFIRM: manual/backfill runs previously had no status filter at all,
                // meaning they'd pull orders in every status. If you only ever want
                // ready_to_ship orders regardless of entry point, uncomment below.
                // paramDict.Add("status", "ready_to_ship");
            }
            else
            {
                var created_after = DateTimeOffset.UtcNow
                    .ToOffset(TimeSpan.FromHours(8))
                    .AddHours(-5)
                    .ToString("yyyy-MM-ddTHH:mm:sszzz");

                paramDict.Add("update_after", created_after);
                paramDict.Add("status", "pending");/// "ready_to_ship"// //shipped //packed // pending
            }

            var stringToSign = apiPath;
            foreach (var kvp in paramDict)
                stringToSign += kvp.Key + kvp.Value;

            var sign = GenerateSignature(stringToSign, token.AppSecretKey);
            var queryString = string.Join("&",
                paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

            var response = await _httpClient.GetStringAsync(fullUrl);
            var result = JsonConvert.DeserializeObject<LazadaOrderResponse>(response);

            if (result?.data == null)
                throw new InvalidOperationException($"Unexpected Lazada response: {response}");

            return result.data;
        }
        private static string GenerateSignature(string stringToSign, string appSecret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(appSecret);
            var msgBytes = Encoding.UTF8.GetBytes(stringToSign);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }

        // -------------------------------------------------------
        // Fix JSON — remove trailing commas
        // -------------------------------------------------------
        private static string FixJson(string json)
        {
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }
    }
}