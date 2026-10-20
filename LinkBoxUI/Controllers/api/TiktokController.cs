
using LinkBoxUI.Context;
using LinkBoxUI.Services.Tiktok;
using LinkBoxUI.Services.Zalora;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("Tiktok")]
    public class TiktokController : ApiController
    {

        //private readonly TiktokSalesOrderService service = new TiktokSalesOrderService();
        private readonly TiktokFullfillmentService fullfillmentService = new TiktokFullfillmentService();
        private readonly TiktokItemProductService itemProductService = new TiktokItemProductService();

        private readonly TikTokSalesOrderServiceSAP tiktoksap = new TikTokSalesOrderServiceSAP();
        private readonly GetRefreshToken tokenService = new GetRefreshToken();
        LinkboxDb _context = new LinkboxDb();

        //[HttpGet]
        //[Route("api/PostOrder")]
        //public async Task<IHttpActionResult> PostSalesOrder(string Code)
        //{
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
                    
        //            var service = new TiktokSalesOrderService(new APISetup());

        //            var response = await service.TiktokOrderPostToNetsuite(mapIds, Code);

        //        }
        //        proc.IsRunning = false;
        //        _context.SaveChanges();

        //        return Ok("Process completed successfully.");

        //    }
        //    catch (Exception ex)
        //    {

        //        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //        {
        //            Task = "GetZaloraOrders",
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
        //[HttpGet]
        //[Route("api/ManualPostOrder")]
        //public async Task<IHttpActionResult> ManualPostOrder(string Code, string DateFrom = null, string DateTo = null)
        //{
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

        //            var response = await service.TiktokOrderPostToNetsuite(mapIds, Code,DateFrom,DateTo);

        //        }
        //        proc.IsRunning = false;
        //        _context.SaveChanges();

        //        return Ok("Process completed successfully.");

        //    }
        //    catch (Exception ex)
        //    {

        //        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //        {
        //            Task = "TiktokSalesOrder",
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
        [Route("api/GetColumnOrder")]
        public async Task<HttpResponseMessage> GetColumnOrder(string ordernumber,string code)
        {
            try
            {

                var apiSetup = _context.APISetups.FirstOrDefault(x => x.APICode == code);
                if (apiSetup == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, $"No API setup found for code '{code}'.");

               var _service= new TiktokSalesOrderService(apiSetup);

                var json = await _service.GetColumn(ordernumber);
                JObject parsed = JObject.Parse(json);

                JArray items = (JArray)parsed["data"]["orders"];
                if (items == null || items.Count == 0)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No orders found.");

                JObject fullOrder = (JObject)items[0];

                var keys = new List<string>();
                FlattenKeys(fullOrder, "", keys);

                var response = new
                {
                    Data = keys,
                    Code = "0",
                    Message = "Success"
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"An error occurred: {ex.Message}");
            }
        }

        private void FlattenKeys(JToken token, string prefix, List<string> keys)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    string path = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    string typeMarker = GetTypeMarker(prop.Value);
                    keys.Add(path + typeMarker);
                    FlattenKeys(prop.Value, path, keys);
                }
            }
            else if (token is JArray arr)
            {
                if (arr.Count > 0)
                {
                    string path = $"{prefix}[]";
                    FlattenKeys(arr[0], path, keys);
                }
            }
        }

        private string GetTypeMarker(JToken value)
        {
            if (value.Type == JTokenType.Object) return " {}";
            if (value.Type == JTokenType.Array) return " []";
            return ""; // leaf value (string, number, bool, null) — no marker
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

                        await fullfillmentService.CheckFulfillment();

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
        [Route("api/PostItem")]
        public async Task<IHttpActionResult> PostItem(int id)
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

                        await  itemProductService.PostItemtoTiktok(Code,dt);


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
        [Route("api/PostOrderSAP")]
        public async Task<IHttpActionResult> PostOrderSAP(string Code, string DateFrom = null, string DateTo = null)
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

                    var response = await tiktoksap.TiktokOrderPostToSAP(mapIds, Code, DateFrom, DateTo);

                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "TiktokSalesOrder",
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
        [Route("api/GenerateAccessToken")]
        public async Task<IHttpActionResult> GenerateAccessToken(string appKey, string appSecret, string authCode)
        {
            try
            {
                var result = await tokenService._GenerateAccessToken(appKey, appSecret, authCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Error : Failed to generate access token.- {ex}"));
            }

        }
    }
}