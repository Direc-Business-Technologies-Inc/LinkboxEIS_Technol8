using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer;
using DomainLayer.Models.Tiktok;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using Microsoft.Owin.Security.Provider;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Helpers;
using static DomainLayer.ViewModels.MapCreateViewModel;

namespace LinkBoxUI.Services.Tiktok
{
    public class TiktokSalesOrderService
    {

        //SANDBOX
        //private const string AppKey = "6j705mi7urqp9";
        //private const string AppSecret = "491ea51ce5bfdd5a7d01a3a930db6dc2c1966c7e";
        //private const string BaseUrl = "https://open-api.tiktokglobalshop.com";
        private const string RequestPath = "/order/202309/orders/search";
        private static int Interval =>
    int.Parse(ConfigurationManager.AppSettings["TiktokInterval"]);
        private readonly APISetup _config;
        LinkboxDb context = new LinkboxDb();
        public TiktokSalesOrderService(APISetup config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }


        //    private static string AppKey =>
        //        ConfigurationManager.AppSettings["TikTokAppKey"];

        //    private static string AppSecret =>
        //   ConfigurationManager.AppSettings["TikTokAppSecret"];

        //    private static string ShopCipher =>
        //  ConfigurationManager.AppSettings["TikTokShopCipher"];

        private static string BaseUrl =>
        ConfigurationManager.AppSettings["TiktokBaseUrl"];


        //LIVE
        //private const string AppKey = "6j705mi7urqp9";
        //private const string AppSecret = "491ea51ce5bfdd5a7d01a3a930db6dc2c1966c7e";
        //private const string ShopCipher = "ROW__BmOPQAAAACMrBYuDcN3-zzTPR8WqTGX"; //Live- "ROW_Z7lgdAAAAAAQNNI4xxel0MueZjo2Otvd";
        //private const string BaseUrl = "https://open-api.tiktokglobalshop.com";
        //private const string RequestPath = "/order/202309/orders/search";

        private static readonly HttpClient client = new HttpClient();
        NetsuiteHelperMethod nsHelperMethods = new NetsuiteHelperMethod();



        //OLD GET ORDERS
        //public async Task<List<TikTokOrder>> GetOrders()
        //{
        //    try
        //    {
        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //        //string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        //        var timestamp = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds().ToString();
        //        var parameters = new Dictionary<string, string>
        //        {
        //            { "app_key", AppKey },
        //            { "timestamp", timestamp },
        //            { "shop_cipher", ShopCipher },
        //            { "page_size", "100" },
        //            { "order_status", "AWAITING_SHIPMENT" },
        //            //{ "order_status", "IN_TRANSIT" },

        //            //{ "page_token", "aDV5MXFHQ1FaRXhMVG1STTlpU0lia3QxOHZnUnE5TSttT08yT1h3NkJ5RVhHQT09" }
        //        };

        //        string sign = GenerateSign(parameters);
        //        parameters.Add("sign", sign);

        //        string query = BuildQuery(parameters);

        //        string url = BaseUrl + RequestPath + "?" + query;

        //        var request = new HttpRequestMessage(HttpMethod.Post, url);

        //        // Body required for POST (even if empty)
        //        request.Content = new StringContent("", Encoding.UTF8, "application/json");

        //        //Make a To get Refresh the AccessToken
        //        string AccessToken = await GetRefreshToken.RefreshAccessToken();

        //        request.Headers.Add("x-tts-access-token", AccessToken);

        //        // Content-Type already set by StringContent
        //        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        //        var response = await client.SendAsync(request);

        //        response.EnsureSuccessStatusCode();

        //        var json = await response.Content.ReadAsStringAsync();
        //        //var _response = JsonConvert.DeserializeObject<TiktokOrderModel>(json);
        //        //List<TikTokOrder> orders = _response.Data.Orders;

        //        var _response = JsonConvert.DeserializeObject<TiktokOrderModel>(json);
        //        return _response.data.orders ?? new List<TikTokOrder>();
        //    }
        //    catch(Exception ex)
        //    {
        //        throw new Exception($"Error - {ex.Message}");
        //    }


        //}

        public async Task<List<TikTokOrder>> GetOrders(string DateFrom = null, string DateTo = null)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string accessToken = "";//await GetRefreshToken.RefreshAccessToken(_config);

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

                    createTimeGe = new DateTimeOffset(DateTime.SpecifyKind(parsedFrom.Date, DateTimeKind.Utc)).ToUnixTimeSeconds();
                    createTimeLt = new DateTimeOffset(DateTime.SpecifyKind(parsedTo.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)).ToUnixTimeSeconds();
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
                { "app_key",     _config.APIKey },
                { "timestamp",   timestamp },
                { "shop_cipher", _config.ShopCode },
                { "page_size",   "20" }
                //{"ids",OrderIds }
            };

                    if (!isManual)
                        parameters.Add("order_status", "AWAITING_SHIPMENT");

                    if (!string.IsNullOrEmpty(pageToken))
                        parameters.Add("page_token", pageToken);

                    string bodyJson = isManual
                        ? $"{{\"create_time_ge\":{createTimeGe},\"create_time_lt\":{createTimeLt},\"page_size\":20}}"
                        : "";

                    // string bodyJson = "";//for test

                    string sign = GenerateSign(parameters, bodyJson,"/order/202309/orders/search");
                    parameters.Add("sign", sign);

                    string query = BuildQuery(parameters);
                    string url = _config.APILoginUrl + RequestPath + "?" + query;

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
        public async Task<string> GetColumn(string OrdNumber)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string timestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Interval).ToString();

            // Query-string params only (common/auth params + pagination)
            var parameters = new Dictionary<string, string>
            {
                { "app_key", _config.APIKey },
                { "timestamp", timestamp },
                { "shop_cipher", _config.ShopCode },
                { "page_size", "1" },
            };

            // Body params (filters) — these go in the JSON payload, not the query string
            var bodyObj = new
            {
                order_status = "COMPLETED"
            };
            string bodyJson = JsonConvert.SerializeObject(bodyObj);

            // Sign using both query params AND the body content
            string sign = GenerateSign(parameters, bodyJson, "/order/202309/orders/search");
            parameters.Add("sign", sign);

            string query = BuildQuery(parameters);
            string url = BaseUrl + "/order/202309/orders/search" + "?" + query;

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
            };

            string AccessToken = await GetRefreshToken.RefreshAccessToken(_config);
            request.Headers.Add("x-tts-access-token", AccessToken);

            var response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"TikTok API HTTP error {(int)response.StatusCode}: {responseBody}");
            }
            return responseBody;
        }


        private string GenerateSign(Dictionary<string, string> parameters, string bodyJson = "", string reqpath=null)
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
            signString = _config.APISecretKey + signString + _config.APISecretKey;

            // Step 6: HMAC-SHA256
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_config.APISecretKey)))
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

        private readonly LinkboxDb _context = new LinkboxDb();

        NetsuiteHelper NShelper = new NetsuiteHelper();
        public async Task<string> TiktokOrderPostToNetsuite(int[] MapIds, string Code, string DateFrom = null, string DateTo = null)
        {
            var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();
            var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();

            var Schedule = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();

            foreach (var MapId in MapIds)
            {
                var fieldsets = _context.Headers.Where(x => x.MapId == MapId && x.SourceTableName == "Header").ToList();
                var fieldrows = _context.Headers.Where(x => x.MapId == MapId && x.SourceTableName == "Row").ToList();
                var baseQuery =
               _context.FieldMappings
                   .Where(fm => fm.MapId == MapId)
                   .Join(
                       _context.APISetups,
                       fm => fm.APICode,
                       api => api.APICode,
                       (fm, api) => new { FieldMapping = fm, API = api }
                   );

                var APIid = baseQuery.First().API.APIId;

                var result = await GetOrders(DateFrom,DateTo);

                foreach (var order in result)
                {

                    //string OrderId = order.id.ToString();
                    //string packageId = order.line_items.FirstOrDefault()?.package_id.ToString();
                    //double.TryParse(order.payment?.platform_discount ?? "0", out double platform_discount);
                    //double.TryParse(order.payment?.handling_fee ?? "0", out double handling_fee);

                    //double seller_discount = double.Parse(order.payment.seller_discount) + platform_discount + handling_fee;
                    //double shipping_discount = double.Parse(order.payment.shipping_fee);



                    string OrderId = order.id.ToString();
                    string packageId = order.line_items.FirstOrDefault()?.package_id?.ToString() ?? "";
                    if (string.IsNullOrEmpty(packageId))
                        continue;

                    double.TryParse(order.payment?.platform_discount ?? "0", out double platform_discount);
                    double.TryParse(order.payment?.handling_fee ?? "0", out double handling_fee);
                    double.TryParse(order.payment?.seller_discount ?? "0", out double sellerDiscountValue);
                    double.TryParse(order.payment?.shipping_fee ?? "0", out double shipping_discount);

                    shipping_discount += handling_fee;
                    double seller_discount = sellerDiscountValue + platform_discount;

                        bool exist = _context.PlatformTable.Any(x => x.ReferenceId == OrderId && x.Platform == "Tiktok" && x.SyncStatus == "Sync");
                        if (exist) continue;

                    Schedule.RunTime = DateTime.Now;
                    _context.SaveChanges();

                    StringBuilder json = new StringBuilder();
                    json.AppendLine("   { ");

                    foreach (var header in fieldsets)
                    {

                        var DestinationField = header.DestinationField;
                        var SourceField = header.SourceFieldId;


                        object fieldvalue = null;
                        switch (SourceField)
                        {
                            case "Query":

                                string input = header.ConditionalQuery;
                                var output = Regex.Matches(input, $"#(.+?)#")
                                                                    .Cast<System.Text.RegularExpressions.Match>()
                                                                    .Select(m => m.Groups[1].Value);
                                foreach (var columnName in output)
                                {

                                    //eRROR IF THE U_HBSCode is Not Exist in the SAP.. There is no Row at position 0 
                                    //input = QueryAccess.OriginalReplaceQueryParameter(input
                                    //    , $"#{columnName}#"
                                    //    , items., null);


                                }

                                fieldvalue = await NShelper.GetNetSuiteData(QueryEndpoint, input);
                                if (fieldvalue != null)
                                {
                                    // Attempt to parse fieldValue as a number
                                    if (int.TryParse(fieldvalue.ToString(), out int intValue))
                                    {
                                        // If it's an integer, use intValue
                                        fieldvalue = intValue;
                                    }
                                    else if (double.TryParse(fieldvalue.ToString(), out double doubleValue))
                                    {
                                        // If it's a decimal/float number, use doubleValue
                                        fieldvalue = doubleValue;
                                    }

                                    else
                                    {
                                        // Keep fieldValue as a string if it's not a number
                                        fieldvalue = fieldvalue.ToString();
                                    }
                                }

                                break;
                            case "Static Value":
                                fieldvalue = header.ConditionalQuery;
                                if (DestinationField.Equals("entity") || DestinationField.Equals("class") || DestinationField.Equals("location"))
                                {
                                    json.AppendLine($@"""{DestinationField}"": {{");
                                    json.AppendLine($@"  ""id"": ""{fieldvalue}""");
                                    json.AppendLine("},");
                                    continue;
                                }
                                break;
                            case "recipient_address.name":
                                fieldvalue = order.recipient_address.name.ToString();

                                break;
                            case "create_time" when header.DestinationField.Equals("tranDate"):

                                //var x_prop = order.GetType().GetProperty(SourceField);
                                //var x_fv = x_prop?.GetValue(order);

                                //long timestamp = Convert.ToInt64(x_fv);
                                //DateTime localTime = DateTimeOffset
                                //            .FromUnixTimeSeconds(timestamp)
                                //            .ToOffset(TimeSpan.FromHours(8))
                                //            .DateTime;

                                fieldvalue = DateTime.Now.ToString("yyyy-MM-dd"); // Format the DateTime value to "yyyy-MM-dd"

                                break;

                            case "id" when header.DestinationField.Equals("tranId"):
                                var _prop = order.GetType().GetProperty(
                                      SourceField,
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                                  );

                                var _fv = _prop?.GetValue(order);
                                fieldvalue = $"SO{DateTime.Now.ToString("MMyy")}-#{_fv.ToString()}"; // Format the DateTime value to "yyyy-MM-dd"

                                break;

                            default:
                                //var prop = order[SourceField];
                                var prop = order.GetType().GetProperty(SourceField);
                                var fv = prop?.GetValue(order);

                                if (fv != null)
                                {
                                    if (SourceField.Equals("create_time"))
                                    {
                                        long timestamp = Convert.ToInt64(fv);

                                        DateTime localTime = DateTimeOffset
                                            .FromUnixTimeSeconds(timestamp)
                                            .ToOffset(TimeSpan.FromHours(8))
                                            .DateTime;

                                        fieldvalue = localTime.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                    else
                                    {
                                        fieldvalue = fv.ToString();
                                    }
                                }


                                break;

                        }
                        json.AppendLine($@"""{DestinationField}"":""{fieldvalue}"",");



                    }
                    //end of header
                    json.AppendLine(@"""item"": {");
                    json.AppendLine(@"  ""items"": [");
                    int i = 0;
                    double TotalSellerDisCount = 0;

                    var groupeditems = order.line_items
                    .GroupBy(it => it.seller_sku)
                    .Select(g => new
                    {
                        sku = g.Key,
                        quantity = g.Count(),
                        unitprice = g.First().original_price,
                        is_gift = g.First().is_gift,
                        Items = g.ToList()

                    })
                    .ToList();

                    //string Order_number = order.Number.ToString();
                    foreach (var rowdata in groupeditems) //order.Items
                    {
                        json.AppendLine("{");
                        var sku = rowdata.sku;
                        var Quantity = rowdata.quantity;
                        double UnitPrice = double.Parse(rowdata.unitprice.ToString());

                        if(rowdata.is_gift == "true")
                        {
                            UnitPrice = 0.1;
                            TotalSellerDisCount += UnitPrice;
                        }
                        //double shipping_fee = rowdata.;
                        foreach (var rows in fieldrows)
                        {

                            var destinationfield = rows.DestinationField;
                            var sourcefield = rows.SourceFieldId;

                            //var fieldvalue = "";
                            object rowvalue = null;
                            switch (sourcefield)
                            {
                                case "Query":

                                    try
                                    {
                                        string input = rows.ConditionalQuery;
                                        var output = Regex.Matches(input, $"#(.+?)#")
                                                                            .Cast<System.Text.RegularExpressions.Match>()
                                                                            .Select(m => m.Groups[1].Value);

                                        string netsuite_price = string.Empty;

                                        //var lineprice = rowdata.gettype().getproperty("unitprice");
                                        //var shopify_price = lineprice?.getvalue(rowdata);


                                        foreach (var columnname in output)
                                        {


                                            input = QueryAccess.OriginalReplaceQueryParameter(input
                                                , $"#{columnname}#"
                                                , sku.ToString(), null);

                                        }

                                        rowvalue = await NShelper.GetListNetSuiteData(QueryEndpoint, input);
                                        if (rowvalue != null)
                                        {
                                            //var rowvaluetype = rowvalue.gettype();
                                            var listvalue = rowvalue as List<string>;
                                            if (listvalue.Count > 1)
                                            {
                                                // get the first element (index 0)
                                                var firstelement = listvalue[0];

                                                // get the second element (index 1)
                                                netsuite_price = listvalue[1];

                                                if (int.TryParse(firstelement.ToString(), out int intvalue))
                                                {
                                                    // if it's an integer, use intvalue
                                                    rowvalue = intvalue;
                                                }
                                                else if (double.TryParse(firstelement.ToString(), out double doublevalue))
                                                {
                                                    // if it's a decimal/float number, use doublevalue
                                                    rowvalue = doublevalue;
                                                }
                                                else
                                                {
                                                    // keep fieldvalue as a string if it's not a number
                                                    rowvalue = firstelement.ToString();
                                                }

                                            }

                                        }

                                        if (destinationfield.Equals("item"))
                                        {
                                            json.AppendLine(@"""item"": {");
                                            json.AppendLine($@"  ""id"": ""{rowvalue}""");
                                            json.AppendLine("   }, ");



                                            int quantity = 0;

                                            int.TryParse(Quantity.ToString(), out quantity);


                                            json.AppendLine($@"  ""quantity"": {quantity},");
                                            //json.appendline($@"  ""amount"": {unitprice}");
                                            json.AppendLine($@"  ""rate"": {UnitPrice}");
                                            //json.AppendLine("   }, ");


                                            //convert netsuite_price to double
                                            //double netsuiteprice = 0;
                                            //if (!string.IsNullOrEmpty(netsuite_price))
                                            //{
                                            //    double.TryParse(netsuite_price, out netsuiteprice);
                                            //}

                                            //double _result = Math.Abs(UnitPrice - netsuiteprice);

                                            //if (_result > 0)
                                            //{
                                            //    json.AppendLine($@"  ,""custcol_sellerdis"": -{_result}");
                                            //    TotalSellerDisCount += _result;
                                            //}

                                            continue;
                                        }

                                        break;
                                    }
                                    catch
                                    {
                                        break;
                                    }

                                case "static value":
                                    rowvalue = rows.ConditionalQuery;

                                    break;

                                default:
                                    var propertyvalue = rowdata.GetType().GetProperty(sourcefield);
                                    if (propertyvalue != null)
                                    {
                                        rowvalue = propertyvalue.GetValue(rowdata).ToString(); // retrieve the value of the property
                                    }

                                    break;
                            }

                        }

                        if (i < order.line_items.Count - 1)
                        {
                            json.AppendLine("   },");
                        }
                        else
                        {
                            if (shipping_discount > 0 || TotalSellerDisCount > 0 || seller_discount > 0)
                            {
                                json.AppendLine("   },");
                            }
                            else
                            {
                                json.AppendLine("   }");
                            }

                        }

                        //json.AppendLine("   }" + (i < order.Items.Count - 1 ? "," : ""));
                        i++;
                        if (i > groupeditems.Count - 1)
                        {
                            if (shipping_discount > 0)
                            {
                                json.AppendLine("   { ");
                                json.AppendLine(@"""item"": {");
                                json.AppendLine($@"  ""id"": ""227"" ");
                                json.AppendLine("   }, ");
                                json.AppendLine($@"  ""custcol_voucherdisc"": {shipping_discount},");
                                json.AppendLine($@"  ""custcol_posnetprice"": {shipping_discount},");
                                json.AppendLine($@"  ""amount"": {shipping_discount}");
                                //json.AppendLine("   }" + (TotalSellerDisCount > 0 ? "," : ""));
                                json.AppendLine("}" + (TotalSellerDisCount > 0 || seller_discount > 0 ? "," : ""));

                            }
                            if (TotalSellerDisCount > 0)
                            {
                                json.AppendLine("   { ");
                                json.AppendLine(@"""item"": {");
                                json.AppendLine($@"  ""id"": ""232"" ");
                                json.AppendLine("   }, ");
                                json.AppendLine($@"  ""custcol_voucherdisc"": -{TotalSellerDisCount},");
                                json.AppendLine($@"  ""custcol_posnetprice"": -{TotalSellerDisCount},");
                                json.AppendLine($@"  ""amount"": -{TotalSellerDisCount},");
                                json.AppendLine(@"""price"": {");
                                json.AppendLine($@"  ""id"": ""-1"" ");
                                json.AppendLine("   } ");

                                json.AppendLine("   }" + (seller_discount > 0 ? "," : ""));

                            }
                            if (seller_discount > 0)
                            {
                                json.AppendLine("   { ");
                                json.AppendLine(@"""item"": {");
                                json.AppendLine($@"  ""id"": ""231"" ");
                                json.AppendLine("   }, ");
                                json.AppendLine($@"  ""custcol_voucherdisc"": -{seller_discount},");
                                json.AppendLine($@"  ""custcol_posnetprice"": -{seller_discount},");
                                json.AppendLine($@"  ""amount"": -{seller_discount},");
                                json.AppendLine(@"""price"": {");
                                json.AppendLine($@"  ""id"": ""-1"" ");
                                json.AppendLine("   } ");
                                json.AppendLine("}");
                            }
                        }

                    }



                    json.AppendLine("   ]} ");
                    json.AppendLine("   } ");

                    json.Length--;

                    string fixedJson = FixJson(json.ToString());
                    string jsonStringFromBuilder = fixedJson.ToString();

                    //string Posting_result = await NShelper.PostDataToNetSuite(PostDataEndpoint, jsonStringFromBuilder, "salesorder");

                    //var err_exist = _context.PlatformTable
                    //.FirstOrDefault(x => x.ReferenceId == OrderId);

                    //if (Posting_result.ToLower().Contains("successfully posted") && err_exist==null)
                    //{
                    //    _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                    //    {
                    //        ReferenceId = OrderId,
                    //        Platform = "Tiktok",
                    //        BuildJson = jsonStringFromBuilder,
                    //        PlatformJson = Posting_result,
                    //        SyncStatus = "Sync",
                    //        CreatedDate = DateTime.Now,
                    //        UploadDate = DateTime.Now,
                    //        Message = "Successfully Posted",
                    //        OrderNumber = packageId
                    //    });
                    //    _context.SaveChanges();

                    //}
                    //else
                    //{


                    //    if (err_exist != null && Posting_result.ToLower().Contains("successfully posted"))
                    //    {
                    //            err_exist.BuildJson = jsonStringFromBuilder;
                    //            err_exist.PlatformJson = Posting_result;
                    //            err_exist.Message = "Successfully Posted";
                    //            err_exist.CreatedDate = DateTime.Now; // Optionally, you can update CreatedDate or leave it unchanged
                    //            err_exist.SyncStatus = "Sync";
                    //            // Optionally, update other fields as needed, such as OrderNumber
                    //            err_exist.ReferenceId = order.id;
                    //            err_exist.UploadDate = DateTime.Now;
                    //            // Save the updated record
                    //            _context.SaveChanges();

                    //    }



                    //    if (err_exist != null && !Posting_result.Contains("A record with this ID already exists"))
                    //    {
                    //        // If the existing record is found and BuildJson or Posting_result differs, update the record
                    //        if (err_exist.BuildJson != jsonStringFromBuilder || err_exist.Message != Posting_result)
                    //        {
                    //            err_exist.BuildJson = jsonStringFromBuilder;
                    //            err_exist.PlatformJson = Posting_result;
                    //            err_exist.Message = Posting_result.ToString();
                    //            err_exist.CreatedDate = DateTime.Now; // Optionally, you can update CreatedDate or leave it unchanged

                    //            // Optionally, update other fields as needed, such as OrderNumber
                    //          //  err_exist.OrderNumber = order.id;

                    //            // Save the updated record
                    //            _context.SaveChanges();
                    //        }
                    //    }
                    //    else if (err_exist == null && Posting_result.Contains("A record with this ID already exists"))
                    //    {
                    //        _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                    //        {
                    //            ReferenceId = OrderId,
                    //            Platform = "Tiktok",
                    //            BuildJson = jsonStringFromBuilder,
                    //            PlatformJson = Posting_result,
                    //            CreatedDate = DateTime.Now,
                    //            SyncStatus = "Sync",
                    //            UploadDate = DateTime.Now,
                    //            Message = "Successfully Posted - {Order Already Exist}",
                    //            OrderNumber = packageId,

                    //        });
                    //        _context.SaveChanges();
                    //    }
                    //    else if (err_exist == null && !Posting_result.Contains("A record with this ID already exists"))
                    //    {
                    //        _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                    //        {
                    //            ReferenceId = order.id,
                    //            Platform = "Tiktok",
                    //            BuildJson = jsonStringFromBuilder,
                    //            PlatformJson = Posting_result,
                    //            CreatedDate = DateTime.Now,
                    //            Message = Posting_result.ToString(),
                    //            OrderNumber = packageId,

                    //        });
                    //        _context.SaveChanges();
                    //    }
                    //    else if (err_exist != null && Posting_result.Contains("A record with this ID already exists"))
                    //    {
                    //        err_exist.BuildJson = jsonStringFromBuilder;
                    //        err_exist.PlatformJson = Posting_result;
                    //        err_exist.Message = "Successfully Posted";
                    //        err_exist.UploadDate = DateTime.Now; // Optionally, you can update CreatedDate or leave it unchanged
                    //        err_exist.SyncStatus = "Sync";

                    //        // Save the updated record
                    //        _context.SaveChanges();

                    //    }
                    //    else
                    //    {
                    //        err_exist.UploadDate = DateTime.Now;
                    //        _context.SaveChanges();
                    //    }
                    //}

                    ///NEW IMPLEM
                    string postingResult = await NShelper.PostDataToNetSuite(PostDataEndpoint, jsonStringFromBuilder, "salesorder");

                    bool isSuccess = postingResult != null && postingResult.IndexOf("successfully posted", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool alreadyExists = postingResult != null && postingResult.IndexOf("A record with this ID already exists", StringComparison.OrdinalIgnoreCase) >= 0;


                    // --- Upsert PlatformTable record ---
                    var existing = _context.PlatformTable.FirstOrDefault(x => x.ReferenceId == OrderId);

                    if (isSuccess || alreadyExists)
                    {
                        if (existing != null)
                        {
                            existing.ReferenceId = OrderId;
                            existing.BuildJson = jsonStringFromBuilder;
                            existing.PlatformJson = postingResult;
                            existing.Message = "Successfully Posted";
                            existing.SyncStatus = "Sync";
                            existing.UploadDate = DateTime.Now;
                            existing.OrderNumber = packageId;
                        }
                        else
                        {
                            _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                            {
                                ReferenceId = OrderId,
                                Platform = "Tiktok",
                                BuildJson = jsonStringFromBuilder,
                                PlatformJson = postingResult,
                                SyncStatus = "Sync",
                                CreatedDate = DateTime.Now,
                                UploadDate = DateTime.Now,
                                Message = "Successfully Posted",
                                OrderNumber = packageId
                            });
                        }
                    }
                    else
                    {
                        // --- Log the result ---
                        nsHelperMethods.Save_SystemLogs(
                             orderId: OrderId,
                             apiUrl: PostDataEndpoint,
                             json: jsonStringFromBuilder,
                             errorMsg: isSuccess ? string.Empty : postingResult,
                             module: "Tiktok"
                         );

                        if (existing != null)
                        {
                            existing.ReferenceId = OrderId;
                            existing.BuildJson = jsonStringFromBuilder;
                            existing.PlatformJson = postingResult;
                            existing.Message = postingResult;
                            existing.OrderNumber = packageId;
                        }
                        else
                        {
                            _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                            {
                                ReferenceId = OrderId,
                                Platform = "Tiktok",
                                BuildJson = jsonStringFromBuilder,
                                PlatformJson = postingResult,
                                CreatedDate = DateTime.Now,
                                Message = postingResult,
                                OrderNumber = packageId
                            });
                        }
                    }

                    _context.SaveChanges(); // Single save for PlatformTable
                }
                //return "";
            }
            return "";
        }
        static string FixJson(string json)
        {
            // Removes trailing commas before a closing bracket or brace
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }
    }
}