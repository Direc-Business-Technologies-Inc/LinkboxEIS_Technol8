using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.Word;
using DomainLayer.Models;
using DomainLayer.Models.Shopify;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Properties;
using LinkBoxUI.Services;
using LinkBoxUI.Services.Shopify;
using LinkBoxUI.SwaggerDocuments;
using LinqToExcel.Extensions;
using Microsoft.Owin.Security.Provider;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Remotion.Mixins;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Http;

namespace LinkBoxUI.Controllers.api
{

    [RoutePrefix("Shopify")]
    public class ShopifyController : ApiController
    {
        NetsuiteService netsuiteServices = new NetsuiteService();
        LinkboxDb _context = new LinkboxDb();
        NsInventoryItemToShopifyService nsInventoryItemToShopifyService = new NsInventoryItemToShopifyService();
        ShopifyTrackingService shopifyTrackingService = new ShopifyTrackingService();

        //FieldMapping Get Column
        [HttpGet]
        [Route("api/GetShopifyOrderColumn")]
        public async Task<HttpResponseMessage> GetShopifyOrder(string module)
        {
            try
            {
                var APISetup = _context.APISetups.Where(x => x.APICode == "ShopifyAPI").FirstOrDefault();

                //string apiKey = APISetup.APIKey;
                //string SecretKey = APISetup.APISecretKey;
                string accessToken = APISetup.APIToken;

                string url = $"{APISetup.APIURL}/{module}.json?limit=1";

                // Shopify API access token (use your actual token here)



                //string authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{accessToken}"));
                using (var client = new HttpClient())
                {
                    // Set Authorization header with the Shopify Access Token
                    // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // Send GET request asynchronously
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Check if the response is successful
                    if (!response.IsSuccessStatusCode)
                    {
                        return Request.CreateErrorResponse(response.StatusCode, $"Error: {response.ReasonPhrase}");
                    }

                    // Read the response content
                    string json = await response.Content.ReadAsStringAsync();
                    JObject parsed = JObject.Parse(json);
                    JArray orders = (JArray)parsed[module];

                    if (orders.Count == 0)
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No orders found.");

                    // RETURN THE ENTIRE FIRST ORDER OBJECT
                    //JObject fullOrder = (JObject)orders[0];

                    JObject fullOrder = (JObject)orders[0];

                    // Extract ONLY the KEYS
                    var keys = fullOrder.Properties().Select(p => p.Name).ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, keys);

                    //return Request.CreateResponse(HttpStatusCode.OK, fullOrder);
                }
            }
            catch (Exception ex)
            {
                // Return a generic error message if an exception occurs
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
            }
        }


        //Get Sales Order for Posting based on Parameter on FieldMapping
            [HttpGet]
            [Route("api/GetShopifySalesOrder")]
            public async Task<IHttpActionResult> GetShopifySalesOrder(int MapId, int ApiCode, string DateFrom = null, string DateTo = null)
            {
                try
                {
                    var APISetup = _context.APISetups.Where(x => x.APIId == ApiCode).FirstOrDefault();
                    var DocParam = _context.DocumentMaps.Where(x => x.DocId == MapId && x.UpdateUserId == ApiCode).ToList();
                    string apiKey = APISetup.APIKey;
                    string SecretKey = APISetup.APISecretKey;
                    string accessToken = APISetup.APIToken;

                    var urlBuilder = new StringBuilder($"https://{apiKey}:{SecretKey}@strokes-beauty.myshopify.com/admin/api/2025-10/orders.json");

                     bool isManual = !string.IsNullOrEmpty(DateFrom) && !string.IsNullOrEmpty(DateTo);

                if (isManual)
                {
                    if (!DateTime.TryParse(DateFrom, out var parsedFrom) ||
                        !DateTime.TryParse(DateTo, out var parsedTo))
                    {
                        return BadRequest("Invalid date format for DateFrom or DateTo.");
                    }

                    var dateStart = parsedFrom.Date.ToString("yyyy-MM-ddTHH:mm:sszzz");
                    var dateEnd = parsedTo.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz");

                    urlBuilder.Append($"?created_at_min={dateStart}&created_at_max={dateEnd}&status=Any");
                }
                else
                {
                    var isFirstParam = true;
                    foreach (var param in DocParam)
                    {
                        string paramvalue = param.QueryField.ToString().Contains("DateTime.Now")
                            ? InterpretDateExpression(param.QueryField.ToString())
                            : param.QueryField.ToString();

                        string separator = isFirstParam ? "?" : "&";
                        urlBuilder.Append($"{separator}{param.CrystalParam.ToString()}={paramvalue}");
                        isFirstParam = false;
                    }
                }
                    
                    string url = urlBuilder.ToString();

                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        HttpResponseMessage response = await client.GetAsync(url);

                        if (!response.IsSuccessStatusCode)
                        {
                            return Content(response.StatusCode, $"Error: {response.ReasonPhrase}");
                        }

                        string json = await response.Content.ReadAsStringAsync();
                        var jsonObj = JsonConvert.DeserializeObject<object>(json);
                        return Json(jsonObj);
                    }
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }

        //Get ITem Sku per variant
        [HttpGet]
        [Route("api/GetItemSku")]
        public async Task<IHttpActionResult> GetItemSku(int MapId, int ApiCode,string productId,string variantId)
        {
            try
            {
                var APISetup = _context.APISetups.Where(x => x.APIId == ApiCode).FirstOrDefault();
                //var DocParam = _context.DocumentMaps.Where(x => x.DocId == MapId && x.UpdateUserId == ApiCode).ToList();
                string apiKey = APISetup.APIKey;
                string SecretKey = APISetup.APISecretKey;
                string accessToken = APISetup.APIToken;

                var urlBuilder = new StringBuilder($"https://{apiKey}:{SecretKey}@strokes-beauty.myshopify.com/admin/api/2025-10/products/{productId}/variants/{variantId}.json");


                string url = urlBuilder.ToString();

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        return Content(response.StatusCode, $"Error: {response.ReasonPhrase}");
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    var jsonObj = JsonConvert.DeserializeObject<ShopifyProductModel>(json);


                    return Json(jsonObj);

                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        public string InterpretDateExpression(string expression)
        {
            if (!string.IsNullOrWhiteSpace(expression))
            {
                var parts = expression.Split(',');

                // 1. Base token
                string baseToken = parts[0].Trim();
                DateTime baseDate;

                if (baseToken.Equals("DateTime.Now", StringComparison.OrdinalIgnoreCase))
                    baseDate = DateTime.Now;
                else
                    throw new ArgumentException($"Unknown base token: {baseToken}");

                // 2. Offset (default = days)
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    if (!int.TryParse(parts[1].Trim(), out int offset))
                        throw new ArgumentException($"Invalid numeric offset: {parts[1]}");

                    baseDate = baseDate.AddDays(offset);
                }

                // 3. Return formatted as yyyy-MM-dd
                return baseDate.ToString("yyyy-MM-dd");
            }
            return "";
        }


        //Post Image to shopify
        [HttpGet]
        [Route("api/shopify/image")]
        public async Task<IHttpActionResult> ShopifyPostImage(int id)//string Code
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


                    //int scritpt = 672;
                    //string platform = proc.CrystalExtract.QueryCode;
                    //string sinceDate = DateTime.Now.ToString("yyyy-MM-dd"); ;
                    await nsInventoryItemToShopifyService.PostShopifyImage(proc.UploadSchedule.SchedCode);


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

        //[HttpGet]
        //[Route("tracking")]
        //public async Task<IHttpActionResult> TrackingNumberUpdate(int id)
        //{

        //    var apiSync = _context.CrystalExtractSetup
        //                 .FirstOrDefault(x => x.Id == id);

        //    if (apiSync != null)
        //    {

        //        string Code = apiSync.Name;
        //        var proc = _context.Schedules.Where(x => x.Process == Code).FirstOrDefault();
        //        try
        //        {

        //            if (proc.RunTime == null)
        //            {
        //                proc.RunTime = DateTime.Now.AddMinutes(-8);
        //                _context.SaveChanges();
        //            }

        //            var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.Process == Code).Select(x => x.RunTime).FirstOrDefault();
        //            var currtime = DateTime.Now;

        //            if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
        //            {
        //                proc.IsRunning = false;
        //                _context.SaveChanges();

        //            }

        //            var isrunproc = _context.Schedules.Where(sel => sel.Process == Code).Select(sel => sel.IsRunning).FirstOrDefault();

        //            if (isrunproc == false)
        //            {
        //                proc.IsRunning = true;
        //                proc.RunTime = DateTime.Now;
        //                _context.SaveChanges();

                    

        //                var result = await shopifyTrackingService.ShopifyUpdateTracking();

        //                //if (result is BadRequestErrorMessageResult badRequestResult)
        //                //{
        //                //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "NetsuiteShopifyFulfillment", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{result}", Json = "", Database = "", Table = "", Module = "" });
        //                //    _context.SaveChanges();
        //                //}

        //            }
        //            proc.IsRunning = false;
        //            _context.SaveChanges();

        //            return Ok("Process completed successfully.");


        //        }
        //        catch (Exception ex)
        //        {

        //            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //            {
        //                Task = "Task Error",
        //                CreateDate = DateTime.Now,
        //                ApiUrl = "",
        //                ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
        //                Json = "",
        //                Database = "",
        //                Table = "",
        //                Module = ""
        //            });
        //            proc.IsRunning = false;
        //            _context.SaveChanges();
        //            return InternalServerError(ex);
        //        }

        //    }
        //    return Ok("apiSync Not Found");

        //}

        //[HttpGet]
        //[Route("api/image")]
        //public async Task<IHttpActionResult> ShopifyPostImage(int id)//string Code
        //{
        //    var dt = DateTime.Now;
        //    var proc = _context.Schedules
        //            .Join(
        //                _context.CrystalExtractSetup,
        //                us => us.Process,          // UploadSchedules.Process
        //                ce => ce.Name,             // CrystalExtractSetups.Name
        //                (us, ce) => new
        //                {
        //                    UploadSchedule = us,
        //                    CrystalExtract = ce
        //                }
        //            )
        //            .Where(x => x.CrystalExtract.Id == id)
        //            .FirstOrDefault();


        //    try
        //    {

        //        if (proc.UploadSchedule.RunTime == null)
        //        {
        //            proc.UploadSchedule.RunTime = DateTime.Now.AddMinutes(-8);
        //            _context.SaveChanges();
        //        }

        //        var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == proc.UploadSchedule.SchedCode).Select(x => x.RunTime).FirstOrDefault();
        //        var currtime = DateTime.Now;

        //        if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
        //        {
        //            proc.UploadSchedule.IsRunning = false;
        //            _context.SaveChanges();

        //        }

        //        var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == proc.UploadSchedule.SchedCode).Select(sel => sel.IsRunning).FirstOrDefault();

        //        if (isrunproc == false)
        //        {
        //            proc.UploadSchedule.IsRunning = true;
        //            proc.UploadSchedule.RunTime = DateTime.Now;
        //            _context.SaveChanges();


        //            //int scritpt = 672;
        //            //string platform = proc.CrystalExtract.QueryCode;
        //            //string sinceDate = DateTime.Now.ToString("yyyy-MM-dd"); ;
        //            await nsInventoryItemToShopifyService.PostShopifyImage(proc.UploadSchedule.SchedCode);


        //        }
        //        proc.UploadSchedule.IsRunning = false;
        //        _context.SaveChanges();

        //        return Ok("Process completed successfully.");

        //    }
        //    catch (Exception ex)
        //    {

        //        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //        {
        //            Task = "NetsuiteImageSyncToShopify",
        //            CreateDate = DateTime.Now,
        //            ApiUrl = "api/shopify/syncImage",
        //            ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
        //            Json = "",
        //            Database = "",
        //            Table = "",
        //            Module = ""
        //        });
        //        proc.UploadSchedule.IsRunning = false;
        //        _context.SaveChanges();
        //        return InternalServerError(ex);
        //    }

        //}
    }
            
}