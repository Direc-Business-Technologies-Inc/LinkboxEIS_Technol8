
using ClosedXML.Excel;
using DataAccessLayer.Class;
using DomainLayer.Models.Shopify;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace LinkBoxUI.Services.Shopify
{
    public class ShopifyFullfillmentService : ApiController
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();
        public async Task<bool> CheckFulfillment()
        {

            try
            {

                var order_numbers = _context.PlatformTable.Where(x => x.SyncStatus.Equals("Sync") && x.Fulfillment == null && x.Platform == "Shopify").Select(x => x.OrderNumber).ToList();
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
                                    and custbody_status='Pending Billing';"; //next time the status is not Pending Billing

                    var result = await NShelper.ListGetNetSuiteData(QueryEndpoint, Query);

                    if (result != null && result.Count > 0)
                    {
                        foreach (var res in result)
                        {
                            string ReferenceId = _context.PlatformTable.Where(x => x.OrderNumber == res.ordernumber && x.SyncStatus.Equals("Sync") && x.Fulfillment == null).Select(x => x.ReferenceId).FirstOrDefault();

                            ShopifyHelper shopifyHelper = new ShopifyHelper();

                            string fulfilmentOrderId = await shopifyHelper.CreateFulfillment(ReferenceId);

                            var payload = new FulfillmentRequestModel
                            {
                                fulfillment = new FulfillmentRequest
                                {
                                    line_items_by_fulfillment_order = new List<FulfillmentLineItem>
                                    {
                                        new FulfillmentLineItem
                                        {
                                            fulfillment_order_id = fulfilmentOrderId
                                        }
                                    }
                                }
                            };

                            var JsonSerialize = JsonConvert.SerializeObject(payload);

                            var isPosted = await shopifyHelper.PostFulfillment(JsonSerialize);

                            var platformRow = _context.PlatformTable
                                  .FirstOrDefault(x => x.ReferenceId == ReferenceId);

                            if (isPosted.Contains("status= closed"))
                            {
                                if (platformRow != null)
                                {
                                    platformRow.Fulfillment = "Fulfilled";   // UPDATE FIELD
                                    platformRow.FulfilledDate = DateTime.Now; // optional
                                    _context.SaveChanges();
                                }
                            }
                            else
                            {
                                var fulfillmentResponse =
                                JsonConvert.DeserializeObject<FulfillmentModel>(isPosted);

                                string status = fulfillmentResponse?.fulfillment?.status ?? "";


                                if (status.ToLower().Equals("success"))
                                {
                                    if (platformRow != null)
                                    {
                                        platformRow.Fulfillment = "Fulfilled";   // UPDATE FIELD
                                        platformRow.FulfilledDate = DateTime.Now; // optional
                                        _context.SaveChanges();
                                    }

                                }
                                else
                                {
                                    platformRow.Message = "Fulfillment Failed";
                                }
                            }//else 

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
    }
}