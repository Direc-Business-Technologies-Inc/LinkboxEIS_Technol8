using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Drawing.Charts;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Services.Shopee;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Remotion.Data.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinkBoxUI.Services.Lazada
{
    public class LazadaToNetsuite
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        NetsuiteHelperMethod nsHelperMethods = new NetsuiteHelperMethod();

        private readonly string _baseUrl = "https://api.lazada.com.ph/rest";

        // -------------------------------------------------------
        // POST Lazada Orders to NetSuite as Sales Orders
        // -------------------------------------------------------
        public async Task Post(int[] MapIds, string Code, string DateFrom = null, string DateTo = null)
        {
            try
             {
                var QueryEndpoint = _context.APISetups
                    .Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint"))
                    .Select(x => x.APIURL)
                    .FirstOrDefault();

                var PostDataEndpoint = _context.APISetups
                    .Where(x => x.APICode.Contains("NetsuitePostEndpoint"))
                    .Select(x => x.APIURL)
                    .FirstOrDefault();

                var Schedule = _context.Schedules
                    .Where(x => x.SchedCode == Code)
                    .FirstOrDefault();

                foreach (var MapId in MapIds)
                {
                    var fieldsets = _context.Headers
                        .Where(x => x.MapId == MapId && x.SourceTableName == "Header")
                        .ToList();

                    var fieldrows = _context.Headers
                        .Where(x => x.MapId == MapId && x.SourceTableName == "Row")
                        .ToList();
                    //string[] orderIds =
                    //    {
                    //        "1101910757203499",
                    //        "1101907567908316",
                    //        "1101899332756217",
                    //        "1093226882079236",
                    //        "1101845379897247",
                    //        "1101699505456666",
                    //        "1076159879508460",
                    //        "1084745371291546",
                    //        "1084727962332572"
                    //    };
                    //foreach (string orderId in orderIds)
                    //{
                        // Use orderId here
            
                    // ← Get Lazada orders directly from Lazada API
                    var orders = await GetLazadaOrdersAsync(DateFrom,DateTo);

                    foreach (var order in orders)
                    {
                        string order_Status = order.statuses[0].ToString();
                        //if (order_Status.ToLower().Contains("pending"))
                        //{
                        //    continue;
                        //}
                        string OrderId = order.order_id.ToString();
                        string OrderNumber = order.package_id;

                        //Update schedule runtime
                        if (Schedule != null)
                        {
                            Schedule.RunTime = DateTime.Now;
                            _context.SaveChanges();
                        }

                        // Skip if already synced
                        bool exist = _context.PlatformTable.Any(x =>
                            x.ReferenceId == OrderId &&
                            x.Platform == "Lazada" &&
                            x.SyncStatus == "Sync");

                        if (exist) continue;

                        StringBuilder json = new StringBuilder();

                        try
                        {
                            json.AppendLine("{");

                            // -------------------------------------------------------
                            // Header fields
                            // -------------------------------------------------------
                            foreach (var header in fieldsets)
                            {
                                var DestinationField = header.DestinationField;
                                var SourceField = header.SourceFieldId;
                                object fieldvalue = null;

                                switch (SourceField)
                                {
                                    case "Query":
                                        string input = header.ConditionalQuery;
                                        var output = System.Text.RegularExpressions.Regex.Matches(input, "#(.+?)#")
                                            .Cast<System.Text.RegularExpressions.Match>()
                                            .Select(m => m.Groups[1].Value);

                                        foreach (var columnName in output)
                                        {
                                            var prop = order.GetType().GetProperty(columnName,
                                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                            var val = prop?.GetValue(order)?.ToString() ?? "";
                                            input = input.Replace($"#{columnName}#", val);
                                        }

                                        fieldvalue = await NShelper.GetNetSuiteData(QueryEndpoint, input);

                                        if (fieldvalue != null)
                                        {
                                            if (int.TryParse(fieldvalue.ToString(), out int intVal))
                                                fieldvalue = intVal;
                                            else if (double.TryParse(fieldvalue.ToString(), out double dblVal))
                                                fieldvalue = dblVal;
                                            else
                                                fieldvalue = fieldvalue.ToString();
                                        }
                                        break;

                                    case "Static Value":
                                        fieldvalue = header.ConditionalQuery;
                                        if (DestinationField.Equals("entity") ||
                                            DestinationField.Equals("class") ||
                                            DestinationField.Equals("location"))
                                        {
                                            json.AppendLine($@"""{DestinationField}"": {{");
                                            json.AppendLine($@"  ""id"": ""{fieldvalue}""");
                                            json.AppendLine("},");
                                            continue;
                                        }
                                        break;

                                    case "created_at" when header.DestinationField.Equals("tranDate"):
                                        fieldvalue = DateTime.Now.ToString("yyyy-MM-dd");
                                        break;

                                    case "order_number" when header.DestinationField.Equals("tranId"):
                                        var orderNumProp = order.GetType().GetProperty(SourceField,
                                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                        var orderNumVal = orderNumProp?.GetValue(order);
                                        var cur_date = DateTime.Now.ToString("MMddyy");
                                        fieldvalue = $"LAZADA-SO{cur_date}-{orderNumVal}";
                                        break;

                                    case "customer_first_name":
                                        var firstName = order.customer_first_name ?? "";
                                        var lastName = order.customer_last_name ?? "";
                                        fieldvalue = $"{firstName} {lastName}".Trim();
                                        break;

                                    default:
                                        var defaultProp = order.GetType().GetProperty(SourceField,
                                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                        if (defaultProp != null)
                                            fieldvalue = defaultProp.GetValue(order)?.ToString();
                                        break;
                                }

                                json.AppendLine($@"""{DestinationField}"":""{fieldvalue}"",");

                            } // end header

                        // -------------------------------------------------------
                        // Line items
                        // -------------------------------------------------------
                        var groupeditems = order.items
                            .GroupBy(it => it.sku)
                            .Select(g => new
                            {
                                sku = g.Key,
                                quantity = g.Count(),
                                unitprice = g.First().item_price,
                                Items = g.ToList(),
                                shipping_amount = g.Sum(x => x.shipping_amount),
                                voucher_platform = g.Sum(x => x.voucher_platform),
                                voucher_seller = g.Sum(x => x.voucher_seller),

                                voucker_amount = g.Sum(x => x.voucher_amount)
                            })
                            .ToList();


                        json.AppendLine(@"""item"": {");
                            json.AppendLine(@"  ""items"": [");

                            int i = 0;
                            double TotalSellerDisCount = 0;
                            double shipping_fee = 0;
                            double voucher_platform = 0;
                            double voucher_seller = 0;


                            var items = order.items ?? new List<LazadaOrderItemModel>();

                            foreach (var rowdata in groupeditems)
                            {

                                 shipping_fee += rowdata.shipping_amount;
                                TotalSellerDisCount += rowdata.voucher_platform;
                                TotalSellerDisCount += rowdata.voucher_seller;
                               
                                json.AppendLine("{");
                                foreach (var rows in fieldrows)
                                {
                                    var DestinationField = rows.DestinationField;
                                    var SourceField = rows.SourceFieldId;
                                    object rowvalue = null;

                                    switch (SourceField)
                                    {
                                        case "Query":
                                            try
                                            {
                                                string input = rows.ConditionalQuery;
                                                var output = Regex.Matches(input, "#(.+?)#")
                                                    .Cast<System.Text.RegularExpressions.Match>()
                                                    .Select(m => m.Groups[1].Value);

                                                string netsuite_price = string.Empty;
                                                double item_price = 0;
                                                double.TryParse(rowdata.unitprice.ToString(), out item_price);

                                                foreach (var columnName in output)
                                                {
                                                    var colProp = rowdata.GetType().GetProperty(columnName,
                                                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                                    var colVal = colProp?.GetValue(rowdata)?.ToString() ?? "";
                                                    input = input.Replace($"#{columnName}#", colVal);
                                                }

                                                rowvalue = await NShelper.GetListNetSuiteData(QueryEndpoint, input);
                                            if (rowvalue != null)
                                                {
                                                    var listValue = rowvalue as List<string>;
                                                    if (listValue != null && listValue.Count > 1)
                                                    {
                                                        var firstElement = listValue[0];
                                                        netsuite_price = listValue[1];

                                                        if (int.TryParse(firstElement, out int intVal))
                                                            rowvalue = intVal;
                                                        else if (double.TryParse(firstElement, out double dblVal))
                                                            rowvalue = dblVal;
                                                        else
                                                            rowvalue = firstElement;
                                                    }
                                                }

                                                if (DestinationField.Equals("item"))
                                                {
                                                    json.AppendLine(@"""item"": {");
                                                    json.AppendLine($@"  ""id"": ""{rowvalue}""");
                                                    json.AppendLine("},");
                                                    json.AppendLine($@"""quantity"": {rowdata.quantity},");
                                                    json.AppendLine($@"""rate"": {item_price}");

                                                    //double netsuitePrice = 0;
                                                    //double.TryParse(netsuite_price, out netsuitePrice);

                                                    //double discResult = Math.Abs(item_price - netsuitePrice);
                                                    //if (discResult > 0)
                                                    //{
                                                    //    //json.AppendLine($@",""custcol_sellerdis"": -{discResult}");
                                                    //    TotalSellerDisCount += discResult;
                                                    //}
                                                    continue;
                                                }
                                                break;
                                            }
                                            catch { break; }

                                        case "Static Value":
                                            rowvalue = rows.ConditionalQuery;
                                            break;

                                        default:
                                            var rowProp = rowdata.GetType().GetProperty(SourceField,
                                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                            if (rowProp != null)
                                                rowvalue = rowProp.GetValue(rowdata)?.ToString();
                                            break;
                                    }

                                    json.AppendLine($@"""{DestinationField}"":""{rowvalue}"",");
                                }

                                if (i < order.items.Count - 1)
                                    json.AppendLine("},");
                                else
                                {
                                    if (shipping_fee > 0 || TotalSellerDisCount > 0 || voucher_platform > 0)
                                        json.AppendLine("},");
                                    else
                                        json.AppendLine("}");
                                }

                                i++;

                                if (i > groupeditems.Count - 1)
                                {
                                    if (shipping_fee > 0)
                                    {
                                        json.AppendLine("{");
                                        json.AppendLine(@"""item"": {");
                                        json.AppendLine(@"  ""id"": ""227""");
                                        json.AppendLine("},");
                                        json.AppendLine($@"""custcol_voucherdisc"": {shipping_fee},");
                                        json.AppendLine($@"""custcol_posnetprice"": {shipping_fee},");
                                        json.AppendLine($@"""amount"": {shipping_fee}");
                                        json.AppendLine("}" + (TotalSellerDisCount > 0 || voucher_platform > 0 ? "," : ""));
                                    }

                                    if (TotalSellerDisCount > 0)
                                    {
                                        json.AppendLine("{");
                                        json.AppendLine(@"""item"": {");
                                        json.AppendLine(@"  ""id"": ""232""");
                                        json.AppendLine("},");
                                        json.AppendLine($@"""custcol_voucherdisc"": -{TotalSellerDisCount},");
                                        json.AppendLine($@"""custcol_posnetprice"": -{TotalSellerDisCount},");
                                        json.AppendLine($@"""amount"": -{TotalSellerDisCount},");
                                        json.AppendLine(@"""price"": {");
                                        json.AppendLine(@"  ""id"": ""-1""");
                                        json.AppendLine("}");
                                        //json.AppendLine("}");

                                        json.AppendLine("   }" + (voucher_platform > 0 ? "," : ""));

                                    }

                                    if (voucher_platform > 0)
                                    {
                                        json.AppendLine("   { ");
                                        json.AppendLine(@"""item"": {");
                                        json.AppendLine($@"  ""id"": ""231"" ");
                                        json.AppendLine("   }, ");
                                        json.AppendLine($@"  ""custcol_voucherdisc"": -{voucher_platform},");
                                        json.AppendLine($@"  ""custcol_posnetprice"": -{voucher_platform},");
                                        json.AppendLine($@"  ""amount"": -{voucher_platform},");
                                        json.AppendLine(@"""price"": {");
                                        json.AppendLine($@"  ""id"": ""-1"" ");
                                        json.AppendLine("   } ");
                                        json.AppendLine("}");
                                    }
                                }

                            } // end items

                            json.AppendLine("]}");
                            json.AppendLine("}");

                            string fixedJson = FixJson(json.ToString());
                            string jsonStringFromBuilder = fixedJson;

                           

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
                                    existing.OrderNumber = OrderNumber;
                                }
                                else
                                {
                                    _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                                    {
                                        ReferenceId = OrderId,
                                        Platform = "Lazada",
                                        BuildJson = jsonStringFromBuilder,
                                        PlatformJson = postingResult,
                                        SyncStatus = "Sync",
                                        CreatedDate = DateTime.Now,
                                        UploadDate = DateTime.Now,
                                        Message = "Successfully Posted",
                                        OrderNumber = OrderNumber
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
                                     module: "Lazada"
                                 );

                                if (existing != null)
                                {
                                    existing.ReferenceId = OrderId;
                                    existing.BuildJson = jsonStringFromBuilder;
                                    existing.PlatformJson = postingResult;
                                    existing.Message = postingResult;
                                    existing.OrderNumber = OrderNumber;
                                }
                                else
                                {
                                    _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                                    {
                                        ReferenceId = OrderId,
                                        Platform = "Lazada",
                                        BuildJson = jsonStringFromBuilder,
                                        PlatformJson = postingResult,
                                        CreatedDate = DateTime.Now,
                                        Message = postingResult,
                                        OrderNumber = OrderNumber
                                    });
                                }
                            }

                            _context.SaveChanges(); // Single save for PlatformTable

                        } // end try
                        catch (Exception ex)
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = "1-Upload SO Lazada To Netsuite",
                                CreateDate = DateTime.Now,
                                ApiUrl = PostDataEndpoint,
                                Json = FixJson(json.ToString()),
                                ErrorMsg = ex.Message,
                                Module = "'salesorder'"
                            });
                            _context.SaveChanges();
                        }

                   // } // end foreach order

                    }
                } // end foreach MapId

            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "2-Upload SO Lazada To Netsuite",
                    CreateDate = DateTime.Now,
                    ApiUrl = "",
                    Json = "",
                    ErrorMsg = ex.Message,
                    Module = "'salesorder'"
                });
                _context.SaveChanges();
            }
        }

       
        private static readonly HttpClient _httpClient = new HttpClient();

        private async Task<List<LazadaOrderModel>> GetLazadaOrdersAsync(string DateFrom = null, string DateTo = null)
        {
            var tokenService = new LazadaTokenService();
            var token = await tokenService.CallRefreshEndpoint();
            var allOrders = new List<LazadaOrderModel>();
            const int limit = 1;
            int offset = 0;
            int totalCount = int.MaxValue;

            while (offset < totalCount)
            {
                var page = await FetchLazadaOrderPageAsync(token, limit, offset, DateFrom, DateTo);

                if (offset == 0)
                    totalCount = page.countTotal;

                if (page.orders == null || page.orders.Count == 0)
                    break;

                var semaphore = new SemaphoreSlim(5);
                var itemTasks = page.orders.Select(async order =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        order.items = await GetOrderItemsAsync(order.order_id.ToString(), token);
                        order.package_id = order.items?.FirstOrDefault()?.package_id ?? "";
                        return order;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });


                var fetched = await Task.WhenAll(itemTasks);
                allOrders.AddRange(fetched.Where(o => o != null));

            offset += limit;
        }

            return allOrders;
        }




        // Returns LazadaOrderData directly — your model already has count + countTotal + orders
        private async Task<LazadaOrderData> FetchLazadaOrderPageAsync(
            LazadaTokenModel token, int limit, int offset, string DateFrom = null, string DateTo = null)
        {
            var apiPath = "/orders/get";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();


            bool isManual = !string.IsNullOrEmpty(DateFrom) && !string.IsNullOrEmpty(DateTo);
            //TUNAY

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

                var createdAfter = new DateTimeOffset(DateTime.SpecifyKind(parsedFrom.Date, DateTimeKind.Utc))
                                        .ToString("yyyy-MM-ddTHH:mm:sszzz");
                var createdBefore = new DateTimeOffset(DateTime.SpecifyKind(parsedTo.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc))
                                        .ToString("yyyy-MM-ddTHH:mm:sszzz");

                paramDict.Add("created_after", createdAfter);
                paramDict.Add("created_before", createdBefore);
            }
            else
            {
                var update_after = DateTimeOffset.UtcNow
                    .ToOffset(TimeSpan.FromHours(8))
                    .AddHours(-1)
                    .ToString("yyyy-MM-ddTHH:mm:sszzz");

                paramDict.Add("update_after", update_after);
                paramDict.Add("status", "pending");
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
        ///OLD REPLACE



        // -------------------------------------------------------
        // Fetch items for a specific order from Lazada API
        // -------------------------------------------------------
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

        // -------------------------------------------------------
        // Fix JSON — remove trailing commas
        // -------------------------------------------------------
        private static string FixJson(string json)
        {
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }
    }

    // -------------------------------------------------------
    // Models
    // -------------------------------------------------------
    public class LazadaOrderResponse
    {
        public string code { get; set; }
        public LazadaOrderData data { get; set; }
    }

    public class LazadaOrderData
    {
        public int count { get; set; }
        public int countTotal { get; set; }
        public List<LazadaOrderModel> orders { get; set; }
    }

    public class LazadaOrderModel
    {
        public long order_id { get; set; }
        public string order_number { get; set; }
        public string status { get; set; }
        public string price { get; set; }
        public string created_at { get; set; }
        public string customer_first_name { get; set; }
        public string customer_last_name { get; set; }
        public string payment_method { get; set; }
        public List<LazadaOrderItemModel> items { get; set; }
     
        public LazadaAddressBilling address_billing { get; set; }
        public LazadaAddressShipping address_shipping { get; set; }

        public string package_id { get; set; }
        public List<string> statuses { get; set; }
        public LazadaOrderModel Clone()
        {
            return (LazadaOrderModel)MemberwiseClone();
        }
    }

    public class LazadaItemResponse
    {
        public string code { get; set; }
        public List<LazadaOrderItemModel> data { get; set; }
    }

    public class LazadaOrderItemModel
    {
        public long order_item_id { get; set; }
        public string sku { get; set; }
        public string sku_id { get; set; }

        public string name { get; set; }
        public double item_price { get; set; }
        public string shop_sku { get; set; }
        public long order_id { get; set; }
        public string variation { get; set; }
        public string status { get; set; }
        public string package_id { get; set; }
        public double shipping_amount { get; set; }
        public double voucher_amount { get; set; }
        public double voucher_seller { get; set; }
        public double voucher_platform { get; set; }


    }

    public class LazadaAddressBilling
    {
        public string country { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string address5 { get; set; }
        public string city { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }


    }

    public class LazadaAddressShipping
    {
        public string country { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string address5 { get; set; }
        public string city { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }

    }
    
}