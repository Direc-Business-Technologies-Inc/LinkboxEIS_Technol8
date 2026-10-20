using ClosedXML.Excel;
using DataAccessLayer.Class;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Properties;
using LinkBoxUI.Services;
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
using System.Data.Entity;
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
    [RoutePrefix("Linkbox")]
    public class PostController : ApiController
    {
        public SqlHelpers sql = new SqlHelpers();
        NetsuiteHelper nshelper = new NetsuiteHelper();
        public SAPAccess sapAces = new SAPAccess();
        LinkboxDb _context = new LinkboxDb();
        GlobalServices globalServices = new GlobalServices();
        NetsuiteService netsuiteServices = new NetsuiteService();
        EmailHelpers eml = new EmailHelpers();
        NetsuiteHelper netsuiteHelper = new NetsuiteHelper();

        EmailSenderServiceHelper emailsender = new EmailSenderServiceHelper();

        [HttpPost]
        [Route("Post")]
        public bool Post()
        {
            //try
            //{
            //    //Properties.Settings prop = new Properties.Settings();
            //    Settings prop = new Settings();
            //    foreach (var task in TaskSchedulerHelpers.getRunningTask(prop.ServerName, prop.User, prop.Domain, prop.Password))
            //    {

            //        var EmailCreds = globalServices.GetEmailCredentials(task);


            //        if (EmailCreds.EmailCreds != null)
            //        {
            //            if (eml.Send(EmailCreds).ToLower().Contains("success"))
            //            {
            //                return true;
            //            }
            //            else
            //            {
            //                return false;
            //            }

            //        }
            //    }
            return true;
            //}
            //catch (Exception ex)
            //{
            //    return false;
            //}

        }
        [SwaggerOperation(Tags = new[] { "MOOMasterData" })]
        [HttpPost]
        [Route("moo/post/masterdata")]
        public string MOOMasterData(string taskname)
        {
            string ret = "";
            var _apimap = globalServices.MasterDataUploadSAPtoAPI(taskname);
            return ret;
        }

        [HttpPost]
        [Route("ManualPost")]
        public string ManualPost(string Code)
        {
            try
            {

                var EmailCreds = globalServices.GetEmailCredentials(Code);
                if (EmailCreds.EmailCreds != null)
                {
                    var query = EmailCreds.QueryDetails.QueryString;
                    var CheckLogs = sql.Fill_DataTable(EmailCreds, query);
                    if(CheckLogs!=null && CheckLogs.Rows.Count > 0)
                    {
                        return eml.Send(EmailCreds,"");
                    }
                    else
                    {
                        return $"{CheckLogs} result is null";
                    }
                }
                else
                {
                    return $"{EmailCreds} - EmailCreds is null";
                }
                    return "No Data";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        [HttpGet]
        [Route("api/EmailSender")]
        public async Task<IHttpActionResult> EmailSender(string Code)
        {
            var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
          
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

                     await emailsender.SendEmail(Code);

                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "Email Sender",
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



        [SwaggerOperation(Tags = new[] { "LoginToSAPDB" })]
        [HttpPost]
        [Route("sap/login")]
        public bool Login(AuthenticationCredViewModel auth)
        {
            bool result = false;

            if (auth != null)
            {
                sapAces.SaveCredentials(auth);
                result = SAPAccess.LoginAction();
            }

            return result;
        }

        [SwaggerOperation(Tags = new[] { "LoginService" })]
        [HttpPost]
        [Route("sap/post")]
        public string PostSL(AuthenticationCredViewModel auth)
        {
            string ret = "";
            bool blnLogin = false;
            sapAces.SaveCredentials(auth);
            blnLogin = SAPAccess.LoginAction();

            if (blnLogin == true)
            {
                ret = sapAces.SendSLData(auth);
            }
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "UploadDataSAPToAddon" })]
        [HttpPost]
        [Route("sap/upload/documents")]
        public string SapPostDocument(string taskname)
        {
            string ret = "";
            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_POSTDOCS", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
            _context.SaveChanges();
            var _apimap = globalServices.GetApiOrderPostSAP(taskname);

            return ret;
        }

        [SwaggerOperation(Tags = new[] { "UploadDataSAPToAddon" })]
        [HttpPost]
        [Route("sap/upload/documents")]
        public string ScheduleSapUploadDocument(string Code)
        {

            string ret = "";
            var mapcodes = _context.Schedules.Where(x => x.SchedCode == Code)
                                .Join(_context.Process, sc => sc.Process, pr => pr.ProcessCode, (sc, pr) => new { sc, pr })
                                .Join(_context.ProcessMap, scpr => scpr.pr.ProcessId, pm => pm.ProcessId, (scpr, pm) => new { scpr, pm })
                                .Join(_context.FieldMappings, scprpm => scprpm.pm.MapId, fm => fm.MapId, (scprpm, fm) => new
                                {
                                    MapCode = fm.MapCode,
                                }).ToList();
            string[] modules = mapcodes.Select(x => x.MapCode).ToArray();

            //Add Function
            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_POSTDOCS", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
            _context.SaveChanges();
            var _apimap = globalServices.GetSAPPostUploadtoAddon(modules);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "UploadDataSAPToAddon" })]
        [HttpPost]
        [Route("sap/upload/documents")]
        public string SapUploadDocument(string[] modules)
        {

            string ret = "";
            //Add Function
            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_POSTDOCS", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
            _context.SaveChanges();
            var _apimap = globalServices.GetSAPPostUploadtoAddon(modules);
            //var _apimap = globalServices.UploadOPSFiletoSap(modules);


            return ret;
        }

        [SwaggerOperation(Tags = new[] { "UploadDataSAPToAddon" })]
        [HttpPost]
        [Route("sap/upload/data")]
        //public string SaptoSapUploadDocument(string[] modules)
        public string SaptoSapUploadDocument(string modules)
        {
            string[] test = new string[1];
            test[0] = modules;
            string ret = "";
            //Add Function
            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_POSTDOCS", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
            _context.SaveChanges();
            var _apimap = globalServices.GetSAPPostUploadtoSAP(test);
            //var _apimap = globalServices.GetSAPPostUploadtoSAP(modules);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "ShopifyProducts" })]
        [HttpPost]
        [Route("shopify/post/items")]
        public string ShopifyPostProducts(string taskname)
        {
            string ret = "";
            var _apimap = globalServices.GetSAPPostShopiAPI(taskname);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "ZendeskTiket" })]
        [HttpPost]
        [Route("zendesk/post/tickets")]
        public string ZendeskPostTickets(string taskname)
        {
            string ret = "";
            var _apimap = globalServices.ZendeskPostTicket(taskname);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "MOOSalesOrder" })]
        [Authorize]
        [HttpPost]
        [Route("linkbox/post/apitoken/so")]
        public IHttpActionResult LinkboxAPI([FromBody] JObject jObj)
        {
            //dynamic newRetObj = new JObject();
            string res = "";
            try
            {
                if (jObj == null)
                {
                    var dict = new Dictionary<string, string>();
                    dict.Add("error", "Bad format");
                    string JSONresult = JsonConvert.SerializeObject(dict);
                    var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                    return ResponseMessage(response);
                }

                /// Multiple JObject Posting
                res = globalServices.PostJArraySO(jObj.ToString());
            }
            catch (Exception exc)
            {
                string msg = exc.Message;
                string exValid = "Unexpected JSON token";
                if (msg.Trim().ToLower().Contains(exValid.Trim().ToLower()))
                {
                    try
                    {
                        /// Single JObject Posting
                        res = globalServices.PostJObjectSO(jObj.ToString());
                    }
                    catch (Exception ex)
                    {
                        return InternalServerError(ex);
                    }
                }
                else
                {
                    return InternalServerError(exc);
                }
            }
            //return newRetObj;
            return Ok(res);
        }

        [SwaggerOperation(Tags = new[] { "ShopifyToSapBPCatalog" })]
        [HttpPost]
        [Route("sap/post/bpcatalog")]
        public string SapPostCatalog(string taskname)
        {
            string ret = "";
            var _apimap = globalServices.PostSapCatalog(taskname);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "ShopifyInventory" })]
        [HttpPost]
        [Route("shopify/post/inventory")]
        public string ShopifyPostInventory(string taskname)
        {
            string ret = "";
            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_INVTY", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
            _context.SaveChanges();
            var _apimap = globalServices.PostAPIInventory(taskname);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "UploadDataSAPToAddon" })]
        [HttpPost]
        [Route("sap2api/upload")]
        public string SaptoSapUpload(string code)
        {
            string ret = "";
            //Add Function
            var mapcodes = _context.Schedules.Where(x => x.SchedCode == code)
                    .Join(_context.Process, sc => sc.Process, pr => pr.ProcessCode, (sc, pr) => new { sc, pr })
                    .Join(_context.ProcessMap, scpr => scpr.pr.ProcessId, pm => pm.ProcessId, (scpr, pm) => new { scpr, pm })
                    .Join(_context.FieldMappings, scprpm => scprpm.pm.MapId, fm => fm.MapId, (scprpm, fm) => new
                    {
                        MapCode = fm.MapCode,
                    }).ToList();
            string[] modules = mapcodes.Select(x => x.MapCode).ToArray();
            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_POSTDOCS", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
            _context.SaveChanges();
            var _apimap = globalServices.GetSAPPostUploadtoSAP(modules);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "UploadFileToSap" })]
        [HttpPost]
        [Route("filetosap/upload")]
        public string FileUploadToSap(string Code)
        {
            string ret = "";
            var mapcodes = _context.Schedules.Where(x => x.SchedCode == Code)
                                .Join(_context.Process, sc => sc.Process, pr => pr.ProcessCode, (sc, pr) => new { sc, pr })
                                .Join(_context.ProcessMap, scpr => scpr.pr.ProcessId, pm => pm.ProcessId, (scpr, pm) => new { scpr, pm })
                                .Join(_context.FieldMappings, scprpm => scprpm.pm.MapId, fm => fm.MapId, (scprpm, fm) => new
                                {
                                    MapCode = fm.MapCode,
                                }).ToList();
            string[] modules = mapcodes.Select(x => x.MapCode).ToArray();

            //Add Function
            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_POSTDOCS", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
            _context.SaveChanges();
            var _apimap = globalServices.UploadFiletoSap(modules);
            return ret;
        }

        [SwaggerOperation(Tags = new[] { "UploadOPSFileToSap" })]
        [HttpPost]
        [Route("opsupload")]
        public string OPSFileUploadToSap(string Code)
        {
            string ret = "";

            var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();

            //if (_context.Schedules.Where(sel => sel.SchedCode == Code && sel.ForceStop == true).Any())
            //{ return "Stopping Process"; }

            if (proc.RunTime == null)
            {
                proc.RunTime = DateTime.Now.AddMinutes(-8);
                _context.SaveChanges();
            }

            var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == Code).Select(x => x.RunTime).FirstOrDefault();
            var currtime = DateTime.Now;

            if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
            {
                _context.SaveChanges();
                proc.IsRunning = false;
            }
            var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == Code).Select(sel => sel.IsRunning).FirstOrDefault();

            if (isrunproc == false)
            {

                var mapcodes = _context.Schedules.Where(x => x.SchedCode == Code)
                  .Join(_context.Process, sc => sc.Process, pr => pr.ProcessCode, (sc, pr) => new { sc, pr })
                  .Join(_context.ProcessMap, scpr => scpr.pr.ProcessId, pm => pm.ProcessId, (scpr, pm) => new { scpr, pm })
                  .Join(_context.OPSFieldMappings, scprpm => scprpm.pm.MapId, fm => fm.MapId, (scprpm, fm) => new
                  {
                      MapCode = fm.MapCode,
                      VisOrder = scprpm.pm.VisOrder,

                  }).ToList().OrderBy(x => x.VisOrder);

                string[] modules = mapcodes.Select(x => x.MapCode).ToArray();




                proc.IsRunning = true;
                proc.RunTime = DateTime.Now;
                _context.SaveChanges();
               

                try
                {
                    var _apimap = globalServices.UploadOPSFiletoSap(modules, Code);
                    ret = "Success!";
                }
                catch (Exception ex)
                {
                    ret = ex.Message;
                    var errmsg = $@"{ex.Message} | {ex.StackTrace}";
                    var _currDate = DateTime.Now.Date;
                    if (!ex.Message.ToLower().Contains("this method cannot be called until the send method has been called"))
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_SERVICE_EROR", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{ex.Message} | {ex.StackTrace}", Json = "", Database = "", Table = "", Module = "" });
                        _context.SaveChanges();
                    }
                }

                proc.IsRunning = false;
                _context.SaveChanges();

                var lastmsg = _context.SystemLogs.OrderByDescending(x => x.Id).Select(x => x.Task).FirstOrDefault();
                if (lastmsg.ToLower().Contains("upload_end"))
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_END", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                    _context.SaveChanges();
                }

            }//end of if

            return ret;


        }

        [HttpPost]
        [Route("sap/crexport")]
        public string CrystalExtract(int id)
        {

            string ret = "error";
            var crystalExtract = _context.CrystalExtractSetup
                         .FirstOrDefault(x => x.Id == id);

            if (crystalExtract != null)
            {
                string name = crystalExtract.Name;

                var proc = _context.Schedules.Where(x => x.Process == name).FirstOrDefault();

                if (proc.RunTime == null)
                {
                    proc.RunTime = DateTime.Now.AddMinutes(-8);
                    _context.SaveChanges();
                }

                var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.Process == name).Select(x => x.RunTime).FirstOrDefault();
                var currtime = DateTime.Now;

                if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                {
                    _context.SaveChanges();
                    proc.IsRunning = false;
                }

                var isrunproc = _context.Schedules.Where(sel => sel.Process == name).Select(sel => sel.IsRunning).FirstOrDefault();

                if (isrunproc == false)
                {

                    proc.IsRunning = true;
                    proc.RunTime = DateTime.Now;
                    _context.SaveChanges();


                    try
                    {
                        globalServices.ExtractCrystalReport(name);
                        ret = "Success!";
                    }
                    catch (Exception ex)
                    {
                        ret = ex.Message;
                        var errmsg = $@"{ex.Message} | {ex.StackTrace}";
                        var _currDate = DateTime.Now.Date;
                        if (!ex.Message.ToLower().Contains("this method cannot be called until the send method has been called"))
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "CRYSTAL_REPORT_EROR", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{ex.Message} | {ex.StackTrace}", Json = "", Database = "", Table = "", Module = "" });
                            _context.SaveChanges();
                        }
                    }

                    proc.IsRunning = false;
                    _context.SaveChanges();

                    var lastmsg = _context.SystemLogs.OrderByDescending(x => x.Id).Select(x => x.Task).FirstOrDefault();
                    if (lastmsg.ToLower().Contains("upload_end"))
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Crystal Extract End", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                        _context.SaveChanges();
                    }

                }

            }
            return ret;
        }

        [HttpGet]
        [Route("PN/extractPIR")]
        public async Task<string> extractPIR(string ReportName, string payrollperiod,string to_date)
        {

            if (string.IsNullOrEmpty(payrollperiod))
            {
                return "Payroll period cannot be null or Empty.";
            }

            string ret = "error";
            
            try
            {
                ret = await globalServices.PeopleNaveeReport(ReportName, payrollperiod,to_date);

                if (ret.Contains("error"))
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SHAREPOINT_UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{ret}", Json = "", Database = "", Table = "", Module = "" });
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
                if (!ex.Message.ToLower().Contains("this method cannot be called until the send method has been called"))
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SHAREPOINT_UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{ex.Message} | {ex.StackTrace}", Json = "", Database = "", Table = "", Module = "" });
                    _context.SaveChanges();
                }

            }
            finally
            {
                if (ret.Contains("error"))
                {
                    var code = "ECode";
                    var EmailCreds = globalServices.GetEmailCredentials(code);
                    var Filepath = "";
                    if (EmailCreds.EmailCreds != null)
                    {
                        var emailret = eml.Send(EmailCreds, Filepath);
                        if (emailret.ToLower().Contains("error"))
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Email_Error", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = emailret, Json = "", Database = "", Table = "", Module = "" });
                            _context.SaveChanges();
                        }
                    }
                }
            }

            return ret;
        }


        [HttpGet]
        [Route("PN/downloadFile")]
        public async Task<HttpResponseMessage> downloadFile(string ReportName, string payrollperiod,string to_date)
        {
            try
            {
                if (string.IsNullOrEmpty(payrollperiod))
                {
                    throw new Exception("Payroll period cannot be null or empty.");
                }

                DateTime date = DateTime.Parse(to_date);
                // Format the DateTime object to the desired format
                string formattedDate = date.ToString("MMMM dd, yyyy");
                var filename = $"AMC Semi_Payroll Instruction_{formattedDate}.xlsx";
                var fileBytes = await globalServices.PNdownloadFile(ReportName, payrollperiod);

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "File not found or no data available.");
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fileBytes)
                };

                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = filename // Customize the filename as needed
                };

                
                return response;
            }
            catch (Exception ex)
            {
                var errorResponse= new HttpResponseMessage(); ;
                if (ex.Message.Contains("Payroll period cannot be null or empty."))
                {
                    errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("Payroll period cannot be null or empty. Please try again."),
                        ReasonPhrase = "Internal Server Error"
                    };
                    errorResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                }
                else
                {
                     errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("An error occurred while generating the file. Please try again later."),
                        ReasonPhrase = "Internal Server Error"
                    };
                    errorResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                }
                // Log the exception if necessary
                return errorResponse;
            }
        }




        [SwaggerOperation(Tags = new[] { "UploadInvoiceToNetsuite" })]
        //[Authorize]
        [HttpPost]
        [Route("netsuite/post")]
        public async Task<string> PostNetsuite(string Code)
        {
            var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
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

                    var result = await netsuiteServices.NetsuitePost(Code);

                    if (result != "Success")
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Upload_Status", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $@"{result}", Json = "", Database = "", Table = "", Module = "" });
                        _context.SaveChanges();
                    }

                    proc.IsRunning = false;
                    _context.SaveChanges();

                    return result;
                }

                return "Invalid Code";
            }
            catch (Exception ex)
            {
                
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Task Error", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}", Json = "", Database = "", Table = "", Module = "" });
                _context.SaveChanges();
                proc.IsRunning = false;
                _context.SaveChanges();
                return ex.ToString();
            }
        }

        [HttpGet]
        [Route("api/getNSData")]
        public async Task<JsonResponseViewModel> GetNetSuiteData(string ApiId, string Query)
        {
            try
            {
                string JSONresult = "";
                var response = new HttpResponseMessage(HttpStatusCode.OK);

                int apiIdInt = int.Parse(ApiId); // Convert first

                string url = _context.APISetups.Where(x=>x.APIId == apiIdInt).Select(x=>x.APIURL).FirstOrDefault();
                var data = await netsuiteHelper.CallQueryServiceAsync(url, Query);
                //JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                var apiResponse = JsonConvert.DeserializeObject<JsonResponseViewModel>(data.ResponseMessage);

                //response.Content = new StringContent(data, Encoding.UTF8, "application/json");
                return apiResponse;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpGet]
        [Route("NS/GetLocalPath")]
        public async Task<object> GetlocalPath(string PathCode)
        {
            var LocalPath = _context.PathSetup.Where(x => x.PathCode == PathCode).Select(x => new { x.LocalPath , x.BackupPath}).FirstOrDefault();
            return LocalPath;
        }

        //[HttpGet]
        //[Route("api/GetShopifyOrderColumn")]
        //public async Task<HttpResponseMessage> GetShopifyOrder()
        //{
        //    try
        //    {
        //        var APISetup = _context.APISetups.Where(x => x.APICode == "ShopifyOrder").FirstOrDefault();

        //        string apiKey = APISetup.APIKey;
        //        string SecretKey = APISetup.APISecretKey;
        //        string accessToken = APISetup.APIToken;

        //        string url = $"https://{apiKey}:{SecretKey}@strokes-beauty.myshopify.com/admin/api/2025-10/orders.json?limit=1";

        //        // Shopify API access token (use your actual token here)

             

        //        //string authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{accessToken}"));
        //        using (var client = new HttpClient())
        //        {
        //            // Set Authorization header with the Shopify Access Token
        //           // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        //            client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
        //            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //            // Send GET request asynchronously
        //            HttpResponseMessage response = await client.GetAsync(url);

        //            // Check if the response is successful
        //            if (!response.IsSuccessStatusCode)
        //            {
        //                return Request.CreateErrorResponse(response.StatusCode, $"Error: {response.ReasonPhrase}");
        //            }

        //            // Read the response content
        //            string json = await response.Content.ReadAsStringAsync();
        //            JObject parsed = JObject.Parse(json);
        //            JArray orders = (JArray)parsed["orders"];

        //            if (orders.Count == 0)
        //                return Request.CreateResponse(HttpStatusCode.NotFound, "No orders found.");

        //            // RETURN THE ENTIRE FIRST ORDER OBJECT
        //            //JObject fullOrder = (JObject)orders[0];

        //            JObject fullOrder = (JObject)orders[0];

        //            // Extract ONLY the KEYS
        //            var keys = fullOrder.Properties().Select(p => p.Name).ToList();

        //            return Request.CreateResponse(HttpStatusCode.OK, keys);

        //            //return Request.CreateResponse(HttpStatusCode.OK, fullOrder);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Return a generic error message if an exception occurs
        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        //    }
        //}


        //[HttpGet]
        //[Route("api/GetNetsuiteSalesOrderColumn")]
        //public async Task<HttpResponseMessage> GetNetsuiteSalesOrderColumn()
        //{
        //    try
        //    {

        //        string url = $"https://{apiKey}:{SecretKey}@strokes-beauty.myshopify.com/admin/api/2025-10/orders.json?limit=1";

        //        // Shopify API access token (use your actual token here)



        //        //string authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{accessToken}"));
        //        using (var client = new HttpClient())
        //        {
        //            // Set Authorization header with the Shopify Access Token
        //            // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        //            client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
        //            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //            // Send GET request asynchronously
        //            HttpResponseMessage response = await client.GetAsync(url);

        //            // Check if the response is successful
        //            if (!response.IsSuccessStatusCode)
        //            {
        //                return Request.CreateErrorResponse(response.StatusCode, $"Error: {response.ReasonPhrase}");
        //            }

        //            // Read the response content
        //            string json = await response.Content.ReadAsStringAsync();
        //            JObject parsed = JObject.Parse(json);
        //            JArray orders = (JArray)parsed["orders"];

        //            if (orders.Count == 0)
        //                return Request.CreateResponse(HttpStatusCode.NotFound, "No orders found.");

        //            // RETURN THE ENTIRE FIRST ORDER OBJECT
        //            //JObject fullOrder = (JObject)orders[0];

        //            JObject fullOrder = (JObject)orders[0];

        //            // Extract ONLY the KEYS
        //            var keys = fullOrder.Properties().Select(p => p.Name).ToList();

        //            return Request.CreateResponse(HttpStatusCode.OK, keys);

        //            //return Request.CreateResponse(HttpStatusCode.OK, fullOrder);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Return a generic error message if an exception occurs
        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        //    }
        //}



    }
}
