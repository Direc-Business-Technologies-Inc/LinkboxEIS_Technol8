using LinkBoxUI.Context;
using LinkBoxUI.Services.Shopee;
using Microsoft.Owin.Security.Provider;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("Shopee")]
    public class ShopeeController : ApiController
    {
        ShopeeToNetsuite shopeeService = new ShopeeToNetsuite();
        ShopeeFullfillmentService fullfillmentService = new ShopeeFullfillmentService();
        ShopeeTokenService tokenService = new ShopeeTokenService();
        ShopeeItemProductService shopeeItemService = new ShopeeItemProductService();
        LinkboxDb _context = new LinkboxDb();

     

        private static string _baseUrl =>
       ConfigurationManager.AppSettings["Sh_baseUrl"];
        private static string _partnerId =>
            ConfigurationManager.AppSettings["Sh_partnerId"];
        private static string _partnerKey =>
            ConfigurationManager.AppSettings["Sh_partnerKey"];
        private static string _shopId =>
            ConfigurationManager.AppSettings["Sh_shopId"];

        private readonly string _orderDetailOptionalFields = string.Join(",", new[]
        {
            "total_amount",
            "estimated_shipping_fee",
            "actual_shipping_fee",
            "voucher_from_seller",
            "voucher_from_shopee",
            "payment_method",
            "buyer_username",
            "recipient_address",
            "pay_time",
            "shipping_carrier",
            "note",
            "cod",
            "item_list"
        });

        // -------------------------------------------------------
        // GET Column Names — for Field Mapping UI dropdown
        // GET /Shopee/api/GetShopeeOrderColumn
        // -------------------------------------------------------
        [HttpGet]
        [Route("api/GetShopeeOrderColumn")]
        public async Task<HttpResponseMessage> GetShopeeOrderColumn()
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        (message, cert, chain, errors) => true;

                    using (var client = new HttpClient(handler))
                    {
                        var token = await tokenService.CallRefreshEndpoint();

                        // Step 1: Get order list to find a sample order_sn
                        var listPath = "/api/v2/order/get_order_list";
                        var listTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var listTimeFrom = listTimestamp - (15 * 24 * 60 * 60);
                        var listSign = GenerateSignature(listPath, listTimestamp, token.AccessToken);

                        var listUrl = $"{_baseUrl}{listPath}" +
                                      $"?partner_id={_partnerId}" +
                                      $"&timestamp={listTimestamp}" +
                                      $"&sign={listSign}" +
                                      $"&access_token={token.AccessToken}" +
                                      $"&shop_id={_shopId}" +
                                      $"&time_range_field=create_time" +
                                      $"&time_from={listTimeFrom}" +
                                      $"&time_to={listTimestamp}" +
                                      $"&page_size=1" +
                                      $"&page_no=1" +
                                      $"&order_status=READY_TO_SHIP";

                        var listResponse = await client.GetAsync(listUrl);

                        if (!listResponse.IsSuccessStatusCode)
                        {
                            var errorBody = await listResponse.Content.ReadAsStringAsync();
                            throw new HttpRequestException(
                                $"Shopee order list request failed. " +
                                $"Status: {(int)listResponse.StatusCode}. Body: {errorBody}");
                        }

                        var listJson = await listResponse.Content.ReadAsStringAsync();
                        var listParsed = JObject.Parse(listJson);
                        var orders = listParsed["response"]?["order_list"] as JArray;

                        if (orders == null || orders.Count == 0)
                            return Request.CreateResponse(HttpStatusCode.NotFound, "No orders found.");

                        var firstOrderSn = orders[0]?["order_sn"]?.ToString();

                        if (string.IsNullOrEmpty(firstOrderSn))
                            return Request.CreateResponse(HttpStatusCode.NotFound, "No order_sn found.");

                        // Step 2: Get full order detail using the order_sn
                        var detailJson = await GetOrderDetailJsonAsync(client, firstOrderSn, token.AccessToken);
                        var detailParsed = JObject.Parse(detailJson);
                        var orderDetail = detailParsed["response"]?["order_list"] as JArray;

                        if (orderDetail == null || orderDetail.Count == 0)
                            return Request.CreateResponse(HttpStatusCode.NotFound, "No order detail found.");

                        var firstOrder = orderDetail[0] as JObject;
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
        // POST Sales Orders — trigger Shopee → NetSuite sync
        // GET /Shopee/api/PostSalesOrder
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

                    await shopeeService.Post(Code);


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

                    await shopeeService.Post(Code, DateFrom, DateTo);


                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "ShopeePostSalesOrder",
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
        //[HttpGet]
        //[Route("api/PostItem")]
        //public async Task<IHttpActionResult> PostItem()
        //{
        //    await shopeeItemService.SyncShopeItems();

        //    return Ok("Process completed successfully.");

        //}
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

                        await shopeeItemService.SyncShopeItems(Code,dt);


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
        // Get raw order detail JSON string — reusable across endpoints
        // Matches Postman script parameter order exactly
        // -------------------------------------------------------
        private async Task<string> GetOrderDetailJsonAsync(HttpClient client, string orderSn, string accessToken)
        {
            var path = "/api/v2/order/get_order_detail";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);

            var fullUrl = $"{_baseUrl}{path}" +
                          $"?partner_id={_partnerId}" +
                          $"&shop_id={_shopId}" +
                          $"&access_token={accessToken}" +
                          $"&timestamp={timestamp}" +
                          $"&order_sn_list={orderSn}" +
                          $"&response_optional_fields={_orderDetailOptionalFields}" +
                          $"&sign={sign}";

            var response = await client.GetAsync(fullUrl);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Shopee order detail request failed. " +
                    $"Status: {(int)response.StatusCode}. Body: {json}");

            return json;
        }

        // -------------------------------------------------------
        // Generate HMAC-SHA256 signature
        // shop-level: partner_id + path + timestamp + access_token + shop_id
        // -------------------------------------------------------
        private string GenerateSignature(string path, long timestamp, string accessToken)
        {
            var raw = $"{_partnerId}{path}{timestamp}{accessToken}{_shopId}";
            var keyBytes = Encoding.UTF8.GetBytes(_partnerKey);
            var msgBytes = Encoding.UTF8.GetBytes(raw);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}