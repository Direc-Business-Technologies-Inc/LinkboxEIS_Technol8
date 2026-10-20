using DataAccessLayer.Class;
using DomainLayer.ViewModels;
using LinkBoxUI.Helpers;
using LinkBoxUI.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("linkbox")]
    public class GetController : ApiController
    {
        public SqlHelpers sql = new SqlHelpers();
        public SAPAccess sapAces = new SAPAccess();
        GetGlobalServices getGlobalServices = new GetGlobalServices();
        [HttpGet]
        [Route("sap/get")]
        public HttpResponseMessage GetData(AuthenticationCredViewModel auth)
        {
            bool blnLogin = false;
            var response = new HttpResponseMessage();
            sapAces.SaveCredentials(auth);
            blnLogin = SAPAccess.LoginAction();
            if (blnLogin == true)
            {
                string ret = sapAces.SendSLData(auth);
                JObject json = JObject.Parse(ret);
                response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(json.ToString(), System.Text.Encoding.UTF8, "application/json");
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.Unauthorized);
                response.Content = new StringContent("Unauthorized Login Credential");
            }

            return response;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("linkbox/api/forall")]
        public IHttpActionResult Get()
        {
            return Ok("Now server time is: " + DateTime.Now.ToString());
        }

        //[Authorize]
        //[HttpGet]
        //[Route("linkbox/api/authenticate")]
        //public IHttpActionResult GetForAuthenticate()
        //{
        //    var identity = (ClaimsIdentity)User.Identity;
        //    return Ok("Hello " + identity.Name);
        //}

        [SwaggerOperation(Tags = new[] { "BusinessPartner" })]
        [Authorize]
        [HttpGet]
        [Route("linkbox/apitoken/getbpmaster")]
        public IHttpActionResult GetItemMaster()
        {
            try
            {
                var identity = (ClaimsIdentity)User.Identity;

                string JSONresult;
                var res = getGlobalServices.GetData("linkbox/apitoken/getbpmaster");

                //JSONresult = JsonConvert.SerializeObject(res);
                var dict = new Dictionary<string, string>();
                if (res == null)
                {
                    dict.Add("error", "No available setup.");
                    JSONresult = JsonConvert.SerializeObject(dict);
                    var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                    return ResponseMessage(response);
                }
                else
                {
                    JSONresult = JsonConvert.SerializeObject(res);
                }

                return Ok(JsonConvert.DeserializeObject(JSONresult));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [SwaggerOperation(Tags = new[] { "ItemMasterData" })]
        [HttpGet]
        [Route("linkbox/apikey/getItems")]
        public HttpResponseMessage GetQueryData()
        {
            try
            {
                string JSONresult = "";
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                var res = getGlobalServices.GetData("linkbox/apikey/getItems");

                var dict = new Dictionary<string, string>();
                if (res == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    dict.Add("error", "No available setup.");
                    JSONresult = JsonConvert.SerializeObject(dict);
                    response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                    return response;
                }
                else
                {
                    JSONresult = JsonConvert.SerializeObject(res);
                }
                response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        [Route("api/getexcelsheet")]
        public HttpResponseMessage getexcelsheet(string FileName)
        {
            try
            {
                string JSONresult = "";
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                var res = DataAccess.GetExcelSheet(FileName);
                JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        [Route("api/getexcelcolumn")]
        public HttpResponseMessage getexcelcolumn(string FileName, string HeadStart, string RowStart,string SheetName)
        {
            try
            {
                string JSONresult = "";
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                ExcelMapperViewModel.ExcelMapperModel model = new ExcelMapperViewModel.ExcelMapperModel();

                model.HeaderRow = HeadStart;
                model.DataRowStart = RowStart;
                model.Worksheet = SheetName;
                
                var res = DataAccess.GetExcelColumn(FileName, model);
                JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //if(JSONresult =="{}")
                //{
                //    model.Worksheet = "Export Worksheet";
                //    res = DataAccess.GetExcelColumn(FileName, model);
                //    JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //}
                response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        [Route("api/onegetexcelcolumn")]
        public HttpResponseMessage onegetexcelcolumn(string FileName, string HeadStart, string RowStart, string SheetName)
        {
            try
            {
                string JSONresult = "";
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                ExcelMapperViewModel.ExcelMapperModel model = new ExcelMapperViewModel.ExcelMapperModel();

                model.HeaderRow = HeadStart;
                model.DataRowStart = RowStart;
                model.Worksheet = SheetName;

                var res = DataAccess.GetExcelColumn(FileName, model);
                JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //if(JSONresult =="{}")
                //{
                //    model.Worksheet = "Export Worksheet";
                //    res = DataAccess.GetExcelColumn(FileName, model);
                //    JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //}
                response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        [Route("api/NSgetExcelcolumn")]
        public HttpResponseMessage getExcelcolumn(string Path, string ExcelName)
        {
            try
            {
                string JSONresult = "";
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                ExcelMapperViewModel.ExcelMapperModel model = new ExcelMapperViewModel.ExcelMapperModel();

                //model.HeaderRow = HeadStart;
                //model.DataRowStart = RowStart;
                //model.Worksheet = SheetName;

                var res = DataAccess.PNGetExcelColumn(Path, ExcelName);
                JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //if(JSONresult =="{}")
                //{0
                //    model.Worksheet = "Export Worksheet";
                //    res = DataAccess.GetExcelColumn(FileName, model);
                //    JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //}
                response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        [Route("api/NSTransactionColumns")]
        public HttpResponseMessage getNetsuitecolumn(string Path, string table)
        {
            try
            {
                string JSONresult = "";
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                ExcelMapperViewModel.ExcelMapperModel model = new ExcelMapperViewModel.ExcelMapperModel();

                //model.HeaderRow = HeadStart;
                //model.DataRowStart = RowStart;
                //model.Worksheet = SheetName;

                var res = DataAccess.ReadTextFile(Path, table);
                JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //if(JSONresult =="{}")
                //{0
                //    model.Worksheet = "Export Worksheet";
                //    res = DataAccess.GetExcelColumn(FileName, model);
                //    JSONresult = JsonConvert.SerializeObject(res, Formatting.Indented);
                //}
                response.Content = new StringContent(JSONresult, Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

       


    }
}
