using ClosedXML.Excel;
using DataAccessLayer.Class;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Properties;
using LinkBoxUI.Services;
using LinkBoxUI.Services.Lazada;
using LinkBoxUI.Services.Shopee;
using LinkBoxUI.Services.Shopify;
using LinkBoxUI.Services.Tiktok;
using LinkBoxUI.Services.Zalora;
using LinkBoxUI.SwaggerDocuments;
using LinqToExcel.Extensions;
using Microsoft.Graph.Models.Security;
using Microsoft.Owin.Security.Provider;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Remotion.Mixins;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Results;
namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("Netsuite")]
    public class NetsuiteController : ApiController
    {
        NetsuiteService netsuiteServfices = new NetsuiteService();
        LinkboxDb _context = new LinkboxDb();
        ShopifyToNetsuite shopifyToNetsuite = new ShopifyToNetsuite();
        ShopifyFullfillmentService shopifyFullfillmentService   = new ShopifyFullfillmentService();
        NsInventoryItemToShopifyService nsInventoryItemToShopifyService = new NsInventoryItemToShopifyService();
        ZaloraSalesOrderService zaloraservice = new ZaloraSalesOrderService();

        ShopifyTrackingService shopifyTrackingService = new ShopifyTrackingService();
        ZaloraTrackingService zaloraTrackingService = new ZaloraTrackingService();
        LazadaTrackingService LazadaTrackingService = new LazadaTrackingService();
        TiktokTrackingService tiktokTrackingService = new TiktokTrackingService();
        ShopeeTrackingService shopeeTrackingService = new ShopeeTrackingService();



        ShopifyImageService imageService = new ShopifyImageService();
        // ShopifyToNetsuite shopifyToNetsuite = new ShopifyToNetsuite();
        //[HttpGet]
        //[Route("api/GetNetsuiteColumn")]
        //public async Task<HttpResponseMessage> GetNetsuiteSalesOrderColumn(string module, string id, string expand)
        //{
        //    try
        //    {
        //        string webapiurl = ConfigurationManager.AppSettings["WebApiUrl"];
        //        // Construct the URL for the POST request
        //        string url = $"{webapiurl}/api/Netsuite/GetNetsuiteColumn";
            
        //        // Create a JSON object to send in the request body
        //        var requestBody = new JObject
        //        {
        //            { "module", module },
        //            { "id", id },
        //            { "expand", expand }
        //        };

        //        var handler = new HttpClientHandler()
        //        {
        //            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        //        };

        //        // Create HttpClient and send the POST request
        //        using (var client = new HttpClient(handler))
        //        {
        //            // Set necessary headers (e.g., Content-Type)
        //            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //            // Convert the request body to a string and create a StringContent object
        //            var content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");

        //            // Send POST request
        //            HttpResponseMessage response = await client.PostAsync(url, content);

        //            // Check if the response is successful
        //            if (response.IsSuccessStatusCode)
        //            {
        //                // Read the response content as JSON
        //                string jsonResponse = await response.Content.ReadAsStringAsync();

        //                // Parse the response JSON
        //                JObject parsedResponse = JObject.Parse(jsonResponse);

        //                // Check if the response contains 'error' and if it's true
        //                if (parsedResponse["error"].Value<bool>())
        //                {
        //                    // If there's an error, return a response with the message
        //                    return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        //                    {
        //                        Content = new StringContent($"Error: {parsedResponse["message"]}")
        //                    };
        //                }

        //                // Extract the 'data' array from the response
        //                JArray data = (JArray)parsedResponse["data"];

        //                // If 'data' is not empty, return it
        //                if (data != null && data.Count > 0)
        //                {
        //                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        //                    {
        //                        Content = new StringContent(data.ToString())
        //                    };
        //                }
        //                else
        //                {
        //                    // Return a response indicating no data found
        //                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
        //                    {
        //                        Content = new StringContent("No data found.")
        //                    };
        //                }
        //            }
        //            else
        //            {
        //                // Handle unsuccessful response
        //                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        //                {
        //                    Content = new StringContent("An error occurred while calling the API.")
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Return a response for the exception case
        //        //return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        //        //{
        //        //    Content = new StringContent($"An error occurred: {ex.Message}")
        //        //};
        //        return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        //        {
        //            Content = new StringContent($"An error occurred: {ex.Message} - Stack Trace: {ex.StackTrace}")
        //        };
        //    }
        //}

        [HttpGet]
        [Route("api/GetNetsuiteColumn")]
        public async Task<HttpResponseMessage> GetNetsuiteSalesOrderColumn(string module, string id, string expand)
        {
            var columns = new JArray
    {
        // Identification / references
        "id",
        "tranId",
        "externalId",
        "entity",           // customer
        "subsidiary",
        "location",
        "class",
        "department",

        // Dates
        "tranDate",
        "shipDate",
        "dueDate",

        // Status / workflow
        "orderStatus",
        "status",

        // Financials
        "currency",
        "exchangeRate",
        "subtotal",
        "discountTotal",
        "discountItem",
        "discountRate",
        "taxTotal",
        "taxItem",
        "shippingCost",
        "handlingCost",
        "total",

        // Payment
        "terms",
        "paymentMethod",
        "creditCard",
        "pnRefNum",         // payment reference number

        // Shipping
        "shipMethod",
        "shipAddress",
        "shippingAddress",
        "shipIsResidential",

        // Billing
        "billAddress",
        "billingAddress",

        // Sales team / channel
        "salesRep",
        "salesTeam",
        "leadSource",
        "partner",

        // Line items
        "item",

        // Memo / custom body fields (your integration-specific ones)
        "memo",
        "custbody_customername",
        "custbody_createtime",
        "custbody_ordernumber",
        "custbody_paymethod",
        "custbody_source",
        "custbody_status"
    };

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(columns.ToString(), Encoding.UTF8, "application/json")
            };
        }



        //[Authorize]
        [HttpGet]
        [Route("shopify/post")]
        public async Task<IHttpActionResult> PostShopifyToNetsuite(string Code)
        {
                var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
            var mapIds = await _context.Schedules
                .Where(schedule => schedule.SchedCode == Code) // Ensure we filter by the schedule code
                .Join(
                    _context.Process,
                    schedule => schedule.Process,
                    process => process.ProcessCode,
                    (schedule, process) => new { schedule, process }
                )
                .Join(
                    _context.ProcessMap,
                    sp => sp.process.ProcessId,
                    pm => pm.ProcessId,
                    (sp, pm) => pm.MapId
                )
                .ToArrayAsync();
            try
            {

                if (proc.RunTime == null)
                {
                    proc.RunTime = DateTime.Now.AddMinutes(-8);
                    _context.SaveChanges();
                }

                var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == Code).Select(x => x.RunTime).FirstOrDefault();
                var currtime = DateTime.Now;

                if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                {
                    proc.IsRunning = false;
                    _context.SaveChanges();

                }

                var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == Code).Select(sel => sel.IsRunning).FirstOrDefault();

                if (isrunproc == false)
                {
                    proc.IsRunning = true;
                    proc.RunTime = DateTime.Now;
                    _context.SaveChanges();

                    shopifyToNetsuite.Request = new HttpRequestMessage();
                    shopifyToNetsuite.Configuration = new HttpConfiguration();

                    var result = await shopifyToNetsuite.Post(mapIds,Code);

                    if (result is  BadRequestErrorMessageResult badRequestResult)
                    {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Upload_Status", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{result}", Json = "", Database = "", Table = "", Module = "" });
                            _context.SaveChanges();
                    }

                }
                        proc.IsRunning = false;
                        _context.SaveChanges();

                        return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "Task Error",
                    CreateDate = DateTime.Now,
                    ApiUrl = "",
                    ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
                    Json = "",
                    Database = "",
                    Table = "",
                    Module = ""
                });
                proc.IsRunning = false;
                _context.SaveChanges();
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("shopify/Manualpost")]
        public async Task<IHttpActionResult> ManualPostShopifyToNetsuite(string Code, string DateFrom = null, string DateTo = null)
        {
            var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
            var mapIds = await _context.Schedules
                .Where(schedule => schedule.SchedCode == Code) // Ensure we filter by the schedule code
                .Join(
                    _context.Process,
                    schedule => schedule.Process,
                    process => process.ProcessCode,
                    (schedule, process) => new { schedule, process }
                )
                .Join(
                    _context.ProcessMap,
                    sp => sp.process.ProcessId,
                    pm => pm.ProcessId,
                    (sp, pm) => pm.MapId
                )
                .ToArrayAsync();
            try
            {

                if (proc.RunTime == null)
                {
                    proc.RunTime = DateTime.Now.AddMinutes(-8);
                    _context.SaveChanges();
                }

                var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == Code).Select(x => x.RunTime).FirstOrDefault();
                var currtime = DateTime.Now;

                if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                {
                    proc.IsRunning = false;
                    _context.SaveChanges();

                }

                var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == Code).Select(sel => sel.IsRunning).FirstOrDefault();

                if (isrunproc == false)
                {
                    proc.IsRunning = true;
                    proc.RunTime = DateTime.Now;
                    _context.SaveChanges();

                    shopifyToNetsuite.Request = new HttpRequestMessage();
                    shopifyToNetsuite.Configuration = new HttpConfiguration();

                    var result = await shopifyToNetsuite.Post(mapIds, Code,DateFrom,DateTo);

                    if (result is BadRequestErrorMessageResult badRequestResult)
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Upload_Status", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{result}", Json = "", Database = "", Table = "", Module = "" });
                        _context.SaveChanges();
                    }

                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "Task Error",
                    CreateDate = DateTime.Now,
                    ApiUrl = "",
                    ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
                    Json = "",
                    Database = "",
                    Table = "",
                    Module = ""
                });
                proc.IsRunning = false;
                _context.SaveChanges();
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("shopify/fulfillment")]
        public async Task<IHttpActionResult> NetsuiteShopifyFulfillment(int id)
        {

            var apiSync = _context.CrystalExtractSetup
                         .FirstOrDefault(x => x.Id == id);

            if (apiSync != null)
            {

                string Code = apiSync.Name;
                var proc = _context.Schedules.Where(x => x.Process == Code).FirstOrDefault();
                try
                {

                    if (proc.RunTime == null)
                    {
                        proc.RunTime = DateTime.Now.AddMinutes(-8);
                        _context.SaveChanges();
                    }

                    var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.Process == Code).Select(x => x.RunTime).FirstOrDefault();
                    var currtime = DateTime.Now;

                    if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                    {
                        proc.IsRunning = false;
                        _context.SaveChanges();

                    }

                    var isrunproc = _context.Schedules.Where(sel => sel.Process == Code).Select(sel => sel.IsRunning).FirstOrDefault();

                    if (isrunproc == false)
                    {
                        proc.IsRunning = true;
                        proc.RunTime = DateTime.Now;
                        _context.SaveChanges();

                        shopifyToNetsuite.Request = new HttpRequestMessage();
                        shopifyToNetsuite.Configuration = new HttpConfiguration();

                        var result = await shopifyFullfillmentService.CheckFulfillment();

                        //if (result is BadRequestErrorMessageResult badRequestResult)
                        //{
                        //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "NetsuiteShopifyFulfillment", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{result}", Json = "", Database = "", Table = "", Module = "" });
                        //    _context.SaveChanges();
                        //}

                    }
                        proc.IsRunning = false;
                        _context.SaveChanges();

                    return Ok("Process completed successfully.");


                }
                catch (Exception ex)
                {

                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                    {
                        Task = "Task Error",
                        CreateDate = DateTime.Now,
                        ApiUrl = "",
                        ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
                        Json = "",
                        Database = "",
                        Table = "",
                        Module = ""
                    });
                    proc.IsRunning = false;
                    _context.SaveChanges();
                    return InternalServerError(ex);
                }

            }
                    return Ok("apiSync Not Found");

        }

        [HttpGet]
        [Route("shopify/syncItems")]
        public async Task<IHttpActionResult> NetsuiteItemSyncToShopify(string Code)
        {
            var dt = DateTime.Now;
            var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
            var mapIds = await _context.Schedules
                .Where(schedule => schedule.SchedCode == Code) // Ensure we filter by the schedule code
                .Join(
                    _context.Process,
                    schedule => schedule.Process,
                    process => process.ProcessCode,
                    (schedule, process) => new { schedule, process }
                )
                .Join(
                    _context.ProcessMap,
                    sp => sp.process.ProcessId,
                    pm => pm.ProcessId,
                    (sp, pm) => pm.MapId
                )
                .ToArrayAsync();
            try
            {

                if (proc.RunTime == null)
                {
                    proc.RunTime = DateTime.Now.AddMinutes(-8);
                    _context.SaveChanges();
                }

                var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == Code).Select(x => x.RunTime).FirstOrDefault();
                var currtime = DateTime.Now;

                if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                {
                    proc.IsRunning = false;
                    _context.SaveChanges();

                }

                var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == Code).Select(sel => sel.IsRunning).FirstOrDefault();

                if (isrunproc == false)
                {
                    proc.IsRunning = true;
                    proc.RunTime = DateTime.Now;
                    _context.SaveChanges();

                    shopifyToNetsuite.Request = new HttpRequestMessage();
                    shopifyToNetsuite.Configuration = new HttpConfiguration();

           
                        await nsInventoryItemToShopifyService.GetNsInventoryItemToShopify(mapIds, dt);


                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }   
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "Task Error",
                    CreateDate = DateTime.Now,
                    ApiUrl = "",
                    ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
                    Json = "",
                    Database = "",
                    Table = "",
                    Module = ""
                });
                proc.IsRunning = false;
                _context.SaveChanges();
                return InternalServerError(ex);
            }

        }


        //Ilipat sa 
        [HttpGet]
        [Route("api/shopify/syncImage")]
        public async Task<IHttpActionResult> NetsuiteImageSyncToShopify(int id)//string Code
        {
            var dt = DateTime.Now;
            var proc = _context.Schedules
                    .Join(
                        _context.CrystalExtractSetup,
                        us => us.Process,          // UploadSchedules.Process
                        ce => ce.Name,             // CrystalExtractSetups.Name
                        (us, ce) => new
                        {
                            UploadSchedule = us,
                            CrystalExtract = ce
                        }
                    )
                    .Where(x => x.CrystalExtract.Id == id)
                    .FirstOrDefault();


            try
            {

                if (proc.UploadSchedule.RunTime == null)
                {
                    proc.UploadSchedule.RunTime = DateTime.Now.AddMinutes(-8);
                    _context.SaveChanges();
                }

                var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == proc.UploadSchedule.SchedCode).Select(x => x.RunTime).FirstOrDefault();
                var currtime = DateTime.Now;

                if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                {
                    proc.UploadSchedule.IsRunning = false;
                    _context.SaveChanges();

                }

                var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == proc.UploadSchedule.SchedCode).Select(sel => sel.IsRunning).FirstOrDefault();

                if (isrunproc == false)
                {
                    proc.UploadSchedule.IsRunning = true;
                    proc.UploadSchedule.RunTime = DateTime.Now;
                    _context.SaveChanges();


                    int scritpt = 677;
                    string platform = proc.CrystalExtract.QueryCode;
                    string sinceDate = DateTime.Now.ToString("yyyy-MM-dd");

                    //await nsInventoryItemToShopifyService.GetParentHeader_Images(scritpt,platform, proc.UploadSchedule.SchedCode);

                    var res = await imageService.SyncImageToShopify();

                }
                proc.UploadSchedule.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "NetsuiteImageSyncToShopify",
                    CreateDate = DateTime.Now,
                    ApiUrl = "api/shopify/syncImage",
                    ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
                    Json = "",
                    Database = "",
                    Table = "",
                    Module = ""
                });
                proc.UploadSchedule.IsRunning = false;
                _context.SaveChanges();
                return InternalServerError(ex);
            }

        }
        [HttpGet]
        [Route("tracking")]
        public async Task<IHttpActionResult> TrackingNumberUpdate(int id)
        {

            var apiSync = _context.CrystalExtractSetup
                         .FirstOrDefault(x => x.Id == id);

            if (apiSync != null)
            {

                string Code = apiSync.Name;
                var proc = _context.Schedules.Where(x => x.Process == Code).FirstOrDefault();
                try
                {

                    if (proc.RunTime == null)
                    {
                        proc.RunTime = DateTime.Now.AddMinutes(-8);
                        _context.SaveChanges();
                    }

                    var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.Process == Code).Select(x => x.RunTime).FirstOrDefault();
                    var currtime = DateTime.Now;

                    if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                    {
                        proc.IsRunning = false;
                        _context.SaveChanges();

                    }

                    var isrunproc = _context.Schedules.Where(sel => sel.Process == Code).Select(sel => sel.IsRunning).FirstOrDefault();

                     if (isrunproc == false)
                    {
                        proc.IsRunning = true;
                        proc.RunTime = DateTime.Now;
                        _context.SaveChanges();
                        var result = false;

                        switch (apiSync.QueryCode?.ToString())
                        {
                            case "Shopify":
                                result = await shopifyTrackingService.ShopifyUpdateTracking();
                                break;

                            case "Zalora":
                                result = await zaloraTrackingService.ZaloraUpdateTracking();
                                break;

                            case "Lazada":
                                result = await LazadaTrackingService.LazadaUpdateTracking();
                                break;

                            case "Shopee":
                                result = await shopeeTrackingService.ShopeeUpdateTracking();
                                break;

                            case "Tiktok":
                                 result = await tiktokTrackingService.TiktokUpdateTracking();
                                break;

                            default:
                                result = false;
                                break;
                        }

                    }
                    proc.IsRunning = false;
                    _context.SaveChanges();

                    return Ok("Process completed successfully.");


                }
                catch (Exception ex)
                {

                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                    {
                        Task = "Task Error",
                        CreateDate = DateTime.Now,
                        ApiUrl = "",
                        ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
                        Json = "",
                        Database = "",
                        Table = "",
                        Module = ""
                    });
                    proc.IsRunning = false;
                    _context.SaveChanges();
                    return InternalServerError(ex);
                }

            }
            return Ok("apiSync Not Found");

        }



    }

}
