using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using DomainLayer.Models.Zalora;
using LinkBoxUI.Context;
using LinkBoxUI.Services.Shopify;
using LinkBoxUI.Services.Zalora;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("Zalora")]
    public class ZaloraController : ApiController
    {
        ZaloraSalesOrderService zaloraservice = new ZaloraSalesOrderService();
        ZaloraItemProductServices zaloraItemService = new ZaloraItemProductServices();
        ZaloraAccessTokenService   zaloraAccessTokenService = new ZaloraAccessTokenService();
        ZaloraFullfillmentService zaloraFullfillmentService = new ZaloraFullfillmentService();  
        ZaloraTrackingService   zaloraTrackingService = new ZaloraTrackingService();
        LinkboxDb _context = new LinkboxDb();
        [HttpGet]
        [Route("api/GetShopifySalesOrder")]
        public async Task<IHttpActionResult> GetShopifySalesOrder(int MapId, int ApiCode)
        {
            //NetsuiteService netsuiteServices = new NetsuiteService();
            LinkboxDb _context = new LinkboxDb();
            try
            {
                //var APISetup = _context.APISetups.Where(x => x.APIId == ApiCode).FirstOrDefault();
                //var DocParam = _context.DocumentMaps.Where(x => x.DocId == MapId && x.UpdateUserId == ApiCode).ToList();
                //string apiKey = APISetup.APIKey;
                //string SecretKey = APISetup.APISecretKey;
                //string accessToken = APISetup.APIToken;


                //var urlBuilder = new StringBuilder($"https://{apiKey}:{SecretKey}@strokes-beauty.myshopify.com/admin/api/2025-10/orders.json");

                //var isFirstParam = true;
                //foreach (var param in DocParam)
                //{
                //    string paramvalue = param.QueryField.ToString().Contains("DateTime.Now")
                //        ? InterpretDateExpression(param.QueryField.ToString())
                //        : param.QueryField.ToString();

                //    string separator = isFirstParam ? "?" : "&";
                //    urlBuilder.Append($"{separator}{param.CrystalParam.ToString()}={paramvalue}");
                //    isFirstParam = false;
                //}

                //string url = urlBuilder.ToString();

                //using (var client = new HttpClient())
                //{
                //    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //    HttpResponseMessage response = await client.GetAsync(url);

                //    if (!response.IsSuccessStatusCode)
                //    {
                //        return Content(response.StatusCode, $"Error: {response.ReasonPhrase}");
                //    }

                //    string json = await response.Content.ReadAsStringAsync();
                //    var jsonObj = JsonConvert.DeserializeObject<object>(json);
                //return Json(jsonObj);
                //}
                return null;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

           
        }

        [HttpGet]
        [Route("api/Token")]
        public async Task<IHttpActionResult> GetZaloraAccessToken()
        {
            try
            {
                var response = await zaloraAccessTokenService.AccessToken();

                    return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
       

        [HttpGet]
        [Route("api/GetZaloraOrderColumn")]
        public async Task<HttpResponseMessage> GetZaloraColumn()
        {
            try
            {
                // 1️⃣ Get cached bearer token (will NOT re-call if valid)
                var accessToken = await zaloraAccessTokenService.AccessToken();

                var url =
                    $"https://sellercenter-api.zalora.com.ph/v2/orders" +
                    $"?limit=1";
                   

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken.ToString());

                    var response = await client.GetAsync(url);

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

                    JObject parsed = JObject.Parse(json);
                    JArray items = (JArray)parsed["items"];

                    if (items.Count == 0)
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No orders found.");

                    // RETURN THE ENTIRE FIRST ORDER OBJECT
                    //JObject fullOrder = (JObject)orders[0];

                    JObject fullOrder = (JObject)items[0];

                    // Extract ONLY the KEYS
                    var keys = fullOrder.Properties().Select(p => p.Name).ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, keys);
                }


            }
            catch (Exception ex)
            {
                // Return a generic error message if an exception occurs
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("api/PostSalesOrder")]
        public async Task<IHttpActionResult> PostSalesOrder(string Code)
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

                    var response = await zaloraservice.ZaloraOrderPostToNetsuite(mapIds, Code);

                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "GetZaloraOrders",
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
        [Route("api/ManualPostSalesOrder")]
        public async Task<IHttpActionResult> ManualPostSalesOrder(string Code, string DateFrom = null, string DateTo = null)
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

                    var response = await zaloraservice.ZaloraOrderPostToNetsuite(mapIds, Code,DateFrom,DateTo);

                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "GetZaloraOrders",
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
        //[HttpGet]
        //[Route("api/PostItem")]
        //public async Task<IHttpActionResult> PostItemProducts(string Code)
        //{
        //    var dt = DateTime.Now;
        //    var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
        //    var mapIds = await _context.Schedules
        //        .Where(schedule => schedule.SchedCode == Code) // Ensure we filter by the schedule code
        //        .Join(
        //            _context.Process,
        //            schedule => schedule.Process,
        //            process => process.ProcessCode,
        //            (schedule, process) => new { schedule, process }
        //        )
        //        .Join(
        //            _context.ProcessMap,
        //            sp => sp.process.ProcessId,
        //            pm => pm.ProcessId,
        //            (sp, pm) => pm.MapId
        //        )
        //        .ToArrayAsync();
        //    try
        //    {

        //        if (proc.RunTime == null)
        //        {
        //            proc.RunTime = DateTime.Now.AddMinutes(-8);
        //            _context.SaveChanges();
        //        }

        //        var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == Code).Select(x => x.RunTime).FirstOrDefault();
        //        var currtime = DateTime.Now;

        //        if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
        //        {
        //            proc.IsRunning = false;
        //            _context.SaveChanges();

        //        }

        //        var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == Code).Select(sel => sel.IsRunning).FirstOrDefault();

        //        if (isrunproc == false)
        //        {
        //            proc.IsRunning = true;
        //            proc.RunTime = DateTime.Now;
        //            _context.SaveChanges();

        //            await zaloraItemService.SyncItems(mapIds, dt);

        //        }
        //        proc.IsRunning = false;
        //        _context.SaveChanges();

        //        return Ok("Process completed successfully.");

        //    }
        //    catch (Exception ex)
        //    {

        //        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //        {
        //            Task = "Task Error",
        //            CreateDate = DateTime.Now,
        //            ApiUrl = "",
        //            ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
        //            Json = "",
        //            Database = "",
        //            Table = "",
        //            Module = ""
        //        });
        //        proc.IsRunning = false;
        //        _context.SaveChanges();
        //        return InternalServerError(ex);
        //    }

        //}

        [HttpGet]
        [Route("api/PostItem")]
        public async Task<IHttpActionResult> PostItemProducts(int id)
        {
            var dt = DateTime.Now;
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

                        await zaloraItemService.SyncItems(Code,dt);

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
        [Route("api/Fullfillment")]
        public async Task<IHttpActionResult> Fullfillment(int id)
        {
            var dt = DateTime.Now;
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

                        await zaloraFullfillmentService.CheckFulfillment();

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



        //                var result = await zaloraTrackingService.ZaloraUpdateTracking();


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
    }
}