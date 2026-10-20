using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using LinkBoxUI.Context;
using LinkBoxUI.Services.Lazada;
using LinkBoxUI.Services.Tiktok;
using LinkBoxUI.Services.Zalora;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("Lazada")]
    public class LazadaController : ApiController
    {
        LazadaToNetsuite lazadaService = new LazadaToNetsuite();
        LazadaTokenService tokenService = new LazadaTokenService();
        LazadaItemProductService lazadaItemService = new LazadaItemProductService();
        LazadaSalesOrderServiceSAP lazadasap = new LazadaSalesOrderServiceSAP();
        LinkboxDb _context = new LinkboxDb();

        // -------------------------------------------------------
        // GET Column Names — for Field Mapping UI dropdown
        // GET /Lazada/api/GetLazadaOrderColumn
        // -------------------------------------------------------
        [HttpGet]
        [Route("api/GetLazadaOrderColumn")]
        public async Task<HttpResponseMessage> GetLazadaOrderColumn()
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        (message, cert, chain, errors) => true;

                    using (var client = new HttpClient(handler))
                    {
                        // ← Get access token from refresh endpoint
                        //var token = await tokenService.CallRefreshEndpoint();
                        var token = await tokenService.CallRefreshEndpoint();

                        var apiPath = "/orders/get";
                        var baseUrl = "https://api.lazada.com.ph/rest";
                        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                        var createdAfter = "2023-01-01T00:00:00+08:00";


                        // ← Build params sorted alphabetically
                        var paramDict = new SortedDictionary<string, string>
                        {
                            { "access_token",  token.AccessToken },
                            { "app_key",       token.AppKey },
                            { "created_after", createdAfter },
                            { "sign_method",   "sha256" },
                            //{ "status",        "canceled" },
                            { "timestamp",     timestamp },
                            { "limit", "1"}
                        };

                        // ← Generate string to sign
                        var stringToSign = apiPath;
                        foreach (var kvp in paramDict)
                            stringToSign += kvp.Key + kvp.Value;

                        // ← Generate HMAC-SHA256 signature
                        var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                        // ← Build full URL
                        var queryString = string.Join("&",
                            paramDict.Select(kvp =>
                                $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                        var fullUrl = $"{baseUrl}{apiPath}?{queryString}&sign={sign}";

                        var response = await client.GetAsync(fullUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();
                            throw new HttpRequestException(
                                $"Lazada orders request failed. " +
                                $"Status: {(int)response.StatusCode}. Body: {errorBody}");
                        }

                        var json = await response.Content.ReadAsStringAsync();
                        JObject parsed = JObject.Parse(json);
                        JArray orders = (JArray)parsed["data"]?["orders"];

                        if (orders == null || orders.Count == 0)
                            return Request.CreateResponse(HttpStatusCode.NotFound, "No orders found.");

                        JObject firstOrder = (JObject)orders[0];
                        var keys = firstOrder.Properties().Select(p => p.Name).ToList();

                        return Request.CreateResponse(HttpStatusCode.OK, keys);
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    $"An error occurred: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // FULLFILLMENT
        // -------------------------------------------------------

        [HttpGet]
        [Route("api/Fullfillment")]
        public async Task<IHttpActionResult> CheckFulfillment(int id)
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

                        //await zaloraFullfillmentService.CheckFulfillment();
                        var fulfillmentService = new LazadaFullfillmentService();
                        await fulfillmentService.CheckFulfillment();
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

        // -------------------------------------------------------
        // POST Sales Orders — trigger Lazada → NetSuite sync
        // GET /Lazada/api/PostSalesOrder?Code=LazadaSO
        // -------------------------------------------------------
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

                    await lazadaService.Post(mapIds, Code);


                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "LazadaPostSalesOrder",
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

                    await lazadaService.Post(mapIds, Code, DateFrom, DateTo);


                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "LazadaPostSalesOrder",
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

                        //await itemProductService.PostItemtoTiktok(Code, dt);
                        await lazadaItemService.SyncItems(Code, dt);


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
        [Route("api/TestImage")]
        public async Task<IHttpActionResult> TestImage(string fileId)
        {
            try
            {
                var imageService = new LazadaImageService();
                var result = await imageService.GetImageDetails(fileId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // -------------------------------------------------------
        // Generate HMAC-SHA256 signature — same as Postman script
        // -------------------------------------------------------
        private string GenerateSignature(string stringToSign, string appSecret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(appSecret);
            var msgBytes = Encoding.UTF8.GetBytes(stringToSign);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }   


        [HttpGet]
        [Route("api/GetColumnOrder")]
        public async Task<HttpResponseMessage> GetColumnOrder(string ordernumber,string code)
        {
            try
            {
                var apiSetup = _context.APISetups.FirstOrDefault(x => x.APICode == code);
                if (apiSetup == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, $"No API setup found for code '{code}'.");

                var token = await tokenService._GenerateRefreshTokenAsync(apiSetup); // fetched once, reused for both calls
               
                if (token.AccessToken == "" && token.Code != "0")
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, token.Message);
                }
                using (var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                })
                using (var client = new HttpClient(handler))
                {
                    // --- Call 1: order/get ---
                    var orderJson = await CallLazadaApi(client, token, "/order/get",
                        new SortedDictionary<string, string> { { "order_id", ordernumber } });

                    var orderData = orderJson["data"];
                    if (orderData == null || !orderData.HasValues)
                        throw new Exception("No order found.");

                    // --- Call 2: order/items/get ---
                    var itemsJson = await CallLazadaApi(client, token, "/order/items/get",
                        new SortedDictionary<string, string> { { "order_id", ordernumber } });

                    var itemsData = itemsJson["data"];

                    // Collect column names only, merged into one list, arrays collapsed to "[]"
                    var seen = new HashSet<string>();
                    var columns = new List<string>();

                    FlattenKeys(orderData, "", seen, columns);
                    FlattenKeys(itemsData, "items", seen, columns);

                    var response = new
                    {
                        Data = columns,
                        Code = "0",
                        Message = "Success"
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, response);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        // Recursively collects dotted key paths only (no values, no per-element indices).
        // Arrays collapse every element into a single "[]" placeholder, since line items
        // (and any nested arrays) share the same shape regardless of how many there are.
        // "seen" dedupes so repeated array elements don't produce repeated columns.
        private void FlattenKeys(JToken token, string prefix, HashSet<string> seen, List<string> result)
        {
            if (token == null) return;

            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                        FlattenKeys(prop.Value, key, seen, result);
                    }
                    break;

                case JTokenType.Array:
                    var array = (JArray)token;
                    var arrayKey = $"{prefix}[]";
                    if (array.Count == 0)
                    {
                        // still record the array itself so the column exists even if empty
                        if (seen.Add(arrayKey)) result.Add(arrayKey);
                        break;
                    }
                    // only need to walk one representative element — every element has the same shape
                    FlattenKeys(array[0], arrayKey, seen, result);
                    break;

                case JTokenType.String:
                    var strVal = token.Value<string>();
                    if (LooksLikeJson(strVal) && TryParseJson(strVal, out var parsed))
                    {
                        FlattenKeys(parsed, prefix, seen, result); // expand nested JSON string, e.g. product_options
                    }
                    else if (seen.Add(prefix))
                    {
                        result.Add(prefix);
                    }
                    break;

                default:
                    if (seen.Add(prefix))
                    {
                        result.Add(prefix);
                    }
                    break;
            }
        }

        private async Task<JObject> CallLazadaApi(
          HttpClient client,
          LazadaTokenModel token,
          string apiPath,
          SortedDictionary<string, string> extraParams)
            {
                const string baseUrl = "https://api.lazada.com.ph/rest";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var paramDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key", token.AppKey },
                    { "sign_method", "sha256" },
                    { "timestamp", timestamp }
                };
            foreach (var kvp in extraParams)
                paramDict[kvp.Key] = kvp.Value;

            var stringToSign = apiPath;
            foreach (var kvp in paramDict)
                stringToSign += kvp.Key + kvp.Value;

            var sign = GenerateSignature(stringToSign, token.AppSecretKey);

            var queryString = string.Join("&",
                paramDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var fullUrl = $"{baseUrl}{apiPath}?{queryString}&sign={sign}";

            var response = await client.GetAsync(fullUrl);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Lazada request to {apiPath} failed. Status: {(int)response.StatusCode}. Body: {body}");

            return JObject.Parse(body);
        }

        private bool LooksLikeJson(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            return (s.StartsWith("{") && s.EndsWith("}")) || (s.StartsWith("[") && s.EndsWith("]"));
        }

        private bool TryParseJson(string s, out JToken parsed)
        {
            try
            {
                parsed = JToken.Parse(s);
                return true;
            }
            catch
            {
                parsed = null;
                return false;
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

                   var response = await lazadasap.LazadaOrderPostToSAP(mapIds, Code, DateFrom, DateTo);

                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "LazadaSalesOrder",
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

    }
}