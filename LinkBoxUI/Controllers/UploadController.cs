using LinkBoxUI.SessionChecker;
using DomainLayer.ViewModels;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LinkBoxUI.Context;
using InfrastructureLayer.Repositories;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace LinkBoxUI.Controllers
{
    [SessionCheck]
    public class UploadController : Controller
    {
        private readonly IUploadRepository _uploadrepo;
        private readonly IGlobalRepository _globalrepo;

        public UploadController(IUploadRepository upload, IGlobalRepository global)
        {
            _uploadrepo = upload;
            _globalrepo = global;
        }

        LinkboxDb db = new LinkboxDb();
        // GET: Upload
        public ActionResult Uploader()
        {
            Session["ModuleAccess"] = 10;
            return View(_uploadrepo.GetUploadDetails());
            //return View(_uploadrepo.View_Upload());
        }
        public JsonResult GetModule(int Id)
        {
            return Json(_globalrepo.GetModules(Id));
        }

        public JsonResult GetDetails(string MapCode)
        {
            return Json(_uploadrepo.Get_Details(MapCode));
        }

        public JsonResult GetUploadDetails()
        {
            return Json(_uploadrepo.GetUploadDetails());
        }

        public JsonResult TryExecute(string MapCode)
        {

            string message = "";
            DataTable dt = _uploadrepo.GetData(MapCode);
            if (dt == null)
            {
                return Json(new { message, success = false, JsonRequestBehavior.AllowGet });
            }
            else
            {
                var JSONresult = JsonConvert.SerializeObject(dt);
                return Json(new { JSONresult, success = true, JsonRequestBehavior.AllowGet });
            }
        }

        #region SAP to SAP
        public JsonResult PopulateSAPtoSAPProcess(string ProcessCode)
        {
            return Json(_uploadrepo.GetSAPtoSAPProcess(ProcessCode));
        }
 
        public JsonResult GetSAPtoSAPProgress(string ProcessCode)
        {
            return Json(_uploadrepo.GetProgress(ProcessCode));
        }
        #endregion

        public JsonResult PopulateProcess(string ProcessCode)
        {
            return Json(_uploadrepo.GetProcess(ProcessCode));
        }

        public JsonResult GetProgress(string ProcessCode)
        {
            return Json(_uploadrepo.GetProgress(ProcessCode));
        }

        public async Task<JsonResult> Upload(string[] MapCode)
        {
            //return Json(await _uploadrepo.Upload(MapCode));
            return Json(await _uploadrepo.UploadMultiple(MapCode));
        }
        public async Task<JsonResult> UploadSAPtoSAP(string[] MapCode)
        {
            //return Json(await _uploadrepo.Upload(MapCode));
            return Json(await _uploadrepo.UploadMultipleSAPtoSAP(MapCode));

        }

        public async Task<JsonResult> UploadToPlatforms(string Code,string DateFrom,string DateTo)
        {
            //return Json(await _uploadrepo.Upload(MapCode));
            return Json(await _uploadrepo.UploadPlatForm(Code, DateFrom, DateTo));

        }



        //public JsonResult GetDeposit()
        //{

        //    return Json(_uploadrepo.Get_Deposit());
        //}
    }
}