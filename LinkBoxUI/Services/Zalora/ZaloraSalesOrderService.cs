using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.Models.Shopify;
using DomainLayer.Models.Zalora;
using LinkBoxUI.Context;
using LinkBoxUI.Controllers.api;
using LinkBoxUI.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.UI.Design;

namespace LinkBoxUI.Services.Zalora
{

    public class ZaloraSalesOrderService
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        NetsuiteHelper NShelper = new NetsuiteHelper();
        NetsuiteHelperMethod nsHelperMethods = new NetsuiteHelperMethod();

        ZaloraAccessTokenService accessTokenService = new ZaloraAccessTokenService();

      
        public async Task<string>ZaloraOrderPostToNetsuite(int[] MapIds,string Code, string DateFrom = null, string DateTo = null)
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

                var result = await GetPendingOrders(MapId, APIid,DateFrom,DateTo);

                foreach (var order in result.Items)
                {
                    string OrderId = order.Id.ToString();

                    bool exist = _context.PlatformTable.Any(x => x.ReferenceId == OrderId && x.Platform == "Zalora" && x.SyncStatus == "Sync");
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
                            case "createdAt" when header.DestinationField.Equals("tranDate"):
                                fieldvalue = DateTime.Now.ToString("yyyy-MM-dd"); // Format the DateTime value to "yyyy-MM-dd"

                                break;

                            case "number" when header.DestinationField.Equals("tranId"):
                                var _prop = order.GetType().GetProperty(
                                      SourceField,
                                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                                  );

                                var _fv = _prop?.GetValue(order);
                                fieldvalue = $"ZALORA-SO{DateTime.Now.ToString("MMyy")}-{_fv.ToString()}"; // Format the DateTime value to "yyyy-MM-dd"

                                break;
                            
                            case "customer":
                              
                                var firstName = GetNestedPropertyValue(order, "Customer.FirstName");
                                var lastName = GetNestedPropertyValue(order, "Customer.LastName");


                                fieldvalue = $"{firstName} {lastName}";

                                break;
                            default:
                                var prop = order.GetType().GetProperty(
                                        SourceField,
                                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                                    );

                                var fv = prop?.GetValue(order);

                                if (fv != null)
                                {
                                    fieldvalue = fv.ToString();
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
                    double voucher_platform = 0;
                    var groupedItems = order.Items
                    .Where(it => it.Product != null && !string.IsNullOrWhiteSpace(it.Product.SellerSku))
                    .GroupBy(it => it.Product.SellerSku)
                    .Select(g => new
                    {
                        SellerSku = g.Key,
                        Quantity = g.Count(),
                        UnitPrice = g.First().UnitPrice,
                        shippingFee = g.Sum(it => it.shippingFee),
                        shippingVoucherAmount = g.Sum(it => it.shippingVoucherAmount), // if nullable
                        discountAmount = g.First().discountAmount * g.Count(), // if nullable
                        walletCredits = g.First().walletCredits * g.Count(), // if nullable
                        Items = g.ToList()
                    })
                    .ToList();

                    string Order_number = order.Number.ToString();
                    double shipping_fee = 0;
                    foreach (var rowdata in groupedItems) //order.Items
                    {

                        var sku = rowdata.SellerSku;
                        var Quantity = rowdata.Quantity;
                        double UnitPrice = double.Parse(rowdata.UnitPrice.ToString());
                        shipping_fee +=rowdata.shippingFee;
                        TotalSellerDisCount += rowdata.discountAmount;
                        TotalSellerDisCount += rowdata.walletCredits;
                        voucher_platform += rowdata.shippingVoucherAmount;

                        json.AppendLine("   { ");
                        foreach (var rows in fieldrows)
                        {

                            var DestinationField = rows.DestinationField;
                            var SourceField = rows.SourceFieldId;

                            //var fieldvalue = "";
                            object rowvalue = null;
                            switch (SourceField)
                            {
                                case "Query":

                                    try
                                    {
                                        string input = rows.ConditionalQuery;
                                        var output = Regex.Matches(input, $"#(.+?)#")
                                                                            .Cast<System.Text.RegularExpressions.Match>()
                                                                            .Select(m => m.Groups[1].Value);

                                        string netsuite_price = string.Empty;

                                        var lineprice = rowdata.GetType().GetProperty("UnitPrice");
                                        var shopify_price = lineprice?.GetValue(rowdata);


                                        foreach (var columnName in output)
                                        {


                                            input = QueryAccess.OriginalReplaceQueryParameter(input
                                                , $"#{columnName}#"
                                                , sku.ToString(), null);

                                        }

                                        rowvalue = await NShelper.GetListNetSuiteData(QueryEndpoint, input);
                                        if (rowvalue != null)
                                        {
                                            //var rowvaluetype = rowvalue.GetType();
                                            var listValue = rowvalue as List<string>;
                                            if (listValue.Count > 1)
                                            {
                                                // Get the first element (index 0)
                                                var firstElement = listValue[0];

                                                // Get the second element (index 1)
                                                netsuite_price = listValue[1];

                                                if (int.TryParse(firstElement.ToString(), out int intValue))
                                                {
                                                    // If it's an integer, use intValue
                                                    rowvalue = intValue;
                                                }
                                                else if (double.TryParse(firstElement.ToString(), out double doubleValue))
                                                {
                                                    // If it's a decimal/float number, use doubleValue
                                                    rowvalue = doubleValue;
                                                }
                                                else
                                                {
                                                    // Keep fieldValue as a string if it's not a number
                                                    rowvalue = firstElement.ToString();
                                                }

                                            }

                                        }

                                        if (DestinationField.Equals("item"))
                                        {
                                            json.AppendLine(@"""item"": {");
                                            json.AppendLine($@"  ""id"": ""{rowvalue}""");
                                            json.AppendLine("   }, ");

                                            var propsz = rowdata.GetType().GetProperty("quantity");
                                            var valsz = propsz?.GetValue(rowdata);

                                            int quantity = 0;

                                            int.TryParse(Quantity.ToString(), out quantity);


                                            json.AppendLine($@"  ""quantity"": {quantity},");
                                            //json.AppendLine($@"  ""amount"": {UnitPrice}");
                                            json.AppendLine($@"  ""rate"": {UnitPrice}");


                                            // Convert netsuite_price to double
                                            double netsuitePrice = 0;
                                            if (!string.IsNullOrEmpty(netsuite_price))
                                            {
                                                double.TryParse(netsuite_price, out netsuitePrice);
                                            }

                                            double _result = Math.Abs(UnitPrice - netsuitePrice);

                                            if (_result > 0)
                                            {
                                                json.AppendLine($@"  ,""custcol_sellerdis"": -{_result}");
                                                TotalSellerDisCount += _result;
                                            }

                                            continue;
                                        }

                                        break;
                                    }
                                    catch
                                    {
                                        break;
                                    }

                                case "Static Value":
                                    rowvalue = rows.ConditionalQuery;

                                    break;
                                default:
                                    var propertyvalue = rowdata.GetType().GetProperty(SourceField);
                                    if (propertyvalue != null)
                                    {
                                        rowvalue = propertyvalue.GetValue(rowdata).ToString(); // Retrieve the value of the property
                                    }

                                    break;
                            }

                        }
                        if (i < order.Items.Count - 1)
                        {
                            json.AppendLine("   },");
                        }
                        else
                        {
                            if (shipping_fee > 0 || TotalSellerDisCount > 0 || voucher_platform > 0)
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
                        if (i > groupedItems.Count - 1)
                        {
                            if (shipping_fee > 0)
                            {
                                json.AppendLine("   { ");
                                json.AppendLine(@"""item"": {");
                                json.AppendLine($@"  ""id"": ""227"" ");
                                json.AppendLine("   }, ");
                                json.AppendLine($@"  ""custcol_voucherdisc"": {shipping_fee},");
                                json.AppendLine($@"  ""custcol_posnetprice"": {shipping_fee},");
                                json.AppendLine($@"  ""amount"": {shipping_fee}");
                                //json.AppendLine("   }" + (TotalSellerDisCount > 0 ? "," : ""));
                                json.AppendLine("}" + (TotalSellerDisCount > 0 || voucher_platform > 0 ? "," : ""));
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
                                //json.AppendLine("   } ");
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
                    }
                    


                    json.AppendLine("   ]} ");
                    json.AppendLine("   } ");

                    json.Length--;

                    string fixedJson = FixJson(json.ToString());
                    string jsonStringFromBuilder = fixedJson.ToString();

                    //NEW Implem
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
                            existing.OrderNumber = Order_number;
                        }
                        else
                        {
                            _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                            {
                                ReferenceId = OrderId,
                                Platform = "Zalora",
                                BuildJson = jsonStringFromBuilder,
                                PlatformJson = postingResult,
                                SyncStatus = "Sync",
                                CreatedDate = DateTime.Now,
                                UploadDate = DateTime.Now,
                                Message = "Successfully Posted",
                                OrderNumber = Order_number
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
                             module: "Zalora"
                         );

                        if (existing != null)
                        {
                            existing.ReferenceId = OrderId;
                            existing.BuildJson = jsonStringFromBuilder;
                            existing.PlatformJson = postingResult;
                            existing.Message = postingResult;
                            existing.OrderNumber = Order_number;
                        }
                        else
                        {
                            _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                            {
                                ReferenceId = OrderId,
                                Platform = "Zalora",
                                BuildJson = jsonStringFromBuilder,
                                PlatformJson = postingResult,
                                CreatedDate = DateTime.Now,
                                Message = postingResult,
                                OrderNumber = Order_number
                            });
                        }
                    }

                    _context.SaveChanges(); // Single save for PlatformTable
                                            //End of NEW Implem

                }
            }
            

            
            return "";
        }
        static string FixJson(string json)
        {
            // Removes trailing commas before a closing bracket or brace
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }
        public static object GetNestedPropertyValue(object obj, string path)
        {
            object current = obj;

            foreach (var part in path.Split('.'))
            {
                if (current == null) return null;

                var prop = current.GetType().GetProperty(
                    part,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                );

                current = prop?.GetValue(current);
            }

            return current;
        }
        public async Task<ZaloraOrderResponse> GetPendingOrders(int MapId,int ApiCode, string DateFrom = null, string DateTo = null)
        {
            try
            {
                // 1️⃣ Get cached bearer token (will NOT re-call if valid)
                var accessToken = await accessTokenService.AccessToken();

                // 2️⃣ Build dates (UTC+8)
                var tz = TimeSpan.FromHours(8);
                var today = DateTimeOffset.UtcNow.ToOffset(tz);


                //var urlBuilder = new StringBuilder($"https://sellercenter-api.zalora.com.ph/v2/orders?orderNumbers[]=264712847&orderNumbers[]=279712847");
                var urlBuilder = new StringBuilder($"https://sellercenter-api.zalora.com.ph/v2/orders");



                bool isManual = !string.IsNullOrEmpty(DateFrom) && !string.IsNullOrEmpty(DateTo);

                if (isManual)
                {

                    if (!DateTime.TryParse(DateFrom, out var parsedFrom) ||
                        !DateTime.TryParse(DateTo, out var parsedTo))
                    {
                        throw new ArgumentException("Invalid date format for DateFrom or DateTo.");
                    }

                    var dateStart = Uri.EscapeDataString(parsedFrom.Date.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz"));
                    var dateEnd = Uri.EscapeDataString(parsedTo.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz"));

                    urlBuilder.Append($"?dateStart={dateStart}&dateEnd={dateEnd}");
                }
                else
                {
                    var DocParam = _context.DocumentMaps.Where(x => x.DocId == MapId && x.UpdateUserId == ApiCode).ToList();

                    var isFirstParam = true;
                    foreach (var param in DocParam)
                    {
                        string paramvalue = param.QueryField.ToString();
                        if (param.CrystalParam.ToString().Contains("Start"))
                        {
                            int start_offset = int.Parse(param.QueryField.ToString().Split(',')[1].Trim());

                            var dateStart = today.AddDays(start_offset).Date; // yesterday 00:00
                            paramvalue = Uri.EscapeDataString(
                           dateStart.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz"));
                        }
                        if (param.CrystalParam.ToString().Contains("End"))
                        {
                            int end_offset = int.Parse(param.QueryField.ToString().Split(',')[1].Trim());
                            var dateEnd = today.Date.AddDays(end_offset).AddTicks(-1); // today 23:59:59.9999999
                            paramvalue = Uri.EscapeDataString(
                           dateEnd.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz"));
                        }

                        string separator = isFirstParam ? "?" : "&";
                        urlBuilder.Append($"{separator}{param.CrystalParam.ToString()}={paramvalue}");
                        isFirstParam = false;
                    }
                }



               //var  urlBuilder_v2 = new StringBuilder("https://sellercenter-api.zalora.com.ph/v2/orders?limit=100&offset=0&orderNumbers%5B%5D=298563627&sortDir=asc");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await client.GetAsync(urlBuilder.ToString());

                    // 5️⃣ Throw if NOT success
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException(
                            $"Zalora orders request failed. " +
                            $"Status: {(int)response.StatusCode}. Body: {errorBody}");
                    }

                    // 6️⃣ Return response (JSON)
                    var json = await response.Content.ReadAsStringAsync();

                    var result = System.Text.Json.JsonSerializer.Deserialize<ZaloraOrderResponse>(
                        json,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );

                    // Return strongly-typed object
                    return result;
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Error fetching pending orders: " + ex.Message, ex);
            }
         
        }


    }
    public class ZaloraAccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}