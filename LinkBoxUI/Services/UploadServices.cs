using LinkBoxUI.Context;
using InfrastructureLayer.Repositories;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer.ViewModels;
using DataAccessLayer.Class;
using LinkBoxUI.Helpers;
using System.Threading.Tasks;
using System.Diagnostics;
using LinkBoxUI.Properties;
using LinkBoxUI.Controllers.api;
using Microsoft.Graph.Models;
using NPOI.SS.Formula.Functions;

namespace LinkBoxUI.Services
{
    public class UploadServices : IUploadRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        private TaskSchedHelper tsched = new TaskSchedHelper();
        private PostController postController = new PostController();
        private ShopeeController _shopee = new ShopeeController();
        private LazadaController _lazada = new LazadaController();
        private TiktokController _tiktok = new TiktokController();
        private ZaloraController _zalora = new ZaloraController();
        private NetsuiteController _shopify = new NetsuiteController();


        private PrimaryAccountsController primary = new PrimaryAccountsController();
        private EISController eis = new EISController();



        //public UploadViewModel Get_Authorization(int id)
        //{
        //    var model = new UploadViewModel();
        //    model.AuthList = _context.Users.Join(_context.Authorizations, a => a.AuthorizationID, b => b.AuthId, (a, b) => new { a, b }).Where(a => a.a.UserId == id)
        //                                   .Join(_context.AuthorizationModules, ab => ab.b.AuthId, c => c.AuthId, (ab, c) => new { ab, c }).Where(c => c.c.IsActive == true && c.c.ModId == 5)
        //                                   .Join(_context.Modules, abc => abc.c.ModId, d => d.ModId, (abc, d) => new UploadViewModel.Authorization
        //                                   {
        //                                       authorization = d.ModName
        //                                   }).ToList();
        //    return model;
        //}

        //public UploadViewModel Get_Deposit()
        //{
        //    UploadViewModel model = new UploadViewModel();
        //    model.DepositList = _context.Deposits.Where(x => x.IsActive == true).Select(x => new UploadViewModel.Deposit
        //    {
        //        BrkId = x.BrkId,
        //        BrkCode = x.BrkCode,
        //        BrkDescription = x.BrkDescription
        //    }).ToList();
        //    return model;
        //}

        public UploadViewModel GetUploadDetails()
        {
            var model = new UploadViewModel();


            model.ProcessList = _context.Process.Where(x => x.IsActive == true)
                        .Select((z) => new UploadViewModel.Process
                        {
                            ProcessId = z.ProcessId,
                            ProcessName = z.ProcessName,
                            ProcessCode = z.ProcessCode,
                        }).ToList();
                        //.Where(x=>x.ProcessCode.Contains("Order"))

            return model;
        }

        public UploadViewModel Get_Details(string map)
        {

            UploadViewModel model = new UploadViewModel();



            // model.Headers = _context.
            //var fieldpath = _context.FieldMappings.Join(_context.Headers, a => a.MapId, b => b.MapId, (a, b) => new { a, b }).Where(a => a.a.MapName == map)
            //                                      .Join(_context.Rows, ab => ab.a.MapId, c => c.MapId, (ab, c) => new { ab, c })
            //                                      .Join(_context.PathSetup, abc => abc.ab.a.PathCode, d => d.PathCode, (abc, d) => new { abc, d })
            //                                      .Join(_context.AddonSetup, abcd => abcd.abc.ab.a.AddonCode, e => e.AddonCode, (abcd, e) => new { abcd, e })
            //                                      .Join(_context.SAPSetup, abcde => abcde.abcd.abc.ab.a.SAPCode, f => f.SAPCode, (abcde, f) => new
            //                                      {
            //                                          MapId = abcde.abcd.abc.ab.a.MapId,
            //                                          AddonDBName = abcde.e.AddonDBName,
            //                                          HeaderName = abcde.abcd.abc.ab.b.TableName,
            //                                          RowName = abcde.abcd.abc.c.TableName

            //                                      }).FirstOrDefault();

            //DataTable AddonData = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_GetFieldsAndValue {fieldpath.AddonDBName}," +
            //                                       $"{fieldpath.HeaderName},{fieldpath.RowName},{fieldpath.MapId},3");


            //model.UploadList = AddonData.AsEnumerable().Select(x => new UploadViewModel.Upload
            //{
            //    TransactionId = Convert.ToInt32(x[0].ToString()),
            //    BranchCode = x[1].ToString(),
            //    TranDate = Convert.ToDateTime(x[2].ToString()).ToString("MM/dd/yyyy"),
            //    CashierName = x[4].ToString(),
            //    Status = x[5].ToString(),
            //    Message = x[6].ToString()

            //}).ToList();

            //return model;
            return model;
        }

        public UploadViewModel View_Upload()
        {
            var model = new UploadViewModel();
            model.SAPList = new List<UploadViewModel.SAPConfig>();
            //model.SAPList = _context.FieldMappings.Select(x => new UploadViewModel.SAPConfig
            //{ Code = x.MapCode }).ToList();
            var dtproc = _context.Schedules
                          .Join(_context.Process, sch => sch.Process, pr => pr.ProcessCode, (sch, pr) => new { sch, pr })
                          //.Where(sel => sel.pr.MapId == 0)
                          .Select(sel => sel.sch.SchedCode).ToList();
            dtproc.ForEach(fe =>
            {
                model.SAPList.Add(new UploadViewModel.SAPConfig { Code = fe });
            });
            return model;
        }


        public DataTable GetData(string Code)
        {
            var model = new DashboardViewModel();
            var config = new MapCreateViewModel();
            var Setup = new SetupCreateViewModel();
            DataTable table = new DataTable();

            config.FieldMapDetails = _context.FieldMappings.AsEnumerable()?.Where(x => x.MapCode == Code).Select(x => new MapCreateViewModel.FieldMapping
            {
                MapId = x.MapId,
                MapCode = x.MapCode,
                AddonCode = x.AddonCode,
                SAPCode = x.SAPCode,
                FileName = x.FileName,
                FileType = x.FileType,
                HeaderWorksheet = x.HeaderWorksheet,
                MapName = x.MapName,
                ModuleName = x.ModuleName,
                PathCode = x.PathCode,
                RowWorksheet = x.RowWorksheet,

            }).FirstOrDefault();

            if (config.FieldMapDetails != null)
            {
                var AddonSettings = _context.AddonSetup.Where(x => x.AddonCode == config.FieldMapDetails.AddonCode).FirstOrDefault();

                var header = _context.Headers.Where(x => x.MapId == config.FieldMapDetails.MapId).FirstOrDefault();
                var rows = _context.Rows.Where(x => x.MapId == config.FieldMapDetails.MapId).FirstOrDefault();
                var con = QueryAccess.con2(AddonSettings.AddonDBName, AddonSettings.AddonIPAddress, 40000, AddonSettings.AddonServerName, AddonSettings.AddonDBuser, AddonSettings.AddonDBPassword);

                table = DataAccess.Select(con, $@"SELECT * FROM {header.SourceTableName} WHERE Status IN ('O','E')");
            }

            return table;
        }

        #region SAP to SAP
        public UploadViewModel GetSAPtoSAPProcess(string Code)
        {
            var model = new UploadViewModel();

            model.ProcessList = _context.FieldMappings.Join(_context.ProcessMap, fm => fm.MapId, pm => pm.MapId, (fm, pm) => new { fm, pm }).
                                      Join(_context.Process, fmpm => fmpm.pm.ProcessId, pr => pr.ProcessId, (fmpm, pr) => new { fmpm, pr }).
                                      Where(fmpmpr => fmpmpr.pr.ProcessCode == Code).
                                      Select(fmpmpr => new UploadViewModel.Process
                                      {
                                          MapCode = fmpmpr.fmpm.fm.MapCode,
                                          MapName = fmpmpr.fmpm.fm.MapName,
                                          Module = fmpmpr.fmpm.fm.ModuleName,

                                      }).ToList();

            ////Get uploading progress
            //foreach (var item in model.ProcessList)
            //{
            //    string qry = "";
            //    DataTable ReportTable = new DataTable();
            //    DataTable Tables = new DataTable();
            //    Tables = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
            //                                      item.SAPServerName,
            //                                      item.SAPDBuser,
            //                                      item.SAPDBPassword,
            //                                      item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME, SCHEMA_NAME,'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT""
            //                                                                        FROM SYS.COLUMNS
            //                                                                        WHERE SCHEMA_NAME = '{item.SAPDBName}'  AND  TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'")

            //                                      :
            //                                      DataAccess.Select(QueryAccess.MSSQL_conString(
            //                                      item.SAPServerName,
            //                                      item.SAPDBuser,
            //                                      item.SAPDBPassword,
            //                                      item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME,'' as ""SCHEMA_NAME"",'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT"" FROM INFORMATION_SCHEMA.COLUMNS  
            //                                                                        WHERE TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'");

            //    foreach (DataRow row in Tables.Rows)
            //    {
            //        qry += item.SAPDBVersion.Contains("HANA") ? $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{item.SAPDBName}"".""{row["TABLE_NAME"].ToString()}"""
            //                                                  :
            //                                                    $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{row["TABLE_NAME"].ToString()}""";
            //    }
            //    ReportTable = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
            //                                                          item.SAPServerName,
            //                                                          item.SAPDBuser,
            //                                                          item.SAPDBPassword,
            //                                                          item.SAPDBName), $@"
            //                                                                        SELECT DISTINCT TABLE_NAME
            //                                                                        , SCHEMA_NAME
            //                                                                        ,	(SELECT SUM(""SAP_DATA_COUNT"")
	           //                                                                         FROM 
	           //                                                                         (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.SAPDBName}"".""{item.Module}""
	           //                                                                         {qry})) as ""SAP_DATA_COUNT"" 
	           //                                                                     ,'0' as ""ADDON_DATA_COUNT""
            //                                                                        FROM SYS.COLUMNS
            //                                                                        WHERE SCHEMA_NAME = '{item.SAPDBName}'  
            //                                                                        AND  (TABLE_NAME = '{item.Module}')
            //                                                                        ")

            //                                                          :
            //                                                          DataAccess.Select(QueryAccess.MSSQL_conString(
            //                                                          item.SAPServerName,
            //                                                          item.SAPDBuser,
            //                                                          item.SAPDBPassword,
            //                                                          item.SAPDBName), $@"  
            //                                                                        SELECT DISTINCT TABLE_NAME
            //                                                                          ,'{item.SAPDBName}' as ""SCHEMA_NAME""
            //                                                                          ,(SELECT SUM(""SAP_DATA_COUNT"")
	           //                                                                         FROM 
	           //                                                                         (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.Module}""
	           //                                                                         {qry})  as ""SAP_DATA_COUNT"" )
	           //                                                                       ,'0' as ""ADDON_DATA_COUNT"" ) as ""ADDON_DATA_COUNT""
            //                                                                          FROM INFORMATION_SCHEMA.COLUMNS  
            //                                                                        WHERE TABLE_NAME = '{item.Module}'
            //                                                                        ");

            //    qry = "";

            //    foreach (DataRow row in ReportTable.Rows)
            //    {
            //        DataRow DataCount = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
            //                                      item.SAPServerName,
            //                                      item.SAPDBuser,
            //                                      item.SAPDBPassword,
            //                                      item.SAPDBName), $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.SAPDBName}_{item.Module}""")
            //                                        .AsEnumerable().Select(x => x).FirstOrDefault()
            //                                      :
            //                                      DataAccess.Select(QueryAccess.MSSQL_conString(
            //                                      item.SAPServerName,
            //                                      item.SAPDBuser,
            //                                      item.SAPDBPassword,
            //                                      item.SAPDBName), $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.SAPDBName}_{item.Module}")
            //                                      .AsEnumerable().Select(x => x).FirstOrDefault();
            //        row["ADDON_DATA_COUNT"] = DataCount["ADDON_DATA_COUNT"];
            //    }
            //    item.SAPData = Convert.ToDouble(ReportTable.Rows[0][2]);
            //    item.AddonData = Convert.ToDouble(ReportTable.Rows[0][3]);
            //    item.Progress = (item.AddonData / item.SAPData) * 100;
            //}

            return model;
        }

        public UploadViewModel GetSAPtoSAPProgress(string Code)
        {
            var model = new UploadViewModel();

            model.ProcessList = _context.FieldMappings.Join(_context.ProcessMap, fm => fm.MapId, pm => pm.MapId, (fm, pm) => new { fm, pm }).
                                      Join(_context.Process, fmpm => fmpm.pm.ProcessId, pr => pr.ProcessId, (fmpm, pr) => new { fmpm, pr }).
                                      Join(_context.SAPSetup, fmpmpr => fmpmpr.fmpm.fm.SAPCode, sa => sa.SAPCode, (fmpmpr, sa) => new { fmpmpr, sa }).
                                      Where(fmpmprsa => fmpmprsa.fmpmpr.pr.ProcessCode == Code).
                                      Select(fmpmprsa => new UploadViewModel.Process
                                      {
                                          ProcessId = fmpmprsa.fmpmpr.pr.ProcessId,
                                          ProcessCode = fmpmprsa.fmpmpr.pr.ProcessCode,
                                          ProcessName = fmpmprsa.fmpmpr.pr.ProcessName,
                                          MapCode = fmpmprsa.fmpmpr.fmpm.fm.MapCode,
                                          MapName = fmpmprsa.fmpmpr.fmpm.fm.MapName,
                                          Module = fmpmprsa.fmpmpr.fmpm.fm.ModuleName,
                                          SAPDBName = fmpmprsa.sa.SAPDBName,
                                          SAPDBPassword = fmpmprsa.sa.SAPDBPassword,
                                          SAPDBuser = fmpmprsa.sa.SAPDBuser,
                                          SAPDBVersion = fmpmprsa.sa.SAPDBVersion,
                                          SAPServerName = fmpmprsa.sa.SAPServerName,

                                      }).ToList();

            //Get uploading progress
            foreach (var item in model.ProcessList)
            {
                string qry = "";
                DataTable ReportTable = new DataTable();
                DataTable Tables = new DataTable();
                Tables = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME, SCHEMA_NAME,'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT""
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{item.SAPDBName}'  AND  TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'")

                                                  :
                                                  DataAccess.Select(QueryAccess.MSSQL_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME,'' as ""SCHEMA_NAME"",'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT"" FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'");

                foreach (DataRow row in Tables.Rows)
                {
                    qry += item.SAPDBVersion.Contains("HANA") ? $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{item.SAPDBName}"".""{row["TABLE_NAME"].ToString()}"""
                                                              :
                                                                $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{row["TABLE_NAME"].ToString()}""";
                }
                ReportTable = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                                      item.SAPServerName,
                                                                      item.SAPDBuser,
                                                                      item.SAPDBPassword,
                                                                      item.SAPDBName), $@"
                                                                                    SELECT DISTINCT TABLE_NAME
                                                                                    , SCHEMA_NAME
	                                                                                ,'0' as ""SAP_DATA_COUNT""
                                                                                    ,	(SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.SAPDBName}_{item.Module}"") as ""ADDON_DATA_COUNT"" 
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{item.SAPDBName}'  
                                                                                    AND  (TABLE_NAME = '{item.Module}')
                                                                                    ")

                                                                      :
                                                                      DataAccess.Select(QueryAccess.MSSQL_conString(
                                                                      item.SAPServerName,
                                                                      item.SAPDBuser,
                                                                      item.SAPDBPassword,
                                                                      item.SAPDBName), $@"  
                                                                                    SELECT DISTINCT TABLE_NAME
                                                                                      ,'{item.SAPDBName}' as ""SCHEMA_NAME""
                                                                                      ,'0' as ""SAP_DATA_COUNT""
                                                                                      ,(SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.SAPDBName}_{item.Module}) as ""ADDON_DATA_COUNT"" 
                                                                                      FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME = '{item.Module}'
                                                                                    ");

                item.AddonData = Convert.ToDouble(ReportTable.Rows[0][3]);
            }

            return model;
        }
        #endregion

        public UploadViewModel GetProcess(string Code)
        {
            var model = new UploadViewModel();

            model.ProcessList = _context.FieldMappings.Join(_context.ProcessMap, fm => fm.MapId, pm => pm.MapId, (fm, pm) => new { fm, pm }).
                                      Join(_context.Process, fmpm => fmpm.pm.ProcessId, pr => pr.ProcessId, (fmpm, pr) => new { fmpm, pr }).
                                      Join(_context.AddonSetup, fmpmpr => fmpmpr.fmpm.fm.AddonCode, ad => ad.AddonCode, (fmpmpr, ad) => new { fmpmpr, ad }).
                                      Join(_context.SAPSetup, fmpmprad => fmpmprad.fmpmpr.fmpm.fm.SAPCode, sa => sa.SAPCode, (fmpmprad, sa) => new { fmpmprad, sa }).
                                      Where(fmpmpradsa => fmpmpradsa.fmpmprad.fmpmpr.pr.ProcessCode == Code).
                                      Select(fmpmpradsa => new UploadViewModel.Process
                                      {
                                          ProcessId = fmpmpradsa.fmpmprad.fmpmpr.pr.ProcessId,
                                          ProcessCode = fmpmpradsa.fmpmprad.fmpmpr.pr.ProcessCode,
                                          ProcessName = fmpmpradsa.fmpmprad.fmpmpr.pr.ProcessName,
                                          MapCode = fmpmpradsa.fmpmprad.fmpmpr.fmpm.fm.MapCode,
                                          MapName = fmpmpradsa.fmpmprad.fmpmpr.fmpm.fm.MapName,
                                          Module = fmpmpradsa.fmpmprad.fmpmpr.fmpm.fm.ModuleName,
                                          AddonCode = fmpmpradsa.fmpmprad.ad.AddonCode,
                                          AddonDBVersion = fmpmpradsa.fmpmprad.ad.AddonDBVersion,
                                          AddonServerName = fmpmpradsa.fmpmprad.ad.AddonServerName,
                                          AddonDBuser = fmpmpradsa.fmpmprad.ad.AddonDBuser,
                                          AddonDBPassword = fmpmpradsa.fmpmprad.ad.AddonDBPassword,
                                          AddonDBName = fmpmpradsa.fmpmprad.ad.AddonDBName,
                                          SAPDBName = fmpmpradsa.sa.SAPDBName,
                                          SAPDBPassword = fmpmpradsa.sa.SAPDBPassword,
                                          SAPDBuser = fmpmpradsa.sa.SAPDBuser,
                                          SAPDBVersion = fmpmpradsa.sa.SAPDBVersion,
                                          SAPServerName = fmpmpradsa.sa.SAPServerName,

                                      }).ToList();

            //Get uploading progress
            foreach(var item in model.ProcessList)
            {
                string qry = "";
                DataTable ReportTable = new DataTable();
                DataTable Tables = new DataTable();
                Tables = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME, SCHEMA_NAME,'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT""
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{item.SAPDBName}'  AND  TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'")

                                                  :
                                                  DataAccess.Select(QueryAccess.MSSQL_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME,'' as ""SCHEMA_NAME"",'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT"" FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'");

                foreach (DataRow row in Tables.Rows)
                {
                    qry += item.SAPDBVersion.Contains("HANA") ? $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{item.SAPDBName}"".""{row["TABLE_NAME"].ToString()}"""
                                                              :
                                                                $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{row["TABLE_NAME"].ToString()}""";
                }
                ReportTable = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                                      item.SAPServerName,
                                                                      item.SAPDBuser,
                                                                      item.SAPDBPassword,
                                                                      item.SAPDBName), $@"
                                                                                    SELECT DISTINCT TABLE_NAME
                                                                                    , SCHEMA_NAME
                                                                                    ,	(SELECT SUM(""SAP_DATA_COUNT"")
	                                                                                    FROM 
	                                                                                    (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.SAPDBName}"".""{item.Module}""
	                                                                                    {qry})) as ""SAP_DATA_COUNT"" 
	                                                                                ,'0' as ""ADDON_DATA_COUNT""
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{item.SAPDBName}'  
                                                                                    AND  (TABLE_NAME = '{item.Module}')
                                                                                    ")

                                                                      :
                                                                      DataAccess.Select(QueryAccess.MSSQL_conString(
                                                                      item.SAPServerName,
                                                                      item.SAPDBuser,
                                                                      item.SAPDBPassword,
                                                                      item.SAPDBName), $@"  
                                                                                    SELECT DISTINCT TABLE_NAME
                                                                                      ,'{item.SAPDBName}' as ""SCHEMA_NAME""
                                                                                      ,(SELECT SUM(""SAP_DATA_COUNT"")
	                                                                                    FROM 
	                                                                                    (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.Module}""
	                                                                                    {qry})  as ""SAP_DATA_COUNT"" )
	                                                                                  ,'0' as ""ADDON_DATA_COUNT"" ) as ""ADDON_DATA_COUNT""
                                                                                      FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME = '{item.Module}'
                                                                                    ");

                qry = "";

                foreach (DataRow row in ReportTable.Rows)
                {
                    DataRow DataCount = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.SAPDBName}_{item.Module}""")
                                                    .AsEnumerable().Select(x => x).FirstOrDefault()
                                                  :
                                                  DataAccess.Select(QueryAccess.MSSQL_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.SAPDBName}_{item.Module}")
                                                  .AsEnumerable().Select(x => x).FirstOrDefault();
                    row["ADDON_DATA_COUNT"] = DataCount["ADDON_DATA_COUNT"];
                }
                item.SAPData = Convert.ToDouble(ReportTable.Rows[0][2]);
                item.AddonData = Convert.ToDouble(ReportTable.Rows[0][3]);
                item.Progress = (item.AddonData / item.SAPData) * 100;
            }

            return model;
        }

        public UploadViewModel GetProgress(string Code)
        {
            var model = new UploadViewModel();

            model.ProcessList = _context.FieldMappings.Join(_context.ProcessMap, fm => fm.MapId, pm => pm.MapId, (fm, pm) => new { fm, pm }).
                                      Join(_context.Process, fmpm => fmpm.pm.ProcessId, pr => pr.ProcessId, (fmpm, pr) => new { fmpm, pr }).
                                      Join(_context.SAPSetup, fmpmpr => fmpmpr.fmpm.fm.SAPCode, sa => sa.SAPCode, (fmpmpr, sa) => new { fmpmpr, sa }).
                                      Where(fmpmprsa => fmpmprsa.fmpmpr.pr.ProcessCode == Code).
                                      Select(fmpmprsa => new UploadViewModel.Process
                                      {
                                          ProcessId = fmpmprsa.fmpmpr.pr.ProcessId,
                                          ProcessCode = fmpmprsa.fmpmpr.pr.ProcessCode,
                                          ProcessName = fmpmprsa.fmpmpr.pr.ProcessName,
                                          MapCode = fmpmprsa.fmpmpr.fmpm.fm.MapCode,
                                          MapName = fmpmprsa.fmpmpr.fmpm.fm.MapName,
                                          Module = fmpmprsa.fmpmpr.fmpm.fm.ModuleName,
                                          SAPDBName = fmpmprsa.sa.SAPDBName,
                                          SAPDBPassword = fmpmprsa.sa.SAPDBPassword,
                                          SAPDBuser = fmpmprsa.sa.SAPDBuser,
                                          SAPDBVersion = fmpmprsa.sa.SAPDBVersion,
                                          SAPServerName = fmpmprsa.sa.SAPServerName,

                                      }).ToList();

            //Get uploading progress
            foreach (var item in model.ProcessList)
            {
                string qry = "";
                DataTable ReportTable = new DataTable();
                DataTable Tables = new DataTable();
                Tables = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME, SCHEMA_NAME,'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT""
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{item.SAPDBName}'  AND  TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'")

                                                  :
                                                  DataAccess.Select(QueryAccess.MSSQL_conString(
                                                  item.SAPServerName,
                                                  item.SAPDBuser,
                                                  item.SAPDBPassword,
                                                  item.SAPDBName), $@"SELECT DISTINCT TABLE_NAME,'' as ""SCHEMA_NAME"",'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT"" FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME LIKE '{item.Module.Substring(1, item.Module.Length - 1)}%'");

                foreach (DataRow row in Tables.Rows)
                {
                    qry += item.SAPDBVersion.Contains("HANA") ? $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{item.SAPDBName}"".""{row["TABLE_NAME"].ToString()}"""
                                                              :
                                                                $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{row["TABLE_NAME"].ToString()}""";
                }
                ReportTable = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                                      item.SAPServerName,
                                                                      item.SAPDBuser,
                                                                      item.SAPDBPassword,
                                                                      item.SAPDBName), $@"
                                                                                    SELECT DISTINCT TABLE_NAME
                                                                                    , SCHEMA_NAME
	                                                                                ,'0' as ""SAP_DATA_COUNT""
                                                                                    ,	(SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.SAPDBName}_{item.Module}"") as ""ADDON_DATA_COUNT"" 
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{item.SAPDBName}'  
                                                                                    AND  (TABLE_NAME = '{item.Module}')
                                                                                    ")

                                                                      :
                                                                      DataAccess.Select(QueryAccess.MSSQL_conString(
                                                                      item.SAPServerName,
                                                                      item.SAPDBuser,
                                                                      item.SAPDBPassword,
                                                                      item.SAPDBName), $@"  
                                                                                    SELECT DISTINCT TABLE_NAME
                                                                                      ,'{item.SAPDBName}' as ""SCHEMA_NAME""
                                                                                      ,'0' as ""SAP_DATA_COUNT""
                                                                                      ,(SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.SAPDBName}_{item.Module}) as ""ADDON_DATA_COUNT"" 
                                                                                      FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME = '{item.Module}'
                                                                                    ");

                //qry = "";

                //foreach (DataRow row in ReportTable.Rows)
                //{
                //    DataRow DataCount = item.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                //                                  item.SAPServerName,
                //                                  item.SAPDBuser,
                //                                  item.SAPDBPassword,
                //                                  item.SAPDBName), $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.Module}""")
                //                                    .AsEnumerable().Select(x => x).FirstOrDefault()
                //                                  :
                //                                  DataAccess.Select(QueryAccess.MSSQL_conString(
                //                                  item.SAPServerName,
                //                                  item.SAPDBuser,
                //                                  item.SAPDBPassword,
                //                                  item.SAPDBName), $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.Module}")
                //                                  .AsEnumerable().Select(x => x).FirstOrDefault();
                //    row["ADDON_DATA_COUNT"] = DataCount["ADDON_DATA_COUNT"];
                //}
                //item.SAPData = Convert.ToDouble(ReportTable.Rows[0][2]);
                item.AddonData = Convert.ToDouble(ReportTable.Rows[0][3]);
                //item.Progress = (item.AddonData / item.SAPData) * 100;
            }

            return model;
        }

        public async Task<bool> Upload(string code)
        {
            bool ret;
            if (code != "" || code != string.Empty)
            {
                #region Version_1
                ////FIND SCHEDULE TASK OF FIELD MAPPING
                //var taskcode = _context.FieldMappings.Join(_context.Process, xf => xf.MapId, xp => xp.MapId, (xf, xp) => new { xp, xf })
                //                                        .Join(_context.Schedules, xs => xs.xp.ProcessCode, sc => sc.Process, (xs, sc) => new { xs, sc })
                //                                        .Where(x => x.xs.xf.MapCode == code)
                //                                        .Select(x => x.sc.SchedCode).FirstOrDefault();
                //if (!string.IsNullOrEmpty(taskcode))
                //{
                //    Properties.Settings prop = new Properties.Settings();
                //    //TaskSchedulerHelpers.RunTask(taskcode, prop.ServerName, prop.User, prop.Domain, prop.Password);
                //    tsched.RunTask(code, prop.ServerName, prop.User, prop.Domain, prop.Password);
                //    ret = true;
                //}
                //else if (!string.IsNullOrEmpty(code) && string.IsNullOrEmpty(taskcode))
                //{
                //    ////THIS IS FOR THE SINGLE PROCESS W/OUT FIELD MAPPING
                //    Properties.Settings prop = new Properties.Settings();
                //    //TaskSchedulerHelpers.RunTask(code, prop.ServerName, prop.User, prop.Domain, prop.Password);
                //    tsched.RunTask(code, prop.ServerName, prop.User, prop.Domain, prop.Password);
                //    ret = true;
                //}
                //else
                //{
                //    ret = false;
                //}
                #endregion

                #region Version_2

                var url = _context.Schedules.Where(sel => sel.SchedCode == code)
                          .Join(_context.Process, sch => sch.Process, pr => pr.ProcessCode, (sch, pr) => new { sch, pr })
                          .Join(_context.APISetups, apc => apc.pr.APICode, ap => ap.APICode, (apc, ap) => new { apc, ap })
                          .Select(sel => sel.ap.APIURL).FirstOrDefault();
                if (!string.IsNullOrEmpty(url))
                {
                    Settings setting = new Settings();
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    System.Security.SecureString ssPwd = new System.Security.SecureString();
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.FileName = "powershell.exe";
                    proc.StartInfo.Arguments = $@"-Command ""Invoke-RestMethod -Method 'Post' -Uri {url}?taskname={code}"" ";
                    proc.StartInfo.Domain = setting.Domain;
                    proc.StartInfo.UserName = setting.User;
                    string password = setting.Password;
                    for (int x = 0; x < password.Length; x++)
                    {
                        ssPwd.AppendChar(password[x]);
                    }
                    password = "";
                    proc.StartInfo.Password = ssPwd;
                    proc.Start();

                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "TASK RUNNING", CreateDate = DateTime.Now, ApiUrl = $@"POST {url}", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                    _context.SaveChanges();

                    ret = true;
                }
                else { ret = false; }
                #endregion
            }
            else
            {
                ret = false;
            }

            return ret;
        }

        public async Task<string> UploadMultiple(string[] code)
        {
            return postController.SapUploadDocument(code);
        }

        public async Task<string> UploadMultipleSAPtoSAP(string[] code)
        {
            return postController.SapUploadDocument(code);
        }
        
        public async Task<string> UploadPlatForm(string code, string DateFrom, string DateTo)
        {

            var ProcessType = _context.Process.Where(x => x.ProcessCode == code).Select(x => x.ProcessType).FirstOrDefault();


            var SchedCode = "";
            if (ProcessType.Contains("Shopee"))
            {
                //SchedCode = _context.Schedules.Where(x => x.Process.Equals("Shopee_SalesOrder")).Select(x => x.SchedCode).FirstOrDefault();
                //var res= await _shopee.ManualPostSalesOrder(SchedCode, DateFrom, DateTo);
                //if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                //{
                //    return okResult.Content;
                //}

                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }else if (ProcessType.Contains("Lazada"))
            {
                SchedCode = _context.Schedules.Where(x => x.Process == code).Select(x => x.SchedCode).FirstOrDefault();
                var res = await _lazada.PostOrderSAP(SchedCode, DateFrom, DateTo);
                if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                {
                    return okResult.Content;
                }
                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }
            else if (ProcessType.Contains("Tiktok"))
            {
                 SchedCode = _context.Schedules.Where(x => x.Process == code).Select(x => x.SchedCode).FirstOrDefault();
                var res = await _tiktok.PostOrderSAP(SchedCode, DateFrom, DateTo);
                if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                {
                    return okResult.Content;
                }
                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }
            else if (ProcessType.Contains("Zalora"))
            {
                //SchedCode = _context.Schedules.Where(x => x.Process.Equals("Zalora_SalesOrder")).Select(x => x.SchedCode).FirstOrDefault();
                //var res = await _zalora.ManualPostSalesOrder(SchedCode, DateFrom, DateTo);
                //if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                //{
                //    return okResult.Content;
                //}
                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }
            else if (ProcessType.Contains("Shopify"))
            {
                //SchedCode = _context.Schedules.Where(x => x.Process.Equals("Shopify_SalesOrder")).Select(x => x.SchedCode).FirstOrDefault();
                //var res = await _shopify.ManualPostShopifyToNetsuite(SchedCode, DateFrom, DateTo);
                //if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                //{
                //    return okResult.Content;
                //}
                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }
            else if (ProcessType.Contains("Sales Order"))
            {
                SchedCode = _context.Schedules.Where(x => x.Process == code).Select(x => x.SchedCode).FirstOrDefault();
                var res = await primary.UploadPrimaryAccount(SchedCode);

                if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                {
                    return okResult.Content;
                }
                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }
            else if (ProcessType.Contains("Return To Vendor"))
            {
                SchedCode = _context.Schedules.Where(x => x.Process == code).Select(x => x.SchedCode).FirstOrDefault();
                var res = await primary.UploadPrimaryAccount(SchedCode);

                if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                {
                    return okResult.Content;
                }

                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }
            else if (ProcessType.Contains("EIS"))
            {
                SchedCode = _context.Schedules.Where(x => x.Process == code).Select(x => x.SchedCode).FirstOrDefault();
                var res = await eis.GenerateInvoiceJson(SchedCode);

                if (res is System.Web.Http.Results.OkNegotiatedContentResult<string> okResult)
                {
                    return okResult.Content;
                }

                // Handle other possible IHttpActionResult types as needed
                return string.Empty;
            }
            return "";
        }
    }
}