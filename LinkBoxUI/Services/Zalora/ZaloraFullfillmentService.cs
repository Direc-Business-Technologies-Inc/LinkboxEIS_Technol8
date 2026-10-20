
using ClosedXML.Excel;
using DataAccessLayer.Class;
using DomainLayer.Models.Shopify;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Services.Shopify;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinkBoxUI.Services.Zalora
{
    public class ZaloraFullfillmentService
    {
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        LinkboxDb _context = new LinkboxDb();
        ZaloraAccessTokenService accessTokenService = new ZaloraAccessTokenService();
        public async Task<bool> CheckFulfillment()
        {
            try
            {

                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == null && x.Platform == "Zalora").Select(x => x.OrderNumber).ToList();
                if (order_numbers.Count > 0 && order_numbers != null)
                {
                    var orderNumbersCsv = string.Join(",", order_numbers.Select(n => $"'{n}'"));
                    //orderNumbersCsv = "277627747";
                    var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                    var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();

                    string Query = $@"SELECT 
                                       id,
                                       custbody_ordernumber as ordernumber,
                                       custbody_status as status
                                    FROM ""transaction""
                                    WHERE type = 'SalesOrd'
                                    AND REGEXP_REPLACE(TRIM(custbody_ordernumber), '[^0-9]', '') 
                                    IN ({orderNumbersCsv}) 
                                    and custbody_status='Pending Billing';";

                    var result = await nsHelper.ListGetNetSuiteData(QueryEndpoint, Query);

                    if (result != null && result.Count > 0)
                    {
                        foreach (var res in result)
                        {
                            string ReferenceId = _context.PlatformTable.Where(x => x.OrderNumber == res.ordernumber && x.SyncStatus.Equals("Sync") && x.Fulfillment == null && x.Platform=="Lazada").Select(x => x.ReferenceId).FirstOrDefault();

                            //ReferenceId = "5887966";
                            var fulfilmentOrderId = await GetZaloraOrderId(ReferenceId);


                            var obj = JObject.Parse(fulfilmentOrderId.Body);

                            var orderItemIds = obj["orderItemIds"]?.ToObject<List<long>>() ?? new List<long>();

                            var statusList = obj["statusList"] as JObject;

                            bool isCancelled = statusList?["canceled"] != null;

                            if (isCancelled)
                            {
                                var platformRow = _context.PlatformTable
                                  .FirstOrDefault(x => x.ReferenceId == ReferenceId);
                                if (platformRow != null)
                                {
                                    platformRow.Fulfillment = "Canceled";
                                    platformRow.FulfilledDate = DateTime.Now;
                                    _context.SaveChanges();
                                }
                                continue;
                            }

                            var payload = new
                            {
                                orderItems = orderItemIds
                                    .Select(id => new { id })
                                    .ToList()
                            };

                            var JsonSerialize = JsonConvert.SerializeObject(payload);


                            //var JsonSerialize = JsonConvert.SerializeObject(payload);

                            var isPosted = await PostFulfillment(JsonSerialize);

                            if (isPosted.IsSuccess)
                            {
                                var platformRow = _context.PlatformTable
                                .FirstOrDefault(x => x.ReferenceId == ReferenceId);
                                if (platformRow != null)
                                {
                                    platformRow.Fulfillment = "Fulfilled";   
                                    platformRow.FulfilledDate = DateTime.Now; 
                                    _context.SaveChanges();
                                }
                            }
                            else
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = $"Line : Code 113",
                                    ApiUrl = "await PostFulfillment(JsonSerialize)",
                                    Json = $"{JsonSerialize}",
                                    ErrorMsg = $"Error {isPosted.StatusCode}: {isPosted.ReasonPhrase} - {isPosted.Body}",
                                    CreateDate = DateTime.Now,
                                });
                                _context.SaveChanges();
                            }

                        }


                    }// end of result 

                }

               return true;
            }
            catch (Exception ex)
            {

                throw;

            }
        }
        public async Task<ZaloraApiResult> GetZaloraOrderId(string ordernumber)
        {
            try
            {

                var url = $"https://sellercenter-api.zalora.com.ph/v2/orders/{ordernumber}";

                var accessToken = await accessTokenService.AccessToken();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var response = await client.GetAsync(url);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new ZaloraApiResult
                        {
                            StatusCode = 404,
                            IsSuccess = true,
                            Body = responseBody
                        };
                    }

                    return new ZaloraApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody
                    };

                }
            }
            catch (Exception ex)
            {
                throw;
            }


        }
        public async Task<ZaloraApiResult> PostFulfillment(string json)
        {
        
            var url = "https://sellercenter-api.zalora.com.ph/v2/orders/statuses/set-to-ready-to-ship";

            var accessToken = await accessTokenService.AccessToken();

           
            using (var client = new HttpClient())
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                return new ZaloraApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    Body = responseBody
                };
            }
        }
        private class ZaloraOrderResponse
        {
            public long id { get; set; }
            public List<long> orderItemIds { get; set; }
        }
        private class ReadyToShipRequest
        {
            public List<OrderItemRef> orderItems { get; set; }
        }

        private class OrderItemRef
        {
            public long id { get; set; }
        }
    }
  
}