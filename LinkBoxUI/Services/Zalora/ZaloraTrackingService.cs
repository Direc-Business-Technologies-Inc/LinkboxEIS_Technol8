
using DataAccessLayer.Class;
using LinkBoxUI.Context;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static LinkBoxUI.Services.Shopify.ShopifyTrackingService;

namespace LinkBoxUI.Services.Zalora
{
    public class ZaloraTrackingService
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        ZaloraAccessTokenService accessTokenService = new ZaloraAccessTokenService();   
        public async Task<bool> ZaloraUpdateTracking()
        {

            try
            {

                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == "Fulfilled" && x.Platform == "Zalora").Select(x => x.OrderNumber).ToList();
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
                                    IN ('211465427')
                                    --and custbody_status='Pending Billing'
                                    and entity = 323
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
                            string track_number = await GetTrackingNumber(platformRecord.ReferenceId);//platformRecord.ReferenceId
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

        private async Task<string> GetTrackingNumber(string ReferenceId)
        {
            if (string.IsNullOrEmpty(ReferenceId))
                return null;
            try
            {
                var APISetup = _context.APISetups.Where(x => x.APICode == "ShopifyAPI").FirstOrDefault();

                var accessToken = await accessTokenService.AccessToken();

                var urlBuilder = new StringBuilder($"https://sellercenter-api.zalora.com.ph/v2/orders/{ReferenceId}");

                string url = urlBuilder.ToString();

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync(url);

                    //if (!response.IsSuccessStatusCode)
                    //{
                    //    return Content(response.StatusCode, $"Error: {response.ReasonPhrase}");
                    //}

                    string json = await response.Content.ReadAsStringAsync();
                    var orderResponse = JsonConvert.DeserializeObject<ZaloraOrderResponse>(json);

                    string trackingNumber = string.Join(",",
                     orderResponse?.Items?
                         .Where(x => !string.IsNullOrWhiteSpace(x?.Shipment?.TrackingCode))
                         .Select(x => x.Shipment.TrackingCode.Trim())
                         .Distinct()
                     ?? Enumerable.Empty<string>()
                 );

                    return trackingNumber;

                    //return trackingNumbers != null && trackingNumbers.Any()
                    //    ? string.Join(",", trackingNumbers)
                    //    : "";
                }

            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
        }
       

        public class ZaloraOrderResponse
        {
            [JsonProperty("items")]
            public List<ZaloraOrderItem> Items { get; set; }
        }

        public class ZaloraOrderItem
        {
            [JsonProperty("shipment")]
            public ZaloraShipment Shipment { get; set; }
        }

        public class ZaloraShipment
        {
            [JsonProperty("trackingCode")]
            public string TrackingCode { get; set; }
        }

    }
}