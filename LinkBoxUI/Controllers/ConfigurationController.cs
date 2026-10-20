using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.Ink;
using DomainLayer;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.SessionChecker;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Spreadsheet;
using Remotion.FunctionalProgramming;
//using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace LinkBoxUI.Controllers
{
    [SessionCheck]
    public class ConfigurationController : Controller
    {
        private readonly IConfigurationRepository _configrepo;
        private readonly IMappingRepository _maprepo;
        private readonly IGlobalRepository _global;
        public ConfigurationController(IConfigurationRepository configrepo, IGlobalRepository global, IMappingRepository maprepo)
            : this(GlobalConfiguration.Configuration)
        {
            _configrepo = configrepo;
            _global = global;
            _maprepo = maprepo;
        }
        public HttpConfiguration Configuration { get; private set; }
        public ConfigurationController(HttpConfiguration config)
        {
            Configuration = config;
        }

        LinkboxDb db = new LinkboxDb();
        [SetupCheck]
        public ActionResult Setup()
        {
            Session["ModuleAccess"] = 3;
            ViewBag.Title = "Setup";
            return View(_configrepo.View_Setup(Configuration));
        }
        //[FieldMappingCheck]
        //public async Task<ActionResult> MappingConfig()
        //{
        //    Session["ModuleAccess"] = 4;
        //    return View(await _maprepo.View_Mapping());
        //}
        [FieldMappingCheck]
        public ActionResult FieldMappingConfig()
        {
            Session["ModuleAccess"] = 4;
            return View("FieldMappingConfig");
        }

        public ActionResult QueryManager()
        {
            Session["ModuleAccess"] = 10;
            return View(_maprepo.View_Query());
        }

        public ActionResult APIManager()
        {
            Session["ModuleAccess"] = 11;
            return View(_maprepo.View_API());
        }

        [SetupCheck]
        public JsonResult CreateAddon(SetupCreateViewModel.AddonViewModel model)
        {
            AddonSetup addon = AutoMapper.Mapper.Map<SetupCreateViewModel.AddonViewModel, AddonSetup>(model);
            _configrepo.Create_AddonSetup(addon, Convert.ToInt32(Session["Id"]));
            _configrepo.SaveChanges();
            return Json(true);
        }

        [FieldMappingCheck]
        public PartialViewResult LoadMapping(string mappingOption)
        {
            Session["ModuleAccess"] = 4;
            var model = _maprepo.View_Mapping();

            string partialViewName;

            switch (mappingOption)
            {
                case "mysql-sap-submit":
                    partialViewName = "PartialViewMapping/MySqlToSapForm";
                    break;

                case "shopify-api-netsuite-submit":
                    partialViewName = "PartialViewMapping/ShopifyAPIToNetsuite";
                    break;
                case "platform-api-sap-submit":
                    partialViewName = "PartialViewMapping/PlatformApitoSapB1";
                    break;
                case "primary-api-sap-submit":
                    partialViewName = "PartialViewMapping/PrimaryToSAPB1";
                    break;
                case "sap-eis-submit":
                    partialViewName = "PartialViewMapping/SAPb1ToBirEIS";
                    break;

                default:
                    partialViewName = "~/Views/Shared/Error.cshtml";
                    break;
            }

            ViewBag.SelectedOption = mappingOption;
            return PartialView(partialViewName, model);
        }
        public JsonResult FindAddon(int Id)
        {
            return Json(_configrepo.Find_Addon(Id));
        }

        public JsonResult FindConnectionString(string Type)
        {
            return Json(_configrepo.Find_ConnectionString(Type));
        }

        public ActionResult ExecuteQuery(string Type, string ConString, string Query,int Id)
        {
            var model = _configrepo.GetConnection(Type, ConString);

            string message = "Please contact the administrator";
            DataTable dt = _configrepo.Fill_DataTable(model, Query,Id);

            if (dt.Rows.Count > 0 && dt != null)
            {
                var JSONresult = JsonConvert.SerializeObject(dt);

                return Content(JSONresult, "appllication/json");
            }
            else
            {
                return Content(message, "plain/text");

            }

            //return Json(null);
        }
        public JsonResult DocSetUpMap(SetupCreateViewModel.DocumentMapView model,List<string[]> StoreHeaderVal,int doc_id, string Code, string SavePath, string FileStatus, string FileCred, int editQueryManager)
        {

            var doc_model = db.Documents.Find(doc_id);

            if (doc_model != null)
            {
                doc_model.SavePath = SavePath;
                doc_model.Code = Code;
                doc_model.QueryManagerId = editQueryManager;
                doc_model.Credential = FileCred;
                doc_model.IsActive = !string.IsNullOrEmpty(FileStatus) && FileStatus.ToLower().Equals("checked") ? true : false;
                doc_model.UpdateDate = DateTime.Now;
                db.Entry(doc_model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }

            DocumentMap map = AutoMapper.Mapper.Map<SetupCreateViewModel.DocumentMapView, DocumentMap>(model);
            _configrepo.Create_DocMap(map, StoreHeaderVal, Convert.ToInt32(Session["Id"]), doc_id);
            return Json(true);

        }

            public JsonResult CreateQuery(QueryManagerViewModel.QueryManager model,QueryManagerMapViewModel.QueryManagerMap modelmap ,List<string[]> StoreHeaderVal)
        {
           
            QueryManager query = AutoMapper.Mapper.Map<QueryManagerViewModel.QueryManager, QueryManager>(model);

            QueryManagerMap map= AutoMapper.Mapper.Map<QueryManagerMapViewModel.QueryManagerMap, QueryManagerMap>(modelmap);
            using (var context = new LinkboxDb())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        _configrepo.Create_Query(query, Convert.ToInt32(Session["Id"]));
                        _configrepo.Create_QueryMap(map, StoreHeaderVal, Convert.ToInt32(Session["Id"]), map.Id);
                        return Json(true);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(ex.Message);
                    }
                }
                    
            }

                        
        }

        public JsonResult ValidateQueryManager(string Code)
        {
            return Json(_configrepo.ValidateQueryManager(Code));
        }

        public JsonResult FindQuery(int Id)
        {
            return Json(_configrepo.Find_Query(Id));
        }

        public JsonResult FindQueryMap(int Id)
        {
            return Json(_configrepo.Find_QueryMap(Id));
        }

        public JsonResult UpdateQuery( QueryManagerViewModel.QueryManager model, QueryManagerMapViewModel.QueryManagerMap modelmap, List<string[]> StoreHeaderVal )
        {
            QueryManager query = AutoMapper.Mapper.Map<QueryManagerViewModel.QueryManager, QueryManager>(model);
            QueryManagerMap map = AutoMapper.Mapper.Map<QueryManagerMapViewModel.QueryManagerMap, QueryManagerMap>(modelmap);
           
                        _configrepo.Update_Query(query, Convert.ToInt32(Session["Id"]));

                        _configrepo.Update_QueryMap(map, Convert.ToInt32(Session["Id"]), StoreHeaderVal);
                       
                   

                return Json(true);
            
        }

        [SetupCheck]
        public JsonResult ValidateCode(string Code)
        {
            return Json(_configrepo.ValidateCode(Code));
        }

        
        [SetupCheck]
        public JsonResult GetMethod(string APIUrl)
        {
            return Json(_configrepo.GetMethod(Configuration, APIUrl));
        }
        [SetupCheck]
        public JsonResult UpdateAddon(SetupCreateViewModel.AddonViewModel model)
        {
            AddonSetup addon = AutoMapper.Mapper.Map<SetupCreateViewModel.AddonViewModel, AddonSetup>(model);
            return Json(_configrepo.Update_AddonSetup(addon, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }
        [SetupCheck]
        public JsonResult CreateSAP(SetupCreateViewModel.SAPViewModel model)
        {
            SAPSetup sap = AutoMapper.Mapper.Map<SetupCreateViewModel.SAPViewModel, SAPSetup>(model);
            _configrepo.Create_SapSetup(sap, Convert.ToInt32(Session["Id"]));
            _configrepo.SaveChanges();
            return Json(true);
        }
        public JsonResult FindSAPString(string code)
        {
            return Json(_configrepo.Find_SAP(code));
        }
        public JsonResult FindSAP(int Id)
        {
            return Json(_configrepo.Find_SAP(Id));
        }
      
        [SetupCheck]
        public JsonResult UpdateSAP(SetupCreateViewModel.SAPViewModel model)
        {
            SAPSetup sap = AutoMapper.Mapper.Map<SetupCreateViewModel.SAPViewModel, SAPSetup>(model);
            return Json(_configrepo.Update_SapSetup(sap, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }

        [SetupCheck]
        public JsonResult CreateAPI(SetupCreateViewModel.APIViewModel model)
        {
            APISetup api = AutoMapper.Mapper.Map<SetupCreateViewModel.APIViewModel, APISetup>(model);
            _configrepo.Create_APISetup(api, Convert.ToInt32(Session["Id"]));
            _configrepo.SaveChanges();
            return Json(true);
        }

        [SetupCheck]
        public JsonResult CreateEmail(SetupCreateViewModel.EmailViewModel model)
        {
            EmailSetup email = AutoMapper.Mapper.Map<SetupCreateViewModel.EmailViewModel, EmailSetup>(model);
            _configrepo.Create_EmailSetup(email, Convert.ToInt32(Session["Id"]));
            _configrepo.SaveChanges();
            return Json(true);
        }

        public JsonResult FindAPI(int Id)
        {
            return Json(_configrepo.Find_API(Id));
        }
        public JsonResult GetAPIMethod(string code)
        {
            return Json(_configrepo.GetAPIMethod(code));
        }
        public JsonResult FindAPIManager(int Id)
        {
            return Json(_configrepo.Find_APIManager(Id));
        }
        public JsonResult GetQuerySetup()
        {
            return Json(_configrepo.GetQueries(), JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetSAPSetup()
        {
            return Json(_configrepo.GetSAPSetups(), JsonRequestBehavior.AllowGet);
        }
        public JsonResult CreateAPISetup(MapCreateViewModel.APIManager model)
        {
            APIManager query = AutoMapper.Mapper.Map<MapCreateViewModel.APIManager, APIManager>(model);
            _configrepo.Create_API(query, Convert.ToInt32(Session["Id"]));
            _configrepo.SaveChanges();
            return Json(true);
        }
        public JsonResult UpdateAPISetup(MapCreateViewModel.APIManager model)
        {
            APIManager query = AutoMapper.Mapper.Map<MapCreateViewModel.APIManager, APIManager>(model);
            return Json(_configrepo.Update_APIManager(query, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }
        public JsonResult FindEmail(int Id)
        {
            return Json(_configrepo.Find_Email(Id));
        }
       
        [SetupCheck]
        public JsonResult UpdateAPI(SetupCreateViewModel.APIViewModel model)
        {
            APISetup api = AutoMapper.Mapper.Map<SetupCreateViewModel.APIViewModel, APISetup>(model);
            return Json(_configrepo.Update_APISetup(api, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }

        [SetupCheck]
        public JsonResult UpdateEmail(SetupCreateViewModel.EmailViewModel model)
        {
            EmailSetup email = AutoMapper.Mapper.Map<SetupCreateViewModel.EmailViewModel, EmailSetup>(model);
            return Json(_configrepo.Update_EmailSetup(email, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }

        [SetupCheck]
        public JsonResult CreatePath(SetupCreateViewModel.PathViewModel model)
        {
            PathSetup path = AutoMapper.Mapper.Map<SetupCreateViewModel.PathViewModel, PathSetup>(model);
            _configrepo.Create_PathSetup(path, Convert.ToInt32(Session["Id"]));
            _configrepo.SaveChanges();
            return Json(true);
        }
        [System.Web.Mvc.HttpPost]
        public ActionResult UploadFile()
        {
            // Checking no of files injected in Request object  
            if (Request.Files.Count > 0)
            {
                try
                {
                    //  Get all files from Request object  
                    HttpFileCollectionBase files = Request.Files;
                    for (int i = 0; i < files.Count; i++)
                    {
                        //string path = AppDomain.CurrentDomain.BaseDirectory + "Uploads/";  
                        //string filename = Path.GetFileName(Request.Files[i].FileName);  

                        HttpPostedFileBase file = files[i];
                        string fname;

                        // Checking for Internet Explorer  
                        if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split(new char[] { '\\' });
                            fname = testfiles[testfiles.Length - 1];
                        }
                        else
                        {
                            fname = file.FileName;
                        }

                        // Get the complete folder path and store the file inside it.  
                        fname = Path.Combine(Server.MapPath("~/FileUpload/"), fname);
                        file.SaveAs(fname);
                    }
                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }

        public JsonResult FindPath(int Id)
        {
            return Json(_configrepo.Find_Path(Id));
        }
      

        [SetupCheck]
        public JsonResult UpdatePath(SetupCreateViewModel.PathViewModel model)
        {
            PathSetup path = AutoMapper.Mapper.Map<SetupCreateViewModel.PathViewModel, PathSetup>(model);
            return Json(_configrepo.Update_PathSetup(path, Convert.ToInt32(Session["Id"])), JsonRequestBehavior.AllowGet);
        }
        public  JsonResult FindMap(int Id)
        {
            var model =  _maprepo.Find_Map(Id);
            //Session["UpdateHead"] = model.Headers.Count;
            //Session["UpdateRow"] = model.Rows.Count;
            return Json(model);
        }
        public JsonResult APIFindMap(int Id)
        {
            var model = _maprepo.APItoAPIFind_Map(Id);
            //Session["UpdateHead"] = model.Headers.Count;
            //Session["UpdateRow"] = model.Rows.Count;
            return Json(model);
        }
        public JsonResult DataFileToSAP(int Id)
        {
            var model = _maprepo.DataFileToSAP(Id);
            //Session["UpdateHead"] = model.Headers.Count;
            //Session["UpdateRow"] = model.Rows.Count;
            return Json(model);
        }

        
        [System.Web.Http.HttpPost]
        public async Task<JsonResult> FetchAPIUrl(string Code)
        {
            using (var context = new LinkboxDb())
            {
                // Perform the query asynchronously
                var apiSetup = await context.APISetups
                                             .Where(x => x.APICode == Code)
                                             .Select(x => x.APIURL)
                                             .FirstOrDefaultAsync();

                // Check if the apiSetup was found
                if (apiSetup == null)
                {
                    return Json(new { success = false, message = "API setup not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, data = apiSetup }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult opsFindMap(int Id)
        {
            var model = _maprepo.opsFind_Map(Id);
            //Session["UpdateHead"] = model.Headers.Count;
            //Session["UpdateRow"] = model.Rows.Count;
            return Json(model);
        }
        public JsonResult ValidateMapCode(string Code)
        {
            return Json(_maprepo.Validate_MapCode(Code)); ;
        }
        public JsonResult GetFieldMap(int Id, string TableName)
        {
            var model = _maprepo.GetFieldMap(Id, TableName);  
            return Json(model);
        }

        [FieldMappingCheck]
        public JsonResult ValidateMap(string Code)
        {
            return Json(_maprepo.ValidateMap(Code));
        }
        [FieldMappingCheck]
        public JsonResult ValidateMapModule(string SAPCode, string Module)
        {
            return Json(_maprepo.ValidateMapModule(SAPCode, Module));
        }
        public JsonResult OPSFindFile(int Id)
        {
            return Json(_maprepo.OPSFind_File(Id));
        }
        public JsonResult AddTableOpsMapId(int Id,string TableMap)
        {
            return Json(_maprepo.AddTable_OpsMapId(Id, TableMap));
        }









        //json field mapping
        [FieldMappingCheck]
        public JsonResult CreateField(MapCreateViewModel.FieldMapping model, int Check,
                                      string RowTable, string HeaderTable,
                                      List<string[]> HeaderVal,
                                      List<string[]> RowVal,
                                      List<string[]> StoreHeaderVal,
                                      List<string[]> StoreRowVal,
                                      List<string[]> ParamVal)
        {
            //string filetype = "";
            //if (FileType != null)
            //{
            //    foreach (var item in FileType) { filetype += $"{item.ToString()},"; }
            //}

            SetupCreateViewModel SAPDetails = _configrepo.Find_SAP(model.SAPCode);

            SetupCreateViewModel.SAPViewModel SAP = new SetupCreateViewModel.SAPViewModel();
            SAP = SAPDetails.SAPView.Select((x) => new SetupCreateViewModel.SAPViewModel
            {
                SAPCode = x.SAPCode,
                SAPDBName = x.SAPDBName,
                SAPDBPassword = x.SAPDBPassword,
                SAPDBPort = x.SAPDBPort,
                SAPDBuser = x.SAPDBuser,
                SAPDBVersion = x.SAPDBVersion,
                SAPIPAddress = x.SAPIPAddress,
                SAPLicensePort = x.SAPLicensePort,
                SAPPassword = x.SAPPassword,
                SAPServerName = x.SAPServerName,
                SAPUser = x.SAPUser,
                SAPVersion = x.SAPVersion,

            }).FirstOrDefault();

            MapCreateViewModel Header = new MapCreateViewModel();
            MapCreateViewModel Rows = new MapCreateViewModel();


            //GetHeaderTable
            //Header = _configrepo.PopulateSAPDBTableRequired(SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(), model.ModuleName, SAP);
            //Rows = _configrepo.PopulateSAPDBTableRowRequired(SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(), model.ModuleName, SAP);

            HeaderTable = model.ModuleName;

            int count = 0;
            StoreHeaderVal = new List<string[]>();

            foreach (var item in Header.Headers)
            {
                string[] details = { (count + 1).ToString(), item.TableName, item.DataType, item.Length, item.IsNullable };
                StoreHeaderVal.Add(details);
                count++;
                //StoreHeaderVal[count][0] = (count + 1).ToString();
                //StoreHeaderVal[count][1] = item.TableName;
                //StoreHeaderVal[count][2] = item.DataType;
                //StoreHeaderVal[count][3] = item.Length;
                //StoreHeaderVal[count][4] = item.IsNullable;
                //count++;
            }

            count = 0;
            StoreRowVal = new List<string[]>();
            foreach (var item in Rows.Headers)
            {
                string[] details = { (count + 1).ToString(), item.SourceFieldId, item.DataType, item.Length, item.IsNullable, item.TableName };
                StoreRowVal.Add(details);
                count++;
            }

            //Get list Primary Key in Header
            List<DataRow> HeaderPrimaryKey = DataAccess.Select(SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(), SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault().Contains("HANA") ? $@"
                                                                                                                 SELECT COLUMN_NAME FROM ""CONSTRAINTS"" 
                                                                                                                WHERE SCHEMA_NAME = '{SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault()}' 
                                                                                                                AND TABLE_NAME = '{model.ModuleName}' AND IS_PRIMARY_KEY = 'TRUE';
                                                                                                                "
                                                                                                                :
                                                                                                                $@"SELECT 
                                                                                                                column_name as COLUMN_NAME
                                                                                                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 

                                                                                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                                                                                                                ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                                                                                                                AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                                                                                                                AND KU.table_name='{model.ModuleName}'

                                                                                                            ORDER BY 
                                                                                                                 KU.TABLE_NAME
                                                                                                                ,KU.ORDINAL_POSITION
                                                                                                            ; ")
                                                  .AsEnumerable().Select(x => x).ToList();

            //Get list of rows
            List<DataRow> RowTableList = DataAccess.Select(SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault(),
                                                  SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(),
                                                                                    SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault().Contains("HANA") ?
                                                                                    $@"SELECT DISTINCT TABLE_NAME 
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault()}' AND TABLE_NAME LIKE '{model.ModuleName.Substring(1, model.ModuleName.Length - 1)}%' AND TABLE_NAME <> '{model.ModuleName}'
                                                                                    ORDER BY TABLE_NAME"
                                                                                    :
                                                                                    $@"SELECT DISTINCT TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME LIKE '{model.ModuleName.Substring(1, model.ModuleName.Length - 1)}%' AND TABLE_NAME <> '{model.ModuleName}'
                                                                                    ORDER BY TABLE_NAME")
                                                  .AsEnumerable().Select(x => x).ToList();

            //Get List of Primary Key in rows
            string rowtablesqry = "";
            foreach (var item in RowTableList)
            {
                rowtablesqry += $"'{item["TABLE_NAME"]}',";
            }

            List<DataRow> RowsPrimaryKey = DataAccess.Select(SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault(),
                      SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                      SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
                      SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault(),
                      SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(), SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault().Contains("HANA") ? $@"
                                                                                     SELECT TABLE_NAME, COLUMN_NAME FROM ""CONSTRAINTS"" 
                                                                                     WHERE SCHEMA_NAME = '{SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault()}' 
                                                                                     AND TABLE_NAME IN({(rowtablesqry == "" ? "''" : rowtablesqry.Remove(rowtablesqry.Length - 1, 1))}) AND IS_PRIMARY_KEY = 'TRUE';
                                                                                     "
                                                                                     :
                                                                                     $@"SELECT 
                                                                                     TABLE_NAME,column_name as COLUMN_NAME
                                                                                     FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
                                                                                     INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                                                                                     ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                                                                                     AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                                                                                     AND KU.table_name IN({(rowtablesqry == "" ? "''" : rowtablesqry.Remove(rowtablesqry.Length - 1, 1))})
                                                                                     ORDER BY 
                                                                                     KU.TABLE_NAME
                                                                                     ,KU.ORDINAL_POSITION
                                                                                     ;")
                      .AsEnumerable().Select(x => x).ToList();

            var AddonPath = _maprepo.Select_AddonSetup(model.AddonCode);

            if (Check == 0)
            {
                Session["UpdateHead"] = 0;
                Session["UpdateRow"] = 0;
                model.FileName = Path.GetFileName(model.FileName);
                FieldMapping field = AutoMapper.Mapper.Map<MapCreateViewModel.FieldMapping, FieldMapping>(model);
                _maprepo.Create_FieldMapping(field, Convert.ToInt32(Session["Id"]));
                _maprepo.SaveChanges();
            }
            else
            {
                FieldMapping field = AutoMapper.Mapper.Map<MapCreateViewModel.FieldMapping, FieldMapping>(model);
                _maprepo.Update_FieldMapping(field, Convert.ToInt32(Session["Id"]));
                _maprepo.SaveChanges();
            }

            //if (HeaderVal != null || RowVal != null)
            //{
            RowVal = new List<string[]>();
            var NewHead = _maprepo.NewFieldValue(HeaderVal, Convert.ToInt32(Session["UpdateHead"]));
            var NewRow = _maprepo.NewFieldValue(RowVal, Convert.ToInt32(Session["UpdateRow"]));

            var oMapId = db.FieldMappings.OrderByDescending(x => x.MapId).FirstOrDefault();
            //saving of headers and rows to addon db
            //if (HeaderVal != null)
            //{
            var _sess = Convert.ToInt32(Session["UpdateHead"]);
            if (model.MapId > 0)
            {
                _maprepo.Create_Headers(StoreHeaderVal, Check, HeaderTable, Convert.ToInt32(Session["Id"]), model.MapId, oMapId.MapId);
            }
            else //else if (HeaderVal.Any() && Convert.ToInt32(Session["UpdateHead"]) == 0)
            {
                _maprepo.Create_Headers(StoreHeaderVal, Convert.ToInt32(Session["Id"]), HeaderTable, Check, oMapId.MapId, model.MapId);
            }
            //}

            //if (RowVal != null)
            //{
            _sess = Convert.ToInt32(Session["UpdateRow"]);
            if (model.MapId > 0)
            {
                _maprepo.Create_Rows(StoreRowVal, Check, RowTable, Convert.ToInt32(Session["Id"]), model.MapId, oMapId.MapId);
            }
            else //else if (RowVal.Any() && Convert.ToInt32(Session["UpdateRow"]) == 0)
            {
                _maprepo.Create_Rows(StoreRowVal, Convert.ToInt32(Session["Id"]), RowTable, Check, oMapId.MapId, model.MapId);
            }
            //}
            //}

            ////ADD API PARAMETERS IF EXIST

            _maprepo.CreateParameters(ParamVal, (model.MapId > 0 ? model.MapId : oMapId.MapId), model.APICode);


            ////EXCLUDE FILE TO API SINCE NO DATABASE FOR FILE
            if (model.DataType != "FILEAPI")
            {
                string constr = _maprepo.Get_Constring(AddonPath);

                _maprepo.GenerateHeaderTable(AddonPath.AddonDBName, constr,
                                       StoreHeaderVal, HeaderTable,
                                       Convert.ToInt32(Session["UpdateHead"]), HeaderPrimaryKey);

                _maprepo.GenerateRowTable(AddonPath.AddonDBName, constr,
                       StoreRowVal, RowTableList, RowTable,
                       Convert.ToInt32(Session["UpdateRow"]), RowsPrimaryKey);
            }

            ////CREATE DB FOR SAP
            //CHECK IF EXISTING
            List<DataRow> DBCount = DataAccess.Select(SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault(),
              SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
              SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
              SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault(),
              SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(), SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault().Contains("HANA") ? $@"
                                                                                SELECT * FROM SCHEMAS WHERE SCHEMA_NAME = 'SAOLIVE_LINKBOX';"
                                                                                :
                                                                                $@"SELECT name, database_id, create_date   
                                                                                FROM sys.databases WHERE name = 'SAOLIVE_LINKBOX'; ")
              .AsEnumerable().Select(x => x).ToList();

            //CREATE DB AND/OR CREATE TABLE IF NOT EXISTING
            if (SAPDetails.SAPView.Select(x => x.SAPDBVersion).FirstOrDefault().Contains("HANA"))
            {
                QueryAccess.conHana("SAOLIVE_LINKBOX",
                                        SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBPort).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault());

                QueryAccess.CreateTableHana(HeaderTable,
                    SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(),
                    "SAOLIVE_LINKBOX",
                    SAPDetails.SAPView.Select(x => x.SAPDBPort).FirstOrDefault(),
                    SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                    SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
                    SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault());
            }
            else
            {
                QueryAccess.con("SAOLIVE_LINKBOX",
                                        SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBPort).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault());
                QueryAccess.CreateTable(HeaderTable,
                                        SAPDetails.SAPView.Select(x => x.SAPDBName).FirstOrDefault(),
                                        "SAOLIVE_LINKBOX",
                                        SAPDetails.SAPView.Select(x => x.SAPDBPort).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPServerName).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBuser).FirstOrDefault(),
                                        SAPDetails.SAPView.Select(x => x.SAPDBPassword).FirstOrDefault());
            }

            return Json(true);
        }

        [FieldMappingCheck]
        public JsonResult HanaTablePopulate(string HanaTable, string SAPCode, string HeaderTable, string RowTable)
        {
            return Json(_maprepo.Populate(HanaTable, SAPCode, HeaderTable, RowTable));
        }

        [FieldMappingCheck]
        public JsonResult PopulateSAPDBTable(string Schema, string Module, SetupCreateViewModel.SAPViewModel model)
        {
            return Json(_configrepo.PopulateSAPDBTable(Schema, Module, model));
        }

        [FieldMappingCheck]
        public JsonResult PopulateSAPDBTableRow(string Schema, string Module, SetupCreateViewModel.SAPViewModel model)
        {
            return Json(_configrepo.PopulateSAPDBTableRow(Schema, Module, model));
        }

        [FieldMappingCheck]
        public JsonResult HanaTablePopulateAPI(string HanaTable, string SAPCode, string HeaderTable, string RowTable, string APICode, int MapId)
        {
            return Json(_maprepo.PopulateAPI(HanaTable, SAPCode, HeaderTable, RowTable, APICode, MapId));
        }
        [FieldMappingCheck]
        public JsonResult PopulateFileAPI(string FileName, string SAPCode, string HeaderTable, string RowTable, string APICode)
        {
            return Json(_maprepo.PopulateFileAPI(FileName, SAPCode, HeaderTable, RowTable, APICode));
        }

        #region SAP API TO SAP API

        [FieldMappingCheck]
        public JsonResult PopulateSAPAPI(string SourceAPICode, string HeaderTable, string RowTable, string DestinationAPICode, string Module)
        {
            return Json(_maprepo.PopulateSAPAPI(SourceAPICode, HeaderTable, RowTable, DestinationAPICode, Module));
        }

        [FieldMappingCheck]
        public JsonResult CreateSAPtoSAPField(MapCreateViewModel.FieldMapping model, int Check,
                              string RowTable, string HeaderTable,
                              List<string[]> HeaderVal,
                              List<string[]> RowVal,
                              List<string[]> StoreHeaderVal,
                              List<string[]> StoreRowVal,
                              string[] MapTables)
        {
            using (var context = new LinkboxDb())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        #region FieldMapping
                        int MapId = 0;
                        string con = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                        // Parse the connection string to get the database name
                        var builder = new SqlConnectionStringBuilder(con);
                        string databaseName = builder.InitialCatalog;

                        //Get Primary Key
                        string PrimaryKey = context.ModuleSetup.Where(x => x.ModuleName == model.ModuleName).FirstOrDefault()?.PrimaryKey;

                        FieldMapping fm = new FieldMapping();

                        if (Convert.ToBoolean(Check))
                        {
                            fm = context.FieldMappings.Find(model.MapId);
                        }

                        fm.MapCode = model.MapCode;
                        fm.MapName = model.MapName;
                        fm.ModuleName = model.ModuleName;
                        fm.DestModule = model.DestModule;
                        fm.PathCode = model.PathCode;
                        fm.FileType = model.FileType;
                        fm.FileName = model.FileName;
                        fm.DataType = model.DataType;
                        fm.SAPCode = model.SAPCode; /*SOURCE*/
                        fm.APICode = model.APICode; /*DESTINATION*/

                        if (Convert.ToBoolean(Check))
                        {
                            fm.UpdateDate = DateTime.Now;
                            fm.UpdateUserID = Convert.ToInt32(Session["Id"]);
                            context.Entry(fm).State = System.Data.Entity.EntityState.Modified;
                        }
                        else
                        {
                            fm.CreateDate = DateTime.Now;
                            fm.CreateUserID = Convert.ToInt32(Session["Id"]);
                            context.FieldMappings.Add(fm);
                        }

                        context.SaveChanges();
                        #endregion

                        if (model.MapId > 0)
                        {
                            MapId = model.MapId; 
                            _maprepo.Create_Headers(StoreHeaderVal, Check, HeaderTable, Convert.ToInt32(Session["Id"]), model.MapId, fm.MapId);                            
                        }
                        else
                        {
                            MapId = fm.MapId;
                            _maprepo.Create_Headers(StoreHeaderVal, Convert.ToInt32(Session["Id"]), HeaderTable, Check, fm.MapId, model.MapId);
                        }

                        if (model.MapId > 0)
                        {
                            //_maprepo.Create_Rows(StoreRowVal, Check, RowTable, Convert.ToInt32(Session["Id"]), model.MapId, fm.MapId);
                        }
                        else 
                        {
                            //_maprepo.Create_Rows(StoreRowVal, Convert.ToInt32(Session["Id"]), RowTable, Check, fm.MapId, model.MapId);
                        }

                        #region CREATE TABLE FOR TRANSACTION LOGS
                        //GET PRIMARY KEY USING MODULE CODE

                        //CREATE TABLES
                        //foreach(var tableName in MapTables)
                        //{
                        //    QueryAccess.CreateTransactionLogs(tableName, con, databaseName, PrimaryKey, MapId);
                        //}
                        #endregion

                        // Perform database operations here
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(ex.Message);
                    }
                }
            }

            return Json(true);
        }

        [FieldMappingCheck]
        public JsonResult CreateFiletoNetsuite(
                          int MapId,string MapCode,string MapName,string QueryCode,string ModuleName,string FieldPath,string FileName,List<string[]>RowVal)
        {
            using (var context = new LinkboxDb())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        string con = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";
                        // Parse the connection string to get the database name
                        var builder = new SqlConnectionStringBuilder(con);

                        FieldMapping fm = new FieldMapping();

                        if (MapId > 0)
                        {
                            fm = context.FieldMappings.Where(x=>x.MapId==MapId).FirstOrDefault();
                        }
                        
                        fm.MapCode = MapCode;
                        fm.MapName = MapName;
                        fm.ModuleName = ModuleName;
                        fm.SAPCode = QueryCode;
                        fm.PathCode = FieldPath;
                        fm.FileName = FileName;

                        if(MapId == 0)
                        {

                            fm.CreateDate = DateTime.Now;
                            fm.CreateUserID = 1;
                            context.FieldMappings.Add(fm);
                            context.SaveChanges();

                            foreach (var row in RowVal)
                            {
                                Header Row = new Header
                                {
                                    MapId = fm.MapId,
                                    SourceFieldId = row[0],  // Adjust according to your table structure
                                    SourceTableName = row[4] == "header" ? "Header" : "Line",
                                    DestinationField = row[1],
                                    DestinationTableName = "transaction",
                                    VisOrder = int.Parse(row[3]),
                                    ConditionalQuery = row[2],
                                    CreateDate = DateTime.Now,
                                    CreateUserID = 1
                                    // Add other columns here
                                };

                                context.Headers.Add(Row);
                            }
                        }
                        else
                        {
                            fm.UpdateDate = DateTime.Now;
                            fm.UpdateUserID = Convert.ToInt32(Session["Id"]);
                            context.Entry(fm).State = System.Data.Entity.EntityState.Modified;
                            context.SaveChanges();


                            var deletedRecords = context.Headers.Where(x => x.MapId == MapId).ToList();
                            context.Headers.RemoveRange(deletedRecords);
                            context.SaveChanges();

                            foreach (var row in RowVal)
                            {
                                Header Row = new Header
                                {
                                    MapId = MapId,
                                    SourceFieldId = row[0],  // Adjust according to your table structure
                                    SourceTableName = row[4] == "header"? "Header":"Line",
                                    DestinationField = row[1],
                                    DestinationTableName = "transaction",
                                    VisOrder = int.Parse(row[3]),
                                    ConditionalQuery = row[2],
                                    CreateDate = DateTime.Now,
                                    CreateUserID = 1
                                    // Add other columns here
                                };

                                context.Headers.Add(Row);
                            }

                            // **3. Save and commit transaction**
                            

                        }

                        context.SaveChanges();
                        transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(ex.Message);
                    }
                }
            }
           
            return Json(true);
        }

        [FieldMappingCheck]
        public JsonResult PrimaryCreateMapping(MapCreateViewModel.FieldMapping model, List<string[]> Source, List<string[]> Dest, List<string[]> MapFields)
        {
            using (var context = new LinkboxDb())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        int MapId = model.MapId;
                        // existing logic up to creation of FieldMapping fm...
                        FieldMapping fm = new FieldMapping();
                        fm.MapCode = model.MapCode;
                        fm.MapName = model.MapName;
                        fm.PathCode = model.PathCode;
                        fm.RowWorksheet = model.RowWorksheet;
                        fm.ModuleName = model.ModuleName;
                        fm.DestModule = model.DestModule;
                        fm.SAPCode = model.SAPCode;
                      

                        //int APIid_source = context.APISetups.Where(x=>x.APICode == model.APICode).Select(x => x.APIId).FirstOrDefault();
                        //int APIid_dest = context.APISetups.Where(x => x.APICode == model.SAPCode).Select(x => x.APIId).FirstOrDefault();

                        #endregion

                        if (model.MapId > 0)
                        {
                            var FieldMapping = context.FieldMappings
                                .FirstOrDefault(x => x.MapId == MapId);

                            if (FieldMapping != null)
                            {
                                FieldMapping.MapName = model.MapName;
                                //FieldMapping.APICode = model.APICode;
                                FieldMapping.SAPCode = model.SAPCode;
                                FieldMapping.UpdateDate = DateTime.Now;
                                FieldMapping.UpdateUserID = Convert.ToInt32(Session["Id"]);

                                context.SaveChanges();
                            }


                            var FieldSets = context.Headers.Where(x => x.MapId == MapId).ToList();

                            if (FieldSets.Any())
                            {
                                context.Headers.RemoveRange(FieldSets);
                                context.SaveChanges();
                            }

                            int visorder = 0;
                            foreach (var fms in MapFields)
                            {
                                Header fieldsets = new Header
                                {
                                    MapId = MapId,
                                    SourceFieldId = fms[0].ToString(),
                                    DestinationField = fms[1].ToString(),
                                    ConditionalQuery = fms[2].ToString(),
                                    IsKeyValue = fms[3].ToString() == "1" ? true : false,
                                    SourceTableName = "Sales Order",
                                    VisOrder = visorder++,
                                    DestinationTableName = fms[4].ToString(),
                                    CreateDate = DateTime.Now,
                                    CreateUserID = Session["Id"] != null ? Convert.ToInt32(Session["Id"]) : 1

                                };

                                context.Headers.Add(fieldsets);

                            }
                            context.SaveChanges();



                        }//end of if model.MapId
                        else
                        {
                            fm.CreateDate = DateTime.Now;
                            fm.CreateUserID = Convert.ToInt32(Session["Id"]);
                            context.FieldMappings.Add(fm);
                            context.SaveChanges();

                            MapId = fm.MapId;



                            if (MapFields != null)
                            {
                                int visorder = 0;
                                foreach (var fms in MapFields)
                                {
                                    Header fieldsets = new Header
                                    {
                                        MapId = MapId,
                                        SourceFieldId = fms[0].ToString(),
                                        DestinationField = fms[1].ToString(),
                                        ConditionalQuery = fms[2].ToString(),
                                        IsKeyValue = fms[3].ToString() == "1" ? true : false,
                                        SourceTableName = "Sales Order",
                                        VisOrder = visorder++,
                                        DestinationTableName = fms[4].ToString(),
                                        CreateDate = DateTime.Now,
                                        CreateUserID = Session["Id"] != null ? Convert.ToInt32(Session["Id"]) : 1

                                    };

                                    context.Headers.Add(fieldsets);

                                }
                                context.SaveChanges();
                            }



                        }


                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(ex.Message);
                    }
                }
            }

            return Json(true);
        }


        [FieldMappingCheck]
        public JsonResult CreateMapping(MapCreateViewModel.OPSFieldMapping model, List<string[]> Source, List<string[]> Dest, List<string[]> MapFields)
        {
            using (var context = new LinkboxDb())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        int MapId = model.MapId;  
                        // existing logic up to creation of FieldMapping fm...
                        FieldMapping fm = new FieldMapping();
                        fm.MapCode = model.MapCode;
                        fm.MapName = model.MapName;
                        fm.FileName = model.FileName;
                        fm.FileType = model.FileType;
                        fm.ModuleName = model.DestModule;
                        fm.SAPCode = model.SAPCode;
                        fm.DestModule = "Platform";
                        fm.APICode = model.APICode;

                        //int APIid_source = context.APISetups.Where(x=>x.APICode == model.APICode).Select(x => x.APIId).FirstOrDefault();
                        //int APIid_dest = context.APISetups.Where(x => x.APICode == model.SAPCode).Select(x => x.APIId).FirstOrDefault();



                        if (model.MapId > 0)
                        {
                            var FieldMapping = context.FieldMappings
                                .FirstOrDefault(x => x.MapId == MapId);

                            if (FieldMapping != null)
                            {
                                FieldMapping.MapName = model.MapName;
                                //FieldMapping.APICode = model.APICode;
                                FieldMapping.SAPCode = model.SAPCode;
                                FieldMapping.APICode = model.APICode;
                                FieldMapping.FileName = model.FileName;
                                FieldMapping.FileType = model.FileType;

                                FieldMapping.UpdateDate = DateTime.Now;
                                FieldMapping.UpdateUserID = Convert.ToInt32(Session["Id"]);

                                context.SaveChanges();  
                            }


                            var FieldSets = context.Headers.Where(x => x.MapId == MapId).ToList();

                            if (FieldSets.Any())
                            {
                                context.Headers.RemoveRange(FieldSets);
                                context.SaveChanges();
                            }

                            int visorder = 0;
                            foreach (var fms in MapFields)
                            {
                                Header fieldsets = new Header
                                {
                                    MapId = MapId,
                                    SourceFieldId = fms[0].ToString(),
                                    DestinationField = fms[1].ToString(),
                                    ConditionalQuery = fms[2].ToString(),
                                    IsKeyValue = fms[3].ToString() == "1" ? true : false,
                                    SourceTableName = "Sales Order",
                                    VisOrder = visorder++,
                                    DestinationTableName = fms[4].ToString(),
                                    CreateDate = DateTime.Now,
                                    CreateUserID = Session["Id"] != null ? Convert.ToInt32(Session["Id"]) : 1

                                };

                                context.Headers.Add(fieldsets);

                            }
                            context.SaveChanges();



                        }//end of if model.MapId
                        else
                        {
                            fm.CreateDate = DateTime.Now;
                            fm.CreateUserID = Convert.ToInt32(Session["Id"]);
                            context.FieldMappings.Add(fm);
                            context.SaveChanges();

                            MapId = fm.MapId;

                          

                            if (MapFields != null)
                            {
                                int visorder = 0;
                                foreach (var fms in MapFields)
                                {
                                    Header fieldsets = new Header
                                    {
                                        MapId = MapId,
                                        SourceFieldId = fms[0].ToString(),
                                        DestinationField = fms[1].ToString(),
                                        ConditionalQuery = fms[2].ToString(),
                                        IsKeyValue = fms[3].ToString() == "1" ? true : false,
                                        SourceTableName = "Sales Order",
                                        VisOrder = visorder++,
                                        DestinationTableName = fms[4].ToString(),
                                        CreateDate = DateTime.Now,
                                        CreateUserID = Session["Id"] != null ? Convert.ToInt32(Session["Id"]) : 1

                                    };

                                    context.Headers.Add(fieldsets);

                                }
                                context.SaveChanges();
                            }



                        }
                        
                    
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(ex.Message);
                    }
                }
            }

            return Json(true);
        }


        [FieldMappingCheck]
        public JsonResult FetchSAPAPIData(string Code, string Module)
        {
            return Json(JsonConvert.SerializeObject(_maprepo.FetchSAPAPIData(Code, Module)), JsonRequestBehavior.AllowGet);
        }

        public JsonResult FetchSAPDataTypes()
        {
            return Json(_maprepo.FetchSAPDataTypes(), JsonRequestBehavior.AllowGet);
        }
   

        [FieldMappingCheck]
        public JsonResult GetDataype(string HanaTable, string SAPCode, string Field)
        {
            return Json(_maprepo.Get_DataType(HanaTable, SAPCode, Field));
        }

        public JsonResult GetModule(int Id)
        {
            return Json(_global.GetModules(Id));
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult SaveFile(HttpPostedFileBase DocFile, string Code, string SavePath, string FileCred,int QueryManager)
        {
            var document = new LinkboxDb().Documents
                               .Where(x => x.Code == Code).FirstOrDefault();

            if (document == null)
            {
                Document model = new Document();
                var savePath = Server.MapPath("~/App_Data/").ToString();
                var InputFileName = Path.GetFileName(DocFile.FileName);
                var Extention = Path.GetExtension(DocFile.FileName);
                var ServerSavePath = Path.Combine(Server.MapPath("~/App_Data/") + InputFileName);
                var directory = new DirectoryInfo(Server.MapPath("~/App_Data/"));
                if (directory.Exists == false)
                {
                    directory.Create();
                }

                DocFile.SaveAs(ServerSavePath);
                model.FileName = DocFile.FileName;
                model.FilePath = Path.Combine(savePath + InputFileName);
                model.SavePath = SavePath;
                model.Code = Code;
                model.Credential = FileCred;
                model.QueryManagerId = QueryManager;
                model.IsActive = true;
                model.CreateDate = DateTime.Now;
                db.Documents.Add(model);
                db.SaveChanges();

                TempData["Message"] = "Successfully Added";

                string message = TempData["Message"] as string;
                ViewBag.Message = message;

                return RedirectToAction("Setup", "Configuration");
            }
            else
            {
                TempData["Message"] = "Error Duplicate Code";

                string message = TempData["Message"] as string;
                ViewBag.Message = message;
                return RedirectToAction("Setup", "Configuration");
            }
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult SaveCompany(HttpPostedFileBase LogoFile, string CompanyName, string Address, string MobileNo, string TelNo)
        {
            CompanyDetail model = new CompanyDetail();
            var savePath = Server.MapPath("~/ImageFile/").ToString();
            var InputFileName = Path.GetFileName(LogoFile.FileName);
            var Extention = Path.GetExtension(LogoFile.FileName);
            var ServerSavePath = Path.Combine(Server.MapPath("~/ImageFile/") + InputFileName);
            var directory = new DirectoryInfo(Server.MapPath("~/ImageFile/"));
            if (directory.Exists == false)
            {
                directory.Create();
            }
            LogoFile.SaveAs(ServerSavePath);
            model.FileName = LogoFile.FileName;
            model.FilePath = Path.Combine(savePath + InputFileName);
            model.CompanyName = CompanyName;
            model.Address = Address;
            model.MobileNo = MobileNo;
            model.TelNo = TelNo;
            model.IsActive = true;
            model.CreateDate = DateTime.Now;
            db.CompanyDetails.Add(model);
            db.SaveChanges();

            return RedirectToAction("Setup", "Configuration");
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult SaveModule(string ModuleCode, string ModuleName, string PrimaryKey, string EntityType, string EntityName)
        { 
            ModuleSetup model = new ModuleSetup();
            model.ModuleCode = ModuleCode;
            model.ModuleName = ModuleName;
            model.PrimaryKey = PrimaryKey;
            model.EntityType = EntityType;
            model.EntityName = EntityName;
            model.CreateUserID = 1;
            model.IsActive = true;
            model.CreateDate = DateTime.Now;
            db.ModuleSetup.Add(model);
            db.SaveChanges(); 

            return RedirectToAction("Setup", "Configuration");
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult UpdateModule(string Id, string ModuleCode, string ModuleName, string PrimaryKey, string ModuleStatus,string EntityType,string EntityName)
        {
            var model = db.ModuleSetup.Find(Convert.ToInt32(Id));
            ModuleStatus = String.IsNullOrEmpty(ModuleStatus) ? "" : ModuleStatus;
            model.ModuleCode = ModuleCode;
            model.ModuleName = ModuleName;
            model.PrimaryKey = PrimaryKey;
            model.EntityType= EntityType;
            model.EntityName = EntityName;
            model.IsActive = ModuleStatus.ToLower().Contains("checked") ? true : false;
            model.UpdateDate = DateTime.Now;
            db.Entry(model).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
             
            return RedirectToAction("Setup", "Configuration");
        }

        public JsonResult FindFile(int Id)
        {
            return Json(_configrepo.Find_File(Id));
        }
        public JsonResult FindCompany(int Id)
        {
            return Json(_configrepo.FindCompany(Id));
        }
        public JsonResult GetCrystalParam(int Id)
        {

            return Json(_configrepo.GetCrystalParam(Id));
        }
        public JsonResult FindQueryString(int Id)
        {

            return Json(_configrepo.FindQueryString(Id));
        }

  

        public JsonResult FindModule(int Id)
        {
            return Json(_configrepo.FindModule(Id));
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult UpdateFile(HttpPostedFileBase DocFile, string Code, string SavePath, int DocId, string FileStatus, string FileCred,int editQueryManager)
        {
            var model = db.Documents.Find(DocId);
            //if (DocFile != null)
            //{
            //    var savePath = Server.MapPath("~/App_Data/").ToString();
            //    var InputFileName = Path.GetFileName(DocFile.FileName);

            //    var ServerSavePath = Path.Combine(Server.MapPath("~/App_Data/") + InputFileName);
            //    var directory = new DirectoryInfo(Server.MapPath("~/App_Data/"));
            //    if (directory.Exists == false)
            //    {
            //        directory.Create();
            //    }
            //    DocFile.SaveAs(ServerSavePath);
            //    var prevFile = model.FileName;
            //    FileInfo[] fi = directory.GetFiles().Where(f => f.Name == prevFile).ToArray();
            //    if (fi.Length != 0 && prevFile != DocFile.FileName)
            //    {
            //        fi[0].Delete();
            //    }
            //    model.FileName = DocFile.FileName;
            //    model.FilePath = Path.Combine(savePath + InputFileName);
            //}

            model.SavePath = SavePath;
            model.Code = Code;
            model.QueryManagerId= editQueryManager;
            model.Credential = FileCred;
            model.IsActive = !string.IsNullOrEmpty(FileStatus) && FileStatus.ToLower().Contains("checked") ? true : false;
            model.UpdateDate = DateTime.Now;
            db.Entry(model).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();





            return RedirectToAction("Setup", "Configuration");
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult UpdateCompany(HttpPostedFileBase LogoFile, string CompanyName, string Address, string MobileNo, string TelNo, int CompanyId, string CompanyStatus)
        {
            var model = db.CompanyDetails.Find(CompanyId);
            if (LogoFile != null)
            {
                var savePath = Server.MapPath("~/ImageFile/").ToString();
                var InputFileName = Path.GetFileName(LogoFile.FileName);
                var ServerSavePath = Path.Combine(Server.MapPath("~/ImageFile/") + InputFileName);
                var directory = new DirectoryInfo(Server.MapPath("~/ImageFile/"));
                if (directory.Exists == false)
                {
                    directory.Create();
                }
                LogoFile.SaveAs(ServerSavePath);
                var prevFile = model.FileName;
                FileInfo[] fi = directory.GetFiles().Where(f => f.Name == prevFile).ToArray();
                if (fi.Length != 0 && prevFile != LogoFile.FileName)
                {
                    fi[0].Delete();
                }
                model.FileName = LogoFile.FileName;
                model.FilePath = Path.Combine(savePath + InputFileName);
            }
            model.CompanyName = CompanyName;
            model.Address = Address;
            model.MobileNo = MobileNo;
            model.TelNo = TelNo;
            model.IsActive = string.IsNullOrEmpty(CompanyStatus) ? false : CompanyStatus.ToLower().Contains("on") || CompanyStatus.ToLower().Contains("checked") ? true : false;
            model.UpdateDate = DateTime.Now;
            db.Entry(model).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Setup", "Configuration");
        }
    }
}