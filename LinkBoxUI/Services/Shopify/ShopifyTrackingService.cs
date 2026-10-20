
using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Drawing.Charts;
using LinkBoxUI.Context;
using Newtonsoft.Json;
using OfficeOpenXml.VBA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LinkBoxUI.Services.Shopify
{
    public class ShopifyTrackingService
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        public async Task<bool> ShopifyUpdateTracking()
        {

            try
            {

                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == "Fulfilled" && x.Platform == "Shopify").Select(x => x.OrderNumber).ToList();
                if (order_numbers.Count > 0 && order_numbers != null)
                {
                    var orderNumbersCsv = string.Join(",", order_numbers.Select(n => $"'{n}'"));

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
                                    and custbody_status='Pending Billing'
                                    and entity = 348
                                    --and id = 1507791
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
                            var track_number = await ShopifyGetTrackingNumber(platformRecord.ReferenceId);//platformRecord.ReferenceId
                            if (!string.IsNullOrEmpty(track_number))
                            {
                                var payload = new
                                {
                                    custbody_trackingcode = track_number
                                }; 

                                string payloadJson = JsonConvert.SerializeObject(payload);
                                string postingResult = await NShelper.PostDataToNetSuite(PostDataEndpoint, payloadJson, "salesorder","PATCH", NetsuiteID);

                                if(postingResult.Contains("successfully posted"))
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

        public async Task<string> ShopifyGetTrackingNumber(string ReferenceId)
        {
            if (string.IsNullOrEmpty(ReferenceId))
                return null;
            try
            {
                var APISetup = _context.APISetups.Where(x => x.APICode == "ShopifyAPI").FirstOrDefault();
             
                string accessToken = APISetup.APIToken;

                var urlBuilder = new StringBuilder($"https://strokes-beauty.myshopify.com/admin/api/2025-10/orders/{ReferenceId}.json");

                string url = urlBuilder.ToString();

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync(url);

                    //if (!response.IsSuccessStatusCode)
                    //{
                    //    return Content(response.StatusCode, $"Error: {response.ReasonPhrase}");
                    //}

                    string json = await response.Content.ReadAsStringAsync();
                    var order = JsonConvert.DeserializeObject<ShopifyOrderResponse>(json);
                    string trackingNumber = order?.order?.fulfillments?.FirstOrDefault()?.tracking_number;

                    return trackingNumber ?? "";
                }

            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }

        public class ShopifyOrderResponse
        {
            public ShopifyOrder order { get; set; }
        }

        public class ShopifyOrder
        {
            public List<ShopifyFulfillment> fulfillments { get; set; }
        }

        public class ShopifyFulfillment
        {
            public string tracking_number { get; set; }
        }

    }
}