using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.Models.Shopify;
using LinkBoxUI.Context;
using LinkBoxUI.Controllers.api;
using LinkBoxUI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using System.Windows.Forms;

namespace LinkBoxUI.Services.Shopify
{
    public class ShopifyToNetsuite : ApiController
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        NetsuiteHelperMethod nsHelperMethods = new NetsuiteHelperMethod();
        public async Task<IHttpActionResult> Post(int[] mapid, string Code, string DateFrom = null, string DateTo = null)
        {
            try
            {
                var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();

                var shopifyController = new ShopifyController();
                var Schedule = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
                foreach (int MapId in mapid)
                {
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

                    // Await the result and cast to OkNegotiatedContentResult<string>
                    shopifyController.Request = new HttpRequestMessage();
                    shopifyController.Configuration = new HttpConfiguration();

                    var response = await shopifyController.GetShopifySalesOrder(MapId, APIid,DateFrom,DateTo);

                    // Convert IHttpActionResult → HttpResponseMessage
                    var httpResponse = await response.ExecuteAsync(CancellationToken.None);

                    // Extract JSON/text
                    string content = await httpResponse.Content.ReadAsStringAsync();

                    var complete_data = JsonConvert.DeserializeObject<ShopifyModel>(content);

                    var fieldsets = _context.Headers.Where(x => x.MapId == MapId && x.SourceTableName == "Header").ToList();
                    var fieldrows = _context.Headers.Where(x => x.MapId == MapId && x.SourceTableName == "Row").ToList();

                    ///BIG COMMENT FOR TESTING PURPOSES'
                    ///
                   // var excludedOrderNumb9ers = new List<string> { "17335", "17347", "17348", "17340", "17349", "17351" };
                    foreach (var data in complete_data.Orders)
                    {
                        //if (!excludedOrderNumb9ers.Contains(data.order_number.ToString()))
                        //{
                        //    continue;
                        //}

                        Schedule.RunTime = DateTime.Now;
                        _context.SaveChanges();
                        //if (data.tags.ToString().Equals("Posted to Netsuite")) continue;
                        StringBuilder json = new StringBuilder();
                        string OrderId = data.id.ToString();
                        string Order_number = data.order_number.ToString();
                        bool exist = _context.PlatformTable.Any(x => x.ReferenceId == OrderId && x.Platform == "Shopify" && x.SyncStatus == "Sync");
                        double shipping_fee = 0;
                        if (data?.shipping_lines != null && data.shipping_lines.Any() && data.shipping_lines[0]?.discounted_price != null)
                        {
                            double.TryParse(data.shipping_lines[0].discounted_price.ToString(), out shipping_fee);
                        }
                        double voucher_discount = 0;
                        // Using null-coalescing if you access it via JObject/JsonElement
                        double TotalSellerDisCount = double.TryParse(data.current_total_discounts?.ToString(), out double parsed) ? parsed : 0;

                        if (data.discount_codes.Count > 0) voucher_discount = double.Parse(data.discount_codes[0].amount.ToString());
                        if (exist) continue;
                        try
                        {
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
                                            input = QueryAccess.OriginalReplaceQueryParameter(input
                                                , $"#{columnName}#"
                                                , data.GetType().GetProperty(columnName).ToString(), null);

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
                                    case "created_at" when header.DestinationField.Equals("tranDate"):
                                        fieldvalue = DateTime.Now.ToString("yyyy-MM-dd"); // Format the DateTime value to "yyyy-MM-dd"

                                        break;
                                    case "order_number" when header.DestinationField.Equals("tranId"):
                                        var prop = data.GetType().GetProperty(SourceField);
                                        var val = prop?.GetValue(data);
                                        var cur_date = DateTime.Now.ToString("MMddyy");

                                        fieldvalue = $"SHOPIFY-SO{cur_date}-{val}";
                                        break;
                                    case "customer":
                                        var customerProp = data.GetType().GetProperty("customer");
                                        var customerObj = customerProp?.GetValue(data);

                                        var firstNameProp = customerObj?.GetType().GetProperty("first_name");
                                        var lastNameProp = customerObj?.GetType().GetProperty("last_name");

                                        string first = firstNameProp?.GetValue(customerObj)?.ToString() ?? "";
                                        string last = lastNameProp?.GetValue(customerObj)?.ToString() ?? "";

                                        fieldvalue = $"{first} {last}".Trim();

                                        break;
                                    case "payment_gateway_names":
                                        var props = data.GetType().GetProperty("payment_gateway_names");
                                        var vals = props?.GetValue(data);

                                        if (vals is IEnumerable<string> list)
                                        {
                                            fieldvalue = list.FirstOrDefault() ?? "";
                                        }
                                        break;
                                    default:
                                        var propertyvalue = data.GetType().GetProperty(SourceField);
                                        if (propertyvalue != null)
                                        {
                                            fieldvalue = propertyvalue.GetValue(data).ToString(); // Retrieve the value of the property
                                        }
                                        break;
                                }
                                json.AppendLine($@"""{DestinationField}"":""{fieldvalue}"",");



                            }//end of header
                            json.AppendLine(@"""item"": {");
                            json.AppendLine(@"  ""items"": [");
                            int i = 0;
                        
                            foreach (var rowdata in data.line_items)
                            {
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

                                                    var lineprice = rowdata.GetType().GetProperty("price");
                                                    var shopify_price = lineprice?.GetValue(rowdata);


                                                foreach (var columnName in output)
                                                {
                                                    var column_value = rowdata.GetType().GetProperty(columnName);
                                                    var valrow = column_value?.GetValue(rowdata);

                                                    string sku = valrow?.ToString() ?? "";

                                                
                                                    if (string.IsNullOrEmpty(sku) && input.Contains("sku"))
                                                    {
                                                        var prod_id = rowdata.GetType().GetProperty("product_id");
                                                        var prod_val = prod_id?.GetValue(rowdata);

                                                        var variant_id = rowdata.GetType().GetProperty("variant_id");
                                                        var variant_data = variant_id?.GetValue(rowdata);

                                                        var responseItem = await shopifyController.GetItemSku(MapId, APIid, prod_val.ToString(), variant_data.ToString());


                                                        // Convert IHttpActionResult → HttpResponseMessage
                                                        var httpResponseItem = await responseItem.ExecuteAsync(CancellationToken.None);

                                                        // Extract JSON/text
                                                        string contentItem = await httpResponseItem.Content.ReadAsStringAsync();

                                                        var skudata = JsonConvert.DeserializeObject<ShopifyProductModel>(contentItem);

                                                        sku = skudata.Variant.sku;
                                                        netsuite_price = "0";
                                                    }

                                                    //eRROR IF THE U_HBSCode is Not Exist in the SAP.. There is no Row at position 0 
                                                    input = QueryAccess.OriginalReplaceQueryParameter(input
                                                        , $"#{columnName}#"
                                                        , sku, null);

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
                                                    //else
                                                    //{
                                                    //    // Handle the case where rowvalue is not a List<string> or does not contain more than one element
                                                    //    rowvalue = ""; // Ensure it's a string if it's not a list
                                                    //}

                                                    // Attempt to parse fieldValue as a number

                                                }

                                                if (DestinationField.Equals("item"))
                                                {
                                                    json.AppendLine(@"""item"": {");
                                                    json.AppendLine($@"  ""id"": ""{rowvalue}""");
                                                    json.AppendLine("   }, ");

                                                    var propsz = rowdata.GetType().GetProperty("quantity");
                                                    var valsz = propsz?.GetValue(rowdata);

                                                    int quantity = 0;
                                                    if (valsz != null)
                                                    {
                                                        if (valsz is int z) quantity = z;
                                                        else int.TryParse(valsz.ToString(), out quantity);
                                                    }
                                                    json.AppendLine($@"  ""quantity"": {quantity},");
                                                    //json.AppendLine($@"  ""amount"": {shopify_price},");
                                                    json.AppendLine($@"  ""rate"": {shopify_price}");

                                                    double shopifyPrice = 0;
                                                    if (shopify_price != null)
                                                    {
                                                        double.TryParse(shopify_price.ToString(), out shopifyPrice);
                                                    }

                                                    // Convert netsuite_price to double
                                                    double netsuitePrice = 0;
                                                    if (!string.IsNullOrEmpty(netsuite_price))
                                                    {
                                                        double.TryParse(netsuite_price, out netsuitePrice);
                                                    }

                                                    // Subtract the prices and ensure the result is positive
                                                    double result = Math.Abs(shopifyPrice - netsuitePrice);

                                                    if(result > 0)
                                                    {
                                                        json.AppendLine($@"  ,""custcol_sellerdis"": -{result}");
                                                        //TotalSellerDisCount += result;
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
                                if (i < data.line_items.Count - 1)
                                {
                                    json.AppendLine("   },");
                                }
                                else
                                {
                                    // This is the last item, so append the closing brace and check if we need to append a comma
                                    if (shipping_fee > 0 || TotalSellerDisCount > 0 || voucher_discount > 0)
                                    {
                                        json.AppendLine("   },");
                                    }
                                    else
                                    {
                                        json.AppendLine("   }");
                                    }
                                }
                                //json.AppendLine("   }" + (i < data.line_items.Count - 1 ? "," : ""));
                                i++;
                                if (i > data.line_items.Count - 1)
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
                                        json.AppendLine("   }" + (TotalSellerDisCount > 0 || voucher_discount > 0 ? "," : ""));
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
                                        json.AppendLine("   }" + (voucher_discount > 0 ? "," : ""));

                                    }
                                    if (voucher_discount > 0)
                                    {
                                        json.AppendLine("   { ");
                                        json.AppendLine(@"""item"": {");
                                        json.AppendLine($@"  ""id"": ""231"" ");
                                        json.AppendLine("   }, ");
                                        json.AppendLine($@"  ""custcol_voucherdisc"": -{voucher_discount},");
                                        json.AppendLine($@"  ""custcol_posnetprice"": -{voucher_discount},");
                                        json.AppendLine($@"  ""amount"": -{voucher_discount},");
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


                            //string Posting_result = "successfully posted";
                            //string Posting_result = await NShelper.PostDataToNetSuite(PostDataEndpoint, jsonStringFromBuilder, "salesorder");


                         

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
                                        Platform = "Shopify",
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
                                     module: "Shopify"
                                 );

                                if (existing != null)
                                {
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
                                        Platform = "Shopify",
                                        BuildJson = jsonStringFromBuilder,
                                        PlatformJson = postingResult,
                                        CreatedDate = DateTime.Now,
                                        Message = postingResult,
                                        OrderNumber = Order_number
                                    });
                                }
                            }

                            _context.SaveChanges(); // Single save for PlatformTable
                            //END OF NEW IMPLEM
                        }//end of try
                        catch (Exception ex)
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = "1-Upload SO Shopify To Netsuite",
                                CreateDate = DateTime.Now,
                                ApiUrl = $@"{PostDataEndpoint}",
                                Json = $"{FixJson(json.ToString()).ToString()}",
                                ErrorMsg = $"{ex.Message}",
                                Module = $@"'Shopify - salesorder'"
                            });
                            _context.SaveChanges();
                        }



                    }//end of data complete_data.Orders
                }//end of MapId



                return Ok("success");

            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "2-Upload SO Shopify To Netsuite",
                    CreateDate = DateTime.Now,
                    ApiUrl = $@"",
                    Json = $"",
                    ErrorMsg = $"{ex.Message}",
                    Module = $@"'Shopify - salesorder'"
                });
                _context.SaveChanges();
                return BadRequest($"Operation failed. -  {ex.Message}");

            }
        }
        static string FixJson(string json)
        {
            // Removes trailing commas before a closing bracket or brace
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }
     
        //public async Task<IHttpActionResult> CheckFulfillment()
        //{
        //    try
        //    {
               
        //        var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == null && x.Platform== "Shopify").Select(x=>x.OrderNumber).ToList();
        //        if(order_numbers.Count > 0 && order_numbers != null)
        //        {
        //            var orderNumbersCsv =  string.Join(",", order_numbers.Select(n => $"'{n}'"));

        //            var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
        //            var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();

        //            string Query = $@"SELECT 
        //                               id,
        //                               custbody_ordernumber as ordernumber,
        //                               custbody_status as status
        //                            FROM ""transaction""
        //                            WHERE type = 'SalesOrd'
        //                            AND REGEXP_REPLACE(TRIM(custbody_ordernumber), '[^0-9]', '') 
        //                            IN ({orderNumbersCsv}) 
        //                            and custbody_status='Pending Billing';";

        //            var result = await NShelper.ListGetNetSuiteData(QueryEndpoint, Query);

        //            if(result!=null && result.Count > 0)
        //            {
        //                foreach(var res in result)
        //                {
        //                    string ReferenceId = _context.PlatformTable.Where(x=>x.OrderNumber == res.ordernumber && x.SyncStatus.Equals("Sync") && x.Fulfillment == null).Select(x=>x.ReferenceId).FirstOrDefault();

        //                    ShopifyHelper shopifyHelper = new ShopifyHelper();

        //                    string fulfilmentOrderId = await shopifyHelper.CreateFulfillment(ReferenceId);

        //                    var payload = new FulfillmentRequestModel
        //                    {
        //                        fulfillment = new FulfillmentRequest
        //                        {
        //                            line_items_by_fulfillment_order = new List<FulfillmentLineItem>
        //                            {
        //                                new FulfillmentLineItem
        //                                {
        //                                    fulfillment_order_id = fulfilmentOrderId
        //                                }
        //                            }
        //                        }
        //                    };

        //                    var JsonSerialize =  JsonConvert.SerializeObject(payload);

        //                    var isPosted = await shopifyHelper.PostFulfillment(JsonSerialize);

        //                    var platformRow = _context.PlatformTable
        //                          .FirstOrDefault(x => x.ReferenceId == ReferenceId);

        //                    if (isPosted.Contains("status= closed"))
        //                    {
        //                        if (platformRow != null)
        //                        {
        //                            platformRow.Fulfillment = "Fulfilled";   // UPDATE FIELD
        //                            platformRow.FulfilledDate = DateTime.Now; // optional
        //                            _context.SaveChanges();
        //                        }
        //                    }
        //                    else
        //                    {
        //                        var fulfillmentResponse =
        //                        JsonConvert.DeserializeObject<FulfillmentModel>(isPosted);

        //                        string status = fulfillmentResponse?.fulfillment?.status ?? "";


        //                        if (status.ToLower().Equals("success"))
        //                        {
        //                            if (platformRow != null)
        //                            {
        //                                platformRow.Fulfillment = "Fulfilled";   // UPDATE FIELD
        //                                platformRow.FulfilledDate = DateTime.Now; // optional
        //                                _context.SaveChanges();
        //                            }

        //                        }
        //                        else
        //                        {
        //                            platformRow.Message = "Fulfillment Failed";
        //                        }
        //                    }//else 
                                
        //                }




        //            }// end of result 

        //        }
             
        //        return Ok("success");
        //    }
        //    catch (Exception ex)
        //    {
                
        //        return BadRequest($"Operation failed. -  {ex.Message}");
             
        //    }
        //}
    }
}
