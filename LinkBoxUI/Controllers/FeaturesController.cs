
using LinkBoxUI.SessionChecker;
using DomainLayer.ViewModels;
using Newtonsoft.Json;
//using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Win32.TaskScheduler;
using LinkBoxUI.Context;
using DomainLayer;
using InfrastructureLayer.Repositories;
using DomainLayer.Models;
using LinkBoxUI.Controllers.api;

namespace LinkBoxUI.Controllers
{
    [SessionCheck]
    public class FeaturesController : Controller
    {
        // GET: Features
        LinkboxDb db = new LinkboxDb();

        private readonly IScheduleRepository _schedrepo;
        private readonly IProcessRepository _processrepo;
        private readonly ISyncRepository _syncrepo;
        private readonly IEmailRepository _emailrepo;
        private readonly IDepositRepository _depositrepo;
        private readonly IGlobalRepository _global;

        public FeaturesController(IScheduleRepository sched, IProcessRepository process,IDepositRepository deposit, 
                                  IGlobalRepository global, ISyncRepository sync, IEmailRepository email)
        {
            _schedrepo = sched;
            _processrepo = process;
            _syncrepo = sync;
            _emailrepo = email;
            _depositrepo = deposit;
            _global = global;
        }

        [UploadSchedCheck]
        public ActionResult Schedule()
        {
            Session["ModuleAccess"] = 8;
            ViewBag.Title = "Upload Schedule";
            return View(_schedrepo.View_Schedule());
        }
        [ProcessSetupCheck]
        public ActionResult Process()
        {
            Session["ModuleAccess"] = 7;
            ViewBag.Title = "Process Setup";
            return View(_processrepo.View_Process());
        }

        [DepositCheck]
        public ActionResult Deposit()
        {
            Session["ModuleAccess"] = 5;
           
            ViewBag.Title = "Deposit";
            return View(_depositrepo.View_Deposit());
        }

        [EmailSetupCheck]
        public ActionResult Email()
        {
            Session["ModuleAccess"] = 6;
            ViewBag.Title = "Email Setup";
            return View(_emailrepo.View_EmailSetup());
        }

        [SyncCheck]
        public ActionResult Sync()
        {
            Session["ModuleAccess"] = 9;
            ViewBag.Title = "Sync Setup";
            return View(_syncrepo.View_Sync());
        }

        [EmailSetupCheck]
        public ActionResult CreateEmail()
        {

            ViewBag.Title = "New Email";
            ViewBag.AddonCode = db.AddonSetup.Select(x => new EmailCreateViewModel.AddonSetup
            {
                AddonCode = x.AddonCode

            }).ToList();
            return View();
        }
        
        [EmailSetupCheck]
        public ActionResult CreateEmailTemplate(EmailCreateViewModel.EmailTemplate model)
        {
            return View();
        }

        [EmailSetupCheck]
        public ActionResult UpdateEmailTemplate(int Id)
        {
            ViewBag.Id = Id;
            return View(_emailrepo.Find_EmailTemplate(Id));
        }
        public JsonResult FindEmailTemplate(int Id)
        {
            return Json(_emailrepo.Find_EmailTemplate(Id));
        }
        public JsonResult ValidateEmailCode(string Code)
        {
            return Json(_emailrepo.ValidateEmailCode(Code));
        }

        [HttpPost]
        [EmailSetupCheck]
        public ActionResult AddEmailTemplate(EmailCreateViewModel.EmailTemplate model)
        {
            if (ModelState.IsValid)
            {
                if (db.EmailTemplate.Where(e => e.Code == model.Code).Any())
                {
                    ViewBag.WarningMessage = "Email Code is already Taken";
                }
                else
                {
                    //EmailTemplate eml = AutoMapper.Mapper.Map<EmailCreateViewModel.EmailTemplate, EmailTemplate>(model);
                    _emailrepo.Create_EmailTemplate(model, Convert.ToInt32(Session["Id"]));
                    _emailrepo.SaveChanges();
                    ViewBag.WarningMessage = "Successfully Created";
                    return View();
                }

            }
            return View(model);
        }

        //[HttpPost]
        //[EmailSetupCheck]
        //public ActionResult EmailTemplateUpdate(EmailCreateViewModel.EmailTemplate model)
        //{
        //    if (ModelState.IsValid)
        //    {

        //            //EmailTemplate eml = AutoMapper.Mapper.Map<EmailCreateViewModel.EmailTemplate, EmailTemplate>(model);
        //            _emailrepo.Update_EmailTemplate(model, Convert.ToInt32(Session["Id"]));
        //            _emailrepo.SaveChanges();
        //            ViewBag.WarningMessage = "Successfully Created";
        //            return View();
        //    }
        //    return View(model);
        //}
        [HttpPost]
        [EmailSetupCheck]
        public JsonResult EmailTemplateUpdate(EmailCreateViewModel.EmailTemplate model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return Json(new { success = false, message = string.Join("; ", errors) });
            }

            try
            {
                _emailrepo.Update_EmailTemplate(model, Convert.ToInt32(Session["Id"]));
                _emailrepo.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // log ex somewhere real — Serilog/NLog/etc.
                return Json(new { success = false, message = "An error occurred while updating the template." });
            }
        }

        [HttpPost]
        [EmailSetupCheck]
        public ActionResult CreateEmail(EmailCreateViewModel.Email model, string AddonCode, string[] Cc)
        {
            try
            {
                ViewBag.Title = "New Email";
                ViewBag.AddonCode = _emailrepo.Get_AddonSetup();
                if (ModelState.IsValid)
                {
                    if (db.Emails.Where(e => e.EmailCode == model.EmailCode).Any())
                    {
                        ViewBag.WarningMessage = "Email Code is already Taken";
                    }
                    else
                    {

                        EmailLogs eml = AutoMapper.Mapper.Map<EmailCreateViewModel.Email, EmailLogs>(model);
                        _emailrepo.Create_EmailSetup(eml, AddonCode, Cc, Convert.ToInt32(Session["Id"]));
                        _emailrepo.SaveChanges();
                        ViewBag.WarningMessage = "Successfully Created";
                        return View();
                    }

                }
                return View(model);
            }
            catch(Exception ex)
            {
                return null;
            }
            

        }

        //Email JSON Codes
        public JsonResult FindEmail(int Id)
        {
            return Json(_emailrepo.Find_EmailSetup(Id));
        }


        public JsonResult ListConnectionString()
        {
            return Json(_emailrepo.ListConnectionString());
        }

        [EmailSetupCheck]
        public JsonResult UpdateEmail(EmailCreateViewModel.Email model, string Addon, string[] Cc)
        {
            EmailLogs eml = AutoMapper.Mapper.Map<EmailCreateViewModel.Email, EmailLogs>(model);
            return Json(_emailrepo.Update_EmailSetup(eml,Addon,Cc,Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }

        [DepositCheck]
        //Deposit JSON Codes
        public JsonResult CreateDeposit(DepositCreateViewModel.Deposit model)
        {
            DomainLayer.Deposit deposit = AutoMapper.Mapper.Map<DepositCreateViewModel.Deposit, DomainLayer.Deposit>(model);
            return Json(_depositrepo.Create_Deposit(deposit, Convert.ToInt32(Session["Id"])));
        }

        public JsonResult FindDeposit(int Id)
        {            
            return Json(_depositrepo.Find_Deposit(Id));
        }

        [DepositCheck]
        public JsonResult UpdateDeposit(DepositCreateViewModel.Deposit model)
        {
            DomainLayer.Deposit deposit = AutoMapper.Mapper.Map<DepositCreateViewModel.Deposit, DomainLayer.Deposit>(model);
            return Json(_depositrepo.Update_Deposit(deposit,Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }

        [DepositCheck]
        public JsonResult ValidateCode(string Code)
        {
            return Json(_depositrepo.ValidateCode(Code));
        }

        [UploadSchedCheck]
        //Schedule JSON Codes
        public JsonResult CreateSchedule(SchedCreateViewMdel.Schedule model)
        {
            UploadSchedule schedule = AutoMapper.Mapper.Map<SchedCreateViewMdel.Schedule, UploadSchedule>(model);
            _schedrepo.Create_Schedule(schedule, Convert.ToInt32(Session["Id"]));
            _schedrepo.SaveChanges();
            return Json(true);
        }

        //public JsonResult OPSCreateSchedule(SchedCreateViewMdel.Schedule model)
        //{
        //    UploadSchedule schedule = AutoMapper.Mapper.Map<SchedCreateViewMdel.Schedule, UploadSchedule>(model);
        //    _schedrepo.OPSCreate_Schedule(schedule, Convert.ToInt32(Session["Id"]));
        //    _schedrepo.SaveChanges();
        //    return Json(true);
        //}

        public JsonResult FindSchedule(int Id, string Type)
        {
            return Json(_schedrepo.Find_Schedule(Id, Type));
        }
        public JsonResult RunSchedule(string Code)
        {
            return Json(_schedrepo.Run_Schedule(Code));
        }

        public JsonResult EnableSchedule(string Code)
        {
            return Json(_schedrepo.Enable_Schedule(Code));
        }

        public JsonResult DisableSchedule(string Code)
        {
            return Json(_schedrepo.Disable_Schedule(Code));
        }
        [UploadSchedCheck]
        public JsonResult UpdateSchedule(SchedCreateViewMdel.Schedule model)
        {
            UploadSchedule schedule = AutoMapper.Mapper.Map<SchedCreateViewMdel.Schedule, UploadSchedule>(model);
            bool json = _schedrepo.Update_Schedule(schedule, Convert.ToInt32(Session["Id"]));
            _schedrepo.SaveChanges();
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        [ProcessSetupCheck]
        //Process JSON Codes
        public JsonResult CreateProcess(ProcessCreateViewModel.process model, string [] FeatureMap)
        {
            ProcessSetup process = AutoMapper.Mapper.Map<ProcessCreateViewModel.process, ProcessSetup>(model);
            return Json(_processrepo.Create_Process(process, FeatureMap, Convert.ToInt32(Session["Id"])));
        }
        public JsonResult OPSCreateProcess(ProcessCreateViewModel.process model, string[] FeatureMap)
        {
            ProcessSetup process = AutoMapper.Mapper.Map<ProcessCreateViewModel.process, ProcessSetup>(model);
            return Json(_processrepo.OPSCreate_Process(process, FeatureMap, Convert.ToInt32(Session["Id"])));
        }

        public JsonResult FindProcess(int Id)
        {
            return Json(_processrepo.Find_Process(Id));
        }
        public JsonResult OPSFindProcess(int Id)
        {
            return Json(_processrepo.OPSFind_Process(Id));
        }

        [ProcessSetupCheck]
        public JsonResult UpdateProcess(ProcessCreateViewModel.process model, string [] FeatureMap)
        {
            ProcessSetup process = AutoMapper.Mapper.Map<ProcessCreateViewModel.process, ProcessSetup>(model);
            return Json(_processrepo.Update_Process(process, FeatureMap, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }

        public JsonResult OPSUpdateProcess(ProcessCreateViewModel.process model, string[] FeatureMap)
        {
            ProcessSetup process = AutoMapper.Mapper.Map<ProcessCreateViewModel.process, ProcessSetup>(model);
            return Json(_processrepo.OPSUpdate_Process(process, FeatureMap, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }

        [ProcessSetupCheck]
        public JsonResult ValidateProcess(string Code)
        {
            return Json(_processrepo.Validate_Process(Code));
        }
        public JsonResult OPSValidateProcess(string Code)
        {
            return Json(_processrepo.OPSValidate_Process(Code));
        }

        [ProcessSetupCheck]
        public JsonResult ValidateSchedule(string Code)
        {
            return Json(_schedrepo.Validate_Schedule(Code));
        }

        [SyncCheck]
        public JsonResult CreateSync(SyncViewModel.Sync model, string CheckPath)
        {
            Sync sync = AutoMapper.Mapper.Map<SyncViewModel.Sync, Sync>(model);
            return Json(_syncrepo.Create_SyncSetup(sync, CheckPath, Convert.ToInt32(Session["Id"])));
        }

        [SyncCheck]
        public JsonResult FindSync(int id)
        {
            return Json(_syncrepo.Find_SyncSteup(id));
        }

        [SyncCheck]
        public JsonResult UpdateSync(SyncViewModel.Sync model, string CheckPath)
        {
            Sync sync = AutoMapper.Mapper.Map<SyncViewModel.Sync, Sync>(model);
            return Json(_syncrepo.Update_SyncSetup(sync, CheckPath, Convert.ToInt32(Session["Id"])));
        }

        [SyncCheck]
        public JsonResult CreateQuery(SyncViewModel.Query model)
        {
            Query query = AutoMapper.Mapper.Map<SyncViewModel.Query, Query>(model);
            return Json(_syncrepo.Create_Query(query, Convert.ToInt32(Session["Id"])));
        }

        [SyncCheck]
        public JsonResult FindQuery(int Id)
        {
            return Json(_syncrepo.Find_Query(Id));
        }

        [SyncCheck]
        public JsonResult UpdateQuery(SyncViewModel.Query model)
        {
            QueryManager query = AutoMapper.Mapper.Map<SyncViewModel.Query, QueryManager>(model);
            return Json(_syncrepo.Update_Query(query,Convert.ToInt32(Session["Id"])));
        }

        [SyncCheck]
        public JsonResult CreateSyncQuery(SyncViewModel.SyncQuery model, int selectSyncId, int selectQueryId)
        {
            SyncQuery syncquery = AutoMapper.Mapper.Map<SyncViewModel.SyncQuery,SyncQuery>(model);
            return Json(_syncrepo.Create_SyncQuery(syncquery,selectSyncId, selectQueryId, Convert.ToInt32(Session["Id"])));
        }

        [SyncCheck]
        public JsonResult CreateCrystalQuery(string crystalCode, string selectQueryCode, int selectAPIId)
        {
            
            return Json(_syncrepo.Create_CrystalExtract(crystalCode, selectQueryCode, selectAPIId));
        }
        public JsonResult Create_ReportExtract(SyncViewModel.CrystalExtract model)
        {
            CrystalExtractSetup crystalquery = AutoMapper.Mapper.Map<SyncViewModel.CrystalExtract, CrystalExtractSetup>(model);
            return Json(_syncrepo.Create_ReportExtract(crystalquery,Convert.ToInt32(Session["Id"])));
        }
       

        [SyncCheck]
        public JsonResult FindSyncQuery(int id)
        {
            return Json(_syncrepo.Find_SyncQuery(id));
        }

        [SyncCheck]
        public JsonResult FindCrystalExtract(int id)
        {
            return Json(_syncrepo.Find_CrystalExtract(id));
        }
        [SyncCheck]
        public JsonResult FindReportExtract(int id)
        {
            return Json(_syncrepo.Find_CrystalExtract(id));
        }
      

        [SyncCheck]
        public JsonResult UpdateSyncQuery(SyncViewModel.SyncQuery model)
        {
            SyncQuery syncquery = AutoMapper.Mapper.Map<SyncViewModel.SyncQuery, SyncQuery>(model);
            return Json(_syncrepo.Update_SyncQuery(syncquery,Convert.ToInt32(Session["Id"])));
        }

        public JsonResult UpdateCrystalExtract(SyncViewModel.CrystalExtract model)
        {
            CrystalExtractSetup crystalextract = AutoMapper.Mapper.Map<SyncViewModel.CrystalExtract, CrystalExtractSetup>(model);
            return Json(_syncrepo.Update_CrystalExtract(crystalextract, Convert.ToInt32(Session["Id"])));
        }

        public JsonResult UpdateReportExtract(SyncViewModel.CrystalExtract model)
        {
            CrystalExtractSetup crystalextract = AutoMapper.Mapper.Map<SyncViewModel.CrystalExtract, CrystalExtractSetup>(model);
            return Json(_syncrepo.Update_CrystalExtract(crystalextract, Convert.ToInt32(Session["Id"])));
        }
        public JsonResult SyncValidateCode(string Code)
        {
            return Json(_syncrepo.Validate_Sync(Code));
        }


        public JsonResult TryExecute(int id)
        {
            var model = _syncrepo.Get_SyncQuery(id);

            string message = "";
            DataTable dt = _syncrepo.Fill_DataTable(model);
            var filetype = model.SyncQueryView.Select(x => x.FileType).FirstOrDefault();
            var path = model.SyncQueryView.Select(x => x.Path).FirstOrDefault();
          
            if (dt == null)
            {
                return Json(new { message, success = false,id ,JsonRequestBehavior.AllowGet });
            }
            else
            {
                var JSONresult = JsonConvert.SerializeObject(dt);
                return Json(new { JSONresult, success = true, filetype, path,id ,JsonRequestBehavior.AllowGet });
            }
        }

        public JsonResult TryExport(int id)
        {
            var model = _syncrepo.Get_SyncQuery(id);
            DataTable dt = _syncrepo.Fill_DataTable(model);
            _syncrepo.Export(dt, model);
            return Json("");

        }

        [SyncCheck]

        public JsonResult GetModule(int Id)
        {
            return Json(_global.GetModules(Id));
        }
    }
}