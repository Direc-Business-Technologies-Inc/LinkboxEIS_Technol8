using DataAccessLayer.Class;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Services.Shopee;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StructureMap.Query;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinkBoxUI.Services.Shopee
{
    public class ShopeeToNetsuite
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        NetsuiteHelperMethod nsHelperMethods = new NetsuiteHelperMethod();

        private static string _baseUrl => ConfigurationManager.AppSettings["Sh_baseUrl"];
        private static string _partnerId => ConfigurationManager.AppSettings["Sh_partnerId"];
        private static string _partnerKey => ConfigurationManager.AppSettings["Sh_partnerKey"];
        private static string _shopId => ConfigurationManager.AppSettings["Sh_shopId"];

        // -------------------------------------------------------
        // POST Shopee Orders to NetSuite as Sales Orders
        // mirrors LazadaToNetsuite.Post()
        // -------------------------------------------------------
        public async Task Post(string Code, string dateFrom = null,
    string dateTo = null)
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

                // ← MapId 213 for Shopee
                var fieldsets = _context.Headers
                    .Where(x => x.MapId == 1006 && x.SourceTableName == "Header")
                    .ToList();

                var fieldrows = _context.Headers
                    .Where(x => x.MapId == 1006 && x.SourceTableName == "Row")
                    .ToList();

                // ← Get Shopee orders directly from Shopee API
                var orders = await GetShopeeOrdersAsync(dateFrom, dateTo);

                foreach (var order in orders)
                {
                    // ← Shopee uses order_sn, not order_id
                    string OrderId = order.order_sn;
                    string OrderNumber = order.order_sn;

                    // Skip if already synced
                    bool exist = _context.PlatformTable.Any(x =>
                        x.ReferenceId == OrderId &&
                        x.Platform == "Shopee" &&
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
                                    var output = Regex.Matches(input, "#(.+?)#")
                                        .Cast<Match>()
                                        .Select(m => m.Groups[1].Value);

                                    foreach (var columnName in output)
                                    {
                                        // Replace this block inside the foreach (var header in fieldsets)

                                        var prop = order.GetType().GetProperty(columnName,
                                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                        var val = prop?.GetValue(order)?.ToString() ?? "";

                                        //if (string.IsNullOrEmpty(val))
                                        //{
                                        //    var fallbackProp = order.GetType().GetProperty("item_sku",
                                        //        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                        //    val = fallbackProp?.GetValue(order)?.ToString() ?? "";
                                        //}

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

                                // ← Shopee: create_time is Unix timestamp, convert to date
                                case "create_time" when header.DestinationField.Equals("tranDate"):
                                    fieldvalue = DateTime.Now.ToString("yyyy-MM-dd");
                                    break;

                                // ← Shopee: use order_sn as tranId
                                case "order_sn" when header.DestinationField.Equals("tranId"):
                                    var cur_date = DateTime.Now.ToString("MMddyy");
                                    fieldvalue = $"SHOPEE-SO{cur_date}-{order.order_sn}";
                                    break;

                                // ← Shopee: buyer_username instead of customer_first_name
                                case "buyer_username":
                                    fieldvalue = order.recipient_address?.name ?? "";
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
                        // Line items — group by model_id (Shopee's SKU equivalent)
                        // -------------------------------------------------------
                        //var groupeditems = order.item_list
                        //    .GroupBy(it => it.model_sku)
                        //    .Select(g => new
                        //    {
                        //        sku = g.Key,
                        //        quantity = g.Count(),
                        //        unitprice = g.First().model_discounted_price,
                        //        Items = g.ToList(),
                        //        shipping_amount = g.Sum(x => x.shipping_amount),
                        //        voucker_amount = g.Sum(x => x.voucher_amount)
                        //    })
                        //    .ToList();

                        json.AppendLine(@"""item"": {");
                        json.AppendLine(@"  ""items"": [");

                        int i = 0;
                        double TotalSellerDisCount = (order.order_income?.commission_fee ?? 0) + (order.order_income?.service_fee ?? 0) +
                                                       (order.order_income?.seller_transaction_fee ?? 0) + (order.order_income?.withholding_tax ?? 0);

                        double actual_sf = order?.order_income.actual_shipping_fee ?? 0;
                       double shopee_rebate = order?.order_income?.shopee_shipping_rebate ?? 0;
                        double buyer_paid = order?.order_income?.buyer_paid_shipping_fee ?? 0;


                        double shipping_fee = buyer_paid + shopee_rebate - actual_sf ;
                        double voucher_discount =
                               (order.buyer_payment_info?.seller_voucher ?? 0)
                                - (order.order_income?.shopee_discount ?? 0);

                        if ((order.order_income?.seller_voucher_code == null ||
                            !order.order_income.seller_voucher_code.Any()) && voucher_discount!=0)
                        {
                            voucher_discount = 0;
                        }

                        var items = order.item_list ?? new List<ShopeeOrderItemModel>();

                        bool hasBundlePromotion = items.Any(x =>
                            !string.IsNullOrEmpty(x.promotion_type) &&
                            x.promotion_type.IndexOf("bundle", StringComparison.OrdinalIgnoreCase) >= 0
                        );


                        Dictionary<double, double> price_percentage = new Dictionary<double, double>();

                        if (hasBundlePromotion)
                        {
                            var BundleTotal = items
                                .Where(x => !string.IsNullOrEmpty(x.promotion_type) &&
                                            x.promotion_type.IndexOf("bundle_deal", StringComparison.OrdinalIgnoreCase) >= 0)
                                .GroupBy(x => x.model_original_price)
                                .Select(g => new
                                {
                                    Price = g.Key,
                                    Quantity = g.Sum(x => x.model_quantity_purchased),
                                    Total = g.Sum(x => x.model_original_price * x.model_quantity_purchased)
                                })
                                .ToList();

                            foreach (var itempercentage in BundleTotal)
                            {
                                var getPercent = itempercentage.Price / itempercentage.Total;

                                price_percentage.Add(itempercentage.Price, getPercent);
                            }
                        }


                        foreach (var rowdata in order.item_list)
                        {
                            //shipping_fee += rowdata.shipping_amount;
                            //voucher_discount += rowdata.voucker_amount;

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
                                                .Cast<Match>()
                                                .Select(m => m.Groups[1].Value);

                                            string netsuite_price = string.Empty;
                                            double item_price = 0;
                                            string promoty_type = rowdata.promotion_type;
                                            double.TryParse(rowdata.model_discounted_price.ToString(), out item_price);

                                            if (promoty_type.Contains("free_gift_sub"))
                                            {
                                                item_price = 0.1;
                                                //double.TryParse(rowdata.model_original_price.ToString(), out item_price);

                                                TotalSellerDisCount += item_price;
                                            }

                                            if (promoty_type.Contains("bundle_deal") & hasBundlePromotion)
                                            {
                                                double originalPrice = 0;

                                                if (double.TryParse(rowdata.model_original_price.ToString(), out originalPrice))
                                                {
                                                    if (price_percentage.TryGetValue(originalPrice, out double percentage))
                                                    {
                                                        item_price = percentage * originalPrice;
                                                    }
                                                }

                                            }

                                            foreach (var columnName in output)
                                            {
                                                var colProp = rowdata.GetType().GetProperty(columnName,
                                                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                                var colVal = colProp?.GetValue(rowdata)?.ToString() ?? "";


                                                if (string.IsNullOrEmpty(colVal))
                                                {
                                                    var fallbackProp = rowdata.GetType().GetProperty("item_sku",
                                                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                                    colVal = fallbackProp?.GetValue(rowdata)?.ToString() ?? "";

                                                }

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
                                                json.AppendLine($@"""quantity"": {rowdata.model_quantity_purchased},");
                                                json.AppendLine($@"""rate"": {item_price}");

                                            

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

                            if (i < items.Count - 1)
                            {
                                json.AppendLine("   },");
                            }
                            else
                            {
                                if (shipping_fee != 0 || TotalSellerDisCount > 0 || voucher_discount != 0)
                                {
                                    json.AppendLine("   },");
                                }
                                else
                                {
                                    json.AppendLine("   }");
                                }

                            }

                            i++;

                            if (i > items.Count - 1)
                            {
                                // ← Shipping fee line item (same ID 227)
                                if (shipping_fee != 0)
                                {
                                    json.AppendLine("{");
                                    json.AppendLine(@"""item"": {");
                                    json.AppendLine(@"  ""id"": ""227""");
                                    json.AppendLine("},");
                                    json.AppendLine($@"""custcol_voucherdisc"": {shipping_fee},");
                                    json.AppendLine($@"""custcol_posnetprice"": {shipping_fee},");
                                    json.AppendLine($@"""amount"": {shipping_fee}");
                                    json.AppendLine("}" + (TotalSellerDisCount > 0 || voucher_discount != 0 ? "," : ""));
                                }

                                // ← Seller discount line item (same ID 232)
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
                                    json.AppendLine("   }" + (voucher_discount != 0 ? "," : ""));
                                }

                                // ← Voucher discount line item (same ID 231)
                                if (voucher_discount != 0)
                                {
                                    json.AppendLine("   {");
                                    json.AppendLine(@"""item"": {");
                                    json.AppendLine($@"  ""id"": ""231""");
                                    json.AppendLine("   },");
                                    json.AppendLine($@"  ""custcol_voucherdisc"": {voucher_discount},");
                                    json.AppendLine($@"  ""custcol_posnetprice"": {voucher_discount},");
                                    json.AppendLine($@"  ""amount"": {voucher_discount},");
                                    json.AppendLine(@"""price"": {");
                                    json.AppendLine($@"  ""id"": ""-1""");
                                    json.AppendLine("   }");
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
                                    Platform = "Shopee",
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
                                 module: "Shopee"
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
                                    Platform = "Shopee",
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
                            Task = "1-Upload SO Shopee To Netsuite",
                            CreateDate = DateTime.Now,
                            ApiUrl = PostDataEndpoint,
                            Json = FixJson(json.ToString()),
                            ErrorMsg = ex.Message,
                            Module = "'salesorder'"
                        });
                        _context.SaveChanges();
                    }

                } // end foreach order

            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "2-Upload SO Shopee To Netsuite",
                    CreateDate = DateTime.Now,
                    ApiUrl = "",
                    Json = "",
                    ErrorMsg = ex.Message,
                    Module = "'salesorder'"
                });
                _context.SaveChanges();
            }
        }

        //NEW
        private static readonly HttpClient _httpClient = new HttpClient();

        // -------------------------------------------------------
        // Result class replacing C#7 tuple (not available without NuGet in 4.6)
        // -------------------------------------------------------
        private class OrderPageResult
        {
            public List<ShopeeOrderSummary> Summaries { get; set; }
            public bool More { get; set; }
            public string NextCursor { get; set; }
        }

        // -------------------------------------------------------
        // Main entry point
        // -------------------------------------------------------
        private async Task<List<ShopeeOrderModel>> GetShopeeOrdersAsync(string dateFrom = null,
    string dateTo = null)
        {
            // Token fetched once — implement caching inside GetValidTokenAsync
            var tokenService = new ShopeeTokenService();
            var token = await tokenService.CallRefreshEndpoint();
            // --- Phase 1: collect all order summaries via cursor pagination ---
            var allSummaries = new List<ShopeeOrderSummary>();
            string cursor = null;
            bool hasMore = true;

            while (hasMore)
            {
                var page = await FetchOrderPageAsync(token.AccessToken, cursor, dateFrom,dateTo);
                allSummaries.AddRange(page.Summaries);
                hasMore = page.More;
                cursor = page.NextCursor;
            }

            //allSummaries.Clear();
            //allSummaries.AddRange(new[]
            //{
            //new ShopeeOrderSummary { order_sn = "2605223FUG4FRK" },
            //new ShopeeOrderSummary { order_sn = "2605248KV88Y2S" },
            //});

            // --- Phase 2: fetch detail + escrow in parallel, capped at 5 concurrent ---
            var semaphore = new SemaphoreSlim(5);

            var detailTasks = allSummaries.Select(async summary =>
            {

                await semaphore.WaitAsync();
                try
                {
                    var detail = await GetOrderDetailAsync(summary.order_sn, token.AccessToken);
                    if (detail == null)
                        return null;

                    var escrow = await GetEscrowAsync(summary.order_sn, token.AccessToken);
                    if (escrow != null)
                    {
                        detail.buyer_payment_info = escrow.buyer_payment_info;
                        detail.order_income = escrow.order_income;
                    }

                    return detail;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(detailTasks);
            return results.Where(d => d != null).ToList();
        }

        // -------------------------------------------------------
        // Single page fetch — cursor null or empty = first page
        // -------------------------------------------------------
        //private async Task<OrderPageResult> FetchOrderPageAsync(string accessToken, string cursor)
        //{
        //    var path = "/api/v2/order/get_order_list";
        //    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //    var timeFrom = timestamp - (1 * 60 * 60);


        //    //string timeFrom = "1776873600";   // 2026-04-23 00:00:00
        //    //string timeTo = "1777391999";  // 2026-04-28 23:59:59

        //    var sign = GenerateSignature(path, timestamp, accessToken);
        //    var url = new StringBuilder();
        //    url.Append(_baseUrl).Append(path)
        //       .Append("?partner_id=").Append(_partnerId)
        //       .Append("&timestamp=").Append(timestamp)
        //       .Append("&sign=").Append(sign)
        //       .Append("&access_token=").Append(accessToken)
        //       .Append("&shop_id=").Append(_shopId)
        //       .Append("&time_range_field=update_time") //update_time-create_time
        //       .Append("&time_from=").Append(timeFrom) //change to timeFrom
        //       .Append("&time_to=").Append(timestamp)      //totimestamp
        //       .Append("&page_size=100")
        //       .Append("&order_status=READY_TO_SHIP")///uncomment
        //       ;

        //    // Only append cursor when we have one — Shopee rejects cursor= on first call
        //    if (!string.IsNullOrEmpty(cursor))
        //        url.Append("&cursor=").Append(cursor);

        //    var response = await _httpClient.GetStringAsync(url.ToString());
        //    var result = JsonConvert.DeserializeObject<ShopeeOrderListResponse>(response);

        //    if (result == null || result.response == null)
        //        throw new InvalidOperationException(
        //            $"Unexpected Shopee response. Raw: {response}");

        //    return new OrderPageResult
        //    {
        //        Summaries = result.response.order_list ?? new List<ShopeeOrderSummary>(),
        //        More = result.response.more,
        //        NextCursor = result.response.next_cursor
        //    };
        //}

        //END OF NEW

        private async Task<OrderPageResult> FetchOrderPageAsync(
    string accessToken,
    string cursor,
    string dateFrom = null,
    string dateTo = null)
        {
            var path = "/api/v2/order/get_order_list";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            bool isManual = !dateFrom.IsNullOrEmpty() && !dateTo.IsNullOrEmpty();

            long timeFrom;
            long timeTo;
            string timeRangeField;
            string orderStatus;

            if (isManual)
            {
                if (!DateTime.TryParse(dateFrom, out var parsedFrom) ||
                    !DateTime.TryParse(dateTo, out var parsedTo))
                {
                    throw new ArgumentException("Invalid date format for dateFrom or dateTo.");
                }

                timeFrom = new DateTimeOffset(DateTime.SpecifyKind(parsedFrom.Date, DateTimeKind.Utc)).ToUnixTimeSeconds();
                timeTo = new DateTimeOffset(DateTime.SpecifyKind(parsedTo.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)).ToUnixTimeSeconds();
                timeRangeField = "create_time";
                orderStatus = null; // no status filter for manual range
            }
            else
            {
                timeFrom = timestamp - (1 * 60 * 60);
                timeTo = timestamp;
                timeRangeField = "update_time";
                orderStatus = "READY_TO_SHIP";
            }

            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = new StringBuilder();
            url.Append(_baseUrl).Append(path)
               .Append("?partner_id=").Append(_partnerId)
               .Append("&timestamp=").Append(timestamp)
               .Append("&sign=").Append(sign)
               .Append("&access_token=").Append(accessToken)
               .Append("&shop_id=").Append(_shopId)
               .Append("&time_range_field=").Append(timeRangeField)
               .Append("&time_from=").Append(timeFrom)
               .Append("&time_to=").Append(timeTo)
               .Append("&page_size=100");

            if (!string.IsNullOrEmpty(orderStatus))
                url.Append("&order_status=").Append(orderStatus);

            if (!string.IsNullOrEmpty(cursor))
                url.Append("&cursor=").Append(cursor);

            var response = await _httpClient.GetStringAsync(url.ToString());
            var result = JsonConvert.DeserializeObject<ShopeeOrderListResponse>(response);

            if (result == null || result.response == null)
                throw new InvalidOperationException($"Unexpected Shopee response. Raw: {response}");

            return new OrderPageResult
            {
                Summaries = result.response.order_list ?? new List<ShopeeOrderSummary>(),
                More = result.response.more,
                NextCursor = result.response.next_cursor
            };
        }


        // -------------------------------------------------------
        // Fetch Shopee orders — status TO_PROCESS, last 15 days
        // mirrors LazadaToNetsuite.GetLazadaOrdersAsync()
        // -------------------------------------------------------
        //private async Task<List<ShopeeOrderModel>> GetShopeeOrdersAsync()
        //{
        //    using (var client = new HttpClient())
        //    {
        //        var tokenService = new ShopeeTokenService();
        //        var token = await tokenService.CallRefreshEndpoint();
        //        var allOrders = new List<ShopeeOrderModel>();

        //        try
        //        {
        //            var path = "/api/v2/order/get_order_list";
        //            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //            //var timeFrom = timestamp - (1 * 24 * 60 * 60);  // 24 hours ago
        //            var timeFrom = timestamp - (1 * 60 * 60);        //  1 hour ago
        //            var sign = GenerateSignature(path, timestamp, token.AccessToken);

        //            var fullUrl = $"{_baseUrl}{path}" +
        //                          $"?partner_id={_partnerId}" +
        //                          $"&timestamp={timestamp}" +
        //                          $"&sign={sign}" +
        //                          $"&access_token={token.AccessToken}" +
        //                          $"&shop_id={_shopId}" +
        //                          $"&time_range_field=update_time" +
        //                          $"&time_from={timeFrom}" +
        //                          $"&time_to={timestamp}" +
        //                          $"&page_size=100" +
        //                          $"&page_no=1" +
        //                          $"&order_status=READY_TO_SHIP";

        //            var response = await client.GetStringAsync(fullUrl);
        //            var result = JsonConvert.DeserializeObject<ShopeeOrderListResponse>(response);

        //            if (result?.response?.order_list != null)
        //            {
        //                foreach (var orderSummary in result.response.order_list)
        //                {
        //                    // ← Get full order detail for each order_sn
        //                    var detail = await GetOrderDetailAsync(orderSummary.order_sn, token.AccessToken);
        //                    var escrow = await GetEscrowAsync(orderSummary.order_sn, token.AccessToken);

        //                    if (detail == null)
        //                        continue;

        //                    if (escrow != null)
        //                    {
        //                        detail.buyer_payment_info = escrow.buyer_payment_info;
        //                        detail.order_income = escrow.order_income;
        //                    }

        //                    allOrders.Add(detail);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error fetching Shopee orders: {ex.Message}");
        //        }

        //        return allOrders;
        //    }
        //}

        // -------------------------------------------------------
        // Fetch full order detail + items for a single order_sn
        // mirrors LazadaToNetsuite.GetOrderItemsAsync()
        // -------------------------------------------------------
        private async Task<ShopeeOrderModel> GetOrderDetailAsync(string orderSn, string accessToken)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var path = "/api/v2/order/get_order_detail";
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var sign = GenerateSignature(path, timestamp, accessToken);

                    var optionalFields = string.Join(",", new[]
                    {
                        "total_amount",
                        "estimated_shipping_fee",
                        "actual_shipping_fee",
                        "voucher_from_seller",
                        "voucher_from_shopee",
                        "payment_method",
                        "buyer_username",
                        "recipient_address",
                        "pay_time",
                        "shipping_carrier",
                        "note",
                        "cod",
                        "item_list"
                    });

                    // Parameter order matches Postman script exactly
                    var fullUrl = $"{_baseUrl}{path}" +
                                  $"?partner_id={_partnerId}" +
                                  $"&shop_id={_shopId}" +
                                  $"&access_token={accessToken}" +
                                  $"&timestamp={timestamp}" +
                                  $"&order_sn_list={orderSn}" +
                                  $"&response_optional_fields={optionalFields}" +
                                  $"&sign={sign}";

                    var response = await client.GetStringAsync(fullUrl);
                    var result = JsonConvert.DeserializeObject<ShopeeOrderDetailResponse>(response);
                    return result?.response?.order_list?.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error fetching Shopee order detail {orderSn}: {ex.Message}");
                throw new Exception($"Error fetching Shopee order detail {orderSn}: {ex.Message}");
            }
        }
        private async Task<ShopeeOrderModel> GetEscrowAsync(string orderSn, string accessToken)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var path = "/api/v2/payment/get_escrow_detail";
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var sign = GenerateSignature(path, timestamp, accessToken);



                    // Parameter order matches Postman script exactly
                    var fullUrl = $"{_baseUrl}{path}" +
                                  $"?partner_id={_partnerId}" +
                                  $"&shop_id={_shopId}" +
                                  $"&access_token={accessToken}" +
                                  $"&timestamp={timestamp}" +
                                  $"&order_sn={orderSn}" +
                                  $"&sign={sign}";

                    var response = await client.GetStringAsync(fullUrl);
                    var result = JsonConvert.DeserializeObject<ShopeeEscrowResponse>(response);
                    return new ShopeeOrderModel
                    {
                        order_sn = result.response.order_sn,
                        buyer_payment_info = result.response.buyer_payment_info,
                        order_income = result.response.order_income
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Shopee order detail {orderSn}: {ex.Message}");
                return null;
            }
        }

        // -------------------------------------------------------
        // Generate HMAC-SHA256 signature
        // shop-level: partner_id + path + timestamp + access_token + shop_id
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

        // -------------------------------------------------------
        // Fix JSON — remove trailing commas
        // -------------------------------------------------------
        private static string FixJson(string json)
        {
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }
    }

    // -------------------------------------------------------
    // Models — mirrors Lazada models but for Shopee response
    // -------------------------------------------------------
    public class ShopeeOrderListResponse
    {
        public string code { get; set; }
        public ShopeeOrderListData response { get; set; }
    }

    public class ShopeeOrderListData
    {
        public bool more { get; set; }
        public string next_cursor { get; set; }
        public List<ShopeeOrderSummary> order_list { get; set; }
    }

    public class ShopeeOrderSummary
    {
        public string order_sn { get; set; }
        public string order_status { get; set; }
    }

    public class ShopeeOrderDetailResponse
    {
        public string code { get; set; }
        public ShopeeOrderDetailData response { get; set; }
    }

    public class ShopeeOrderDetailData
    {
        public List<ShopeeOrderModel> order_list { get; set; }
    }

    public class ShopeeOrderModel
    {
        public string order_sn { get; set; }
        public string order_status { get; set; }
        public string currency { get; set; }
        public string region { get; set; }
        public bool? cod { get; set; }

        public long? create_time { get; set; }
        public long? update_time { get; set; }
        public long? pay_time { get; set; }
        public long? ship_by_date { get; set; }
        public int? days_to_ship { get; set; }

        public string message_to_seller { get; set; }
        public string note { get; set; }
        public string booking_sn { get; set; }

        public double? total_amount { get; set; }
        public double? estimated_shipping_fee { get; set; }
        public double? actual_shipping_fee { get; set; }
        public double? voucher_from_seller { get; set; }
        public double? voucher_from_shopee { get; set; }
        public string payment_method { get; set; }
        public string buyer_username { get; set; }
        public string shipping_carrier { get; set; }
        public double? reverse_shipping_fee { get; set; }

        public string promotion_type { get; set; }


        public ShopeeRecipientAddress recipient_address { get; set; }
        public List<ShopeeOrderItemModel> item_list { get; set; }
        public BuyerPaymentInfoDto buyer_payment_info { get; set; }
        public OrderIncomeDto order_income { get; set; }
    }

    public class ShopeeRecipientAddress
    {
        public string name { get; set; }
    }

    public class ShopeeOrderItemModel
    {
        public string order_sn { get; set; }
        public long item_id { get; set; }
        public string item_name { get; set; }
        public string item_sku { get; set; }
        public long model_id { get; set; }
        public string model_name { get; set; }
        public string model_sku { get; set; }
        public int model_quantity_purchased { get; set; }
        public double model_discounted_price { get; set; }
        public double model_original_price { get; set; }
        public bool add_on_deal { get; set; }
        public string promotion_type { get; set; }

        // Kept from original — mapped manually if needed
        public double shipping_amount { get; set; }
        public double voucher_amount { get; set; }


    }
    public sealed class ShopeeOrderIncomeResponseDto
    {
        public BuyerPaymentInfoDto buyer_payment_info { get; set; }
        public OrderIncomeDto order_income { get; set; }
    }


    public sealed class BuyerPaymentInfoDto
    {
        // VOUCHER DISCOUNT
        // shop voucher paid by seller
        public double seller_voucher { get; set; }
        public double shipping_fee { get; set; }
    }

    public sealed class OrderIncomeDto
    {
        // VOUCHER DISCOUNT
        // product discount rebate from shopee
        public double shopee_discount { get; set; }

        // SELLER DISCOUNT
        public double commission_fee { get; set; }
        public double service_fee { get; set; }
        public double seller_transaction_fee { get; set; }
        public double withholding_tax { get; set; }

        // REBATE
        public double seller_shipping_discount { get; set; }
        public double final_shipping_fee { get; set; }


        public double actual_shipping_fee { get; set; }
        public double buyer_paid_shipping_fee { get; set; }
        public double shopee_shipping_rebate { get; set; }

        public string[] seller_voucher_code { get; set; }


    }
    public class ShopeeEscrowResponse
    {
        public string error { get; set; }
        public string message { get; set; }
        public string request_id { get; set; }
        public ShopeeEscrowData response { get; set; }
    }

    public class ShopeeEscrowData
    {
        public string order_sn { get; set; }
        public BuyerPaymentInfoDto buyer_payment_info { get; set; }
        public OrderIncomeDto order_income { get; set; }
    }

}