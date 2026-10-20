using ClosedXML.Excel;
//using CrystalDecisions.CrystalReports.Engine;
//using CrystalDecisions.Shared;
using DataAccessLayer.Class;
using DomainLayer;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinqToExcel.Extensions;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.WebPages;
using WebGrease.Css.Extensions;
using static DomainLayer.ViewModels.PostingViewModel;
using System.Threading.Tasks;
using LinkBoxUI.Controllers.api;
using NPOI.SS.Formula.Functions;
using Microsoft.Graph.Models;
using System.Net.Http;
using System.Net;

namespace LinkBoxUI.Services
{
    public class GlobalServices : IGlobalRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        public DataAccess da = new DataAccess();
        public SAPAccess sapAces = new SAPAccess();
        public PostHelper phelper = new PostHelper();
        public EmailHelpers eml = new EmailHelpers();
        private readonly QueryAccess qa = new QueryAccess();
        public List<int> GetModules(int id)
        {
            var mod = from um in _context.UserModules.Where(a => a.UserId == id && a.IsActive == true)
                      select um.ModId;
            mod.ToList();
            List<int> mods = new List<int>();
            for (int x = 1; x <= 11; x++)
            {
                mods.Add(x);
            }
            foreach (var x in mod)
            {
                mods.Remove(x);
            }
            return mods;
        }

        public DashboardViewModel GetDashBoardReport()
        {
            var model = new DashboardViewModel();

            DataTable Tables = new DataTable();
            DataTable AddonTables = new DataTable();
            DataTable ReportTable = new DataTable();

            string qry = "";

            try
            {

                model.CounterView = new DashboardViewModel.Counter
                {
                    Tiktok = _context.PlatformTable.Where(x => x.Platform == "Tiktok").ToList().Count(),
                    Lazada = _context.PlatformTable.Where(x => x.Platform == "Lazada").ToList().Count(),
                    Shopee = _context.PlatformTable.Where(x => x.Platform == "Shopee").ToList().Count(),
                    ErrorCount = _context.SystemLogs
                    .Where(x =>
                        (
                            // TikTok/Lazada: Module must have a value
                            (
                                (x.Task.Contains("TIKTOK") || x.Task.Contains("LAZADA")) &&
                                x.Module != null &&
                                x.Module.Trim() != ""
                            )
                            ||
                            // Other Tasks: Module can be null/empty
                            (
                                !x.Task.Contains("TIKTOK") &&
                                !x.Task.Contains("LAZADA")
                            )
                        )
                    )
                    .Count()
                };

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                model.dailyCounterView = new DashboardViewModel.Counter
                {
                    Tiktok = _context.PlatformTable
                        .Count(x => x.Platform == "Tiktok" &&
                                    x.UploadDate >= today &&
                                    x.UploadDate < tomorrow),

                    Lazada = _context.PlatformTable
                        .Count(x => x.Platform == "Lazada" &&
                                    x.UploadDate >= today &&
                                    x.UploadDate < tomorrow),

                    Shopee = _context.PlatformTable
                        .Count(x => x.Platform == "Shopee" &&
                                    x.UploadDate >= today &&
                                    x.UploadDate < tomorrow),

                    ErrorCount = _context.SystemLogs
                                .Where(x =>
                                    x.CreateDate >= today &&
                                    x.CreateDate < tomorrow &&
                                    (
                                        // TikTok/Lazada: Module must have a value
                                        (
                                            (x.Task.Contains("TIKTOK") || x.Task.Contains("LAZADA")) &&
                                            x.Module != null &&
                                            x.Module.Trim() != ""
                                        )
                                        ||
                                        // Other Tasks: Module can be null/empty
                                        (
                                            !x.Task.Contains("TIKTOK") &&
                                            !x.Task.Contains("LAZADA")
                                        )
                                    )
                                )
                                .Count()
                };


                model.DashboardFieldMappingList = new List<DashboardViewModel.DashboardFieldMapping>();
                //model.DashboardFieldMappingList = _context.FieldMappings.Join(_context.SAPSetup,
                //                            x => x.SAPCode,
                //                            y => y.SAPCode,
                //                            (x, y) => new { x, y })
                //                    .Join(_context.AddonSetup,
                //                            a => a.x.AddonCode,
                //                            b => b.AddonCode,
                //                            (b, a) => new { b, a })
                //                    .Select((z) => new DashboardViewModel.DashboardFieldMapping
                //                    {
                //                        MapCode = z.b.x.MapCode,
                //                        MapName = z.b.x.MapName,
                //                        ModuleName = z.b.x.ModuleName,
                //                        AddonCode = z.b.x.AddonCode,
                //                        AddonDBVersion = z.a.AddonDBVersion,
                //                        AddonServerName = z.a.AddonServerName,
                //                        AddonDBuser = z.a.AddonDBuser,
                //                        AddonDBPassword = z.a.AddonDBPassword,
                //                        AddonDBName = z.a.AddonDBName,
                //                        SAPDBVersion = z.b.y.SAPDBVersion,
                //                        SAPServerName = z.b.y.SAPServerName,
                //                        SAPDBuser = z.b.y.SAPDBuser,
                //                        SAPDBPassword = z.b.y.SAPDBPassword,
                //                        SAPDBName = z.b.y.SAPDBName
                //                    }).OrderBy(x => x.AddonDBName).ThenBy(x => x.ModuleName).ToList();

                //model.DashboardReportList = new List<DashboardViewModel.DashboardReport>();

                //foreach (var item in model.DashboardFieldMappingList)
                //{
                //    #region GetTables
                //    Tables = DataAccess.Select(item.SAPDBVersion,
                //                                    item.SAPServerName,Hello po Ms. Kate Asking lang po si Vea if kelan kayo available . to endorse ung issue sa Other Income na naka negative . Thank you poHello po Ms. Kate Asking lang po si Vea if kelan kayo available . to endorse ung issue sa Other Income na naka negative . Thank you po
                //                                    item.SAPDBuser,
                //                                    item.SAPDBPassword,
                //                                    item.SAPDBName, $@"SELECT DISTINCT TABLE_NAME, SCHEMA_NAME,'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT""" +
                //                                                 (item.SAPDBVersion.Contains("HANA") ? $@"FROM SYS.COLUMNS
                //                                                WHERE SCHEMA_NAME = '{item.SAPDBName}'  AND  TABLE_NAME LIKE '{item.ModuleName.Substring(1, item.ModuleName.Length - 1)}%'
                //                                                ORDER BY TABLE_NAME"
                //                                                :
                //                                                $@"FROM INFORMATION_SCHEMA.COLUMNS  
                //                                                WHERE TABLE_NAME LIKE '{item.ModuleName.Substring(1, item.ModuleName.Length - 1)}%'
                //                                                ORDER BY TABLE_NAME")
                //                                                );
                //    #endregion

                //    #region GetSAPDataCount
                //    foreach (DataRow row in Tables.Rows)
                //    {
                //        qry += item.SAPDBVersion.Contains("HANA") ? $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{item.SAPDBName}"".""{row["TABLE_NAME"].ToString()}"""
                //                                                  :
                //                                                    $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{row["TABLE_NAME"].ToString()}""";
                //    }
                //    ReportTable = DataAccess.Select(item.SAPDBVersion,
                //                                    item.SAPServerName,
                //                                    item.SAPDBuser,
                //                                    item.SAPDBPassword,
                //                                    item.SAPDBName, item.SAPDBVersion.Contains("HANA") ? $@"
                //                                                SELECT DISTINCT TABLE_NAME
                //                                                , SCHEMA_NAME
                //                                                ,	(SELECT SUM(""SAP_DATA_COUNT"")
                //                                                 FROM 
                //                                                 (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.SAPDBName}"".""{item.ModuleName}""
                //                                                 {qry})) as ""SAP_DATA_COUNT"" 
                //                                             ,'0' as ""ADDON_DATA_COUNT""
                //                                                FROM SYS.COLUMNS
                //                                                WHERE SCHEMA_NAME = '{item.SAPDBName}'  
                //                                                AND  (TABLE_NAME = '{item.ModuleName}')
                //                                                "
                //                                                :
                //                                                $@"  
                //                                                SELECT DISTINCT TABLE_NAME
                //                                                    ,'{item.SAPDBName}' as ""SCHEMA_NAME""
                //                                                    ,(SELECT SUM(""SAP_DATA_COUNT"")
                //                                                 FROM 
                //                                                 (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.ModuleName}""
                //                                                 {qry})  as ""SAP_DATA_COUNT"" )
                //                                                 ,'0' as ""ADDON_DATA_COUNT"" ) as ""ADDON_DATA_COUNT""
                //                                                    FROM INFORMATION_SCHEMA.COLUMNS  
                //                                                WHERE TABLE_NAME = '{item.ModuleName}'");
                //    #endregion

                //    #region GetAddonDataCount
                //    qry = "";

                //    foreach (DataRow row in ReportTable.Rows)
                //    {
                //        DataRow DataCount = DataAccess.Select(item.SAPDBVersion,
                //                                      item.SAPServerName,
                //                                      item.SAPDBuser,
                //                                      item.SAPDBPassword,
                //                                      item.SAPDBName, item.SAPDBVersion.Contains("HANA") ?
                //                                      $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.SAPDBName}_{item.ModuleName}"""
                //                                      :
                //                                      $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.SAPDBName}_{item.ModuleName}"
                //                                      ).AsEnumerable().Select(x => x).FirstOrDefault();
                //        row["ADDON_DATA_COUNT"] = DataCount["ADDON_DATA_COUNT"];
                //    }
                //    #endregion

                //    //GetIssues
                //    int IssueCount = _context.SystemLogs.Where(x => x.Task == "UPLOAD_ERROR").Count();

                //    DashboardViewModel.DashboardReport dashboardReport = new DashboardViewModel.DashboardReport();
                //    foreach (DataRow x in ReportTable.Rows)
                //    {
                //        string db = x["SCHEMA_NAME"].ToString();
                //        string table = x["TABLE_NAME"].ToString();
                //        dashboardReport.DBName = x["SCHEMA_NAME"].ToString();
                //        dashboardReport.Module = x["TABLE_NAME"].ToString();
                //        dashboardReport.SAPDataNo = Convert.ToInt32(x["SAP_DATA_COUNT"]);
                //        dashboardReport.AddonDataNo = Convert.ToInt32(x["ADDON_DATA_COUNT"]);
                //        //dashboardReport.IssueNo = IssueCount;
                //        dashboardReport.IssueNo = _context.SystemLogs.Where(y => y.Task == "UPLOAD_ERROR" && y.Database == db && y.Module == table && y.CreateDate >= DateTime.Today).Count();
                //        //dashboardReport.IssueNo = _context.SystemLogs.Where(y => y.Task == "UPLOAD_ERROR" && y.Database == db && y.Module == table).Count();
                //    }

                //    model.DashboardReportList.Add(dashboardReport);

                //}

                return model;
            }
            catch (Exception ex)
            {
                return model;
            }

        }

        public DashboardViewModel GetReportProgress()
        {
            var model = new DashboardViewModel();

            DataTable Tables = new DataTable();
            DataTable AddonTables = new DataTable();
            DataTable ReportTable = new DataTable();

            string qry = "";

            try
            {
                model.DashboardFieldMappingList = new List<DashboardViewModel.DashboardFieldMapping>();
                //model.DashboardFieldMappingList = _context.FieldMappings.Join(_context.SAPSetup,
                //                            x => x.SAPCode,
                //                            y => y.SAPCode,
                //                            (x, y) => new { x, y })
                //                    .Join(_context.AddonSetup,
                //                            a => a.x.AddonCode,
                //                            b => b.AddonCode,
                //                            (b, a) => new { b, a })
                //                    .Select((z) => new DashboardViewModel.DashboardFieldMapping
                //                    {
                //                        MapCode = z.b.x.MapCode,
                //                        MapName = z.b.x.MapName,
                //                        ModuleName = z.b.x.ModuleName,
                //                        AddonCode = z.b.x.AddonCode,
                //                        AddonDBVersion = z.a.AddonDBVersion,
                //                        AddonServerName = z.a.AddonServerName,
                //                        AddonDBuser = z.a.AddonDBuser,
                //                        AddonDBPassword = z.a.AddonDBPassword,
                //                        AddonDBName = z.a.AddonDBName,
                //                        SAPDBVersion = z.b.y.SAPDBVersion,
                //                        SAPServerName = z.b.y.SAPServerName,
                //                        SAPDBuser = z.b.y.SAPDBuser,
                //                        SAPDBPassword = z.b.y.SAPDBPassword,
                //                        SAPDBName = z.b.y.SAPDBName
                //                    }).OrderBy(x => x.AddonDBName).ThenBy(x => x.ModuleName).ToList();

                model.DashboardReportList = new List<DashboardViewModel.DashboardReport>();



                //foreach (var item in model.DashboardFieldMappingList)
                //{
                //    #region GetTables
                //    Tables = DataAccess.Select(item.SAPDBVersion,
                //                                item.SAPServerName,
                //                                item.SAPDBuser,
                //                                item.SAPDBPassword,
                //                                item.SAPDBName, item.SAPDBVersion.Contains("HANA") ? $@"SELECT DISTINCT TABLE_NAME, SCHEMA_NAME,'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT""
                //                                            FROM SYS.COLUMNS
                //                                            WHERE SCHEMA_NAME = '{item.SAPDBName}'  AND  TABLE_NAME LIKE '{item.ModuleName.Substring(1, item.ModuleName.Length - 1)}%'" 
                //                                            :
                //                                            $@"SELECT DISTINCT TABLE_NAME,'' as ""SCHEMA_NAME"",'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT"" FROM INFORMATION_SCHEMA.COLUMNS  
                //                                            WHERE TABLE_NAME LIKE '{item.ModuleName.Substring(1, item.ModuleName.Length - 1)}%'");
                //    #endregion

                //    #region GetAddonDataCount
                //    ReportTable = DataAccess.Select(item.SAPDBVersion,
                //                                    item.SAPServerName,
                //                                    item.SAPDBuser,
                //                                    item.SAPDBPassword,
                //                                    item.SAPDBName, item.SAPDBVersion.Contains("HANA") ? $@"
                //                                                SELECT DISTINCT TABLE_NAME
                //                                                , SCHEMA_NAME
                //                                             ,'0' as ""SAP_DATA_COUNT""
                //                                                ,	(SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.SAPDBName}_{item.ModuleName}"") as ""ADDON_DATA_COUNT"" 
                //                                                FROM SYS.COLUMNS
                //                                                WHERE SCHEMA_NAME = '{item.SAPDBName}'  
                //                                                AND  (TABLE_NAME = '{item.ModuleName}')
                //                                                "
                //                                                :
                //                                                $@"  
                //                                                SELECT TABLE_NAME
                //                                                    ,'{item.SAPDBName}' as ""SCHEMA_NAME""
                //                                                    ,'0' as ""SAP_DATA_COUNT""
                //                                                    ,(SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.SAPDBName}_{item.ModuleName}) as ""ADDON_DATA_COUNT"" 
                //                                                    FROM INFORMATION_SCHEMA.COLUMNS  
                //                                                WHERE TABLE_NAME = '{item.ModuleName}'
                //                                                ");
                //    #endregion

                //    //GetIssues
                //    int IssueCount = _context.SystemLogs.Where(x => x.Task == "UPLOAD_ERROR").Count();

                //    DashboardViewModel.DashboardReport dashboardReport = new DashboardViewModel.DashboardReport();
                //    foreach (DataRow x in ReportTable.Rows)
                //    {
                //        string db = x["SCHEMA_NAME"].ToString();
                //        string table = x["TABLE_NAME"].ToString();
                //        dashboardReport.DBName = x["SCHEMA_NAME"].ToString();
                //        dashboardReport.Module = x["TABLE_NAME"].ToString();
                //        dashboardReport.SAPDataNo = Convert.ToInt32(x["SAP_DATA_COUNT"]);
                //        dashboardReport.AddonDataNo = Convert.ToInt32(x["ADDON_DATA_COUNT"]);
                //        //dashboardReport.IssueNo = IssueCount;
                //        dashboardReport.IssueNo = _context.SystemLogs.Where(y => y.Task == "UPLOAD_ERROR" && y.Database == db && y.Module == table && y.CreateDate >= DateTime.Today).Count();
                //        //dashboardReport.IssueNo = _context.SystemLogs.Where(y => y.Task == "UPLOAD_ERROR" && y.Database == db && y.Module == table).Count();
                //    }

                //    model.DashboardReportList.Add(dashboardReport);

                //}

                return model;
            }
            catch (Exception ex)
            {
                return model;
            }

        }

        public DashboardViewModel GetItemStock()
        {
            var model = new DashboardViewModel();
            model.SAPList = _context.FieldMappings.Select(x => new DashboardViewModel.SAPConfig
            { Code = x.MapCode }).ToList();

            return model;
        }

        public DashboardViewModel GetItemStock(string Code)
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
                var PathSettings = _context.PathSetup.Where(x => x.PathCode == config.FieldMapDetails.PathCode).FirstOrDefault();
                if (PathSettings != null)
                {
                    Setup.SAPDbDetails = _context.SAPSetup.AsEnumerable()?.Where(x => x.SAPCode == Code).Select(x => new SetupCreateViewModel.SAPViewModel
                    {
                        SAPId = x.SAPId,
                        SAPCode = x.SAPCode,
                        SAPDBVersion = x.SAPDBVersion,
                        SAPServerName = x.SAPServerName,
                        SAPIPAddress = x.SAPIPAddress,
                        SAPDBName = x.SAPDBName,
                        SAPVersion = x.SAPVersion,
                        SAPLicensePort = x.SAPLicensePort,
                        SAPDBPort = x.SAPDBPort,
                        SAPDBuser = x.SAPDBuser,
                        SAPDBPassword = x.SAPDBPassword,
                        SAPUser = x.SAPUser,
                        SAPPassword = x.SAPPassword,
                        IsActive = x.IsActive
                    }).FirstOrDefault();

                    foreach (var item in DataAccess.GetFileListFTP(PathSettings.RemotePath, PathSettings.RemoteUserName, PathSettings.RemotePassword))
                    {
                        if ((item.ToLower().Contains("d") && !item.ToLower().Contains("z")) && item.ToLower().Contains(".csv"))
                        {
                            Stream stream = DataAccess.ReadFromFTP(PathSettings.RemotePath + item, PathSettings.RemoteUserName, PathSettings.RemotePassword);
                            table.Merge(ExcelAccessV2.GetFileData(stream, item).Tables[0]);
                        }
                    }

                    model.ItemList = Execute.ExecuteQuery(Setup)?.AsEnumerable()?.Select(x => new DashboardViewModel.Item
                    {
                        ItemCode = x["ItemCode"].ToString(),
                        ItemName = x["ItemName"].ToString(),
                        Uom = x["SalUnitMsr"].ToString(),
                        Price = ConcatDecimal(x["Price"].ToString()),
                        LastUpdate = DateTime.Now.ToString(),
                        Stock = da.ConcatDecimal(x["Stock"].ToString()),
                        SalesPrice = da.ConcatDecimal(table.AsEnumerable()?.Where(y => y["ProductID"].ToString() == x["ItemCode"].ToString()).Select(y => y["UnitPrice"].ToString()).FirstOrDefault()),
                        SalesQty = table.AsEnumerable()?.Where(y => y["ProductID"].ToString() == x["ItemCode"].ToString()).Sum(y => Convert.ToDouble(y["QtySold"].ToString())).ToString(),
                        CurrentStock = da.ConcatDecimal((Convert.ToDouble(x["Stock"].ToString()) - table.AsEnumerable().Where(y => y["ProductID"].ToString() == x["ItemCode"].ToString()).Sum(y => Convert.ToDouble(y["QtySold"].ToString()))).ToString())

                    }).ToList() ?? new List<DashboardViewModel.Item>();

                    model.ItemComparisonList = model.ItemList.AsEnumerable().Where(x => x.Price != x.SalesPrice && Convert.ToDouble(x.SalesPrice) != 0).ToList();
                }
            }


            return model;
        }

        public PostingViewModel GetCredentials(string Task)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Join(_context.Headers, fm => fm.MapId, h => h.MapId, (fm, h) => new { fm, h })
                          .Join(_context.Rows, fmh => fmh.fm.MapId, r => r.MapId, (fmh, r) => new { fmh, r })
                          .Join(_context.PathSetup, fmhr => fmhr.fmh.fm.PathCode, p => p.PathCode, (fmhr, p) => new { fmhr, p })
                          .Join(_context.AddonSetup, fmhrp => fmhrp.fmhr.fmh.fm.AddonCode, a => a.AddonCode, (fmhrp, a) => new { fmhrp, a })
                          .Join(_context.SAPSetup, fmhrpa => fmhrpa.fmhrp.fmhr.fmh.fm.SAPCode, s => s.SAPCode, (fmhrpa, s) => new { fmhrpa, s })
                          .Join(_context.Process, fmhrpas => fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.MapId, pr => pr.MapId, (fmhrpas, pr) => new { fmhrpas, pr })
                          .Join(_context.Schedules.Where(x => x.SchedCode == Task), fmhrpaspr => fmhrpaspr.pr.ProcessCode, sh => sh.Process, (fmhrpaspr, sh) => new PostingViewModel.Credential
                          {
                              MapId = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.MapId,
                              AddonCode = fmhrpaspr.fmhrpas.fmhrpa.a.AddonCode,
                              AddonDBName = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBName,
                              AddonIPAddress = fmhrpaspr.fmhrpas.fmhrpa.a.AddonIPAddress,
                              AddonPort = fmhrpaspr.fmhrpas.fmhrpa.a.AddonPort,
                              AddonServerName = fmhrpaspr.fmhrpas.fmhrpa.a.AddonServerName,
                              AddonDBuser = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBuser,
                              AddonDBPassword = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBPassword,
                              LocalPath = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.LocalPath,
                              FTPPath = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.RemotePath,
                              FTPUser = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.RemoteUserName,
                              FTPPass = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.RemotePassword,
                              BackupPath = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.BackupPath,
                              HeaderName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.h.SourceTableName,
                              RowName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.r.TableName,
                              HeaderWorksheet = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.HeaderWorksheet,
                              RowWorksheet = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.RowWorksheet,
                              FileType = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.FileType,
                              FileName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.FileName,
                              SAPServerName = fmhrpaspr.fmhrpas.s.SAPServerName,
                              SAPIPAddress = fmhrpaspr.fmhrpas.s.SAPIPAddress,
                              SAPDBName = fmhrpaspr.fmhrpas.s.SAPDBName,
                              SAPLicensePort = fmhrpaspr.fmhrpas.s.SAPLicensePort,
                              SAPDBPort = fmhrpaspr.fmhrpas.s.SAPDBPort,
                              SAPDBuser = fmhrpaspr.fmhrpas.s.SAPDBuser,
                              SAPDBPassword = fmhrpaspr.fmhrpas.s.SAPDBPassword,
                              SAPUser = fmhrpaspr.fmhrpas.s.SAPUser,
                              SAPPassword = fmhrpaspr.fmhrpas.s.SAPPassword,
                              PostSAP = fmhrpaspr.pr.PostSAP,
                              Module = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.ModuleName
                          }).FirstOrDefault();

            return model;
        }

        public PostingViewModel GetCredentialsMapCode(string Code)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Where(x => x.MapCode == Code).Join(_context.Headers, fm => fm.MapId, h => h.MapId, (fm, h) => new { fm, h })
                          .GroupJoin(_context.Rows, fmh => fmh.fm.MapId, r => r.MapId, (fmh, r) => new { fmh, r })
                          .Join(_context.AddonSetup, fmhrp => fmhrp.fmh.fm.AddonCode, a => a.AddonCode, (fmhrp, a) => new { fmhrp, a })
                          .Join(_context.SAPSetup, fmhrpa => fmhrpa.fmhrp.fmh.fm.SAPCode, s => s.SAPCode, (fmhrpa, s) => new { fmhrpa, s })
                          .Join(_context.ProcessMap, fmhrpas => fmhrpas.fmhrpa.fmhrp.fmh.fm.MapId, pm => pm.MapId, (fmhrpas, pm) => new { fmhrpas, pm })
                          .Join(_context.Process, fmhrpaspm => fmhrpaspm.pm.ProcessId, pr => pr.ProcessId, (fmhrpaspm, pr) => new { fmhrpaspm, pr })
                          .Join(_context.ModuleSetup, fmhrpaspmpr => fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.ModuleName, ms => ms.ModuleCode, (fmhrpaspmpr, ms) => new PostingViewModel.Credential
                          {
                              MapId = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.MapId,
                              AddonCode = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonCode,
                              AddonDBName = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonDBName,
                              AddonDBVersion = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonDBVersion,
                              AddonIPAddress = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonIPAddress,
                              AddonPort = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonPort,
                              AddonServerName = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonServerName,
                              AddonDBuser = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonDBuser,
                              AddonDBPassword = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.a.AddonDBPassword,
                              LocalPath = "",
                              FTPPath = "",
                              FTPUser = "",
                              FTPPass = "",
                              BackupPath = "",
                              HeaderName = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.h.SourceTableName,
                              RowName = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.r.Select(sel => sel.TableName).FirstOrDefault(),
                              HeaderWorksheet = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.HeaderWorksheet,
                              RowWorksheet = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.RowWorksheet,
                              FileType = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.FileType,
                              FileName = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.FileName,
                              SAPServerName = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPServerName,
                              SAPIPAddress = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPIPAddress,
                              SAPDBName = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPDBName,
                              SAPDBVersion = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPDBVersion,
                              SAPLicensePort = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPLicensePort,
                              SAPDBPort = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPDBPort,
                              SAPDBuser = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPDBuser,
                              SAPDBPassword = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPDBPassword,
                              SAPSldServer = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPSldServer,
                              SAPUser = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPUser,
                              SAPPassword = fmhrpaspmpr.fmhrpaspm.fmhrpas.s.SAPPassword,
                              PostSAP = fmhrpaspmpr.pr.PostSAP,
                              Module = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.ModuleName,
                              LastSync = fmhrpaspmpr.fmhrpaspm.fmhrpas.fmhrpa.fmhrp.fmh.fm.LastSync,
                              PrimaryKey = ms.PrimaryKey

                          }).FirstOrDefault();
            return model;
        }

        public PostingViewModel GetFileToSAPCredentialsMapCode(string Code)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Where(x => x.MapCode == Code)
                        .GroupJoin(_context.PathSetup
                                    , fmap => fmap.PathCode
                                    , pset => pset.PathCode
                                    , (fmap, ps) => ps.Select(sel => new { fm = fmap, ps = sel }).DefaultIfEmpty(new { fm = fmap, ps = (PathSetup)null }))
                        .SelectMany(g => g)
                        .GroupJoin(_context.SAPSetup
                                    , fm => fm.fm.SAPCode
                                    , so => so.SAPCode
                                    , (fm, so) => so.Select(sel => new { fm = fm, so = sel }).DefaultIfEmpty(new { fm = fm, so = (SAPSetup)null }))
                        .SelectMany(g1 => g1)
                        .GroupJoin(_context.ModuleSetup
                                    , fmstd => fmstd.fm.fm.DestModule
                                    , msd => msd.ModuleCode
                                    , (fmstd, msd) => msd.Select(sel => new { fmstd = fmstd, msd = sel }).DefaultIfEmpty(new { fmstd = fmstd, msd = (ModuleSetup)null }))
                        .SelectMany(g2 => g2).ToList()
                        .Select(f => new PostingViewModel.Credential
                        {
                            MapId = f.fmstd.fm.fm.MapId,
                            Module = f.fmstd.fm.fm.ModuleName,

                            LocalPath = f.fmstd.fm.ps.LocalPath,
                            FTPPath = f.fmstd.fm.ps.RemotePath,
                            FTPUser = f.fmstd.fm.ps.RemoteUserName,
                            FTPPass = f.fmstd.fm.ps.RemotePassword,
                            BackupPath = f.fmstd.fm.ps.BackupPath,
                            FileType = f.fmstd.fm.fm.FileType,
                            FileName = f.fmstd.fm.fm.FileName,

                            SAPDBName = f.fmstd.so.SAPDBName,
                            SAPIPAddress = f.fmstd.so.SAPIPAddress,
                            SAPSldServer = f.fmstd.so.SAPSldServer,
                            SAPLicensePort = f.fmstd.so.SAPLicensePort,
                            SAPDBuser = f.fmstd.so.SAPDBuser,
                            SAPDBPassword = f.fmstd.so.SAPDBPassword,
                            SAPUser = f.fmstd.so.SAPUser,
                            SAPPassword = f.fmstd.so.SAPPassword,

                            EntityType = f?.msd?.EntityType ?? "",
                            EntityName = f?.msd?.EntityName ?? "",

                        }).FirstOrDefault();
            return model;
        }
        public PostingViewModel OPSGetFileToSAPCredentialsMapCode(string Code)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.OPSFieldMappings.Where(x => x.MapCode == Code)
                        .GroupJoin(_context.OPSFieldTable
                                    , opsfm => opsfm.MapId
                                    , opsft => opsft.MapId
                                    , (opsfm, opsft) => opsft.Select(sel => new { opsfm = opsfm, opsft = sel }).DefaultIfEmpty(new { opsfm = opsfm, opsft = (OPSFieldTable)null }))
                        .FirstOrDefault()

                        .GroupJoin(_context.PathSetup
                                    , opsft => opsft.opsft.PathCode
                                    , pset => pset.PathCode
                                    , (fmap, ps) => ps.Select(sel => new { fm = fmap, ps = sel }).DefaultIfEmpty(new { fm = fmap, ps = (PathSetup)null }))
                        .SelectMany(g => g)
                        .GroupJoin(_context.SAPSetup
                                    , fm => fm.fm.opsfm.SAPCode
                                    , so => so.SAPCode
                                    , (fm, so) => so.Select(sel => new { fm = fm, so = sel }).DefaultIfEmpty(new { fm = fm, so = (SAPSetup)null }))
                        .SelectMany(g1 => g1)
                        .GroupJoin(_context.ModuleSetup
                                    , fmstd => fmstd.fm.fm.opsfm.ModuleName
                                    , msd => msd.ModuleCode
                                    , (fmstd, msd) => msd.Select(sel => new { fmstd = fmstd, msd = sel }).DefaultIfEmpty(new { fmstd = fmstd, msd = (ModuleSetup)null }))
                        .SelectMany(g2 => g2).ToList()
                        .Select(f => new PostingViewModel.Credential
                        {
                            MapId = f.fmstd.fm.fm.opsfm.MapId,
                            Module = f.fmstd.fm.fm.opsfm.ModuleName,

                            LocalPath = f.fmstd.fm.ps.LocalPath,
                            FTPPath = f.fmstd.fm.ps.RemotePath,
                            FTPUser = f.fmstd.fm.ps.RemoteUserName,
                            FTPPass = f.fmstd.fm.ps.RemotePassword,
                            BackupPath = f.fmstd.fm.ps.BackupPath,
                            FileType = f.fmstd.fm.fm.opsft.FileType,
                            FileName = f.fmstd.fm.fm.opsft.FileName,

                            SAPDBName = f.fmstd.so.SAPDBName,
                            SAPIPAddress = f.fmstd.so.SAPIPAddress,
                            SAPSldServer = f.fmstd.so.SAPSldServer,
                            SAPLicensePort = f.fmstd.so.SAPLicensePort,
                            SAPDBuser = f.fmstd.so.SAPDBuser,
                            SAPDBPassword = f.fmstd.so.SAPDBPassword,
                            SAPUser = f.fmstd.so.SAPUser,
                            SAPPassword = f.fmstd.so.SAPPassword,

                            EntityType = f?.msd?.EntityType ?? "",
                            EntityName = f?.msd?.EntityName ?? "",

                        }).FirstOrDefault();
            return model;
        }

        public PostingViewModel GetSAPCredentialsMapCode(string Code)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Where(x => x.MapCode == Code)
                        .Join(_context.SAPSetup, fm => fm.APICode, so => so.SAPCode, (fm, so) => new { fm, so })
                        .Join(_context.SAPSetup, fmso => fmso.fm.SAPCode, st => st.SAPCode, (fmso, st) => new { fmso, st })
                        .Join(_context.ModuleSetup, fmst => fmst.fmso.fm.ModuleName, ms => ms.ModuleCode, (fmst, ms) => new { fmst, ms })
                        .Join(_context.ModuleSetup, fmstd => fmstd.fmst.fmso.fm.DestModule, msd => msd.ModuleCode, (fmstd, msd) => new PostingViewModel.Credential
                        {
                            MapId = fmstd.fmst.fmso.fm.MapId,
                            Module = fmstd.fmst.fmso.fm.ModuleName,
                            AddonDBName = fmstd.fmst.fmso.so.SAPDBName,
                            AddonIPAddress = fmstd.fmst.fmso.so.SAPIPAddress,
                            AddonPort = fmstd.fmst.fmso.so.SAPLicensePort,
                            AddonDBuser = fmstd.fmst.fmso.so.SAPUser,
                            AddonDBPassword = fmstd.fmst.fmso.so.SAPPassword,
                            SAPDBName = fmstd.fmst.st.SAPDBName,
                            SAPIPAddress = fmstd.fmst.st.SAPIPAddress,
                            SAPLicensePort = fmstd.fmst.st.SAPLicensePort,
                            SAPUser = fmstd.fmst.st.SAPUser,
                            SAPPassword = fmstd.fmst.st.SAPPassword,

                            EntityType = msd.EntityType,
                            EntityName = msd.EntityName,

                        }).FirstOrDefault();
            return model;
        }
        public PostingViewModel GetAPICredentials(string Task)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Join(_context.Headers, fm => fm.MapId, h => h.MapId, (fm, h) => new { fm, h })
                          .GroupJoin(_context.Rows, fmh => fmh.fm.MapId, r => r.MapId, (fmh, r) => new { fmh, r })
                          .Join(_context.AddonSetup, fmhrp => fmhrp.fmh.fm.AddonCode, a => a.AddonCode, (fmhrp, a) => new { fmhrp, a })
                          .Join(_context.SAPSetup, fmhrpa => fmhrpa.fmhrp.fmh.fm.SAPCode, s => s.SAPCode, (fmhrpa, s) => new { fmhrpa, s })
                          .Join(_context.Process, fmhrpas => fmhrpas.fmhrpa.fmhrp.fmh.fm.MapId, pr => pr.MapId, (fmhrpas, pr) => new { fmhrpas, pr })
                          .Join(_context.Schedules.Where(x => x.SchedCode == Task), fmhrpaspr => fmhrpaspr.pr.ProcessCode, sh => sh.Process, (fmhrpaspr, sh) => new PostingViewModel.Credential
                          {
                              MapId = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmh.fm.MapId,
                              AddonCode = fmhrpaspr.fmhrpas.fmhrpa.a.AddonCode,
                              AddonDBName = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBName,
                              AddonIPAddress = fmhrpaspr.fmhrpas.fmhrpa.a.AddonIPAddress,
                              AddonPort = fmhrpaspr.fmhrpas.fmhrpa.a.AddonPort,
                              AddonServerName = fmhrpaspr.fmhrpas.fmhrpa.a.AddonServerName,
                              AddonDBuser = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBuser,
                              AddonDBPassword = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBPassword,
                              LocalPath = "",
                              FTPPath = "",
                              FTPUser = "",
                              FTPPass = "",
                              BackupPath = "",
                              HeaderName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmh.h.SourceTableName,
                              RowName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.r.Select(sel => sel.TableName).FirstOrDefault(),
                              HeaderWorksheet = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmh.fm.HeaderWorksheet,
                              RowWorksheet = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmh.fm.RowWorksheet,
                              FileType = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmh.fm.FileType,
                              FileName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmh.fm.FileName,
                              SAPServerName = fmhrpaspr.fmhrpas.s.SAPServerName,
                              SAPIPAddress = fmhrpaspr.fmhrpas.s.SAPIPAddress,
                              SAPDBName = fmhrpaspr.fmhrpas.s.SAPDBName,
                              SAPDBVersion = fmhrpaspr.fmhrpas.s.SAPDBVersion,
                              SAPLicensePort = fmhrpaspr.fmhrpas.s.SAPLicensePort,
                              SAPDBPort = fmhrpaspr.fmhrpas.s.SAPDBPort,
                              SAPDBuser = fmhrpaspr.fmhrpas.s.SAPDBuser,
                              SAPDBPassword = fmhrpaspr.fmhrpas.s.SAPDBPassword,
                              SAPSldServer = fmhrpaspr.fmhrpas.s.SAPSldServer,
                              SAPUser = fmhrpaspr.fmhrpas.s.SAPUser,
                              SAPPassword = fmhrpaspr.fmhrpas.s.SAPPassword,
                              PostSAP = fmhrpaspr.pr.PostSAP,
                              Module = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmh.fm.ModuleName
                          }).FirstOrDefault();
            return model;
        }
        public PostingViewModel GetAPICredentialSkipRow(string Task)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Join(_context.Headers, fm => fm.MapId, h => h.MapId, (fm, h) => new { fm, h })
                          .Join(_context.AddonSetup, fmhrp => fmhrp.fm.AddonCode, a => a.AddonCode, (fmhrp, a) => new { fmhrp, a })
                          //.Join(_context.SAPSetup, fmhrpa => fmhrpa.fmhrp.fm.SAPCode, s => s.SAPCode, (fmhrpa, s) => new { fmhrpa, s })                          
                          .Join(_context.Process, fmhrpas => fmhrpas.fmhrp.fm.MapId, pr => pr.MapId, (fmhrpas, pr) => new { fmhrpas, pr })
                          .Join(_context.PathSetup, fmhrph => fmhrph.fmhrpas.fmhrp.fm.PathCode, ph => ph.PathCode, (fmhrph, ph) => new { fmhrph, ph })
                          .Join(_context.Schedules.Where(x => x.SchedCode == Task), fmhrpaspr => fmhrpaspr.fmhrph.pr.ProcessCode, sh => sh.Process, (fmhrpaspr, sh) => new PostingViewModel.Credential
                          {
                              MapId = fmhrpaspr.fmhrph.fmhrpas.fmhrp.fm.MapId,
                              AddonCode = fmhrpaspr.fmhrph.fmhrpas.a.AddonCode,
                              AddonDBName = fmhrpaspr.fmhrph.fmhrpas.a.AddonDBName,
                              AddonIPAddress = fmhrpaspr.fmhrph.fmhrpas.a.AddonIPAddress,
                              AddonPort = fmhrpaspr.fmhrph.fmhrpas.a.AddonPort,
                              AddonServerName = fmhrpaspr.fmhrph.fmhrpas.a.AddonServerName,
                              AddonDBuser = fmhrpaspr.fmhrph.fmhrpas.a.AddonDBuser,
                              AddonDBPassword = fmhrpaspr.fmhrph.fmhrpas.a.AddonDBPassword,
                              LocalPath = fmhrpaspr.ph.LocalPath,
                              FTPPath = "",
                              FTPUser = "",
                              FTPPass = "",
                              BackupPath = "",
                              HeaderName = fmhrpaspr.fmhrph.fmhrpas.fmhrp.h.SourceTableName,
                              RowName = fmhrpaspr.fmhrph.fmhrpas.fmhrp.h.SourceTableName,
                              HeaderWorksheet = fmhrpaspr.fmhrph.fmhrpas.fmhrp.fm.HeaderWorksheet,
                              RowWorksheet = fmhrpaspr.fmhrph.fmhrpas.fmhrp.fm.RowWorksheet,
                              FileType = fmhrpaspr.fmhrph.fmhrpas.fmhrp.fm.FileType,
                              FileName = fmhrpaspr.fmhrph.fmhrpas.fmhrp.fm.FileName,
                              SAPServerName = "",
                              SAPIPAddress = "",
                              SAPDBName = "",
                              SAPDBVersion = "",
                              SAPLicensePort = 0,
                              SAPDBPort = 0,
                              SAPDBuser = "",
                              SAPDBPassword = "",

                              SAPUser = "",
                              SAPPassword = "",
                              PostSAP = fmhrpaspr.fmhrph.pr.PostSAP,
                              PostUrl = "",
                              Module = fmhrpaspr.fmhrph.fmhrpas.fmhrp.fm.ModuleName
                          }).FirstOrDefault();
            return model;
        }

        public PostingViewModel GetCredentials_N_A(string task)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.Schedules.Where(sel => sel.SchedCode == task)
                          .Join(_context.Process, sch => sch.Process, pr => pr.ProcessCode, (sch, pr) => new { sch, pr })
                          .Join(_context.APISetups, schpr => schpr.pr.APICode, aip => aip.APICode, (schpr, aip) => new { schpr, aip })
                          .Join(_context.SAPSetup, prsap => prsap.schpr.pr.SAPCode, sap => sap.SAPCode, (prsap, sap) => new PostingViewModel.Credential
                          {
                              MapId = 0,
                              AddonCode = "",
                              AddonDBName = "",
                              AddonIPAddress = "",
                              AddonPort = 0,
                              AddonServerName = "",
                              AddonDBuser = "",
                              AddonDBPassword = "",
                              LocalPath = "",
                              FTPPath = "",
                              FTPUser = "",
                              FTPPass = "",
                              BackupPath = "",
                              HeaderName = "",
                              RowName = "",
                              HeaderWorksheet = "",
                              RowWorksheet = "",
                              FileType = "",
                              FileName = "",
                              SAPServerName = sap.SAPServerName,
                              SAPIPAddress = sap.SAPIPAddress,
                              SAPDBName = sap.SAPDBName,
                              SAPDBVersion = sap.SAPDBVersion,
                              SAPLicensePort = sap.SAPLicensePort,
                              SAPDBPort = sap.SAPDBPort,
                              SAPDBuser = sap.SAPDBuser,
                              SAPDBPassword = sap.SAPDBPassword,
                              SAPSldServer = sap.SAPSldServer,
                              SAPUser = sap.SAPUser,
                              SAPPassword = sap.SAPPassword,
                              PostSAP = prsap.schpr.pr.PostSAP,
                              PostUrl = prsap.aip.APILoginUrl,
                              PostUrlUser = prsap.aip.APIKey,
                              PostUrlPwd = prsap.aip.APISecretKey,
                              Module = "",
                          }).FirstOrDefault();
            return model;
        }
        public PostingViewModel GetPaymentCredentials()
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Where(x => x.ModuleName.ToLower().Contains("payment")).Join(_context.Headers, fm => fm.MapId, h => h.MapId, (fm, h) => new { fm, h })
                          .Join(_context.Rows, fmh => fmh.fm.MapId, r => r.MapId, (fmh, r) => new { fmh, r })
                          .Join(_context.PathSetup, fmhr => fmhr.fmh.fm.PathCode, p => p.PathCode, (fmhr, p) => new { fmhr, p })
                          .Join(_context.AddonSetup, fmhrp => fmhrp.fmhr.fmh.fm.AddonCode, a => a.AddonCode, (fmhrp, a) => new { fmhrp, a })
                          .Join(_context.SAPSetup, fmhrpa => fmhrpa.fmhrp.fmhr.fmh.fm.SAPCode, s => s.SAPCode, (fmhrpa, s) => new { fmhrpa, s })
                          .Join(_context.Process, fmhrpas => fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.MapId, pr => pr.MapId, (fmhrpas, pr) => new { fmhrpas, pr })
                          .Join(_context.Schedules, fmhrpaspr => fmhrpaspr.pr.ProcessCode, sh => sh.Process, (fmhrpaspr, sh) => new PostingViewModel.Credential
                          {
                              MapId = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.MapId,
                              AddonCode = fmhrpaspr.fmhrpas.fmhrpa.a.AddonCode,
                              AddonDBName = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBName,
                              AddonIPAddress = fmhrpaspr.fmhrpas.fmhrpa.a.AddonIPAddress,
                              AddonPort = fmhrpaspr.fmhrpas.fmhrpa.a.AddonPort,
                              AddonServerName = fmhrpaspr.fmhrpas.fmhrpa.a.AddonServerName,
                              AddonDBuser = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBuser,
                              AddonDBPassword = fmhrpaspr.fmhrpas.fmhrpa.a.AddonDBPassword,
                              LocalPath = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.LocalPath,
                              FTPPath = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.RemotePath,
                              FTPUser = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.RemoteUserName,
                              FTPPass = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.RemotePassword,
                              BackupPath = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.p.BackupPath,
                              HeaderName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.h.SourceTableName,
                              RowName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.r.TableName,
                              HeaderWorksheet = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.HeaderWorksheet,
                              RowWorksheet = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.RowWorksheet,
                              FileType = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.FileType,
                              FileName = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.FileName,
                              SAPServerName = fmhrpaspr.fmhrpas.s.SAPServerName,
                              SAPIPAddress = fmhrpaspr.fmhrpas.s.SAPIPAddress,
                              SAPDBName = fmhrpaspr.fmhrpas.s.SAPDBName,
                              SAPLicensePort = fmhrpaspr.fmhrpas.s.SAPLicensePort,
                              SAPDBPort = fmhrpaspr.fmhrpas.s.SAPDBPort,
                              SAPDBuser = fmhrpaspr.fmhrpas.s.SAPDBuser,
                              SAPDBPassword = fmhrpaspr.fmhrpas.s.SAPDBPassword,
                              SAPUser = fmhrpaspr.fmhrpas.s.SAPUser,
                              SAPPassword = fmhrpaspr.fmhrpas.s.SAPPassword,
                              PostSAP = fmhrpaspr.pr.PostSAP,
                              Module = fmhrpaspr.fmhrpas.fmhrpa.fmhrp.fmhr.fmh.fm.ModuleName
                          }).FirstOrDefault();
            return model;
        }

        public PostingViewModel GetSyncCredentials(string Task)
        {
            PostingViewModel model = new PostingViewModel();
            model.SyncCredsDetails = _context.SyncQueries.Join(_context.Queries, sq => sq.QueryId, q => q.Id, (sq, q) => new { sq, q })
                          .Join(_context.Syncs, sqq => sqq.sq.SyncId, s => s.Id, (sqq, s) => new { sqq, s })
                          .Join(_context.Schedules.Where(x => x.SchedCode == Task), sqqs => sqqs.sqq.sq.SyncQueryCode, sc => sc.Process, (sqqs, sc) => new PostingViewModel.SyncCreds
                          {
                              Path = sqqs.s.Path,
                              FileType = sqqs.s.FileType,
                              IpAddress = sqqs.s.IpAddress,
                              DbName = sqqs.s.DbName,
                              DbVersion = sqqs.s.DbVersion,
                              DbUser = sqqs.s.DbUser,
                              DbPass = sqqs.s.DbPass,
                              QueryString = sqqs.sqq.q.QueryString,
                              Code = sqqs.sqq.q.Code,
                              FtpPath = sqqs.s.FtpPath,
                              FtpUser = sqqs.s.FtpUser,
                              FtpPass = sqqs.s.FtpPass,

                          }).FirstOrDefault();
            return model;
        }

        public EmailViewModel GetEmailCredentials(string Task)
        {

            EmailViewModel model = new EmailViewModel();
            model.EmailCreds = _context.Schedules.AsEnumerable().Where(x => x.SchedCode == Task).Select(x => new EmailViewModel.EmailCredentials
            {
                SchedCode = x.Process,
                EmailCredCode = x.Credential,

            }).FirstOrDefault();
            if (model.EmailCreds != null)
            {
                var emailtemplate = _context.EmailTemplate.Where(x => x.Code == model.EmailCreds.SchedCode).FirstOrDefault();
                var emailsetup = _context.EmailSetup.Where(x => x.EmailCode == model.EmailCreds.EmailCredCode).FirstOrDefault();
                var filesetup = _context.Documents.AsEnumerable().Where(x => x.Id == Convert.ToDouble(emailtemplate.FileCode)).FirstOrDefault();
                var pathsetup = filesetup != null ? _context.PathSetup.Where(x => x.PathId.ToString() == filesetup.SavePath).FirstOrDefault() : null;
                model.EmailCreds.EmailFrom = emailsetup.Email;
                model.EmailCreds.EmailDesc = emailsetup.DisplayName;
                model.EmailCreds.EmailCc = string.Join(",", emailtemplate.CC);
                model.EmailCreds.EmailHost = emailsetup.SMTPClient;
                model.EmailCreds.EmailPassword = emailsetup.Password;
                model.EmailCreds.EmailPort = emailsetup.Port;
                model.EmailCreds.EmailSubject = emailtemplate.Subject;
                model.EmailCreds.EmailTo = string.Join(",", emailtemplate.To);
                model.EmailCreds.Body = emailtemplate.Body;
                model.EmailCreds.ToQuery = emailtemplate.QueryTo;
                model.EmailCreds.CcQuery = emailtemplate.QueryCC;
                model.EmailCreds.QueryData = emailtemplate.QueryCode;
                model.EmailCreds.FilePath = filesetup != null ? filesetup.FilePath : "";
                model.EmailCreds.FileName = filesetup != null ? filesetup.FileName : "";
                model.EmailCreds.SavePath = pathsetup != null ? pathsetup.LocalPath : "";
                model.EmailCreds.FileCred = filesetup != null ? filesetup.Credential : "0";
                model.EmailCreds.Company = emailtemplate.Company;
                //model.EmailCreds = _context.Schedules.Where(x => x.SchedCode == Task).AsEnumerable().Join(_context.EmailTemplate, em => em.Process, a => a.Code, (em, a) => new { em, a }).AsEnumerable()
                //          .Join(_context.EmailSetup, ema => ema.em.Credential, sc => sc.EmailCode, (ema, sc) => new { ema, sc }).Join(_context.Documents,
                //          emasc => emasc.ema.a.FileCode, sched => sched.Code, (emasc, sched) => new { emasc, sched }).Join(_context.PathSetup, emascsched => emascsched.sched.SavePath, path => path.PathId.ToString(), (emascsched, p) => new EmailViewModel.EmailCredentials
                //          {
                //              EmailFrom = emascsched.emasc.sc.Email,
                //              EmailCc = string.Join(",", emascsched.emasc.ema.a.CC),
                //              EmailHost = emascsched.emasc.sc.SMTPClient,
                //              EmailPassword = emascsched.emasc.sc.Password,
                //              EmailPort = emascsched.emasc.sc.Port,
                //              EmailSubject = emascsched.emasc.ema.a.Subject,
                //              EmailTo = string.Join(",", emascsched.emasc.ema.a.To),
                //              Body = emascsched.emasc.ema.a.Body,
                //              ToQuery = emascsched.emasc.ema.a.QueryTo,
                //              CcQuery = emascsched.emasc.ema.a.QueryCC,
                //              QueryData = emascsched.emasc.ema.a.QueryCode,
                //              FilePath = emascsched.sched.FilePath,
                //              FileName = emascsched.sched.FileName,
                //              SavePath = p.LocalPath,
                //              FileCred = emascsched.sched.Credential,
                //          }).FirstOrDefault();
                if (model.EmailCreds != null)
                {

                    //model.CompanyDetails = _context.CompanyDetails.AsEnumerable().Where(x => x.Id == Convert.ToDouble(model.EmailCreds.Company)).Select(x => new EmailViewModel.Company
                    //{
                    //    CompanyName = x.CompanyName,
                    //    FilePath = x.FilePath,
                    //    Address = x.Address,
                    //    MobileNo = x.MobileNo,
                    //    TelNo = x.TelNo,
                    //    FileName = x.FileName
                    //}).FirstOrDefault();
                    //model.FileCredentials = _context.SAPSetup.AsEnumerable().Where(x => x.SAPId == Convert.ToDecimal(string.IsNullOrEmpty(model.EmailCreds.FileCred) ? "0" : model.EmailCreds.FileCred)).Select(x => new EmailViewModel.FileCredential
                    //{
                    //    DbName = x.SAPDBName,
                    //    IpAddress = x.SAPIPAddress,
                    //    SapUser = x.SAPDBuser,
                    //    SapPassword = x.SAPDBPassword,
                    //    ServerName = x.SAPServerName
                    //}).FirstOrDefault();

                    //model.QueryDetails = _context.QueryManager.AsEnumerable().Where(x => x.Id == Convert.ToInt32(model.EmailCreds.ToQuery)).Select(x => new EmailViewModel.QuerySetup
                    //{
                    //    ConnectionString = x.ConnectionString,
                    //    Id = x.Id,
                    //    ConnectionType = x.ConnectionType,
                    //    QueryCode = x.QueryCode,
                    //    QueryString = x.QueryString
                    //}).FirstOrDefault();

                    //model.ToTable = GetQueryData(model);

                    model.QueryDetails = _context.QueryManager.AsEnumerable().Where(x => x.Id == Convert.ToInt32(model.EmailCreds.CcQuery)).Select(x => new EmailViewModel.QuerySetup
                    {
                        ConnectionString = x.ConnectionString,
                        Id = x.Id,
                        ConnectionType = x.ConnectionType,
                        QueryCode = x.QueryCode,
                        QueryString = x.QueryString
                    }).FirstOrDefault();

                    // model.CcTable = GetQueryData(model);

                    model.QueryDetails = _context.QueryManager.AsEnumerable().Where(x => x.Id == Convert.ToInt32(model.EmailCreds.QueryData)).Select(x => new EmailViewModel.QuerySetup
                    {
                        ConnectionString = x.ConnectionString,
                        Id = x.Id,
                        ConnectionType = x.ConnectionType,
                        QueryCode = x.QueryCode,
                        QueryString = x.QueryString
                    }).FirstOrDefault();

                    model.QueryTable = GetQueryData(model);

                }
            }
            return model;
        }

        public PostingViewModel GetQueryCredentialExport(string Task)
        {
            var model = new PostingViewModel();
            model.CredentialDetails = _context.CrystalExtractSetup.Where(x => x.Name == Task)
                        .Join(_context.QueryManager, cs => cs.QueryId, qm => qm.Id, (cs, qm) => new { cs, qm })
                        .Join(_context.Documents, cqs => cqs.cs.DocumentId, ds => ds.Id, (cqs, ds) => new { cqs, ds })
                        .Join(_context.PathSetup, cqp => cqp.ds.SavePath, ps => ps.PathId.ToString(), (cqp, ps) => new { cqp, ps })
                        .Join(_context.SAPSetup, cqss => cqss.cqp.ds.Credential, ss => ss.SAPId.ToString(), (cqss, ss) => new PostingViewModel.Credential
                        {
                            SAPDBVersion = ss.SAPVersion,
                            SAPDBName = ss.SAPDBName,
                            SAPSldServer = ss.SAPSldServer,
                            SAPServerName = ss.SAPServerName,
                            SAPDBPassword = ss.SAPDBPassword,
                            SAPIPAddress = ss.SAPIPAddress,
                            SAPLicensePort = ss.SAPLicensePort,
                            SAPDBPort = ss.SAPDBPort,
                            SAPDBuser = ss.SAPDBuser,
                            SAPUser = ss.SAPUser,
                            SAPPassword = ss.SAPPassword,
                            LocalPath = cqss.ps.LocalPath,
                            FTPUser = cqss.ps.RemoteServerName,
                            FTPPass = cqss.ps.RemotePassword,
                            BackupPath = cqss.ps.BackupPath,
                            FileName = cqss.cqp.ds.FileName,

                        }).FirstOrDefault();

            model.SyncCredsDetails = _context.CrystalExtractSetup.Where(x => x.Name == Task)
                        .Join(_context.QueryManager, cs => cs.QueryId, qm => qm.Id, (cs, qm) => new { cs, qm })
                        .Join(_context.Documents, cqs => cqs.cs.DocumentId, ds => ds.Id, (cqs, ds) => new { cqs, ds })
                        .Join(_context.PathSetup, cqp => cqp.ds.SavePath, ps => ps.PathId.ToString(), (cqp, ps) => new { cqp, ps })
                        .Join(_context.SAPSetup, cqss => cqss.cqp.ds.Credential, ss => ss.SAPId.ToString(), (cqss, ss) => new PostingViewModel.SyncCreds
                        {
                            Path = cqss.cqp.ds.FilePath,
                            QueryString = cqss.cqp.cqs.qm.QueryString,
                            DocumentId = cqss.cqp.ds.Id
                        }).FirstOrDefault();
            return model;

        }
        public PostingViewModel GetSyncQueryCredential(string ReportName)
        {
            PostingViewModel model = new PostingViewModel();

            model.ReportCredentialDetails = _context.CrystalExtractSetup.Where(x => x.Name == ReportName)
             .Join(_context.Syncs, sc => sc.DocumentId, pr => pr.Id, (sc, pr) => new { sc, pr })
             .Join(_context.APISetups, sqm => sqm.sc.APIId, qr => qr.APIId, (sqm, qr) => new PostingViewModel.ReportCredential
             {

                 ReportId = sqm.sc.Id,
                 ReportName = sqm.sc.Name,
                 SyncId = sqm.sc.DocumentId,
                 SyncCode = sqm.sc.DocumentCode,
                 QueryId = sqm.sc.QueryId,
                 SavePath = sqm.pr.Path,
                 IPAddress = sqm.pr.IpAddress,
                 FileType = sqm.pr.FileType,
                 DbName = sqm.pr.DbName,
                 UserName = sqm.pr.DbUser,
                 Password = sqm.pr.DbPass,
                 APIid = sqm.sc.APIId,
                 APIcode = sqm.sc.APICode,
                 Method = qr.APIMethod,
                 APIURL = qr.APIURL,
                 APIKey = qr.APIKey,
                 APISecretKey = qr.APISecretKey,
                 APIToken = qr.APIToken,
                 APILoginURL = qr.APILoginUrl,
                 APILoginBody = qr.APILoginBody
             }).FirstOrDefault();

            return model;
        }
        public DataTable GetQueryData(EmailViewModel model)
        {
            try
            {
                if (model.QueryDetails?.ConnectionType != null)
                {
                    switch (model.QueryDetails.ConnectionType)
                    {
                        case "SAP":
                            model.DatabaseConnectionView = _context.SAPSetup
                                        .AsEnumerable()
                                        .Where(x => x.IsActive == true && x.SAPId == Convert.ToInt32(model.QueryDetails.ConnectionString))
                                        .Select((x) => new EmailViewModel.DbConnection
                                        {
                                            Id = x.SAPId,
                                            QueryName = x.SAPCode,
                                            ConnectionType = x.SAPDBVersion,
                                            ConnectionString = (x.SAPDBVersion.Contains("HANA") ? "DRIVER={HDBODBC32};" + $"SERVERNODE={x.SAPServerName}{(x.SAPDBPort > 0 ? $":{x.SAPDBPort}" : "")};UID={x.SAPDBuser};PWD={x.SAPDBPassword};CS={x.SAPDBName}" :
                                                                                                   $"Data Source={x.SAPServerName}{(x.SAPDBPort > 0 ? $":{x.SAPDBPort}" : "")};Initial Catalog={x.SAPDBName};Persist Security Info=True;User ID={x.SAPDBuser};Password={x.SAPDBPassword}")
                                        }).FirstOrDefault();
                            break;
                        default:
                            model.DatabaseConnectionView = _context.AddonSetup
                                        .AsEnumerable()
                                        .Where(x => x.IsActive == true && x.AddonId == Convert.ToInt32(model.QueryDetails.ConnectionString))
                                        .Select((x) => new EmailViewModel.DbConnection
                                        {
                                            Id = x.AddonId,
                                            QueryName = x.AddonCode,
                                            ConnectionType = x.AddonDBVersion,
                                            ConnectionString = (x.AddonDBVersion.Contains("HANA") ? "DRIVER={HDBODBC32};" + $"SERVERNODE={x.AddonServerName}{(x.AddonPort > 0 ? $":{x.AddonPort}" : "")};UID={x.AddonDBuser};PWD={x.AddonDBPassword};CS={x.AddonDBName}" :
                                                                                                   $"Data Source={x.AddonServerName}{(x.AddonPort > 0 ? $":{x.AddonPort}" : "")};Initial Catalog={x.AddonDBName};Persist Security Info=True;User ID={x.AddonDBuser};Password={x.AddonDBPassword}")
                                        }).FirstOrDefault();
                            break;
                    }
                    return Fill_DataTable(model);
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public DataTable Fill_DataTable(EmailViewModel model)
        {
            try
            {
                DataTable dt = new DataTable();

                if (model.DatabaseConnectionView.ConnectionType.Contains("HANA"))
                {
                    dt = DataAccess.SelectHana(model.DatabaseConnectionView.ConnectionString, model.QueryDetails.QueryString);
                }
                else
                {
                    dt = DataAccess.Select(model.DatabaseConnectionView.ConnectionString, model.QueryDetails.QueryString);
                }

                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public PostingViewModel GetFields(int MapId)
        {
            PostingViewModel model = new PostingViewModel();
            model.HeaderFields = _context.Headers.Where(x => x.MapId == MapId).Select(x => new PostingViewModel.Fields
            {
                SAPFieldId = x.SourceFieldId,
                AddonField = x.DestinationField
            }).ToList() ?? new List<PostingViewModel.Fields>();

            model.RowFields = _context.Rows.Where(x => x.MapId == MapId).Select(x => new PostingViewModel.Fields
            {
                SAPFieldId = x.SAPRowFieldId,
                AddonField = x.AddonRowField
            }).ToList() ?? new List<PostingViewModel.Fields>();

            return model;
        }

        public string ConcatDecimal(string value)
        {
            if (value == "" || value == string.Empty || value == null)
            {
                return "0";
            }
            else if (value.Contains('.'))
            {
                var number = value.Split('.');
                var r = new Regex($@"^\d*[1-9]\d*$");
                if (r.IsMatch(number[1]))
                {
                    return string.Format(new NumberFormatInfo() { NumberDecimalDigits = 2 },
                        "{0:F}", new decimal(Convert.ToDouble(value))).ToString();
                }
                else
                {
                    return number[0];
                }
            }
            else
            {
                return value;
            }


        }

        public PostingViewModel GetApiOrderPostSAP(string taskname)
        {
            var model = new PostingViewModel();
            var Creds = GetAPICredentials(taskname);
            if (Creds.CredentialDetails != null)
            {
                if (Creds.CredentialDetails.PostSAP && PostingHelpers.LoginAction(Creds))
                {
                    if (Creds.CredentialDetails.Module.ToLower().Contains("sales order"))
                    {
                        model.APIView = _context.FieldMappings.Where(x => x.MapId == Creds.CredentialDetails.MapId)
                                   .Join(_context.APISetups, f => f.APICode, a => a.APICode, (f, a) => new { f, a })
                                   .Select(x => new PostingViewModel.APIViewModel
                                   {
                                       APIId = x.a.APIId,
                                       APICode = x.a.APICode,
                                       APIMethod = x.a.APIMethod,
                                       APIURL = x.a.APIURL,
                                       APIKey = x.a.APIKey,
                                       APISecretKey = x.a.APISecretKey,
                                       APIToken = x.a.APIToken,
                                       APILoginUrl = x.a.APILoginUrl,
                                       APILoginBody = x.a.APILoginBody,
                                   }).ToList();

                        var apicode = model.APIView.Select(sel => sel.APICode).FirstOrDefault();
                        List<ApiParameterViewModel.ApiParameter> paramlist = _context.APIParameter.Where(sel => sel.MapId == Creds.CredentialDetails.MapId && sel.APICode == apicode)
                                                                                .Select(sel => new ApiParameterViewModel.ApiParameter
                                                                                {
                                                                                    APICode = sel.APICode,
                                                                                    APIParameter = sel.APIParameter,
                                                                                    APIParamValue = sel.APIParamValue,
                                                                                }).ToList();

                        var apimethod = model.APIView.Select(x => x.APIMethod).FirstOrDefault();
                        var apiurlorig = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                        var apiurl = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                        var apiuser = model.APIView.Select(x => x.APIKey).FirstOrDefault();
                        var apipwd = model.APIView.Select(x => x.APISecretKey).FirstOrDefault();
                        if (apiurl.ToLower().Contains("shopify.com"))
                        {
                            model.HeaderFields = _context.Headers.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                            {
                                SAPFieldId = x.SourceFieldId,
                                AddonField = x.DestinationField,
                                DefaultVal = x.DefaultValue,
                            }).ToList() ?? new List<PostingViewModel.Fields>();

                            model.RowFields = _context.Rows.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                            {
                                SAPFieldId = x.SAPRowFieldId,
                                AddonField = x.AddonRowField,
                                DefaultVal = x.DefaultValue,
                            }).ToList() ?? new List<PostingViewModel.Fields>();

                            ////GENERATE DATA FROM CAMPAIGN SETUP
                            var dtOCPN = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                Creds.CredentialDetails.SAPServerName,
                                                Creds.CredentialDetails.SAPDBuser,
                                                Creds.CredentialDetails.SAPDBPassword,
                                                Creds.CredentialDetails.SAPDBName), QueryAccess.GetCampaignData()).AsEnumerable().ToList()
                                            :
                                            DataAccess.Select(QueryAccess.MSSQL_conString(
                                                Creds.CredentialDetails.SAPServerName,
                                                Creds.CredentialDetails.SAPDBuser,
                                                Creds.CredentialDetails.SAPDBPassword,
                                                Creds.CredentialDetails.SAPDBName), QueryAccess.GetCampaignData()).AsEnumerable().ToList();


                            ////TEMPORARY parameter
                            //apiurl = apiurl + "?limit=1&status=any&financial_status=paid&fulfillment_status=shipped";
                            string ret = sapAces.APIResponse(apimethod, apiurl, "", "", "", apiuser, apipwd, 0, paramlist); //Zero non limit
                            JObject json = JObject.Parse(ret);
                            //var jheader = json["orders"].ToString();
                            //model.APIdatas = JsonConvert.DeserializeObject<List<PostingViewModel.APIData>>(jheader.ToString());

                            if (PostingHelpers.IsValidJson(ret))
                            {
                                //// (1) Remove first the JArray from the JProperty keys to minimize the error                            
                                //// (2) Copy json data to DataTable
                                JArray jcount = (JArray)json["orders"];
                                int jlength = jcount.Count;
                                for (int num = 0; num < jlength; num++)
                                {
                                    DataTable dataheader = new DataTable();
                                    DataTable datarows = new DataTable();
                                    DataTable datashipp = new DataTable();
                                    DataTable datacustomer = new DataTable();
                                    List<DataRow> databatch = new List<DataRow>();
                                    bool ContinuePost = true;

                                    var orderid = json["orders"][num]["id"];
                                    ////INSERT THE CUSTOMER DETAIL                                     
                                    var jToknOrderCustomer = json["orders"][num]["customer"];
                                    JObject jArrOrderCustomer = JObject.Parse(jToknOrderCustomer.ToString());
                                    foreach (var property in jArrOrderCustomer.Children<JProperty>().ToArray())
                                    {
                                        JToken propertyValue = property.Value;
                                        if (propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object)
                                        {
                                            property.Remove();
                                        }
                                    }
                                    DataTable dtOrdersCustomer = JsonConvert.DeserializeObject<DataTable>("[" + jArrOrderCustomer.ToString() + "]");
                                    if (datacustomer.Columns.Count == 0)
                                    {
                                        datacustomer = dtOrdersCustomer.Copy();
                                        datacustomer.Rows.Clear();
                                    }
                                    foreach (DataRow row in dtOrdersCustomer.Rows)
                                    {
                                        //row.SetField("id", orderid.ToString());
                                        //row.AcceptChanges();
                                        datacustomer.Rows.Add(row.ItemArray);
                                    }

                                    ////INSERT THE LINE ITEM 
                                    var jToknOrderLines = json["orders"][num]["line_items"];
                                    JArray jArrOrderLines = JArray.Parse(jToknOrderLines.ToString());
                                    foreach (JObject elem in jArrOrderLines)
                                    {
                                        foreach (var property in elem.Children<JProperty>().ToArray())
                                        {
                                            JToken propertyValue = property.Value;
                                            if (propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object)
                                            {
                                                property.Remove();
                                            }
                                        }
                                    }
                                    DataTable dtOrdersLines = JsonConvert.DeserializeObject<DataTable>(jArrOrderLines.ToString());
                                    if (datarows.Columns.Count == 0)
                                    {
                                        datarows = dtOrdersLines.Copy();
                                        datarows.Rows.Clear();
                                        datarows.Columns.Add("SAPVatGroup");
                                        datarows.Columns.Add("SAPUoMCode");
                                        datarows.Columns.Add("SAPItemPerUnit");
                                        datarows.Columns.Add("SAPOcrCode");
                                        datarows.Columns.Add("SAPProject");
                                    }
                                    foreach (DataRow row in dtOrdersLines.Rows)
                                    {
                                        //row.SetField("id", orderid.ToString());
                                        //row.AcceptChanges();
                                        datarows.Rows.Add(row.ItemArray);
                                    }

                                    ////INSERT THE SHIPPING LINE 
                                    var jToknOrderShipping = json["orders"][num]["shipping_lines"];
                                    JArray jArrOrderShipping = JArray.Parse(jToknOrderShipping.ToString());
                                    foreach (JObject elem in jArrOrderShipping)
                                    {
                                        foreach (var property in elem.Children<JProperty>().ToArray())
                                        {
                                            JToken propertyValue = property.Value;
                                            if (propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object)
                                            {
                                                property.Remove();
                                            }
                                        }
                                    }
                                    DataTable dtOrdersShipping = JsonConvert.DeserializeObject<DataTable>(jArrOrderShipping.ToString());
                                    if (datashipp.Columns.Count == 0)
                                    {
                                        datashipp = dtOrdersShipping.Copy();
                                        datashipp.Rows.Clear();
                                        datashipp.Columns.Add("SAPExpnsCode");
                                        datashipp.Columns.Add("SAPOcrCode");
                                        datashipp.Columns.Add("SAPProject");
                                    }
                                    foreach (DataRow row in dtOrdersShipping.Rows)
                                    {
                                        //row.SetField("id", orderid.ToString());
                                        //row.AcceptChanges();
                                        datashipp.Rows.Add(row.ItemArray);
                                    }

                                    ////INSERT THE HEADER DATA 
                                    ////Note: THIS STEP MUST BE THE LAST, ELSE U COULD NOT GET THE ABOVE DATA
                                    JObject jObjorder = (JObject)json.SelectToken($@"orders[{num}]");
                                    var jlist = jObjorder.Properties().AsEnumerable().Select(x => x).ToList();
                                    jlist.ForEach(x =>
                                    {
                                        JToken propertyValue = x.Value;
                                        if (propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object)
                                        {
                                            jObjorder.Value<JObject>().Remove(x.Name);
                                        }
                                    });
                                    string jstrOrder = "[" + jObjorder.ToString() + "]";
                                    DataTable dtOrders = JsonConvert.DeserializeObject<DataTable>(jstrOrder);
                                    if (dataheader.Columns.Count == 0)
                                    {
                                        dataheader = dtOrders.Copy();
                                        dataheader.Rows.Clear();
                                    }

                                    foreach (DataRow row in dtOrders.Rows)
                                    {
                                        dataheader.Rows.Add(row.ItemArray);
                                    }

                                    #region GETFULFILL LOCATION
                                    ////GET THE LOCATION ON FULFILLMENT 
                                    var locapiurl = apiurlorig.Replace("orders", $@"orders/{orderid}/fulfillments");
                                    string retloc = sapAces.APIResponse(apimethod, locapiurl, "", "", "", apiuser, apipwd, 1, null); //Zero non limit
                                    string _locationid = "";
                                    if (PostingHelpers.IsValidJson(retloc))
                                    {
                                        JObject jsonloc = JObject.Parse(retloc);
                                        JArray jlocAray = (JArray)jsonloc["fulfillments"];
                                        int jloclen = jlocAray.Count;
                                        if (jloclen > 0) _locationid = jsonloc["fulfillments"][0]["location_id"].ToString();
                                    }
                                    #endregion

                                    ////SEARCH THE LOCATION ID FROM SAP WAREHOUSE
                                    string whscode = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                Creds.CredentialDetails.SAPServerName,
                                                Creds.CredentialDetails.SAPDBuser,
                                                Creds.CredentialDetails.SAPDBPassword,
                                                Creds.CredentialDetails.SAPDBName), QueryAccess.GetLocationFromCampaign(_locationid)).AsEnumerable().Select(x => x["WhsCode"].ToString()).FirstOrDefault()
                                            :
                                            DataAccess.Select(QueryAccess.MSSQL_conString(
                                                Creds.CredentialDetails.SAPServerName,
                                                Creds.CredentialDetails.SAPDBuser,
                                                Creds.CredentialDetails.SAPDBPassword,
                                                Creds.CredentialDetails.SAPDBName), QueryAccess.GetLocationFromCampaign(_locationid)).AsEnumerable().Select(x => x["WhsCode"].ToString()).FirstOrDefault();

                                    foreach (DataRow row in datarows.Rows)
                                    {
                                        ////Validation to get Itemcode from SAP if empty SKU
                                        if (string.IsNullOrEmpty(row["sku"].ToString()))
                                        {
                                            string itemcode = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                 Creds.CredentialDetails.SAPServerName,
                                                 Creds.CredentialDetails.SAPDBuser,
                                                 Creds.CredentialDetails.SAPDBPassword,
                                                 Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemCode(row["variant_id"].ToString())).AsEnumerable().Select(x => x["ItemCode"].ToString()).FirstOrDefault()
                                             :
                                             DataAccess.Select(QueryAccess.MSSQL_conString(
                                                 Creds.CredentialDetails.SAPServerName,
                                                 Creds.CredentialDetails.SAPDBuser,
                                                 Creds.CredentialDetails.SAPDBPassword,
                                                 Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemCode(row["variant_id"].ToString())).AsEnumerable().Select(x => x["ItemCode"].ToString()).FirstOrDefault();
                                            if (string.IsNullOrEmpty(itemcode))
                                            {
                                                ContinuePost = false;
                                            }
                                            row["sku"] = itemcode; ////SET THE ITEMCODE FROM SAP                                            
                                        }

                                        string vartitle = row["variant_title"] == null ? "" : row["variant_title"].ToString();
                                        string uomcode = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                 Creds.CredentialDetails.SAPServerName,
                                                 Creds.CredentialDetails.SAPDBuser,
                                                 Creds.CredentialDetails.SAPDBPassword,
                                                 Creds.CredentialDetails.SAPDBName), QueryAccess.GetUomCode(vartitle)).AsEnumerable().Select(x => x["UomCode"].ToString()).FirstOrDefault()
                                             :
                                             DataAccess.Select(QueryAccess.MSSQL_conString(
                                                 Creds.CredentialDetails.SAPServerName,
                                                 Creds.CredentialDetails.SAPDBuser,
                                                 Creds.CredentialDetails.SAPDBPassword,
                                                 Creds.CredentialDetails.SAPDBName), QueryAccess.GetUomCode(vartitle)).AsEnumerable().Select(x => x["UomCode"].ToString()).FirstOrDefault();

                                        string itemsperunit = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                  Creds.CredentialDetails.SAPServerName,
                                                  Creds.CredentialDetails.SAPDBuser,
                                                  Creds.CredentialDetails.SAPDBPassword,
                                                  Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemPerUnit(row["sku"].ToString(), vartitle)).AsEnumerable().Select(x => x["BaseQty"].ToString()).FirstOrDefault()
                                                  :
                                                  DataAccess.Select(QueryAccess.MSSQL_conString(
                                                  Creds.CredentialDetails.SAPServerName,
                                                  Creds.CredentialDetails.SAPDBuser,
                                                  Creds.CredentialDetails.SAPDBPassword,
                                                  Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemPerUnit(row["sku"].ToString(), vartitle)).AsEnumerable().Select(x => x["BaseQty"].ToString()).FirstOrDefault();

                                        List<DataRow> itemExtDet = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                  Creds.CredentialDetails.SAPServerName,
                                                  Creds.CredentialDetails.SAPDBuser,
                                                  Creds.CredentialDetails.SAPDBPassword,
                                                  Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemExtraDetail(row["sku"].ToString())).AsEnumerable().Select(x => x).ToList()
                                                  :
                                                  DataAccess.Select(QueryAccess.MSSQL_conString(
                                                  Creds.CredentialDetails.SAPServerName,
                                                  Creds.CredentialDetails.SAPDBuser,
                                                  Creds.CredentialDetails.SAPDBPassword,
                                                  Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemExtraDetail(row["sku"].ToString())).AsEnumerable().Select(x => x).ToList();



                                        //if (string.IsNullOrEmpty(uomcode))
                                        //{
                                        //    row["variant_title"] = uomcode;
                                        //}
                                        itemsperunit = string.IsNullOrEmpty(uomcode) ? "1" : itemsperunit;
                                        row["SAPVatGroup"] = dtOCPN[0]["U_spTaxGroup"].ToString();
                                        row["SAPUoMCode"] = uomcode;
                                        row["SAPItemPerUnit"] = itemsperunit;
                                        row["SAPOcrCode"] = itemExtDet[0]["U_spOcrCode"].ToString();
                                        row["SAPProject"] = itemExtDet[0]["U_spProject"].ToString();

                                        ////Check if item is Manage by Batch
                                        string manbatchnum = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                 Creds.CredentialDetails.SAPServerName,
                                                 Creds.CredentialDetails.SAPDBuser,
                                                 Creds.CredentialDetails.SAPDBPassword,
                                                 Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemCode(row["sku"].ToString())).AsEnumerable().Select(x => x["ManBtchNum"].ToString()).FirstOrDefault()
                                             :
                                             DataAccess.Select(QueryAccess.MSSQL_conString(
                                                 Creds.CredentialDetails.SAPServerName,
                                                 Creds.CredentialDetails.SAPDBuser,
                                                 Creds.CredentialDetails.SAPDBPassword,
                                                 Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemCode(row["sku"].ToString())).AsEnumerable().Select(x => x["ManBtchNum"].ToString()).FirstOrDefault();

                                        if (manbatchnum == "Y")
                                        {
                                            ////GET THE AVAILABLE BATCH LESS ALLOCATED BATCH
                                            var _databatch = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                    Creds.CredentialDetails.SAPServerName,
                                                    Creds.CredentialDetails.SAPDBuser,
                                                    Creds.CredentialDetails.SAPDBPassword,
                                                    Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemBatches(row["sku"].ToString(), whscode)).AsEnumerable().ToList()
                                                :
                                                DataAccess.Select(QueryAccess.MSSQL_conString(
                                                    Creds.CredentialDetails.SAPServerName,
                                                    Creds.CredentialDetails.SAPDBuser,
                                                    Creds.CredentialDetails.SAPDBPassword,
                                                    Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemBatches(row["sku"].ToString(), whscode)).AsEnumerable().ToList();
                                            databatch.Add(_databatch.FirstOrDefault());
                                        }
                                    };

                                    foreach (DataRow row in datashipp.Rows)
                                    {
                                        string freightcode = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                        Creds.CredentialDetails.SAPServerName,
                                        Creds.CredentialDetails.SAPDBuser,
                                        Creds.CredentialDetails.SAPDBPassword,
                                        Creds.CredentialDetails.SAPDBName), QueryAccess.GetFreightCode(row["code"].ToString())).AsEnumerable().Select(x => x["ExpnsCode"].ToString()).FirstOrDefault()
                                        :
                                        DataAccess.Select(QueryAccess.MSSQL_conString(
                                        Creds.CredentialDetails.SAPServerName,
                                        Creds.CredentialDetails.SAPDBuser,
                                        Creds.CredentialDetails.SAPDBPassword,
                                        Creds.CredentialDetails.SAPDBName), QueryAccess.GetFreightCode(row["code"].ToString())).AsEnumerable().Select(x => x["ExpnsCode"].ToString()).FirstOrDefault();
                                        row["SAPExpnsCode"] = freightcode;
                                    }

                                    //// LOG THE PROCESS
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ORDR_VALIDATION", CreateDate = DateTime.Now, ApiUrl = $@"", Json = $@"Order No: {dataheader.Rows[0]["order_number"].ToString()} ", ErrorMsg = "THIS IS INFORMATION ONLY" });
                                    _context.SaveChanges();

                                    string posjson = "";
                                    if (ContinuePost)
                                    {
                                        ////CHECK IF ORDER NUMBER IS ALREADY POSTED, IF EXIST THEN SKIP
                                        string ordernum = dataheader.Rows[0]["order_number"].ToString();
                                        string docnum = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                               Creds.CredentialDetails.SAPServerName,
                                                               Creds.CredentialDetails.SAPDBuser,
                                                               Creds.CredentialDetails.SAPDBPassword,
                                                               Creds.CredentialDetails.SAPDBName), QueryAccess.GetDocNum(model.HeaderFields.Where(sel => sel.SAPFieldId == "CardCode").Select(sel => sel.DefaultVal).FirstOrDefault(), ordernum)).AsEnumerable().Select(x => x["DocNum"].ToString()).FirstOrDefault()
                                                               :
                                                               DataAccess.Select(QueryAccess.MSSQL_conString(
                                                               Creds.CredentialDetails.SAPServerName,
                                                               Creds.CredentialDetails.SAPDBuser,
                                                               Creds.CredentialDetails.SAPDBPassword,
                                                               Creds.CredentialDetails.SAPDBName), QueryAccess.GetDocNum(model.HeaderFields.Where(sel => sel.SAPFieldId == "CardCode").Select(sel => sel.DefaultVal).FirstOrDefault(), ordernum)).AsEnumerable().Select(x => x["DocNum"].ToString()).FirstOrDefault();

                                        //// LOG THE PROCESS
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ORDR_DOCNO", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = $@"Order No: {ordernum} | Search Result DocNo: {docnum}" });
                                        _context.SaveChanges();

                                        if (string.IsNullOrEmpty(docnum))
                                        {
                                            try
                                            {
                                                posjson = PostingStringBuilderHelpers.BuildJsonSAPOrder(dataheader, datarows, datashipp, model.HeaderFields, model.RowFields, databatch, datacustomer);
                                            }
                                            catch (Exception ex)
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ORDR_JSON", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = ex.Message + "\n\r InnerEx: " + ex.InnerException });
                                                _context.SaveChanges();
                                            }

                                            var result = PostingHelpers.SBOResponse("POST", $@"Orders", posjson, "", Creds);
                                            //// LOG THE PROCESS
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ORDR_REST", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "Posted ORDR: " + result });
                                            _context.SaveChanges();

                                            if (result.ToLower().Contains("error:") || result.ToLower().Contains("error :") || result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"""error"" :"))
                                            {
                                                ////Post to Draft
                                                var draftresult = PostingHelpers.SBOResponse("POST", $@"Drafts", posjson, "DocEntry", Creds);
                                                ////Log the error from Orders
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/Orders", Json = posjson, ErrorMsg = result.ToString() });
                                                _context.SaveChanges();
                                            }
                                            else
                                            {
                                                ////UPDATE SHOPIFY ORDER AS UPLOADED
                                                string updateOrder = "{" + $@" ""order"":" + "{" + $@"""id"": {dataheader.Rows[0]["id"].ToString()},""tags"": ""uploaded""" + "}}";
                                                var updateOrderApiurl = apiurlorig.Replace("orders", $@"orders/{orderid}");
                                                string updatetags = sapAces.APIResponse("PUT", updateOrderApiurl, "", updateOrder, "", apiuser, apipwd, 1, null); //Zero non limit

                                                var obj = JObject.Parse(result);
                                                obj["DocObjectCode"] = "13";
                                                obj.Value<JObject>().Remove("odata.metadata");
                                                obj.Value<JObject>().Remove("DocEntry");
                                                obj.Value<JObject>().Remove("DocNum");   ////TO MINIMIZE A/R RESERVE INVOICE
                                                obj.Value<JObject>().Remove("Series");   ////TO MINIMIZE ERROR ON DOCUMENT SERIES
                                                obj.Value<JObject>().Remove("DocRate");  ////THIS MUST BE REMOVE IN ORDER TO CORRECT THE EXCHANGE RATES
                                                obj.Value<JObject>().Remove("NumberOfInstallments"); ////TO MINIMIZE ERROR ON INSTALLMENT
                                                obj.Value<JObject>().Remove("ReserveInvoice");
                                                obj.Value<JObject>().Remove("JournalMemo");
                                                obj.Value<JObject>().Remove("Reserve");
                                                obj.Value<JObject>().Remove("JournalMemo");
                                                obj.Value<JObject>().Remove("WareHouseUpdateType");  ////TO MINIMIZE POSTING FROM RESERVE INVOICE
                                                obj["ReserveInvoice"] = "tNO";

                                                ////REMOVE PROPERTIES ON ROW DETAILS
                                                foreach (JObject Doclines in (JArray)obj["DocumentLines"])
                                                {
                                                    ////THIS IS ISSUE FOR PRODUCTION
                                                    var u_baseentry = Doclines.GetValue("DocEntry");
                                                    var u_baseline = Doclines.GetValue("LineNum");
                                                    var u_basetype = Doclines.GetValue("DocObjectCode");
                                                    var jdocline = Doclines.Properties().ToList();
                                                    jdocline.ForEach(fe =>
                                                    {
                                                        if (fe.Name != "SerialNumbers" && fe.Name != "BatchNumbers")
                                                        {
                                                            fe.Remove();
                                                        }
                                                    });

                                                    Doclines.Add("BaseEntry", u_baseentry);
                                                    Doclines.Add("BaseLine", u_baseline);
                                                    Doclines.Add("BaseType", "17");

                                                }
                                                foreach (var property in obj["TaxExtension"].Children<JProperty>().ToArray())
                                                {
                                                    if (property.Name == "DocEntry")
                                                    {
                                                        property.Remove();
                                                    }
                                                }
                                                foreach (var property in obj["AddressExtension"].Children<JProperty>().ToArray())
                                                {
                                                    if (property.Name == "DocEntry")
                                                    {
                                                        property.Remove();
                                                    }
                                                }

                                                ////Post to Invoice
                                                PostingHelpers.LoginAction(Creds);
                                                var sresult = PostingHelpers.SBOResponse("POST", $@"Invoices", obj.ToString(), "DocEntry", Creds);
                                                if (sresult.ToLower().Contains("error:") || sresult.ToLower().Contains("error :") || sresult.ToLower().Contains($@"""error"":") || sresult.ToLower().Contains($@"""error"" :"))
                                                {
                                                    ////Post to Draft
                                                    var draftresult = PostingHelpers.SBOResponse("POST", $@"Drafts", obj.ToString(), "DocEntry", Creds);
                                                    ////Log the error from Orders
                                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/Invoices", Json = obj.ToString(), ErrorMsg = sresult.ToString() });
                                                    _context.SaveChanges();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPServerName}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/Orders", Json = posjson, ErrorMsg = "Itemcode is missing" });
                                        _context.SaveChanges();
                                    }
                                }

                            }
                            ////END OF JSON VALIDATION

                            //foreach (var order in model.APIdatas)
                            //{
                            //    var headqry = from i in model.APIdatas where i.id == order.id select i;
                            //    var rowsqry = from i in model.APIdatas where i.id == order.id select i.line_items;
                            //    var shipqry = from i in model.APIdatas where i.id == order.id select i.shipping_lines;
                            //    var customerqry = from i in model.APIdatas where i.id == order.id select i.customer;

                            //    string hjson = Newtonsoft.Json.JsonConvert.SerializeObject(headqry);
                            //    string rjson = Newtonsoft.Json.JsonConvert.SerializeObject(rowsqry);
                            //    string sjson = Newtonsoft.Json.JsonConvert.SerializeObject(shipqry);
                            //    string cjson = Newtonsoft.Json.JsonConvert.SerializeObject(customerqry);

                            //    DataTable dataheader = JsonConvert.DeserializeObject<DataTable>(hjson);
                            //    DataTable datarows = JsonConvert.DeserializeObject<DataTable>(rjson.Substring(1, rjson.Length - 2));
                            //    DataTable datashipp = JsonConvert.DeserializeObject<DataTable>(sjson.Substring(1, sjson.Length - 2));
                            //    DataTable datacustomer = JsonConvert.DeserializeObject<DataTable>(cjson);

                            //}
                        }
                        else if (apiurl.ToLower().Contains("lazada.com"))
                        {

                        }
                    }
                }
            }


            return model;
        }

        public PostingViewModel GetSAPPostShopiAPI(string taskname)
        {
            var model = new PostingViewModel();
            Properties.Settings prop = new Properties.Settings();
            //Settings prop = new Settings();
            var Creds = GetAPICredentials(taskname);
            if (Creds.CredentialDetails != null)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ITEM_CREDS", CreateDate = DateTime.Now, ApiUrl = $@"CREDETIALS  {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                _context.SaveChanges();
                if (PostingHelpers.LoginAction(Creds))
                {
                    if (Creds.CredentialDetails.Module.ToLower().Contains("item master data"))
                    {
                        #region STOP_BPCATALOG_ENTERVENTION                        
                        var _SchedProces = _context.Schedules
                          .Join(_context.Process, sch => sch.Process, pr => pr.ProcessCode, (sch, pr) => new { sch, pr })
                          .Join(_context.APISetups, apc => apc.pr.APICode, ap => ap.APICode, (apc, ap) => new { apc, ap })
                          .Where(sel => sel.ap.APIURL.Contains("sap/post/bpcatalog"))
                          .Select(sel => sel.apc.sch.SchedId).FirstOrDefault();
                        ////FORCE STOP THE BP CATALOG AUTOMATION
                        var sched = _context.Schedules.Where(sel => sel.SchedId == _SchedProces).FirstOrDefault();
                        sched.ForceStop = true;
                        _context.SaveChanges();
                        #endregion

                        model.HeaderFields = _context.Headers.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                        {
                            SAPFieldId = x.SourceFieldId,
                            AddonField = x.DestinationField
                        }).ToList() ?? new List<PostingViewModel.Fields>();
                        model.RowFields = _context.Rows.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                        {
                            SAPFieldId = x.SAPRowFieldId,
                            AddonField = x.AddonRowField
                        }).ToList() ?? new List<PostingViewModel.Fields>();

                        model.APIView = _context.FieldMappings.Where(x => x.MapId == Creds.CredentialDetails.MapId)
                                         .Join(_context.APISetups, f => f.APICode, a => a.APICode, (f, a) => new { f, a })
                                         .Select(x => new PostingViewModel.APIViewModel
                                         {
                                             APIId = x.a.APIId,
                                             APICode = x.a.APICode,
                                             APIMethod = x.a.APIMethod,
                                             APIURL = x.a.APIURL,
                                             APIKey = x.a.APIKey,
                                             APISecretKey = x.a.APISecretKey,
                                             APIToken = x.a.APIToken,
                                             APILoginUrl = x.a.APILoginUrl,
                                             APILoginBody = x.a.APILoginBody,
                                         }).ToList();

                        if (model.APIView.Select(x => x.APIURL).FirstOrDefault().ToLower().Contains("shopify.com"))
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ITEM_MODEL", CreateDate = DateTime.Now, ApiUrl = $@"CREDETIALS  SHOPIFY", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                            _context.SaveChanges();
                            var dr = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                         Creds.CredentialDetails.SAPServerName,
                                         Creds.CredentialDetails.SAPDBuser,
                                         Creds.CredentialDetails.SAPDBPassword,
                                         Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemProduct("hana")).AsEnumerable().ToList()
                                     :
                                     DataAccess.Select(QueryAccess.MSSQL_conString(
                                         Creds.CredentialDetails.SAPServerName,
                                         Creds.CredentialDetails.SAPDBuser,
                                         Creds.CredentialDetails.SAPDBPassword,
                                         Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemProduct("mssql")).AsEnumerable().ToList();
                            var apicode = model.APIView.Select(sel => sel.APICode).FirstOrDefault();
                            List<ApiParameterViewModel.ApiParameter> paramlist = _context.APIParameter.Where(sel => sel.MapId == Creds.CredentialDetails.MapId && sel.APICode == apicode)
                                                                                    .Select(sel => new ApiParameterViewModel.ApiParameter
                                                                                    {
                                                                                        APICode = sel.APICode,
                                                                                        APIParameter = sel.APIParameter,
                                                                                        APIParamValue = sel.APIParamValue,
                                                                                    }).ToList();

                            bool catalogpost = false;
                            dr.ForEach(ex =>
                            {
                                //// SUB QUERY 
                                //ex["SalesQty"] = "";
                                catalogpost = true;
                                string itemcode = ex["ItemCode"].ToString();
                                string itemtitle = ex["FrgnName"].ToString();
                                string parentcode = ex["Parent"].ToString();
                                string bpcat = ex["BpCatalog"].ToString();
                                string bpname = ex["BpName"].ToString();
                                string lisnum = ex["ListNum"].ToString();
                                string ntegid = ex["IntegrationId"].ToString();
                                string imagefilename = ex["ImageFile"].ToString();
                                string imagefileid = ex["ImageId"].ToString();
                                string campno = ex["CampaignNumber"].ToString();

                                //// USE POST TO CREATE : USE PUT TO UPDATE 
                                var apimethod = string.IsNullOrEmpty(ntegid) ? "POST" : "PUT";
                                var origUrl = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                                var apiurl = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                                var apiuser = model.APIView.Select(x => x.APIKey).FirstOrDefault();
                                var apipwd = model.APIView.Select(x => x.APISecretKey).FirstOrDefault();

                                //// GET THE DATA THRU SERVICE LAYER TO MATCH THE FIELD MAPPING
                                string entity = string.IsNullOrEmpty(ntegid) ? ex["ItemType"].ToString().ToLower() : $@"{ex["ItemType"].ToString().ToLower()}/{ntegid}";
                                apiurl = apiurl.ToString().Replace("products", $@"{entity}");
                                ///DataRow dataheader = ex;

                                if (ex["ItemType"].ToString() == "Products")
                                {
                                    string ret = "", shopiprodjson = "";
                                    JToken _id = "", _title = "", _defVarId = "", _defVarTitle = "", _defVarIvtyId = "", _defImage = "", _defImageId = "";
                                    var jsitemrest = PostingHelpers.SBOResponse("GET", $@"Items('{itemcode}')", "", "", Creds);
                                    if (!jsitemrest.ToLower().Contains($@"""error"":") || !jsitemrest.ToLower().Contains($@"error :") || !jsitemrest.ToLower().Contains("error:"))
                                    {
                                        if (PostingHelpers.IsValidJson(jsitemrest))
                                        {
                                            var obj = JObject.Parse(jsitemrest);
                                            obj.Value<JObject>().Remove("odata.metadata");
                                            var jlist = obj.Properties().AsEnumerable().Where(x => x.Name != "odata.metadata").Select(x => x).ToList();
                                            jlist.ForEach(x =>
                                            {
                                                JToken propertyValue = x.Value;
                                                if (propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object)
                                                {
                                                    obj.Value<JObject>().Remove(x.Name);
                                                }
                                            });
                                            string njson = "[" + obj.ToString() + "]";
                                            DataTable dataheaders = JsonConvert.DeserializeObject<DataTable>(njson);
                                            DataRow dataheader = dataheaders.Rows[0];
                                            DataTable dataimages = null;

                                            #region POSTING_PRODUCTS
                                            ////CHECK IF PRODUCT IS ALREADY UPLOADED 
                                            string geturl = origUrl + $@"?title={ex["FrgnName"].ToString()}";
                                            ret = sapAces.APIResponse("GET", geturl, "", "", "", apiuser, apipwd, 0, paramlist); //Zero non limit
                                            if (PostingHelpers.IsValidJson(ret))
                                            {
                                                JObject json = JObject.Parse(ret);
                                                try { _id = json["products"][0]["id"]; _title = json["products"][0]["title"]; } catch { _title = ""; }

                                                if (itemtitle == _title.ToString() || ntegid == _id.ToString())
                                                {
                                                    #region PRODUCT_UPDATE_VERS2
                                                    string putapiurl = $@"{origUrl.Replace("products", $@"products/{_id.ToString()}")}";
                                                    shopiprodjson = PostingStringBuilderHelpers.BuildJsonShopiPostProduct(ex["ItemType"].ToString(), dataheader, model.HeaderFields);
                                                    ret = sapAces.APIResponse("PUT", putapiurl, "", shopiprodjson, "", apiuser, apipwd, 0, paramlist); //Zero non limit
                                                    if (PostingHelpers.IsValidJson(ret))
                                                    {
                                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ITEM_PUTSHOPI", CreateDate = DateTime.Now, ApiUrl = $@"PUT {putapiurl}", Json = shopiprodjson, ErrorMsg = "THIS IS INFORMATION ONLY" });
                                                        _context.SaveChanges();

                                                        JObject putjson = JObject.Parse(ret);
                                                        _id = putjson["product"]["id"];
                                                        _defVarId = putjson["product"]["variants"][0]["id"];
                                                        _defVarTitle = putjson["product"]["variants"][0]["title"];
                                                        _defVarIvtyId = putjson["product"]["variants"][0]["inventory_item_id"];

                                                        try
                                                        {
                                                            _defImage = putjson["product"]["image"]["src"];
                                                            _defImageId = putjson["product"]["image"]["id"];
                                                        }
                                                        catch { _defImage = null; _defImageId = ""; }

                                                        var jimages = putjson["product"]["images"];
                                                        var jimagelist = jimages.ToString().Replace("[", "").Replace("]", "");
                                                        string imgserialjson = "[" + jimagelist.ToString() + "]";
                                                        try { dataimages = JsonConvert.DeserializeObject<DataTable>(imgserialjson); }
                                                        catch { }

                                                        ////UPDATE SAP BP CATALOG ID IF NOT MATCH
                                                        if (ntegid != _id.ToString())
                                                        {
                                                            ////UPDATE SAP ITEM MASTER DATA BP CATALOG                                        
                                                            string sapjson = PostingStringBuilderHelpers.BuildJsonSAPCatalog(itemcode, bpcat, _id.ToString(), parentcode, "", "", "", $"{campno}", imagefilename, "");
                                                            PostingHelpers.LoginAction(Creds);
                                                            var result = PostingHelpers.SBOResponse("POST", $@"AlternateCatNum", sapjson, "", Creds);
                                                            if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                                                            {
                                                                ////Create logs for any error, and delete the uploaded product
                                                                //string DeleteProdUrl = origUrl.Replace("products", $@"products/{_id.ToString()}");
                                                                //string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = sapjson, ErrorMsg = result.ToString() });
                                                                _context.SaveChanges();
                                                                catalogpost = false;
                                                            }
                                                            else
                                                            {
                                                                ////DELETE THE OLD ONE BP CATALOG
                                                                PostingHelpers.LoginAction(Creds);
                                                                PostingHelpers.SBOResponse("DELETE", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{ntegid}')", "", "", Creds);
                                                                catalogpost = true;
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                }
                                                else
                                                {
                                                    ////UPLOAD TO SHOPIFY AS NEW
                                                    shopiprodjson = PostingStringBuilderHelpers.BuildJsonShopiPostProduct(ex["ItemType"].ToString(), dataheader, model.HeaderFields);
                                                    ret = sapAces.APIResponse("POST", origUrl, "", shopiprodjson, "", apiuser, apipwd, 0, paramlist); //Zero non limit
                                                    if (PostingHelpers.IsValidJson(ret))
                                                    {
                                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ITEM_POSTSHOPI", CreateDate = DateTime.Now, ApiUrl = $@"POST {origUrl}", Json = shopiprodjson, ErrorMsg = "THIS IS INFORMATION ONLY" });
                                                        _context.SaveChanges();

                                                        var newjson = JObject.Parse(ret);
                                                        _id = newjson["product"]["id"];
                                                        _defVarId = newjson["product"]["variants"][0]["id"];
                                                        _defVarTitle = newjson["product"]["variants"][0]["title"];
                                                        _defVarIvtyId = newjson["product"]["variants"][0]["inventory_item_id"];

                                                        try
                                                        {
                                                            var _tempdefImage = newjson["product"]["image"]["src"];
                                                            _defImageId = (_tempdefImage == null && _tempdefImage.Type == JTokenType.Null ? "" : _tempdefImage);
                                                            _defImageId = newjson["product"]["image"]["id"];
                                                        }
                                                        catch { _defImage = null; _defImageId = ""; }

                                                        string sapjson = PostingStringBuilderHelpers.BuildJsonSAPCatalog(itemcode, bpcat, _id.ToString(), "", "", "", "", campno, imagefilename, _defImageId.ToString());
                                                        PostingHelpers.LoginAction(Creds);
                                                        var result = PostingHelpers.SBOResponse("POST", $@"AlternateCatNum", sapjson, "", Creds);
                                                        if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                                                        {
                                                            ////NOT APPLICABLE TO DELETE ANYMORE
                                                            ////Create logs for any error, and delete the uploaded product
                                                            //string DeleteProdUrl = origUrl.Replace("products", $@"products/{_newid.ToString()}");
                                                            //string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = sapjson, ErrorMsg = result.ToString() });
                                                            _context.SaveChanges();
                                                            catalogpost = false;
                                                        }
                                                        else
                                                        {
                                                            ////DELETE THE OLD ONE BP CATALOG
                                                            PostingHelpers.LoginAction(Creds);
                                                            PostingHelpers.SBOResponse("DELETE", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{ntegid}')", "", "", Creds);
                                                            catalogpost = true;
                                                        }
                                                    }
                                                }

                                                #region PRODUCT_IMAGE
                                                if (catalogpost == true)
                                                {
                                                    ////POST IMAGE IF NO IMAGE SET OR NOT MATCH WITH FILE NAME                                                
                                                    string _searchimage = "";
                                                    if (dataimages != null)
                                                    {
                                                        _searchimage = dataimages.AsEnumerable().Where(sel => sel["id"].ToString() == _defImageId.ToString()).Select(sel => sel["src"].ToString()).FirstOrDefault();
                                                    }

                                                    string _defproductimage = (string.IsNullOrEmpty(_searchimage) ? "" : _searchimage);
                                                    string _imagefilename = (string.IsNullOrEmpty(imagefilename) ? "" : imagefilename.Substring(0, imagefilename.Length - 4).Replace(" ", "").Trim());
                                                    if (!string.IsNullOrEmpty(_imagefilename) && _defproductimage.Contains(_imagefilename))
                                                    {
                                                        ////IF THE SAME photo then update the imageid                                                    
                                                        string sapimgjson = "{" + $@" ""U_spImageId"": ""{_defImageId.ToString()}"" " + "}";
                                                        PostingHelpers.LoginAction(Creds);
                                                        PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_id.ToString()}')", sapimgjson, "", Creds);
                                                    }
                                                    else
                                                    {
                                                        ////Check if images is on the list of shopify
                                                        string findimage = "";
                                                        if (dataimages != null)
                                                        {
                                                            findimage = dataimages.AsEnumerable().Where(sel => sel["src"].ToString().Contains($@"{_imagefilename}")).Select(sel => sel["id"].ToString()).FirstOrDefault();
                                                        }

                                                        if (!string.IsNullOrEmpty(findimage))
                                                        {
                                                            var imgVarurl = origUrl.Replace("products", $@"products/{_id.ToString()}");
                                                            string imgVarjson = "{" + $@"""product"":" + "{" + $@"""id"": ""{_id.ToString()}"", ""image"": " + "{" + $@" id: {findimage.ToString()}, ""position"": 1 " + "}}}";
                                                            var imgret = sapAces.APIResponse("PUT", imgVarurl, "", imgVarjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                            if (PostingHelpers.IsValidJson(imgret))
                                                            {
                                                                string sapimgjson = "{" + $@" ""U_spImageId"": ""{findimage.ToString()}"" " + "}";
                                                                PostingHelpers.LoginAction(Creds);
                                                                PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_id.ToString()}')", sapimgjson, "", Creds);
                                                            }
                                                        }
                                                        else
                                                        {

                                                            string localpath = AppDomain.CurrentDomain.BaseDirectory + "\\FileImage";
                                                            var files = Directory.GetFiles(localpath, imagefilename, SearchOption.AllDirectories);
                                                            if (files.Length > 0)
                                                            {
                                                                string mvcpath = files.FirstOrDefault();
                                                                byte[] image = System.IO.File.ReadAllBytes(mvcpath);
                                                                var base64 = Convert.ToBase64String(image);
                                                                var imgjson = "{" + $@"""image"":" + "{" + $@"""attachment"": ""{base64}"", ""filename"":""{imagefilename}""" + "}}";
                                                                string imgposturl = origUrl.Replace("products", $@"products/{_id.ToString()}/images");
                                                                var imgret = sapAces.APIResponse("POST", imgposturl, "", imgjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                                if (PostingHelpers.IsValidJson(imgret))
                                                                {
                                                                    JObject jimge = JObject.Parse(imgret);
                                                                    var _imageid = jimge["image"]["id"];

                                                                    ////SET AS DEFAULT IMAGE ON PRODUCT AS POSITION (1) ONE
                                                                    var imgorderposturl = imgposturl.Replace("images", $@"images/{_imageid.ToString()}");
                                                                    string imgorderjson = "{" + $@"""image"":" + "{" + $@"""id"": ""{_imageid.ToString()}"", ""position"": 1 " + "}}";
                                                                    imgret = sapAces.APIResponse("PUT", imgorderposturl, "", imgorderjson, "", apiuser, apipwd, 0, null); //Zero non limit

                                                                    ////UPDATE BPCATALOG FOR IMAGE ID
                                                                    string sapimgjson = "{" + $@" ""U_spImageId"": ""{_imageid.ToString()}"" " + "}";
                                                                    PostingHelpers.LoginAction(Creds);
                                                                    PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_id.ToString()}')", sapimgjson, "", Creds);
                                                                }
                                                                else
                                                                {
                                                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {imgposturl}", Json = imgjson, ErrorMsg = imgret.ToString() });
                                                                    _context.SaveChanges();
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                            }

                                            #endregion

                                            #region POSTING_VARIANTS
                                            if (PostingHelpers.IsValidJson(ret))
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ITEM_VARIANT", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                                                _context.SaveChanges();

                                                if (catalogpost == true)
                                                {
                                                    //// GET & POST VARIANTS
                                                    var drvar = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                        Creds.CredentialDetails.SAPServerName,
                                                        Creds.CredentialDetails.SAPDBuser,
                                                        Creds.CredentialDetails.SAPDBPassword,
                                                        Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemVariant("hana", itemcode, lisnum, bpcat)).AsEnumerable().ToList()
                                                        :
                                                        DataAccess.Select(QueryAccess.MSSQL_conString(
                                                        Creds.CredentialDetails.SAPServerName,
                                                        Creds.CredentialDetails.SAPDBuser,
                                                        Creds.CredentialDetails.SAPDBPassword,
                                                        Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemVariant("mssql", itemcode, lisnum, bpcat)).AsEnumerable().ToList();

                                                    drvar.ForEach(bar =>
                                                    {
                                                        string varItemcode = bar["ItemCode"].ToString();
                                                        string varItemname = bar["FrgnName"].ToString();
                                                        string varIntegid = bar["IntegrationId"].ToString();
                                                        string varUoMCode = bar["spUoMCode"].ToString();
                                                        string varParent = bar["Parent"].ToString();
                                                        string varimagefilename = bar["ImageFile"].ToString();
                                                        string varimagefileid = bar["ImageId"].ToString();
                                                        JToken _varjsonDefimageid = "";


                                                        bool IsUploadedVariant = false; ////NOT RELEVANT. IT WILL CHECK IF ALREADY EXIST  
                                                        varIntegid = IsUploadedVariant == true ? varIntegid : "";
                                                        string varMethod = string.IsNullOrEmpty(varIntegid) ? "POST" : "PUT";
                                                        bool IsMultiUom = (varItemcode == varParent && !string.IsNullOrEmpty(varUoMCode) ? true : false);
                                                        ////RULE: IF integration is empty, then post /products/productid/variants.json 
                                                        ////RULE: ELSE update using /variants/variantid.json

                                                        string varApiurl = string.IsNullOrEmpty(varIntegid) ? origUrl.Replace("products", $@"products/{_id.ToString()}/{bar["ItemType"].ToString().ToLower()}") : origUrl.Replace("products", $@"{bar["ItemType"].ToString().ToLower()}/{varIntegid}");
                                                        DataRow datarows = bar;
                                                        string varshopjson = PostingStringBuilderHelpers.BuildJsonShopiPostVariant(bar["ItemType"].ToString(), datarows, model.RowFields, IsMultiUom);
                                                        string varshopret = sapAces.APIResponse(varMethod, varApiurl, "", varshopjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                        JToken _varid = "", _varTitle = "", _varIvtyId = "";

                                                        ////IF POSTED OR ALREADY EXISTS THEN CONTINUE SAP ITEM BPCATALOG POST OR UPDATE
                                                        if (PostingHelpers.IsValidJson(varshopret) || varshopret.Contains($@"already exists.""" + "]"))
                                                        {

                                                            if (varshopret.Contains($@"already exists.""" + "]"))
                                                            {
                                                                ////GET ALL VARIANT AND FIND THE EXISTING VARIANT THRU TITLE
                                                                varApiurl = origUrl.Replace("products", $@"products/{_id.ToString()}");
                                                                var searchvarshopret = sapAces.APIResponse("GET", varApiurl, "", varshopjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                                if (PostingHelpers.IsValidJson(searchvarshopret))
                                                                {
                                                                    JObject strbuilderjson = JObject.Parse(varshopjson);
                                                                    string _strbuilderjson = (string)strbuilderjson["variant"]["option1"];
                                                                    JObject searchvarjson = JObject.Parse(searchvarshopret);
                                                                    foreach (JObject variants in (JArray)searchvarjson["product"]["variants"])
                                                                    {
                                                                        var _vartitle = variants.GetValue("title");
                                                                        if (_strbuilderjson == (_vartitle == null ? "" : _vartitle.ToString()))
                                                                        {
                                                                            _varid = variants.GetValue("id");
                                                                            _varTitle = variants.GetValue("title");
                                                                            _varIvtyId = variants.GetValue("inventory_item_id");
                                                                            _varjsonDefimageid = variants.GetValue("image_id");
                                                                            break;
                                                                        }
                                                                    }

                                                                    ////PATCH THE SHOPIFY VARIANT
                                                                    string putvarApiurl = origUrl.Replace("products", $@"{bar["ItemType"].ToString().ToLower()}/{_varid}");
                                                                    //// varshopjson is the same from the above code
                                                                    string putvarshopret = sapAces.APIResponse("PUT", putvarApiurl, "", varshopjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                                    if (putvarshopret.ToLower().Contains($@"""error"":") || putvarshopret.ToLower().Contains($@"error :") || putvarshopret.ToLower().Contains("error:"))
                                                                    {
                                                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"PUT {varApiurl}", Json = varshopjson, ErrorMsg = putvarshopret.ToString() });
                                                                        _context.SaveChanges();
                                                                        catalogpost = false;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                JObject varjson = JObject.Parse(varshopret);
                                                                _varid = varjson["variant"]["id"];
                                                                _varTitle = varjson["variant"]["title"];
                                                                _varIvtyId = varjson["variant"]["inventory_item_id"];
                                                            }

                                                            ////UPDATE SAP ITEM MASTER DATA BP CATALOG  

                                                            if (IsMultiUom)
                                                            {
                                                                //// POST THE INTEGRATION ID TO THE SAP UDO Multiple UoM 
                                                                //// CHECK IF EXIST
                                                                PostingHelpers.LoginAction(Creds);
                                                                #region Version1
                                                                //var checkresult = PostingHelpers.SBOResponse("GET", $@"spItemUoM('{varItemcode}')", "", "", Creds);
                                                                //if (PostingHelpers.IsValidJson(checkresult))
                                                                //{
                                                                //    if (checkresult.ToLower().Contains("no matching records found"))
                                                                //    {
                                                                //        ////POST NEW RECORDS ON UDO
                                                                //        model.SapUdoItemUom = new PostingViewModel.SapUdoItemUoM()
                                                                //        {
                                                                //            Code = varItemcode,
                                                                //            Name = "",
                                                                //            U_spCardCode = bpcat,
                                                                //            U_spCardName = bpname,
                                                                //            U_spItemName = varItemname
                                                                //        };
                                                                //        model.SapUdoItemUom.SPITEMUOMDCollection.Add(new PostingViewModel.SPITEMUOMDCollections
                                                                //        {
                                                                //            Code = varItemcode,
                                                                //            LineId = 0,
                                                                //            U_spItemCode = varItemcode,
                                                                //            U_spUomCode = bar["spUomCode"].ToString(),
                                                                //            U_spUomName = bar["spUomName"].ToString(),
                                                                //            U_spSubstitute = _varid.ToString(),
                                                                //            U_spInventoryId = _varIvtyId.ToString(),
                                                                //        });

                                                                //        string sapuomjson = PostingStringBuilderHelpers.BuildJsonSAPUdoItemUoM(varItemcode, "", model.SapUdoItemUom);
                                                                //        var uomresult = PostingHelpers.SBOResponse("POST", $@"spItemUoM('{varItemcode}')", sapuomjson, "DocEntry", Creds);
                                                                //        if (uomresult.ToLower().Contains($@"""error"":") || uomresult.ToLower().Contains($@"error :") || uomresult.ToLower().Contains("error:"))
                                                                //        {
                                                                //            string DeleteProdUrl = origUrl.Replace("products", $@"products/{_id.ToString()}/variants/{_varid.ToString()}");
                                                                //            string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                                //            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPIPAddress}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/spItemUoM('{varItemcode}')", Json = sapuomjson, ErrorMsg = checkresult.ToString() });
                                                                //            _context.SaveChanges();
                                                                //        }
                                                                //    }
                                                                //    else
                                                                //    {
                                                                //        ////UPDATE EXISTING RECORDS ON UDO
                                                                //        if (PostingHelpers.IsValidJson(checkresult))
                                                                //        {
                                                                //            bool newitem = true;
                                                                //            JObject checkjson = JObject.Parse(checkresult);
                                                                //            model.SapUdoItemUom = JsonConvert.DeserializeObject<PostingViewModel.SapUdoItemUoM>(checkjson.ToString());
                                                                //            model.SapUdoItemUom.SPITEMUOMDCollection.ForEach(fe =>
                                                                //            {
                                                                //                if (fe.U_spItemCode == varItemcode && fe.U_spUomName == _varTitle.ToString())
                                                                //                {
                                                                //                    fe.U_spSubstitute = _varid.ToString();
                                                                //                    fe.U_spInventoryId = _varIvtyId.ToString();
                                                                //                    newitem = false;
                                                                //                }
                                                                //            });

                                                                //            if (newitem == false)
                                                                //            {
                                                                //                string sapuomjson = PostingStringBuilderHelpers.BuildJsonSAPUdoItemUoM(varItemcode, "", model.SapUdoItemUom);
                                                                //                var uomresult = PostingHelpers.SBOResponse("PATCH", $@"spItemUoM('{varItemcode}')", sapuomjson, "", Creds);

                                                                //            }
                                                                //            else
                                                                //            {
                                                                //                model.SapUdoItemUom.SPITEMUOMDCollection.Add(new PostingViewModel.SPITEMUOMDCollections
                                                                //                {
                                                                //                    Code = varItemcode,
                                                                //                    LineId = 0,
                                                                //                    U_spItemCode = varItemcode,
                                                                //                    U_spUomCode = bar["spUomCode"].ToString(),
                                                                //                    U_spUomName = bar["spUomName"].ToString(),
                                                                //                    U_spSubstitute = _varid.ToString(),
                                                                //                    U_spInventoryId = _varIvtyId.ToString()
                                                                //                });

                                                                //                string sapuomjson = PostingStringBuilderHelpers.BuildJsonSAPUdoItemUoM(varItemcode, "", model.SapUdoItemUom);
                                                                //                var uomresult = PostingHelpers.SBOResponse("PUT", $@"spItemUoM('{varItemcode}')", sapuomjson, "", Creds);
                                                                //            }
                                                                //        }
                                                                //    }

                                                                //}
                                                                //else
                                                                //{
                                                                //    ////Create logs for any error
                                                                //    string DeleteProdUrl = origUrl.Replace("products", $@"products/{_id.ToString()}/variants/{_varid.ToString()}");
                                                                //    string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                                //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"GET {Creds.CredentialDetails.SAPIPAddress}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/spItemUoM('{varItemcode}')", Json = "", ErrorMsg = checkresult.ToString() });
                                                                //    _context.SaveChanges();
                                                                //}
                                                                #endregion

                                                                #region Version2
                                                                //IsInitialId for Variant 
                                                                bool initialSubstitute = (bar["IntegrationId"].ToString() == bar["spUoMCode"].ToString() ? true : false);
                                                                var _substitute = (initialSubstitute == true ? bar["IntegrationId"].ToString() : _varid.ToString());
                                                                var checkresult = PostingHelpers.SBOResponse("GET", $@"AlternateCatNum(ItemCode='{varItemcode}',CardCode='{bpcat}',Substitute='{_substitute.ToString()}')", "", "", Creds);
                                                                if (PostingHelpers.IsValidJson(checkresult))
                                                                {
                                                                    if (checkresult.ToLower().Contains("no matching records found"))
                                                                    {
                                                                        ////POST NEW BPCATALOG
                                                                        string sapjson = PostingStringBuilderHelpers.BuildJsonSAPCatalog(varItemcode, bpcat, _varid.ToString(), parentcode, _varIvtyId.ToString(), "", "");
                                                                        PostingHelpers.LoginAction(Creds);
                                                                        var uomresult = PostingHelpers.SBOResponse("POST", $@"AlternateCatNum", sapjson, "", Creds);
                                                                        if (uomresult.ToLower().Contains($@"""error"":") || uomresult.ToLower().Contains("error :") || uomresult.ToLower().Contains("error:"))
                                                                        {
                                                                            ////Create logs for any error
                                                                            //string DeleteProdUrl = origUrl.Replace("products", $@"products/{_id.ToString()}/variants/{_varid.ToString()}");
                                                                            //string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = sapjson, ErrorMsg = uomresult.ToString() });
                                                                            _context.SaveChanges();
                                                                        }
                                                                    }
                                                                    else
                                                                    {

                                                                        if (_varid.ToString() == _substitute.ToString())
                                                                        {
                                                                            ////UPDATE BPCATALOG ONLY
                                                                            ////UPDATE BPCATALOG FOR IMAGE ID
                                                                            JToken _imageid = _varjsonDefimageid == null ? "" : _varjsonDefimageid.ToString();
                                                                            if (!string.IsNullOrEmpty(_imageid.ToString()))
                                                                            {
                                                                                string sapimgjson = "{" + $@" ""U_spImageId"": ""{_imageid.ToString()}"" " + "}";
                                                                                PostingHelpers.LoginAction(Creds);
                                                                                PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_varid.ToString()}')", sapimgjson, "", Creds);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            ////POST NEW
                                                                            ////ELSE DELETE FIRST THE EXISTING RECORD THEN POST THE GET-RESULT JSON BUT MUST UPDATE FIRST THE Substitute Value
                                                                            if (!_context.VariantTemp.Where(sel => sel.ItemCode == varItemcode && sel.CardCode == bpcat && sel.Substitute == _substitute.ToString()).Any())
                                                                            {
                                                                                _context.VariantTemp.Add(new DomainLayer.Models.VariantTemp { ItemCode = varItemcode, CardCode = bpcat, Substitute = _substitute.ToString(), UomCode = bar["spUoMCode"].ToString() });
                                                                                _context.SaveChanges();
                                                                            }

                                                                            PostingHelpers.LoginAction(Creds);
                                                                            PostingHelpers.SBOResponse("DELETE", $@"AlternateCatNum(ItemCode='{varItemcode}',CardCode='{bpcat}',Substitute='{_substitute.ToString()}')", "", "", Creds);
                                                                            JObject _checkresult = JObject.Parse(checkresult);
                                                                            _checkresult["Substitute"] = $@"{_varid.ToString()}";
                                                                            _checkresult["U_spInventoryId"] = $@"{_varIvtyId.ToString()}";
                                                                            PostingHelpers.LoginAction(Creds);
                                                                            var uomresult = PostingHelpers.SBOResponse("POST", $@"AlternateCatNum", _checkresult.ToString(), "", Creds);
                                                                            if (uomresult.ToLower().Contains($@"""error"":") || uomresult.ToLower().Contains("error :") || uomresult.ToLower().Contains("error:"))
                                                                            {
                                                                                ////Create logs for any error
                                                                                //string DeleteProdUrl = origUrl.Replace("products", $@"products/{_id.ToString()}/variants/{_varid.ToString()}");
                                                                                //string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = _checkresult.ToString(), ErrorMsg = uomresult.ToString() });
                                                                                _context.SaveChanges();
                                                                            }

                                                                            try
                                                                            {
                                                                                var dbvariant = _context.VariantTemp.Where(sel => sel.ItemCode == varItemcode && sel.CardCode == bpcat && sel.Substitute == _substitute.ToString()).FirstOrDefault();
                                                                                _context.VariantTemp.Remove(dbvariant);
                                                                                _context.SaveChanges();
                                                                            }
                                                                            catch
                                                                            {
                                                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"DELETE dbo.VariantTemps", Json = "", ErrorMsg = $@"DELETE FROM VariantTemps WHERE ItemCode='{varItemcode}' and CardCode='{bpcat}' and Substitute='{_substitute.ToString()}' " });
                                                                                _context.SaveChanges();
                                                                            }
                                                                        }

                                                                    }
                                                                }
                                                                #endregion
                                                            }
                                                            else
                                                            {
                                                                if (varMethod == "POST")
                                                                {
                                                                    string sapjson = PostingStringBuilderHelpers.BuildJsonSAPCatalog(varItemcode, bpcat, _varid.ToString(), parentcode, _varIvtyId.ToString(), "", "");
                                                                    PostingHelpers.LoginAction(Creds);
                                                                    var uomresult = PostingHelpers.SBOResponse("POST", $@"AlternateCatNum", sapjson, "", Creds);
                                                                    if (uomresult.ToLower().Contains($@"""error"":") || uomresult.ToLower().Contains("error :") || uomresult.ToLower().Contains("error:"))
                                                                    {
                                                                        ////Due To Automation, the delete will no longer applicable
                                                                        //string DeleteProdUrl = origUrl.Replace("products", $@"products/{_id.ToString()}/variants/{_varid.ToString()}");
                                                                        //string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                                        ////Create logs for any error
                                                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = sapjson, ErrorMsg = uomresult.ToString() });
                                                                        _context.SaveChanges();
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    ////UPDATE SAP ITEM MASTER DATA BP CATALOG                                        
                                                                    string sapjson = PostingStringBuilderHelpers.BuildJsonSAPCatalog(varItemcode, bpcat, _varid.ToString(), parentcode, _varIvtyId.ToString(), "", "");
                                                                    PostingHelpers.LoginAction(Creds);
                                                                    var result = PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{varItemcode}',CardCode='{bpcat}',Substitute='{_varid.ToString()}')", sapjson, "", Creds);
                                                                    if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                                                                    {
                                                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"PATCH {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = sapjson, ErrorMsg = result.ToString() });
                                                                        _context.SaveChanges();
                                                                        catalogpost = false;
                                                                    }
                                                                }
                                                            }

                                                        }
                                                        else
                                                        {
                                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"{varMethod} {varApiurl}", Json = varshopjson, ErrorMsg = varshopret.ToString() });
                                                            _context.SaveChanges();
                                                        }

                                                        ////POST VARIANT IMAGE IF NO IMAGE SET OR NOT MATCH WITH FILE NAME
                                                        string idjs = (_varjsonDefimageid == null ? "0" : _varjsonDefimageid.ToString());
                                                        string _searchimage = "";
                                                        if (dataimages == null)
                                                        {
                                                            _searchimage = "";
                                                        }
                                                        else
                                                        {
                                                            _searchimage = dataimages.AsEnumerable().Where(sel => sel["id"].ToString() == idjs).Select(sel => sel["src"].ToString()).FirstOrDefault();
                                                        }

                                                        string _defproductimage = (string.IsNullOrEmpty(_searchimage) ? "" : _searchimage);
                                                        string _imagefilename = (string.IsNullOrEmpty(varimagefilename) ? "" : varimagefilename.Substring(0, varimagefilename.Length - 4).Replace(" ", "").Trim());
                                                        if (!string.IsNullOrEmpty(_imagefilename) && _defproductimage.Contains(_imagefilename))
                                                        {
                                                            ////IF THE SAME photo then update the imageid                                                    
                                                            string sapimgjson = "{" + $@" ""U_spImageId"": ""{_defImageId.ToString()}"" " + "}";
                                                            PostingHelpers.LoginAction(Creds);
                                                            PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_varid.ToString()}')", sapimgjson, "", Creds);
                                                        }
                                                        else
                                                        {
                                                            ////Check if images is on the list of shopify
                                                            string findimage = "";
                                                            if (dataimages != null)
                                                            {
                                                                findimage = dataimages.AsEnumerable().Where(sel => sel["src"].ToString().Contains($@"{varimagefilename}")).Select(sel => sel["id"].ToString()).FirstOrDefault();
                                                            }

                                                            if (!string.IsNullOrEmpty(findimage))
                                                            {
                                                                var imgVarurl = origUrl.Replace("products", $@"variants/{_varid.ToString()}");
                                                                string imgVarjson = "{" + $@"""variant"":" + "{" + $@"""id"": ""{_varid.ToString()}"", ""image_id"": ""{findimage.ToString()}"" " + "}}";
                                                                var imgret = sapAces.APIResponse("PUT", imgVarurl, "", imgVarjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                                if (PostingHelpers.IsValidJson(imgret))
                                                                {
                                                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ITEM_SHOPIVARIMG_OLD", CreateDate = DateTime.Now, ApiUrl = $@"PUT {imgVarurl}", Json = imgVarjson, ErrorMsg = "THIS IS INFORMATION ONLY" });
                                                                    _context.SaveChanges();

                                                                    string sapimgjson = "{" + $@" ""U_spImageId"": ""{findimage.ToString()}"" " + "}";
                                                                    PostingHelpers.LoginAction(Creds);
                                                                    PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_varid.ToString()}')", sapimgjson, "", Creds);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                string localpath = AppDomain.CurrentDomain.BaseDirectory + "\\FileImage";
                                                                var files = Directory.GetFiles(localpath, varimagefilename, SearchOption.AllDirectories);
                                                                if (files.Length > 0)
                                                                {
                                                                    string mvcpath = files.FirstOrDefault();
                                                                    byte[] image = System.IO.File.ReadAllBytes(mvcpath);
                                                                    var base64 = Convert.ToBase64String(image);
                                                                    var imgjson = "{" + $@"""image"":" + "{" + $@"""attachment"": ""{base64}"", ""filename"":""{varimagefilename}""" + "}}";
                                                                    string imgposturl = origUrl.Replace("products", $@"products/{_id.ToString()}/images");
                                                                    var imgret = sapAces.APIResponse("POST", imgposturl, "", imgjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                                    if (PostingHelpers.IsValidJson(imgret))
                                                                    {
                                                                        JObject jimge = JObject.Parse(imgret);
                                                                        var _imageid = jimge["image"]["id"];

                                                                        ////SET AS DEFAULT IMAGE ON PRODUCT AS POSITION (1) ONE
                                                                        var imgVarurl = origUrl.Replace("products", $@"variants/{_varid.ToString()}");
                                                                        string imgVarjson = "{" + $@"""variant"":" + "{" + $@"""id"": ""{_varid.ToString()}"", ""image_id"": ""{_imageid.ToString()}"" " + "}}";
                                                                        imgret = sapAces.APIResponse("PUT", imgVarurl, "", imgVarjson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                                        if (PostingHelpers.IsValidJson(imgret))
                                                                        {
                                                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "API_ITEM_SHOPIVARIMG_NEW", CreateDate = DateTime.Now, ApiUrl = $@"PUT {imgVarurl}", Json = imgVarjson, ErrorMsg = "THIS IS INFORMATION ONLY" });
                                                                            _context.SaveChanges();

                                                                            ////UPDATE BPCATALOG FOR IMAGE ID
                                                                            string sapimgjson = "{" + $@" ""U_spImageId"": ""{_imageid.ToString()}"" " + "}";
                                                                            PostingHelpers.LoginAction(Creds);
                                                                            var varImgRes = PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_varid.ToString()}')", sapimgjson, "", Creds);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {imgposturl}", Json = imgjson, ErrorMsg = imgret.ToString() });
                                                                        _context.SaveChanges();
                                                                    }
                                                                }
                                                            }
                                                        }

                                                    });

                                                    if (drvar.Count == 0)
                                                    {
                                                        ////IF no variant then set the SKU of default variant
                                                        string singleVarUrl = origUrl.Replace("products", $@"variants/{_defVarId.ToString()}");
                                                        string varshopjson = PostingStringBuilderHelpers.BuildJsonShopiDefaultVariant(ex["ItemType"].ToString(), dataheader);
                                                        string varshopret = sapAces.APIResponse("PUT", singleVarUrl, "", varshopjson, "", apiuser, apipwd, 0, null); //Zero non limit

                                                        ////UPDATE SAP ITEM MASTER DATA BP CATALOG                                        
                                                        string sapjson = PostingStringBuilderHelpers.BuildJsonSAPCatalog(itemcode, bpcat, _defVarId.ToString(), parentcode, _defVarIvtyId.ToString(), "", "");
                                                        PostingHelpers.LoginAction(Creds);
                                                        var result = PostingHelpers.SBOResponse("PATCH", $@"AlternateCatNum(ItemCode='{itemcode}',CardCode='{bpcat}',Substitute='{_defVarId.ToString()}')", sapjson, "", Creds);
                                                        if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                                                        {
                                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"PATCH {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = sapjson, ErrorMsg = result.ToString() });
                                                            _context.SaveChanges();
                                                            catalogpost = false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ////LEGIT CODE: DELETE THE DEFAULT VARIANT
                                                        if (_defVarTitle.ToString().ToLower().Contains("default title"))
                                                        {
                                                            string DeleteProdUrl = origUrl.Replace("products", $@"variants/{_defVarId.ToString()}");
                                                            string deleteShopret = sapAces.APIResponse("DELETE", DeleteProdUrl, "", "", "", apiuser, apipwd, 0, null); //Zero non limit
                                                        }
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"{apimethod} {apiurl}", Json = shopiprodjson, ErrorMsg = ret.ToString() });
                                                _context.SaveChanges();
                                            }
                                            #endregion

                                        }
                                    }
                                }

                            });

                            ////REGENERATE AGAIN THE MAIN PRODUCTS TO GET THE INTEGRATION ID
                            dr = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                     Creds.CredentialDetails.SAPServerName,
                                     Creds.CredentialDetails.SAPDBuser,
                                     Creds.CredentialDetails.SAPDBPassword,
                                     Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemProduct("hana")).AsEnumerable().ToList()
                                 :
                                 DataAccess.Select(QueryAccess.MSSQL_conString(
                                     Creds.CredentialDetails.SAPServerName,
                                     Creds.CredentialDetails.SAPDBuser,
                                     Creds.CredentialDetails.SAPDBPassword,
                                     Creds.CredentialDetails.SAPDBName), QueryAccess.GetItemProduct("mssql")).AsEnumerable().ToList();

                            ////PATCH THE SAP CAMPAIGN AS UPLOADED
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "ITEM CAMPAIGN", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                            _context.SaveChanges();
                            if (dr.Count > 0)
                            {
                                PostingHelpers.LoginAction(Creds);
                                string cpno = dr.Select(sel => sel["CampaignNumber"].ToString()).FirstOrDefault();
                                string campjson = "{" + $@"""U_spUploadStatus"": ""U""" + "}";
                                var campresult = PostingHelpers.SBOResponse("PATCH", $@"Campaigns({cpno})", campjson, "", Creds);
                                if (campresult.ToLower().Contains($@"""error"":"))
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"PATCH {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/Campaigns({cpno})", Json = campjson, ErrorMsg = campresult.ToString() });
                                    _context.SaveChanges();
                                }
                            }

                            //// UPLOAD COLLECTION   
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "ITEM COLLECT", CreateDate = DateTime.Now, ApiUrl = $@"COLLECTION  SHOPIFY", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                            _context.SaveChanges();
                            var drcollec = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                         Creds.CredentialDetails.SAPServerName,
                                         Creds.CredentialDetails.SAPDBuser,
                                         Creds.CredentialDetails.SAPDBPassword,
                                         Creds.CredentialDetails.SAPDBName), QueryAccess.GetUdoCollection()).AsEnumerable().ToList()
                                         :
                                         DataAccess.Select(QueryAccess.MSSQL_conString(
                                         Creds.CredentialDetails.SAPServerName,
                                         Creds.CredentialDetails.SAPDBuser,
                                         Creds.CredentialDetails.SAPDBPassword,
                                         Creds.CredentialDetails.SAPDBName), QueryAccess.GetUdoCollection()).AsEnumerable().ToList();

                            drcollec.ForEach(dcol =>
                            {
                                var origUrl = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                                var apiurl = origUrl.Replace("products", "custom_collections");
                                var apiuser = model.APIView.Select(x => x.APIKey).FirstOrDefault();
                                var apipwd = model.APIView.Select(x => x.APISecretKey).FirstOrDefault();
                                string ntegid = dcol["U_spSubstitute"].ToString();
                                var checkurl = string.IsNullOrEmpty(ntegid) ? apiurl : apiurl.Replace("custom_collections", $@"custom_collections/{ntegid}");

                                string apimethod = "POST";
                                if (!string.IsNullOrEmpty(ntegid))
                                {
                                    apimethod = "PUT";
                                }
                                string json = "{" + $@"""custom_collection"":" + "{" + $@"""title"":""{dcol["Name"].ToString()}""" + "}}";
                                string ret = sapAces.APIResponse(apimethod, checkurl, "", json, "", apiuser, apipwd, 0, null); //Zero non limit


                                if (PostingHelpers.IsValidJson(ret))
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "ITEM COLLECT", CreateDate = DateTime.Now, ApiUrl = $@"{checkurl}", Json = $@"{json}", ErrorMsg = ret.ToString() });
                                    _context.SaveChanges();
                                    ////UPDATE THE SAP UDO COLLECTION FOR INTEGRATION ID
                                    JObject collectjson = JObject.Parse(ret);
                                    var _collectid = collectjson["custom_collection"]["id"];
                                    var _collecttitle = collectjson["custom_collection"]["title"];
                                    if (apimethod == "POST")
                                    {
                                        string collecjson = "{" + $@"""U_spSubstitute"": ""{_collectid}""" + "}";
                                        var collecresult = PostingHelpers.SBOResponse("PATCH", $@"spCollections('{dcol["Code"].ToString()}')", collecjson, "", Creds);
                                        if (collecresult.ToLower().Contains($@"""error"":") || collecresult.ToLower().Contains($@"""error"" :") || collecresult.ToLower().Contains($@"error:"))
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"PATCH {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/spCollections({dcol["Code"].ToString()})", Json = collecjson, ErrorMsg = collecresult.ToString() });
                                            _context.SaveChanges();
                                        }
                                    }
                                    ////ADD PRODUCTS TO THE COLLECTION
                                    var prod = dr.AsEnumerable().Where(sel => sel["U_spCollection"].ToString() == _collecttitle.ToString()).ToList();
                                    if (prod.Count > 0)
                                    {
                                        apiurl = origUrl.Replace("products", "collects");
                                        prod.ForEach(xid =>
                                        {
                                            string prodcoljson = "{" + $@" ""collect"":" + "{" + $@"""product_id"": {xid["IntegrationId"].ToString()}, ""collection_id"": {_collectid}" + "}}";
                                            string prodcolret = sapAces.APIResponse("POST", apiurl, "", prodcoljson, "", apiuser, apipwd, 0, null); //Zero non limit
                                                                                                                                                    ////SINCE NO METHOD FOR PATCH, THEN EXCLUDE ERROR IF ALREADY EXISTS
                                            if ((!prodcolret.ToLower().Contains("already exists")) && (prodcolret.ToLower().Contains($@"""error"":") || prodcolret.ToLower().Contains($@"""error"" :") || prodcolret.ToLower().Contains($@"error:")))
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"{apimethod} {apiurl}", Json = prodcoljson, ErrorMsg = prodcolret.ToString() });
                                                _context.SaveChanges();
                                            }
                                        });
                                    }
                                }
                            });

                        }
                        else if (model.APIView.Select(x => x.APIURL).FirstOrDefault().ToLower().Contains("lazada.com"))
                        {

                        }

                        ////UPDATE AUTOMATION TO START
                        sched.ForceStop = false;
                        _context.SaveChanges();
                    }
                    else if (Creds.CredentialDetails.Module.ToLower().Contains("sales order"))
                    {
                        if (model.APIView.Select(x => x.APIURL).FirstOrDefault().ToLower().Contains("shopify.com"))
                        {

                        }
                        else if (model.APIView.Select(x => x.APIURL).FirstOrDefault().ToLower().Contains("lazada.com"))
                        {

                        }
                    }
                    else
                    {

                    }
                }
                else
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = "Invalid SBO login Credential" });
                    _context.SaveChanges();
                }
            }

            return model;
        }

        public void RevertLastSync(int id, string sync)
        {
            var mapsync = _context.FieldMappings.Find(id);
            if (mapsync != null)
            {
                mapsync.LastSync = Convert.ToDateTime(sync);
                _context.Entry(mapsync).State = System.Data.Entity.EntityState.Modified;
                _context.SaveChanges();
            }

        }

        public PostingViewModel GetSAPPostUploadtoAddon(string[] modules)
        {
            var model = new PostingViewModel();
            string sync = "";
            int id = 0;
            try
            {
                Properties.Settings prop = new Properties.Settings();
                //Settings prop = new Settings();

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_START", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                _context.SaveChanges();

                foreach (string modulecreds in modules)
                {
                    var Creds = GetCredentialsMapCode(modulecreds);
                    string TableName = "";
                    string ret = "";
                    if (Creds.CredentialDetails != null)
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_ITEM_CREDS", CreateDate = DateTime.Now, ApiUrl = $@"CREDETIALS  {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                        _context.SaveChanges();

                        id = Convert.ToInt32(Creds.CredentialDetails.MapId);
                        List<DataRow> ColumnNameTable = new List<DataRow>();

                        //SET LAST SYNC DATE INTO CURRENT DATETIME
                        var mapsync = _context.FieldMappings.Find(Convert.ToInt32(Creds.CredentialDetails.MapId));
                        mapsync.LastSync = DateTime.Now.Date;
                        _context.Entry(mapsync).State = System.Data.Entity.EntityState.Modified;
                        _context.SaveChanges();

                        #region STOP_BPCATALOG_ENTERVENTION                        
                        //var _SchedProces = _context.Schedules
                        //  .Join(_context.Process, sch => sch.Process, pr => pr.ProcessCode, (sch, pr) => new { sch, pr })
                        //  .Join(_context.APISetups, apc => apc.pr.APICode, ap => ap.APICode, (apc, ap) => new { apc, ap })
                        //  .Where(sel => sel.ap.APIURL.Contains("sap/upload/documents"))
                        //  .Select(sel => sel.apc.sch.SchedId).FirstOrDefault();
                        //////FORCE STOP THE BP CATALOG AUTOMATION
                        //var sched = _context.Schedules.Where(sel => sel.SchedId == _SchedProces).FirstOrDefault();
                        //sched.ForceStop = true;
                        //_context.SaveChanges();
                        #endregion

                        model.HeaderFields = _context.Headers.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                        {
                            DefaultVal = x.SourceTableName,
                        }).Distinct().ToList() ?? new List<PostingViewModel.Fields>();
                        model.RowFields = _context.Rows.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                        {
                            DefaultVal = x.DefaultValue,
                        }).Distinct().ToList() ?? new List<PostingViewModel.Fields>();

                        model.APIView = _context.FieldMappings.Where(x => x.MapId == Creds.CredentialDetails.MapId)
                                         .Join(_context.APISetups, f => f.APICode, a => a.APICode, (f, a) => new { f, a })
                                         .Select(x => new PostingViewModel.APIViewModel
                                         {
                                             APIId = x.a.APIId,
                                             APICode = x.a.APICode,
                                             APIMethod = x.a.APIMethod,
                                             APIURL = x.a.APIURL,
                                             APIKey = x.a.APIKey,
                                             APISecretKey = x.a.APISecretKey,
                                             APIToken = x.a.APIToken,
                                             APILoginUrl = x.a.APILoginUrl,
                                             APILoginBody = x.a.APILoginBody,
                                         }).ToList();

                        //UPLOADING FROM SAP TO ADDON
                        string qrystart = "SET XACT_ABORT ON BEGIN TRANSACTION ";
                        string qryend = " COMMIT TRANSACTION";

                        string updatedatecolumnname = "";
                        string createdatecolumnname = "";

                        //HEADER TABLE
                        string qryheader = "";
                        string qrydocentry = "";
                        sync = Creds.CredentialDetails.LastSync.HasValue ? Convert.ToDateTime(Creds.CredentialDetails.LastSync).ToString("yyyy-MM-dd") : DateTime.MinValue.Date.ToString("yyyy-MM-dd");
                        foreach (var item in model.HeaderFields)
                        {
                            //GET COLUMN NAME FOR UPDATE AND CREATE DATE
                            ColumnNameTable = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                          Creds.CredentialDetails.SAPServerName,
                                          Creds.CredentialDetails.SAPDBuser,
                                          Creds.CredentialDetails.SAPDBPassword,
                                          Creds.CredentialDetails.SAPDBName, $@"SELECT COLUMN_NAME FROM SYS.COLUMNS
                                                                        WHERE SCHEMA_NAME = '{Creds.CredentialDetails.SAPDBName}' 
                                                                        AND TABLE_NAME = '{item.DefaultVal}'
                                                                        AND UPPER(""COLUMN_NAME"") IN('UPDATEDATE','CREATEDATE')")
                                          .AsEnumerable().Select(x => x).ToList();

                            foreach (var columns in ColumnNameTable)
                            {
                                if (columns[0].ToString().ToUpper().Contains("CREATE"))
                                {
                                    createdatecolumnname = columns[0].ToString();
                                }
                                else
                                {
                                    updatedatecolumnname = columns[0].ToString();
                                }
                            }

                            string headertableqry = ColumnNameTable.Count > 0 ? $@" SELECT * FROM {item.DefaultVal} " +
                                          $@" WHERE {(Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? "IFNULL" : "ISNULL")}(""{updatedatecolumnname}"",""{createdatecolumnname}"") >= '{sync}'"
                                          :
                                          $@" SELECT * FROM {item.DefaultVal} ";


                            qrydocentry = "";
                            TableName = item.DefaultVal;
                            List<DataRow> HeaderTable = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                          Creds.CredentialDetails.SAPServerName,
                                          Creds.CredentialDetails.SAPDBuser,
                                          Creds.CredentialDetails.SAPDBPassword,
                                          Creds.CredentialDetails.SAPDBName, headertableqry)
                                          .AsEnumerable().Select(x => x).ToList();

                            if (HeaderTable.Count > 0)
                            {
                                foreach (DataRow row in HeaderTable)
                                {
                                    string primarykeysqrysap = "";
                                    //GET LIST OF COLUMNS WITH PRIMARY KEY
                                    List<DataRow> HeaderPrimaryKey = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                                                          Creds.CredentialDetails.SAPServerName,
                                                                          Creds.CredentialDetails.SAPDBuser,
                                                                          Creds.CredentialDetails.SAPDBPassword,
                                                                          Creds.CredentialDetails.SAPDBName, Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? $@"
                                                                                                    SELECT COLUMN_NAME FROM ""CONSTRAINTS"" 
                                                                                                WHERE SCHEMA_NAME = '{Creds.CredentialDetails.SAPDBName}' 
                                                                                                AND TABLE_NAME = '{item.DefaultVal}' AND IS_PRIMARY_KEY = 'TRUE';
                                                                                                "
                                                                                                                        :
                                                                                                                        $@"SELECT 
                                                                                                column_name as COLUMN_NAME
                                                                                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 

                                                                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                                                                                                ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                                                                                                AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                                                                                                AND KU.table_name='{item.DefaultVal}'

                                                                                            ORDER BY 
                                                                                                    KU.TABLE_NAME
                                                                                                ,KU.ORDINAL_POSITION
                                                                                            ; ")
                                                                          .AsEnumerable().Select(x => x).ToList();

                                    foreach (var key in HeaderPrimaryKey)
                                    {
                                        foreach (DataColumn keycol in HeaderPrimaryKey[HeaderPrimaryKey.IndexOf(key)].Table.Columns)
                                        {
                                            primarykeysqrysap += $@"{row[key[keycol].ToString()]}_";
                                        }
                                    }

                                    //CHECK IF EXISTING IN PROGRESS
                                    List<DataRow> checkexist = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                                                                  Creds.CredentialDetails.SAPServerName,
                                                                                  Creds.CredentialDetails.SAPDBuser,
                                                                                  Creds.CredentialDetails.SAPDBPassword,
                                                                                  Creds.CredentialDetails.SAPDBName, $@" SELECT * FROM ""SAOLIVE_LINKBOX"".""{Creds.CredentialDetails.SAPDBName}_{item.DefaultVal}"" 
                                                                        WHERE 
                                                                        ""DocEntry"" = '{item.DefaultVal}_{primarykeysqrysap.Remove(primarykeysqrysap.Length - 1, 1).Replace("'", "''")}'")
                                                                        .AsEnumerable().Select(x => x).ToList();
                                    //List<DataRow> checkexist = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                    //      Creds.CredentialDetails.SAPServerName,
                                    //      Creds.CredentialDetails.SAPDBuser,
                                    //      Creds.CredentialDetails.SAPDBPassword,
                                    //      Creds.CredentialDetails.SAPDBName, $@" SELECT * FROM ""SAOLIVE_LINKBOX"".""{Creds.CredentialDetails.SAPDBName}_{item.DefaultVal}"" 
                                    //                                    WHERE 
                                    //                                    ""DocEntry"" = '{item.DefaultVal}_{row[0].ToString().Replace("'", "''")}_{row[1].ToString().Replace("'", "''")}'")
                                    //                        .AsEnumerable().Select(x => x).ToList();

                                    //DELETE IF EXISTING
                                    if (checkexist.Count > 0)
                                    {
                                        //GET COLUMN NAME AND DATAS OF PRIMARY KEYS
                                        string primarykeysqryaddon = "";
                                        foreach (var key in HeaderPrimaryKey)
                                        {
                                            foreach (DataColumn keycol in HeaderPrimaryKey[HeaderPrimaryKey.IndexOf(key)].Table.Columns)
                                            {
                                                primarykeysqryaddon += $@"""{key[keycol]}"" = '{row[key[keycol].ToString()].ToString().Replace("'", "''")}' AND";
                                            }
                                        }

                                        ret = DataAccess.Execute(Creds.CredentialDetails.AddonDBVersion,
                                              Creds.CredentialDetails.AddonServerName,
                                              Creds.CredentialDetails.AddonDBuser,
                                              Creds.CredentialDetails.AddonDBPassword,
                                              Creds.CredentialDetails.AddonDBName, $@" DELETE FROM ""{item.DefaultVal}"" 
                                                                                    WHERE
                                                                                    {primarykeysqryaddon.Remove(primarykeysqryaddon.Length - 3, 3)}", row[0].ToString().Replace("'", "''"));

                                        if (!ret.Contains("Error"))
                                        {
                                            ret = DataAccess.Execute(Creds.CredentialDetails.SAPDBVersion,
                                            Creds.CredentialDetails.SAPServerName,
                                            Creds.CredentialDetails.SAPDBuser,
                                            Creds.CredentialDetails.SAPDBPassword,
                                            Creds.CredentialDetails.SAPDBName, $@" DELETE FROM ""SAOLIVE_LINKBOX"".""{Creds.CredentialDetails.SAPDBName}_{item.DefaultVal}"" WHERE ""DocEntry"" = '{item.DefaultVal}_{primarykeysqrysap.Remove(primarykeysqrysap.Length - 1, 1).Replace("'", "''")}'", row[0].ToString().Replace("'", "''"));

                                            if (ret.Contains("Error"))
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                                _context.SaveChanges();
                                            }
                                        }
                                        else
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                            _context.SaveChanges();
                                        }
                                    }

                                    //INSERT DATA
                                    qryheader = $@"INSERT INTO {item.DefaultVal} VALUES (";
                                    foreach (DataColumn col in HeaderTable[HeaderTable.IndexOf(row)].Table.Columns)
                                    {
                                        qryheader += string.IsNullOrEmpty(row[col].ToString()) ? "null," : $@"'{row[col].ToString().Replace("'", "''")}',";
                                    }
                                    qryheader = qryheader.Remove(qryheader.Length - 1, 1);
                                    qryheader += ");";

                                    ret = DataAccess.Execute(Creds.CredentialDetails.AddonDBVersion,
                                      Creds.CredentialDetails.AddonServerName,
                                      Creds.CredentialDetails.AddonDBuser,
                                      Creds.CredentialDetails.AddonDBPassword,
                                      Creds.CredentialDetails.AddonDBName, qrystart + qryheader + qryend, row[0].ToString().Replace("'", "''"));

                                    if (!ret.Contains("Error"))
                                    {
                                        qrydocentry = $@"INSERT INTO ""SAOLIVE_LINKBOX"".""{Creds.CredentialDetails.SAPDBName}_{item.DefaultVal}"" (""DocEntry"") VALUES (";
                                        qrydocentry += $@"'{item.DefaultVal}_{primarykeysqrysap.Remove(primarykeysqrysap.Length - 1, 1).Replace("'", "''")}'";
                                        qrydocentry += ");";

                                        ret = DataAccess.Execute(Creds.CredentialDetails.SAPDBVersion,
                                        Creds.CredentialDetails.SAPServerName,
                                        Creds.CredentialDetails.SAPDBuser,
                                        Creds.CredentialDetails.SAPDBPassword,
                                        Creds.CredentialDetails.SAPDBName, qrydocentry, row[0].ToString().Replace("'", "''"));

                                        if (ret.Contains("Error"))
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                            _context.SaveChanges();
                                        }
                                    }
                                    else if (!ret.Contains("PRIMARY KEY constraint") || !ret.Contains("duplicate key"))
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                        _context.SaveChanges();
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }

                        //ROW TABLE
                        string qryrows = "";
                        string primarykeyrow = "";
                        foreach (var item in model.RowFields)
                        {
                            //CHECK IF COLUMN NAME OF PRIMARY KEY IS THE SAME WITH ROW TABLE
                            List<DataRow> rowconnection = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                  Creds.CredentialDetails.SAPServerName,
                                  Creds.CredentialDetails.SAPDBuser,
                                  Creds.CredentialDetails.SAPDBPassword,
                                  Creds.CredentialDetails.SAPDBName, $@" SELECT TOP 1 COLUMN_NAME FROM ""CONSTRAINTS""
                                                            WHERE SCHEMA_NAME = '{Creds.CredentialDetails.SAPDBName}' 
                                                            AND TABLE_NAME = '{item.DefaultVal}' AND IS_PRIMARY_KEY = 'TRUE' ORDER BY POSITION;")
                                  .AsEnumerable().Select(x => x).ToList();

                            foreach (var row in rowconnection)
                            {
                                primarykeyrow = Creds.CredentialDetails.PrimaryKey == row[0].ToString() ? Creds.CredentialDetails.PrimaryKey : row[0].ToString();
                            }

                            qrydocentry = "";
                            List<DataRow> RowsTable = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                          Creds.CredentialDetails.SAPServerName,
                                          Creds.CredentialDetails.SAPDBuser,
                                          Creds.CredentialDetails.SAPDBPassword,
                                          Creds.CredentialDetails.SAPDBName, $@" SELECT Y.* FROM {item.DefaultVal} Y " +
                                          //(TableName != "OACT" ?
                                          (ColumnNameTable.Count > 0 ?
                                          $@"LEFT JOIN {Creds.CredentialDetails.Module} X ON Y.""{primarykeyrow}"" = X.""{Creds.CredentialDetails.PrimaryKey}""" +
                                          $@" WHERE {(Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? "IFNULL" : "ISNULL")}(X.""{updatedatecolumnname}"",X.""{createdatecolumnname}"") >= '{sync}';"
                                          : ";"))
                                          .AsEnumerable().Select(x => x).ToList();

                            if (RowsTable.Count > 0)
                            {
                                foreach (DataRow row in RowsTable)
                                {
                                    string primarykeysqrysap = "";
                                    //GET LIST OF COLUMNS WITH PRIMARY KEY
                                    List<DataRow> RowPrimaryKey = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                                                          Creds.CredentialDetails.SAPServerName,
                                                                          Creds.CredentialDetails.SAPDBuser,
                                                                          Creds.CredentialDetails.SAPDBPassword,
                                                                          Creds.CredentialDetails.SAPDBName, Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? $@"
                                                                                                    SELECT COLUMN_NAME FROM ""CONSTRAINTS"" 
                                                                                                WHERE SCHEMA_NAME = '{Creds.CredentialDetails.SAPDBName}' 
                                                                                                AND TABLE_NAME = '{item.DefaultVal}' AND IS_PRIMARY_KEY = 'TRUE';
                                                                                                "
                                                                                                                        :
                                                                                                                        $@"SELECT 
                                                                                                column_name as COLUMN_NAME
                                                                                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 

                                                                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                                                                                                ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                                                                                                AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                                                                                                AND KU.table_name='{item.DefaultVal}'

                                                                                            ORDER BY 
                                                                                                    KU.TABLE_NAME
                                                                                                ,KU.ORDINAL_POSITION
                                                                                            ; ")
                                                                          .AsEnumerable().Select(x => x).ToList();

                                    foreach (var key in RowPrimaryKey)
                                    {
                                        foreach (DataColumn keycol in RowPrimaryKey[RowPrimaryKey.IndexOf(key)].Table.Columns)
                                        {
                                            primarykeysqrysap += $@"{row[key[keycol].ToString()]}_";
                                        }
                                    }

                                    //CHECK IF EXISTING IN PROGRESS
                                    List<DataRow> checkexistrow = DataAccess.Select(Creds.CredentialDetails.SAPDBVersion,
                                          Creds.CredentialDetails.SAPServerName,
                                          Creds.CredentialDetails.SAPDBuser,
                                          Creds.CredentialDetails.SAPDBPassword,
                                          Creds.CredentialDetails.SAPDBName, $@" SELECT * FROM ""SAOLIVE_LINKBOX"".""{Creds.CredentialDetails.SAPDBName}_{TableName}"" WHERE ""DocEntry"" = '{item.DefaultVal}_{primarykeysqrysap.Remove(primarykeysqrysap.Length - 1, 1).Replace("'", "''")}'")
                                                            .AsEnumerable().Select(x => x).ToList();

                                    //DELETE IF EXISTING
                                    if (checkexistrow.Count > 0)
                                    {
                                        //GET COLUMN NAME AND DATAS OF PRIMARY KEYS
                                        string primarykeysqryaddon = "";
                                        foreach (var key in RowPrimaryKey)
                                        {
                                            foreach (DataColumn keycol in RowPrimaryKey[RowPrimaryKey.IndexOf(key)].Table.Columns)
                                            {
                                                primarykeysqryaddon += $@"""{key[keycol]}"" = '{row[key[keycol].ToString()].ToString().Replace("'", "''")}' AND";
                                            }
                                        }

                                        ret = DataAccess.Execute(Creds.CredentialDetails.AddonDBVersion,
                                              Creds.CredentialDetails.AddonServerName,
                                              Creds.CredentialDetails.AddonDBuser,
                                              Creds.CredentialDetails.AddonDBPassword,
                                              Creds.CredentialDetails.AddonDBName, $@" DELETE FROM ""{item.DefaultVal}"" 
                                                                                    WHERE
                                                                                    {primarykeysqryaddon.Remove(primarykeysqryaddon.Length - 3, 3)}", row[0].ToString().Replace("'", "''"));

                                        if (!ret.Contains("Error"))
                                        {
                                            ret = DataAccess.Execute(Creds.CredentialDetails.SAPDBVersion,
                                            Creds.CredentialDetails.SAPServerName,
                                            Creds.CredentialDetails.SAPDBuser,
                                            Creds.CredentialDetails.SAPDBPassword,
                                            Creds.CredentialDetails.SAPDBName, $@" DELETE FROM ""SAOLIVE_LINKBOX"".""{Creds.CredentialDetails.SAPDBName}_{TableName}"" WHERE ""DocEntry"" = '{item.DefaultVal}_{primarykeysqrysap.Remove(primarykeysqrysap.Length - 1, 1).Replace("'", "''")}'", row[0].ToString().Replace("'", "''"));

                                            if (ret.Contains("Error"))
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                                _context.SaveChanges();
                                            }
                                        }
                                        else
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                            _context.SaveChanges();
                                        }
                                    }

                                    //INSERT ROW DATA
                                    qryrows = $@"INSERT INTO {item.DefaultVal} VALUES (";
                                    foreach (DataColumn col in RowsTable[RowsTable.IndexOf(row)].Table.Columns)
                                    {
                                        qryrows += string.IsNullOrEmpty(row[col].ToString()) ? "null," : $@"'{row[col].ToString().Replace("'", "''")}',";
                                    }
                                    qryrows = qryrows.Remove(qryrows.Length - 1, 1);
                                    qryrows += ");";

                                    ret = DataAccess.Execute(Creds.CredentialDetails.AddonDBVersion,
                                          Creds.CredentialDetails.AddonServerName,
                                          Creds.CredentialDetails.AddonDBuser,
                                          Creds.CredentialDetails.AddonDBPassword,
                                          Creds.CredentialDetails.AddonDBName, qrystart + qryrows + qryend, row[0].ToString().Replace("'", "''"));

                                    if (!ret.Contains("Error"))
                                    {
                                        qrydocentry = $@"INSERT INTO ""SAOLIVE_LINKBOX"".""{Creds.CredentialDetails.SAPDBName}_{TableName}"" (""DocEntry"") VALUES (";
                                        qrydocentry += $@"'{item.DefaultVal}_{primarykeysqrysap.Remove(primarykeysqrysap.Length - 1, 1).Replace("'", "''")}'";
                                        qrydocentry += ");";

                                        ret = DataAccess.Execute(Creds.CredentialDetails.SAPDBVersion,
                                        Creds.CredentialDetails.SAPServerName,
                                        Creds.CredentialDetails.SAPDBuser,
                                        Creds.CredentialDetails.SAPDBPassword,
                                        Creds.CredentialDetails.SAPDBName, qrydocentry, row[0].ToString().Replace("'", "''"));

                                        if (ret.Contains("Error"))
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                            _context.SaveChanges();
                                        }
                                    }
                                    else if (!ret.Contains("PRIMARY KEY constraint") || !ret.Contains("duplicate key"))
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = ret, Database = Creds.CredentialDetails.SAPDBName, Table = item.DefaultVal, Module = TableName });
                                        _context.SaveChanges();
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }

                        ////UPDATE AUTOMATION TO START
                        //sched.ForceStop = false;
                        _context.SaveChanges();

                    }
                    else
                    {

                    }

                }

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_END", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                _context.SaveChanges();

                return model;
            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = $@"Error: {ex.Message}. Source: {ex.Source}. StackTrace: {ex.StackTrace}" });
                _context.SaveChanges();

                RevertLastSync(id, sync);

                return model;
            }



        }

        public PostingViewModel GetSAPPostUploadtoSAP(string[] modules)
        {
            var model = new PostingViewModel();
            string sync = "";
            int id = 0;
            string jsonchecker = "";
            try
            {
                Properties.Settings prop = new Properties.Settings();
                //Settings prop = new Settings();

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_START", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                _context.SaveChanges();

                foreach (string modulecreds in modules)
                {
                    var Creds = GetSAPCredentialsMapCode(modulecreds);
                    //string TableName = "";
                    //string ret = "";
                    if (Creds.CredentialDetails != null)
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_ITEM_CREDS", CreateDate = DateTime.Now, ApiUrl = $@"CREDETIALS  {Creds.CredentialDetails.SAPSldServer}/{Creds.CredentialDetails.SAPLicensePort}", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                        _context.SaveChanges();

                        id = Convert.ToInt32(Creds.CredentialDetails.MapId);
                        List<DataRow> ColumnNameTable = new List<DataRow>();

                        ////SET LAST SYNC DATE INTO CURRENT DATETIME
                        //var mapsync = _context.FieldMappings.Find(Convert.ToInt32(Creds.CredentialDetails.MapId));
                        //mapsync.LastSync = DateTime.Now.Date;
                        //_context.Entry(mapsync).State = System.Data.Entity.EntityState.Modified;
                        //_context.SaveChanges();

                        #region STOP_BPCATALOG_ENTERVENTION                        
                        //var _SchedProces = _context.Schedules
                        //  .Join(_context.Process, sch => sch.Process, pr => pr.ProcessCode, (sch, pr) => new { sch, pr })
                        //  .Join(_context.APISetups, apc => apc.pr.APICode, ap => ap.APICode, (apc, ap) => new { apc, ap })
                        //  .Where(sel => sel.ap.APIURL.Contains("sap/upload/documents"))
                        //  .Select(sel => sel.apc.sch.SchedId).FirstOrDefault();
                        //////FORCE STOP THE BP CATALOG AUTOMATION
                        //var sched = _context.Schedules.Where(sel => sel.SchedId == _SchedProces).FirstOrDefault();
                        //sched.ForceStop = true;
                        //_context.SaveChanges();
                        #endregion

                        model.HeaderFields = _context.Headers.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                        {
                            SAPFieldId = x.SourceFieldId,
                            AddonField = x.DestinationField,
                            IsRequired = x.IsKeyValue,
                            SourceType = x.SourceType,
                            TableName = x.SourceTableName,
                        }).Distinct().ToList() ?? new List<PostingViewModel.Fields>();
                        model.RowFields = _context.Rows.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                        {
                            SAPFieldId = x.SAPRowFieldId,
                            AddonField = x.AddonRowField,
                            SourceType = x.SourceType,
                            TableName = x.TableName,
                        }).Distinct().ToList() ?? new List<PostingViewModel.Fields>();
                    }
                    var headerfields = model.HeaderFields.AsEnumerable().Where(sel => sel.SourceType.ToLower() == "header").ToList();
                    var rowfields = model.HeaderFields.AsEnumerable().Where(sel => sel.SourceType.ToLower() == "row").ToList();

                    string module = "";
                    string rowname = "";
                    //if (Creds.CredentialDetails.Module.ToLower().Contains("item"))
                    //{ module = "Items"; }
                    //else if (Creds.CredentialDetails.Module.ToLower().Contains("incoming payments"))
                    //{ module = "IncomingPayments"; }
                    //else if (Creds.CredentialDetails.Module.ToLower().Contains("bp master"))
                    //{ module = "BusinessPartners"; rowname = "ContactEmployees"; }
                    //else if (Creds.CredentialDetails.Module.ToLower().Contains("bill of materials"))
                    //{ module = "ProductTrees"; }
                    //else if (Creds.CredentialDetails.Module.ToLower().Contains("unit of measurement"))
                    //{ module = "UnitOfMeasurements"; }
                    //else if (Creds.CredentialDetails.Module.ToLower().Contains("price list"))
                    //{ module = "PriceLists"; }
                    //else if (Creds.CredentialDetails.Module.ToLower().Contains("purchase order"))
                    //{ module = "PurchaseOrders"; rowname = "DocumentLines"; }
                    //else if (Creds.CredentialDetails.Module.ToLower().Contains("sales order"))
                    //{ module = "Orders?$filter=DocType eq 'dDocument_Items'"; rowname = "DocumentLines"; }
                    ////{ module = "Orders"; rowname = "DocumentLines"; }
                    //else
                    //{ module = "Document"; }
                    module = Creds.CredentialDetails.EntityName;

                    //get json data from source db (service layer)
                    List<string> postjsondata = new List<string>();

                    #region Source SAP DB
                    //Get the fields from JSON Service Layer
                    var auth = new AuthenticationCredViewModel
                    {
                        Method = "GET",
                        Action = module, //For META_DATA  VERSION 2
                        JsonString = "{}",
                        SAPSldServer = Creds.CredentialDetails.AddonIPAddress,
                        SAPServer = Creds.CredentialDetails.AddonIPAddress,
                        Port = Creds.CredentialDetails.AddonPort.ToString(),
                        SAPDatabase = Creds.CredentialDetails.AddonDBName,
                        SAPDBUserId = Creds.CredentialDetails.AddonDBuser,
                        SAPDBPassword = Creds.CredentialDetails.AddonDBPassword,
                        SAPUserID = Creds.CredentialDetails.AddonDBuser,
                        SAPPassword = Creds.CredentialDetails.AddonDBPassword
                    };

                    sapAces.SaveCredentials(auth);
                    bool blnLogin = SAPAccess.LoginAction();
                    if (blnLogin == true)
                    {
                        JObject obj = new JObject();
                        do
                        {
                            string sourceJson = sapAces.SendSLData(auth);
                            obj = JObject.Parse(sourceJson);
                            JArray valueArray = (JArray)obj["value"];

                            foreach (var jval in valueArray)
                            {
                                string sourcePostJson = "{";
                                //HEADER                                
                                foreach (var field in headerfields)
                                {
                                    sourcePostJson += $@"""{field.SAPFieldId}"": ""{jval[field.SAPFieldId].ToString()}"",";
                                }
                                //ROWS
                                if (rowfields.Count > 0)
                                {
                                    sourcePostJson += $@"""{rowname}"": [";
                                    foreach (var jrowval in jval[rowname])
                                    {
                                        jsonchecker = jrowval.ToString();
                                        sourcePostJson += "{";
                                        foreach (var rowfield in model.RowFields)
                                        {
                                            if (jrowval[rowfield.SAPFieldId] != null)
                                            {
                                                sourcePostJson += $@"""{rowfield.SAPFieldId}"": ""{jrowval[rowfield.SAPFieldId].ToString()}"",";
                                            }
                                        }
                                        sourcePostJson = sourcePostJson.Substring(0, sourcePostJson.Length - 1);
                                        sourcePostJson += "},";
                                    }
                                    sourcePostJson = sourcePostJson.Substring(0, sourcePostJson.Length - 1);
                                    sourcePostJson += "],";
                                }
                                sourcePostJson = sourcePostJson.Substring(0, sourcePostJson.Length - 1);
                                sourcePostJson += "}";
                                postjsondata.Add(sourcePostJson);
                            }
                            auth.Action = obj["odata.nextLink"] != null && !string.IsNullOrEmpty(obj["odata.nextLink"].ToString()) ? obj["odata.nextLink"].ToString().Replace("/b1s/v1/", "") : auth.Action;
                            auth.URL = "https";
                            sapAces.SaveCredentials(auth);
                        }
                        while (obj["odata.nextLink"] != null && !string.IsNullOrEmpty(obj["odata.nextLink"].ToString()));
                    }
                    else
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_LOGIN_EROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = "", Database = Creds.CredentialDetails.SAPDBName, Table = "", Module = Creds.CredentialDetails.Module });
                        _context.SaveChanges();
                        return model;
                    }
                    #endregion

                    #region Destination SAP DB
                    auth = new AuthenticationCredViewModel
                    {
                        Method = "POST",
                        Action = module, //For META_DATA  VERSION 2
                        JsonString = "{}",
                        SAPSldServer = Creds.CredentialDetails.SAPIPAddress,
                        SAPServer = Creds.CredentialDetails.SAPIPAddress,
                        Port = Creds.CredentialDetails.SAPLicensePort.ToString(),
                        SAPDatabase = Creds.CredentialDetails.SAPDBName,
                        SAPDBUserId = Creds.CredentialDetails.SAPUser,
                        SAPDBPassword = Creds.CredentialDetails.SAPPassword,
                        SAPUserID = Creds.CredentialDetails.SAPUser,
                        SAPPassword = Creds.CredentialDetails.SAPPassword,
                        URL = "https"
                    };
                    sapAces.SaveCredentials(auth);
                    blnLogin = SAPAccess.LoginAction();
                    if (blnLogin == true)
                    {
                        string sourceUniqueName = model.HeaderFields.Where(x => x.IsRequired == true).FirstOrDefault().SAPFieldId;

                        foreach (string json in postjsondata)
                        {
                            JObject obj = new JObject();
                            obj = JObject.Parse(json);
                            string UniqueId = obj[sourceUniqueName].ToString();
                            auth.Method = "GET";
                            int intres = 0; ;
                            string emekemerut = "";
                            if (int.TryParse(UniqueId, out intres))
                            {
                                emekemerut = UniqueId;
                            }
                            else
                            {
                                emekemerut = $@"'{UniqueId}'";
                            }
                            auth.Action = $@"{auth.Action}({emekemerut})";
                            auth.JsonString = "{}";
                            sapAces.SaveCredentials(auth);
                            string checkExists = sapAces.SendSLData(auth);
                            if (checkExists.Contains("error"))
                            {
                                if (sourceUniqueName == "DocEntry")
                                {
                                    obj.Value<JObject>().Remove(sourceUniqueName);
                                }
                                auth.Method = "POST";
                                auth.Action = module;
                                auth.JsonString = obj.ToString(); ;
                                sapAces.SaveCredentials(auth);
                                string postret = sapAces.SendSLData(auth);
                                if (postret != null ? postret.Contains("error") : false)
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = $@"", Json = json, ErrorMsg = postret, Database = Creds.CredentialDetails.AddonDBName, Module = Creds.CredentialDetails.Module });
                                    _context.SaveChanges();
                                }
                            }
                            else
                            {
                                obj.Value<JObject>().Remove(sourceUniqueName);
                                if (module == "PurchaseOrders")
                                {
                                    obj.Value<JObject>().Remove("CardCode");
                                    obj.Value<JObject>().Remove("CardName");
                                    //obj.Value<JObject>().Remove("DocCurrency");
                                }
                                auth.Method = "PATCH";
                                auth.JsonString = obj.ToString();
                                sapAces.SaveCredentials(auth);
                                string postret = sapAces.SendSLData(auth);
                                if (postret.Contains("error"))
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = $@"", Json = json, ErrorMsg = postret, Database = Creds.CredentialDetails.AddonDBName, Module = Creds.CredentialDetails.Module });
                                    _context.SaveChanges();
                                }
                                auth.Action = module;
                            }


                        }
                    }
                    else
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_LOGIN_EROR", CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = "", Database = Creds.CredentialDetails.AddonDBName, Table = "", Module = Creds.CredentialDetails.Module });
                        _context.SaveChanges();
                        return model;
                    }
                    #endregion
                }

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_END", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                _context.SaveChanges();

                return model;
            }
            catch (Exception ex)
            {
                string eme = jsonchecker;
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = $@"Error: {ex.Message}. Source: {ex.Source}. StackTrace: {ex.StackTrace}" });
                _context.SaveChanges();

                RevertLastSync(id, sync);

                return model;
            }



        }

        public PostingViewModel ZendeskPostTicket(string taskname)
        {
            int errcode = 100;
            var model = new PostingViewModel();
            try
            {
                errcode = 120;
                var Creds = GetAPICredentialSkipRow(taskname);
                if (Creds.CredentialDetails != null)
                {
                    model.HeaderFields = _context.Headers.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => new PostingViewModel.Fields
                    {
                        SAPFieldId = x.SourceFieldId,
                        AddonField = x.DestinationField
                    }).ToList() ?? new List<PostingViewModel.Fields>();

                    errcode = 130;
                    model.APIView = _context.FieldMappings.Where(x => x.MapId == Creds.CredentialDetails.MapId)
                                                .Join(_context.APISetups, f => f.APICode, a => a.APICode, (f, a) => new { f, a })
                                                .Select(x => new PostingViewModel.APIViewModel
                                                {
                                                    APIId = x.a.APIId,
                                                    APICode = x.a.APICode,
                                                    APIMethod = x.a.APIMethod,
                                                    APIURL = x.a.APIURL,
                                                    APIKey = x.a.APIKey,
                                                    APISecretKey = x.a.APISecretKey,
                                                    APIToken = x.a.APIToken,
                                                    APILoginUrl = x.a.APILoginUrl,
                                                    APILoginBody = x.a.APILoginBody,
                                                }).ToList();
                    var apimethod = model.APIView.Select(x => x.APIMethod).FirstOrDefault();
                    var apiurl = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                    var apiuser = model.APIView.Select(x => x.APIKey).FirstOrDefault();
                    var apipwd = model.APIView.Select(x => x.APISecretKey).FirstOrDefault();
                    var localpath = _context.FieldMappings.Where(x => x.MapId == Creds.CredentialDetails.MapId)
                                                .Join(_context.PathSetup, p => p.PathCode, x => x.PathCode, (p, x) => new { p, x })
                                                .Select(a => a.x.LocalPath).FirstOrDefault();
                    apiurl = apiurl.Replace("tickets", "imports/tickets"); //THIS URL MUST HAVE A SEPARATE POST URL, HARDCODE TEMPORARY

                    ////Use the File on mapping
                    var filename = _context.FieldMappings.Where(x => x.MapId == Creds.CredentialDetails.MapId).Select(x => x.FileName).FirstOrDefault();
                    //string localpath = AppDomain.CurrentDomain.BaseDirectory;
                    //var filepath = Path.Combine(localpath, $@"FileUpload\{filename}");                    
                    errcode = 140;
                    var di = new DirectoryInfo(localpath);
                    FileInfo[] fi = di.GetFiles($"*.xlsx");
                    foreach (var x in fi)
                    {
                        string filepath = x.FullName;
                        if (File.Exists(filepath))
                        {
                            errcode = 160;
                            ////Add column Status and Ticket Id
                            string readXcel = ExcelAccessV2.AddSheetColumnV2(filepath);
                            if (string.IsNullOrEmpty(readXcel))
                            {
                                bool err = false;
                                errcode = 180;
                                DataTable dt = ExcelAccessV2.Select(filepath, "SELECT * FROM [Sheet1$]");
                                int cnt = 2; //Count start at Row #2
                                foreach (DataRow rows in dt.Rows)
                                {
                                    if (rows["UploadStatus"].ToString() != "success" && !string.IsNullOrEmpty(rows[2].ToString()))
                                    {
                                        errcode = 200;
                                        var posjson = PostingStringBuilderHelpers.BuildJsonZDPostTicket(rows, null, model.HeaderFields, model.RowFields);
                                        var result = sapAces.APIResponse("POST", apiurl, "", posjson, "", apiuser, apipwd, 0, null); //Zero non limit

                                        if (result != null)
                                        {
                                            if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"""error"" :") || result.ToLower().Contains("error:"))
                                            {
                                                err = true;
                                                errcode = 210;
                                                ExcelAccessV2.UpdateSheetRowV2(filepath, cnt, "error", "");
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Zendesk", CreateDate = DateTime.Now, ApiUrl = apiurl, Json = posjson, ErrorMsg = result });
                                                _context.SaveChanges();
                                            }
                                            else
                                            {
                                                errcode = 220;
                                                if (PostingHelpers.IsValidJson(result))
                                                {
                                                    errcode = 240;
                                                    JObject collectjson = JObject.Parse(result);
                                                    var _ticketid = collectjson["ticket"]["id"];
                                                    ExcelAccessV2.UpdateSheetRowV2(filepath, cnt, "success", _ticketid.ToString());
                                                }

                                            }
                                        }
                                        else
                                        {
                                            errcode = 260;
                                            if (PostingHelpers.IsValidJson(result))
                                            {
                                                errcode = 280;
                                                JObject collectjson = JObject.Parse(result);
                                                var _ticketid = collectjson["ticket"]["id"];
                                                ExcelAccessV2.UpdateSheetRowV2(filepath, cnt, "success", _ticketid.ToString());
                                            }
                                        }
                                    }
                                    cnt++;
                                }

                                if (err == false)
                                {
                                    string BackupPath = localpath + $@"\Backup";
                                    errcode = 300;
                                    if (!Directory.Exists(BackupPath))
                                    {
                                        Directory.CreateDirectory(BackupPath);
                                    }
                                    errcode = 320;
                                    File.Move(filepath, $@"{BackupPath}\{x.Name}");
                                }
                            }
                            else
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = "Uploading Zendesk Tickets"
                                                            ,
                                    CreateDate = DateTime.Now
                                                            ,
                                    ApiUrl = "",
                                    Json = "",
                                    ErrorMsg = errcode + $@" {readXcel}"
                                });
                                _context.SaveChanges();
                            }
                        }
                    }
                }
                return model;
            }
            catch (Exception ex)
            {
                string errCode = errcode == 0 ? ex.HResult.ToString() : errcode.ToString();
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "Uploading Zendesk Tickets"
                                            ,
                    CreateDate = DateTime.Now
                                            ,
                    ApiUrl = "",
                    Json = "",
                    ErrorMsg = errCode + $@" {ex.Message}"
                });
                _context.SaveChanges();
                return null;
            }
        }


        public string PostJArraySO(string jsonArr)
        {
            string ret = "";
            List<string> err = new List<string>();
            List<string> succ = new List<string>();
            PostingViewModel creds = new PostingViewModel();

            var sapCode = _context.APIManager.Join(_context.APISetups, x => x.APICode, y => y.APICode, (x, y) => new { x, y })
                .Where(xy => xy.y.APIURL.ToLower().Contains("linkbox/post/apitoken/so".ToLower())).Select(a => a.x.SAPCode).FirstOrDefault();

            if (string.IsNullOrEmpty(sapCode))
                ret = "error: No available setup.";

            creds.CredentialDetails = _context.SAPSetup.Where(x => x.SAPCode == sapCode).Select(x => new PostingViewModel.Credential
            {
                SAPDBVersion = x.SAPDBVersion,
                SAPLicensePort = x.SAPLicensePort,
                SAPServerName = x.SAPServerName,
                SAPIPAddress = x.SAPIPAddress,
                SAPDBName = x.SAPDBName,
                SAPDBPort = x.SAPDBPort,
                SAPDBuser = x.SAPDBuser,
                SAPDBPassword = x.SAPDBPassword,
                SAPUser = x.SAPUser,
                SAPPassword = x.SAPPassword
            }).FirstOrDefault();

            var datableSO = JsonConvert.DeserializeObject<DataSet>(jsonArr);
            foreach (DataTable so in datableSO.Tables)
            {
                foreach (DataRow rData in so.Rows)
                {

                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in rData.Table.Columns)
                    {
                        dict.Add(col.ColumnName, rData[col]);
                    }
                    var sTable = JsonConvert.SerializeObject(dict);
                    var sJson = JsonConvert.DeserializeObject(sTable);

                    var json = JObject.FromObject(sJson);

                    if (PostingHelpers.LoginAction(creds))
                    {
                        var result = PostingHelpers.SBOResponse("POST", $@"Orders", json.ToString(), "DocEntry", creds);

                        if (result.ToLower().Contains("error"))
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = "POST linkbox/post/api/so",
                                CreateDate = DateTime.Now,
                                ApiUrl = $@"POST {creds.CredentialDetails.SAPIPAddress}/{creds.CredentialDetails.SAPLicensePort}/b1s/v1/Orders",
                                Json = json.ToString(),
                                ErrorMsg = result.ToString()
                            });
                            _context.SaveChanges();
                            err.Add(result);
                        }
                        else
                        {
                            succ.Add(result);
                        }

                    }
                    else
                    {
                        return ret = "Can't connect to Server";
                    }
                }

            }

            return (ret = $"Number of Document Posted: {succ.Count()} & " +
                $"Number of Document Error: {err.Count}");
        }

        public string PostJObjectSO(string jsonObj)
        {
            string ret = "success";
            PostingViewModel creds = new PostingViewModel();
            //var sapCode = _context.APIManager.Where(x => x.APICode == "SO_APIPOST").Select(x => x.SAPCode).FirstOrDefault();
            //var model = new SetupCreateViewModel.APIQuerySetup();

            var sapCode = _context.APIManager.Join(_context.APISetups, x => x.APICode, y => y.APICode, (x, y) => new { x, y })
                .Where(xy => xy.y.APIURL.ToLower().Contains("linkbox/post/api/so")).Select(a => a.x.SAPCode).FirstOrDefault();

            if (string.IsNullOrEmpty(sapCode))
                ret = "error: No available setup.";

            creds.CredentialDetails = _context.SAPSetup.Where(x => x.SAPCode == sapCode).Select(x => new PostingViewModel.Credential
            {
                SAPDBVersion = x.SAPDBVersion,
                SAPLicensePort = x.SAPLicensePort,
                SAPServerName = x.SAPServerName,
                SAPIPAddress = x.SAPIPAddress,
                SAPDBName = x.SAPDBName,
                SAPDBPort = x.SAPDBPort,
                SAPDBuser = x.SAPDBuser,
                SAPDBPassword = x.SAPDBPassword,
                SAPUser = x.SAPUser,
                SAPPassword = x.SAPPassword
            }).FirstOrDefault();

            if (PostingHelpers.LoginAction(creds))
            {
                var result = PostingHelpers.SBOResponse("POST", $@"Orders", jsonObj, "DocEntry", creds);

                if (result.ToLower().Contains("error"))
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                    {
                        Task = "POST linkbox/post/api/so",
                        CreateDate = DateTime.Now,
                        ApiUrl = $@"POST {creds.CredentialDetails.SAPIPAddress}/{creds.CredentialDetails.SAPLicensePort}/b1s/v1/Orders",
                        Json = jsonObj.ToString(),
                        ErrorMsg = result.ToString()
                    });
                    _context.SaveChanges();

                    ret = result;
                }
                else
                {
                    ret = $"Document posted successfully: {result}";
                }

            }
            else
            {
                ret = "Can't connect to Server";
            }
            return ret;
        }

        public string PostSapCatalog(string taskname)
        {
            long errcode = 0;
            string ret = ""; ///UNUSED VARIABLE
			Properties.Settings prop = new Properties.Settings();
            //Settings prop = new Settings();
            var sched = _context.Schedules.Where(sel => sel.SchedCode == taskname).FirstOrDefault();
            var rundatetime = _context.Schedules.Where(sel => sel.SchedCode == taskname).Select(sel => sel.RunTime).FirstOrDefault();
            var _d = DateTime.Now - (rundatetime == null ? DateTime.Now.AddMinutes(-4) : rundatetime);
            if (_d.Value.Minutes > 2) ////TIME CHECK IF TASK RUNS LONGER (2) TWO MINUTES
            {
                sched.IsRunning = false;
                _context.SaveChanges();
            }
            var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == taskname).Select(sel => sel.IsRunning).FirstOrDefault();

            if (isrunproc == false)
            {
                try
                {
                    errcode = 1100;
                    var Creds = GetCredentials_N_A(taskname);
                    if (Creds.CredentialDetails != null)
                    {
                        errcode = 1200;
                        sched.IsRunning = true;
                        sched.RunTime = DateTime.Now;
                        _context.SaveChanges();

                        ////CREATE UNLI LOOP SINCE THE TASK SCHED ONLY RUNS EVERY 1 MINUTE
                        for (int lop = 0; lop < 2; lop++)
                        {
                            errcode = 1300;

                            if (_context.Schedules.Where(sel => sel.SchedCode == taskname && sel.ForceStop == false).Any())
                            {
                                ////QUERY GET ITEM UoM
                                var dr = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                                    Creds.CredentialDetails.SAPServerName,
                                    Creds.CredentialDetails.SAPDBuser,
                                    Creds.CredentialDetails.SAPDBPassword,
                                    Creds.CredentialDetails.SAPDBName), QueryAccess.GetUoMVariants("hana")).AsEnumerable().ToList()
                                :
                                DataAccess.Select(QueryAccess.MSSQL_conString(
                                    Creds.CredentialDetails.SAPServerName,
                                    Creds.CredentialDetails.SAPDBuser,
                                    Creds.CredentialDetails.SAPDBPassword,
                                    Creds.CredentialDetails.SAPDBName), QueryAccess.GetUoMVariants("mssql")).AsEnumerable().ToList();
                                //traceObj = dr;

                                dr.ForEach(fe =>
                                {
                                    if (_context.Schedules.Where(sel => sel.SchedCode == taskname && sel.ForceStop == true).Any())
                                    { return; }
                                    var _itemcode = fe["itemcode"].ToString();
                                    var _bpcatalog = fe["Bpcatalog"].ToString();
                                    var _substitute = (fe["ItemType"].ToString() == "Variants" ? fe["spUoMCode"].ToString() : fe["Parent"].ToString());

                                    errcode = 1400;
                                    var _excludetemp = _context.VariantTemp.Where(sel => sel.ItemCode == _itemcode && sel.CardCode == _bpcatalog && sel.Substitute == _substitute).Any();
                                    if (_excludetemp == false)
                                    {
                                        ////Parent Item does not have ParentCode for easy to find 
                                        errcode = 1500;
                                        string sapjson = (fe["ItemType"].ToString() == "Variants" ?
                                                                            PostingStringBuilderHelpers.BuildJsonSAPCatalog(fe["itemcode"].ToString(), fe["Bpcatalog"].ToString(), fe["spUoMCode"].ToString(), fe["itemcode"].ToString(), "", fe["spUomCode"].ToString(), fe["spUoMName"].ToString(), fe["CpnNo"].ToString())
                                                                        : PostingStringBuilderHelpers.BuildJsonSAPCatalog(fe["itemcode"].ToString(), fe["Bpcatalog"].ToString(), fe["Parent"].ToString(), "", "", "", "", fe["CpnNo"].ToString()));

                                        errcode = 1600;
                                        ////POST SAP AS INITIAL ITEM BP CATALOG
                                        var result = phelper.Posting4Automation("POST", $@"AlternateCatNum", sapjson, "", Creds);
                                        if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_BPCATALOG", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = sapjson, ErrorMsg = $"{errcode} " + result.ToString() });
                                            _context.SaveChanges();
                                        }
                                    }
                                    ////LOG RUNNING TASK
                                    sched.RunTime = DateTime.Now;
                                    _context.SaveChanges();
                                });
                            }

                            lop = 0;
                            System.Threading.Thread.Sleep(2000); ////CREATE DELAY FOR 2 SECS
                        }

                        //Return to Zero as ready status
                        var upsched = _context.Schedules.Where(sel => sel.SchedCode == taskname).FirstOrDefault();
                        upsched.IsRunning = false;
                        _context.SaveChanges();

                    }
                }
                catch (Exception ex)
                {
                    string er = ex.Message;
                    //Return to Zero as ready status
                    var upsched = _context.Schedules.Where(sel => sel.SchedCode == taskname).FirstOrDefault();
                    upsched.IsRunning = false;
                    _context.SaveChanges();

                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SAP_BPCATALOG", CreateDate = DateTime.Now, ApiUrl = $@"POST AlternateCatNum", Json = "AUTOPOST BPCATALOG", ErrorMsg = $"{errcode} " + ex.Message });
                    _context.SaveChanges();
                }
            }

            return ret;
        }
        public string PostAPIInventory(string taskname)
        {
            string ret = ""; ///UNUSED VARIABLE
			Properties.Settings prop = new Properties.Settings();
            //Settings prop = new Settings();
            var sched = _context.Schedules.Where(sel => sel.SchedCode == taskname).FirstOrDefault();
            var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == taskname).Select(sel => sel.IsRunning).FirstOrDefault();

            try
            {
                var Creds = GetCredentials_N_A(taskname);
                if (Creds.CredentialDetails != null)
                {

                    ////QUERY GET ITEM UoM
                    var dr = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                            Creds.CredentialDetails.SAPServerName,
                            Creds.CredentialDetails.SAPDBuser,
                            Creds.CredentialDetails.SAPDBPassword,
                            Creds.CredentialDetails.SAPDBName), QueryAccess.GetInventoryVariant("hana")).AsEnumerable().ToList()
                        :
                        DataAccess.Select(QueryAccess.MSSQL_conString(
                            Creds.CredentialDetails.SAPServerName,
                            Creds.CredentialDetails.SAPDBuser,
                            Creds.CredentialDetails.SAPDBPassword,
                            Creds.CredentialDetails.SAPDBName), QueryAccess.GetInventoryVariant("mssql")).AsEnumerable().ToList();
                    ////POST SAP AS INITIAL ITEM BP CATALOG                        
                    dr.ForEach(fe =>
                    {
                        if (!string.IsNullOrEmpty(fe["LocationId"].ToString()))
                        {
                            string jsonbody = "{" + $@"""location_id"":{fe["LocationId"].ToString()},""inventory_item_id"":{fe["InventoryId"].ToString()},""available"":{fe["Available"].ToString()}" + "}";
                            string result = sapAces.APIResponse("POST", Creds.CredentialDetails.PostUrl, "", jsonbody, "", Creds.CredentialDetails.PostUrlUser, Creds.CredentialDetails.PostUrlPwd, 0, null); //Zero non limit
                            if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.PostUrl}", Json = jsonbody, ErrorMsg = result.ToString() });
                                _context.SaveChanges();
                            }
                        }
                    });

                }
            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Shopify", CreateDate = DateTime.Now, ApiUrl = $@"Reading Data from Inventory", Json = "", ErrorMsg = ex.Message });
                _context.SaveChanges();
            }

            return ret;
        }


        public string MasterDataUploadSAPtoAPI(string taskName)
        {
            string ret = "";
            var model = new PostingViewModel();
            var Creds = GetAPICredentials(taskName);
            if (Creds.CredentialDetails != null)
            {
                if (PostingHelpers.LoginAction(Creds))
                {
                    model.HeaderFields = _context.Schedules.Join(_context.Process, a => a.Process, b => b.ProcessCode, (a, b) => new { a, b })
                        .Join(_context.Headers, c => c.b.MapId, d => d.MapId, (d, c) => new { d, c }).Where(z => z.d.a.SchedCode == taskName)
                        .Select(x => new PostingViewModel.Fields
                        {
                            SAPFieldId = x.c.SourceFieldId,
                            AddonField = x.c.DestinationField
                        }).ToList();

                    model.APIView = _context.Schedules.Join(_context.Process, a => a.Process, b => b.ProcessCode, (a, b) => new { a, b }).
                        Join(_context.Headers, c => c.b.MapId, d => d.MapId, (d, c) => new { d, c }).
                        Join(_context.FieldMappings, e => e.d.b.MapId, f => f.MapId, (f, e) => new { f, e }).
                        Join(_context.APISetups, g => g.e.APICode, h => h.APICode, (g, h) => new { g, h }).
                        Where(z => z.g.f.d.a.SchedCode == taskName).
                        Select(x => new PostingViewModel.APIViewModel
                        {
                            APIId = x.h.APIId,
                            APICode = x.h.APICode,
                            APIMethod = x.h.APIMethod,
                            APIURL = x.h.APIURL,
                            APIKey = x.h.APIKey,
                            APISecretKey = x.h.APISecretKey,
                            APIToken = x.h.APIToken,
                            APILoginUrl = x.h.APILoginUrl,
                            APILoginBody = x.h.APILoginBody,
                        }).ToList();



                    var result = PostingHelpers.SBOResponse("GET", $@"BusinessPartners('C0000001')", "", "", Creds);
                    //var result = PostingHelpers.SBOResponse("GET", $@"BusinessPartners?$orderby=CardCode&$top=10&$skip=1", "", "", Creds);
                    if (!result.Contains("error"))
                    {
                        var dSet = new DataSet();
                        JObject jObj = JObject.Parse(result);
                        var tokens = jObj["value"];
                        //var tokens = jObj.SelectTokens("value");
                        if (tokens != null)
                        {
                            foreach (var jtoken in tokens)
                            {
                                var abody = new JObject();
                                var toJObj = JObject.Parse(jtoken.ToString());
                                foreach (var hField in model.HeaderFields)
                                {
                                    JProperty col = toJObj.Properties().Where(x => x.Name == hField.SAPFieldId).First();
                                    abody.Add(hField.AddonField, col.Value);
                                }

                                if (abody != null)
                                {
                                    var origUrl = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                                    var authToken = _context.APISetups.Where(x => x.APIURL.ToLower().EndsWith("/token")).FirstOrDefault();
                                    string token = APITokenAuthorizer.GetToken(authToken.APIMethod, authToken.APIURL, authToken.APIKey, authToken.APISecretKey);
                                    //string output = APITokenAuthorizer.HTTPWebRequest("POST", origUrl, abody.ToString(), authToken.APIKey, authToken.APISecretKey, token, "D365CRM");
                                    //if (output.Contains("error") || output.ToLower().Contains("unauthorized"))
                                    //{
                                    //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = taskName, CreateDate = DateTime.Now, ApiUrl = origUrl, Json = abody.ToString(), ErrorMsg = output.ToString() });
                                    //    _context.SaveChanges();
                                    //}
                                }
                            }

                        }
                        else
                        {
                            var abody = new JObject();
                            foreach (var hField in model.HeaderFields)
                            {

                                DataTable dTable = JsonConvert.DeserializeObject<DataTable>(jObj.ToString());

                                JProperty col = jObj.Properties().Where(x => x.Name == hField.SAPFieldId).First();
                                abody.Add(hField.AddonField, col.Value);
                            }

                            if (abody != null)
                            {
                                var origUrl = model.APIView.Select(x => x.APIURL).FirstOrDefault();
                                var authToken = _context.APISetups.Where(x => x.APIURL.ToLower().EndsWith("/token")).FirstOrDefault();
                                string token = APITokenAuthorizer.GetToken(authToken.APIMethod, authToken.APIURL, authToken.APIKey, authToken.APISecretKey);
                                string output = APITokenAuthorizer.HTTPWebRequest("POST", origUrl, abody.ToString(), authToken.APIKey, authToken.APISecretKey, token, "D365CRM");
                                if (output.Contains("error") || output.ToLower().Contains("unauthorized"))
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = taskName, CreateDate = DateTime.Now, ApiUrl = origUrl, Json = abody.ToString(), ErrorMsg = output.ToString() });
                                    _context.SaveChanges();
                                }
                            }
                        }

                        //var srcArray = jObj.Descendants().Where(d => d is JArray).First();
                        //if (srcArray.Type == JTokenType.Array)
                        //{
                        //    var trgArray = new JArray();
                        //    foreach (JObject row in srcArray.Children<JObject>())
                        //    {

                        //        //JProperty col = row.Properties().Where(x => x.Name == hField.SAPFieldId).First();
                        //        //abody.Add(hField.AddonField, col.Value);

                        //        var cleanRow = new JObject();
                        //        foreach (JProperty column in row.Properties())
                        //        {
                        //            //Only include JValue types
                        //            JToken properties = column.Value;
                        //            if (properties.Type == JTokenType.Array && column.Value.ToString() != "[]")
                        //            {
                        //                var res = setDataTable(column, properties);
                        //                if (res != null)
                        //                {
                        //                    dSet.Tables.Add(res);
                        //                }
                        //            }

                        //            if (column.Value is JValue)
                        //            {
                        //                cleanRow.Add(column.Name, column.Value);
                        //            }

                        //        }
                        //        trgArray.Add(cleanRow);

                        //    }
                        //}

                        //var BusinessPartner = JsonConvert.DeserializeObject<DataTable>(trgArray.ToString());
                    }


                }
                else
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = taskName, CreateDate = DateTime.Now, ApiUrl = "", ErrorMsg = "Invalid SBO login Credential" });
                    _context.SaveChanges();
                }
            }

            return ret;
        }

        public DataTable setDataTable(JProperty jProperty, JToken jValue)
        {
            DataTable dTable = new DataTable();
            JArray jArr = new JArray();
            foreach (JObject row in jValue.Children<JObject>())
            {
                var cleanRow = new JObject();
                foreach (JProperty column in row.Properties())
                {
                    // Only include JValue types
                    if (column.Value is JValue)
                    {
                        cleanRow.Add(column.Name, column.Value);
                    }
                }
                jArr.Add(cleanRow);
            }

            dTable = JsonConvert.DeserializeObject<DataTable>(jArr.ToString());

            return dTable;
        }

        public byte[] DataExport()
        {

            var model = new DashboardViewModel();

            DataTable Tables = new DataTable();
            DataTable AddonTables = new DataTable();
            DataTable ReportTable = new DataTable();

            string qry = "";

            try
            {
                model.DashboardFieldMappingList = _context.FieldMappings.Join(_context.SAPSetup,
                                            x => x.SAPCode,
                                            y => y.SAPCode,
                                            (x, y) => new { x, y })
                                    .Join(_context.AddonSetup,
                                            a => a.x.AddonCode,
                                            b => b.AddonCode,
                                            (b, a) => new { b, a })
                                    .Select((z) => new DashboardViewModel.DashboardFieldMapping
                                    {
                                        MapCode = z.b.x.MapCode,
                                        MapName = z.b.x.MapName,
                                        ModuleName = z.b.x.ModuleName,
                                        AddonCode = z.b.x.AddonCode,
                                        AddonDBVersion = z.a.AddonDBVersion,
                                        AddonServerName = z.a.AddonServerName,
                                        AddonDBuser = z.a.AddonDBuser,
                                        AddonDBPassword = z.a.AddonDBPassword,
                                        AddonDBName = z.a.AddonDBName,
                                        SAPDBVersion = z.b.y.SAPDBVersion,
                                        SAPServerName = z.b.y.SAPServerName,
                                        SAPDBuser = z.b.y.SAPDBuser,
                                        SAPDBPassword = z.b.y.SAPDBPassword,
                                        SAPDBName = z.b.y.SAPDBName
                                    }).OrderBy(x => x.AddonDBName).ThenBy(x => x.ModuleName).ToList();

                model.DashboardReportList = new List<DashboardViewModel.DashboardReport>();

                foreach (var item in model.DashboardFieldMappingList)
                {
                    #region GetTables
                    Tables = DataAccess.Select(item.SAPDBVersion,
                                                item.SAPServerName,
                                                item.SAPDBuser,
                                                item.SAPDBPassword,
                                                item.SAPDBName, item.SAPDBVersion.Contains("HANA") ? $@"SELECT DISTINCT TABLE_NAME, SCHEMA_NAME,'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT""
                                                            FROM SYS.COLUMNS
                                                            WHERE SCHEMA_NAME = '{item.SAPDBName}'  AND  TABLE_NAME LIKE '{item.ModuleName.Substring(1, item.ModuleName.Length - 1)}%'
                                                            ORDER BY TABLE_NAME"
                                                            :
                                                            $@"SELECT DISTINCT TABLE_NAME,'' as ""SCHEMA_NAME"",'' as ""SAP_DATA_COUNT"", '' as ""ADDON_DATA_COUNT"" FROM INFORMATION_SCHEMA.COLUMNS  
                                                            WHERE TABLE_NAME LIKE '{item.ModuleName.Substring(1, item.ModuleName.Length - 1)}%'
                                                            ORDER BY TABLE_NAME");
                    #endregion

                    #region GetSAPDataCount
                    foreach (DataRow row in Tables.Rows)
                    {
                        qry += item.SAPDBVersion.Contains("HANA") ? $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{item.SAPDBName}"".""{row["TABLE_NAME"].ToString()}"""
                                                                  :
                                                                    $@"union all select COUNT(*) as ""SAP_DATA_COUNT"" from ""{row["TABLE_NAME"].ToString()}""";
                    }
                    ReportTable = DataAccess.Select(item.SAPDBVersion,
                                                    item.SAPServerName,
                                                    item.SAPDBuser,
                                                    item.SAPDBPassword,
                                                    item.SAPDBName, item.SAPDBVersion.Contains("HANA") ? $@"
                                                                SELECT DISTINCT TABLE_NAME
                                                                , SCHEMA_NAME
                                                                ,	(SELECT SUM(""SAP_DATA_COUNT"")
	                                                                FROM 
	                                                                (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.SAPDBName}"".""{item.ModuleName}""
	                                                                {qry})) as ""SAP_DATA_COUNT"" 
	                                                            ,'0' as ""ADDON_DATA_COUNT""
                                                                FROM SYS.COLUMNS
                                                                WHERE SCHEMA_NAME = '{item.SAPDBName}'  
                                                                AND  (TABLE_NAME = '{item.ModuleName}')
                                                                "
                                                                :
                                                                $@"  
                                                                SELECT DISTINCT TABLE_NAME
                                                                    ,'{item.SAPDBName}' as ""SCHEMA_NAME""
                                                                    ,(SELECT SUM(""SAP_DATA_COUNT"")
	                                                                FROM 
	                                                                (SELECT COUNT(*) as ""SAP_DATA_COUNT"" FROM ""{item.ModuleName}""
	                                                                {qry})  as ""SAP_DATA_COUNT"" )
	                                                                ,'0' as ""ADDON_DATA_COUNT"" ) as ""ADDON_DATA_COUNT""
                                                                    FROM INFORMATION_SCHEMA.COLUMNS  
                                                                WHERE TABLE_NAME = '{item.ModuleName}'
                                                                ");

                    #endregion

                    #region GetAddonDataCountAndIssues
                    qry = "";

                    foreach (DataRow row in ReportTable.Rows)
                    {
                        DataRow DataCount = DataAccess.Select(item.SAPDBVersion,
                                                      item.SAPServerName,
                                                      item.SAPDBuser,
                                                      item.SAPDBPassword,
                                                      item.SAPDBName, item.SAPDBVersion.Contains("HANA") ? $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM ""SAOLIVE_LINKBOX"".""{item.SAPDBName}_{item.ModuleName}"""
                                                      :
                                                      $@"SELECT COUNT(*) as ""ADDON_DATA_COUNT"" FROM SAOLIVE_LINKBOX.dbo.{item.SAPDBName}_{item.ModuleName}").AsEnumerable().Select(x => x).FirstOrDefault();
                        row["ADDON_DATA_COUNT"] = DataCount["ADDON_DATA_COUNT"];

                    }
                    #endregion

                    ////GetIssues
                    //int IssueCount = _context.SystemLogs.Where(x => x.Task == "UPLOAD_ERROR").Count();

                    DashboardViewModel.DashboardReport dashboardReport = new DashboardViewModel.DashboardReport();
                    foreach (DataRow x in ReportTable.Rows)
                    {
                        string db = x["SCHEMA_NAME"].ToString();
                        string table = x["TABLE_NAME"].ToString();
                        dashboardReport.DBName = x["SCHEMA_NAME"].ToString();
                        dashboardReport.Module = x["TABLE_NAME"].ToString();
                        dashboardReport.SAPDataNo = Convert.ToInt32(x["SAP_DATA_COUNT"]);
                        dashboardReport.AddonDataNo = Convert.ToInt32(x["ADDON_DATA_COUNT"]);
                        dashboardReport.IssueNo = _context.SystemLogs.Where(y => y.Task == "UPLOAD_ERROR" && y.Database == db && y.Module == table && y.CreateDate >= DateTime.Today).Count();
                        //dashboardReport.IssueNo = _context.SystemLogs.Where(y => y.Task == "UPLOAD_ERROR" && y.Database == db && y.Module == table).Count();
                    }

                    model.DashboardReportList.Add(dashboardReport);

                }

                DataTable dt = new DataTable("Sheet1");
                dt.Columns.AddRange(new DataColumn[6] { new DataColumn("Database Name"),
                                            new DataColumn("Module"),
                                            new DataColumn("No of Data - SAP"),
                                            new DataColumn("No of Data - Addon"),
                                            new DataColumn("Completion Percentage"),
                                            new DataColumn("No of Issues")
                });

                foreach (var dr in model.DashboardReportList)
                {
                    dt.Rows.Add(dr.DBName, dr.Module, dr.SAPDataNo, dr.AddonDataNo, Math.Round(((Convert.ToDouble(dr.AddonDataNo) / Convert.ToDouble(dr.SAPDataNo)) * 100), 2), dr.IssueNo);
                }

                using (XLWorkbook wb = new XLWorkbook())
                {
                    wb.Worksheets.Add(dt);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public string UploadFiletoSap(string[] modules)
        {
            string str = "";
            try
            {
                Properties.Settings prop = new Properties.Settings();
                //Settings prop = new Settings();

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_START", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                _context.SaveChanges();

                foreach (string modulecreds in modules)
                {
                    DataSet ds = new DataSet();
                    var Creds = GetFileToSAPCredentialsMapCode(modulecreds);
                    //string TableName = "";
                    //string ret = "";
                    if (Creds.CredentialDetails != null)
                    {
                        var filemodel = new List<ExcelMapperViewModel.ExcelMapperModel>();
                        filemodel = _context.Headers.AsEnumerable()
                                                    .Join(_context.FieldMappings, hf => hf.MapId, fm => fm.MapId, (hf, fm) => new { HF = hf, FM = fm })
                                                    .GroupJoin(_context.ModuleSetup
                                                                , hf => hf.FM.ModuleName
                                                                , ms => ms.ModuleCode
                                                                , (hf, ms) => ms.Select(x => new { HF = hf, MS = x })
                                                               .DefaultIfEmpty(new { HF = hf, MS = (ModuleSetup)null }))
                                                    .SelectMany(g => g)
                                                    .Where(sel => sel.HF.HF.MapId == Creds.CredentialDetails.MapId)
                                                    .GroupBy(gpy => new
                                                    {
                                                        gpy.HF.HF.MapId,
                                                        gpy.HF.HF.SourceHeaderStart,
                                                        gpy.HF.HF.SourceRowStart,
                                                        gpy.HF.HF.SourceTableName,
                                                        gpy.HF.HF.SourceType,
                                                        gpy.HF.HF.DestinationTableName,
                                                        gpy.MS.EntityName
                                                    })
                                                    .ToList()
                        .Select(sel => new ExcelMapperViewModel.ExcelMapperModel
                        {
                            MapId = sel.Key.MapId,
                            HeaderRow = sel.Key.SourceHeaderStart,
                            DataRowStart = sel.Key.SourceRowStart,
                            Worksheet = sel.Key.SourceTableName,
                            SourceType = sel.Key.SourceType,
                            DestinationTable = sel.Key.DestinationTableName,
                            EntityName = sel.Key.EntityName,
                        }).ToList();

                        ds = DataAccess.GetExcelData(Creds.CredentialDetails.FileName, filemodel);

                        var headerfields = _context.Headers.Where(sel => sel.MapId == Creds.CredentialDetails.MapId && sel.SourceType.ToLower() == "header").OrderBy(sel => sel.VisOrder).ToList();
                        var rowfields = _context.Headers.Where(sel => sel.MapId == Creds.CredentialDetails.MapId && sel.SourceType.ToLower() == "row").OrderBy(sel => sel.SourceTableName).ThenBy(sel => sel.VisOrder).ToList();

                        #region TESTING
                        //StringBuilder stringBuilder = new StringBuilder();
                        //stringBuilder.Append("{");
                        //stringBuilder.Append($@" ""CompanyDB"":""QPAC_IM_1024_JE"", ");
                        //stringBuilder.Append($@" ""UserName"":""Direc"" ");
                        //stringBuilder.Append($@" ""Password"":""1234"" ");
                        //stringBuilder.Append("}");

                        ////AnvClient = new RestClient($@"{ConfigurationManager.AppSettings["PNserver"]}");
                        //var options = new RestClientOptions($@"https://192.168.2.200:50000")
                        //{
                        //    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                        //};
                        //RestClient AnvClient = new RestClient(options: options);
                        //RestRequest AnvRequest = new RestRequest("b1s/v1/Login", Method.Post);
                        //AnvRequest.AddHeader("Accept", "application/json");
                        //AnvRequest.AddHeader("Content-Type", "application/json");
                        //AnvRequest.AddParameter("application/json", stringBuilder.ToString(), ParameterType.RequestBody);
                        //AnvClient.Execute(AnvRequest);
                        #endregion

                        StringBuilder json = new StringBuilder();
                        var srcTbl = ds.Tables[filemodel.Where(sel => sel.SourceType.ToLower() == "header").Select(sel => sel.Worksheet).FirstOrDefault()];
                        foreach (DataRow srcRow in srcTbl.Rows)
                        {
                            ////GET THE KEY DATA OF EACH TABLE
                            string _sqlSrcParamField, _sqlSrcParamVal = "";
                            _sqlSrcParamField = headerfields.Where(sel => sel.IsKeyValue == true).Select(sel => sel.SourceFieldId).FirstOrDefault();
                            _sqlSrcParamVal = (string)srcRow[$@"{_sqlSrcParamField}"];

                            json.AppendLine("{"); //Json Start

                            #region HEADER_JSON
                            foreach (var col in headerfields)
                            {

                                var rData = col.SourceFieldId == "-" ? "" : srcRow[col.SourceFieldId]; //TO DO CONDITIONAL QUERY
                                if (col.DestinationField.ToLower().Contains("date"))
                                {
                                    var docdate = DateTime.ParseExact(rData.ToString(), "MM/dd/yyyy", null);
                                    json.AppendLine($@" ""{col.DestinationField}"" : ""{Convert.ToDateTime(docdate).ToString("yyyy-MM-dd")}"",");
                                }
                                else if (col.DestinationField != null)
                                {
                                    json.AppendLine($@" ""{col.DestinationField}"" : ""{rData}"",");
                                }
                            }
                            #endregion

                            #region ROWS_JSON                        
                            //EACH SUB_TABLE
                            foreach (var subTable in filemodel)
                            {

                                _sqlSrcParamField = rowfields.Where(sel => sel.IsKeyValue == true && sel.DestinationTableName == subTable.DestinationTable)
                                                    .Select(sel => sel.SourceFieldId).FirstOrDefault();
                                bool isColm = DataAccess.IsColumnExist(ds.Tables[filemodel.Where(sel => sel.SourceType.ToLower() != "header" && sel.DestinationTable == subTable.DestinationTable).Select(sel => sel.Worksheet).FirstOrDefault()], _sqlSrcParamField);
                                if (isColm)
                                {
                                    var _srcTblrow = ds.Tables[filemodel.Where(sel => sel.SourceType.ToLower() != "header" && sel.DestinationTable == subTable.DestinationTable).Select(sel => sel.Worksheet).FirstOrDefault()]
                                                        .AsEnumerable().Where(sel => sel[$@"{_sqlSrcParamField}"].ToString() == $"{_sqlSrcParamVal}");
                                    DataTable srcTblrow = null;
                                    if (_srcTblrow.Any())
                                        srcTblrow = _srcTblrow.CopyToDataTable();

                                    if (srcTblrow != null)
                                    {
                                        json.AppendLine($@" ""{subTable.DestinationTable}"": [");
                                        foreach (DataRow dtRow in srcTblrow.Rows)
                                        {
                                            var fieldRows = rowfields.Where(sel => sel.DestinationTableName == subTable.DestinationTable);
                                            if (fieldRows != null)
                                            {
                                                if (fieldRows.Count() > 0)
                                                {
                                                    json.AppendLine("   { ");
                                                    foreach (var col in fieldRows)
                                                    {
                                                        var rData = dtRow[col.SourceFieldId]; //TO DO CONDITIONAL QUERY
                                                        if (col.DestinationField.ToLower().Contains("date"))
                                                        {
                                                            var docdate = DateTime.ParseExact(rData.ToString(), "MM/dd/yyyy", null);
                                                            json.AppendLine($@" ""{col.DestinationField}"" : ""{Convert.ToDateTime(docdate).ToString("yyyy-MM-dd")}"",");
                                                        }
                                                        else if (col.DestinationField != null)
                                                        {
                                                            json.AppendLine($@" ""{col.DestinationField}"" : ""{rData}"",");
                                                        }
                                                    }
                                                    json.AppendLine("\n" + "   }, ");
                                                }
                                            }
                                        }

                                        json.AppendLine($@"      ], ");
                                    }
                                }
                            }
                            #endregion

                            json.AppendLine("}"); //Json End

                            //post JSON
                            var entityPost = filemodel.Where(sel => sel.SourceType.ToLower() == "header").Select(sel => sel.EntityName).FirstOrDefault();
                            //var result = phelper.Posting4Automation("POST", $@"{entityPost}", json.ToString(), "", Creds);
                            var result = phelper.Posting4Automation("POST", $@"{entityPost}", json.ToString(), "", Creds);

                            bool IsUploaded = false;

                            string con = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                            // Parse the connection string to get the database name
                            var builder = new SqlConnectionStringBuilder(con);
                            string databaseName = builder.InitialCatalog;

                            int MapId = Creds.CredentialDetails.MapId;

                            if (result.ToLower().Contains($@"""error"":") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = $@"{entityPost}", CreateDate = DateTime.Now, ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = json.ToString(), ErrorMsg = result.ToString() });
                                _context.SaveChanges();

                                //SET ISUPLOADED TO FALSE
                                //IsUploaded = false;
                            }
                            //else
                            //{
                            //    //SET ISUPLOADED TO TRUE
                            //    IsUploaded = true;
                            //}


                            #region POST TO TABLE FOR TRANSACTION LOGS
                            //columns - PrimaryKey, CreateDate, UploadDate, IsUpload, MapId
                            //qa.UploadTransactionLog();

                            #endregion
                        }

                    }
                    #region READ_FILES

                    #endregion
                }
            }
            catch (Exception ex)
            {

            }
            return str;
        }

        public static int HeaderRowCount;
        public string UploadOPSFiletoSap(string[] modules, string code)
        {


            string str = "";
            var errorCode = 0;
            var errorMessage = "";
            var Pkey = "";
            var PKValue = "";
            var PostingErrorMessage = "";
            Dictionary<string, List<string>> ErrorListValue = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> SuccessListValue = new Dictionary<string, List<string>>();


            var Schedule = _context.Schedules.Where(x => x.SchedCode == code).FirstOrDefault();

            try
            {
                Properties.Settings prop = new Properties.Settings();
                //Settings prop = new Settings();

                //_context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_START", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = "THIS IS INFORMATION ONLY" });
                //_context.SaveChanges();

                foreach (string modulecreds in modules)
                {
                    errorCode = 1001;
                    DataSet ds = new DataSet();



                    var Creds = OPSGetFileToSAPCredentialsMapCode(modulecreds);

                    errorCode = 1002;

                    var filename_list = _context.OPSFieldMappings
                        .Where(x => x.MapCode == modulecreds)
                        .Join(_context.OPSFieldTable, opsfm => opsfm.MapId, opsft => opsft.MapId, (opsfm, opsft) => new
                        {
                            opsfm,
                            opsft
                        })
                        .Join(_context.PathSetup, ops => ops.opsft.PathCode, ps => ps.PathCode, (ops, ps) => new
                        {
                            FileName = ops.opsft.FileName,
                            SAPTableNameModule = ops.opsft.SAPTableNameModule,
                            ErrorPath = ps.ErrorPath,
                            LocalPath = ps.LocalPath,
                            BackUpPath = ps.BackupPath
                        }).ToList();


                    var reversed_filename_list = filename_list
                         .OrderBy(x => x.SAPTableNameModule == "Header")  // Sort by whether it's "Header"
                         .ThenBy(x => x.SAPTableNameModule)              // Then sort by the module name
                         .ToList();


                    errorCode = 1003;
                    //string TableName = "";
                    //string ret = "";
                    if (Creds.CredentialDetails != null)
                    {
                        var authPost = new AuthenticationCredViewModel
                        {
                            URL = "https",
                            Method = "GET",
                            Action = Creds.CredentialDetails.EntityName, //For META_DATA  VERSION 2
                            JsonString = "{}",
                            SAPSldServer = Creds.CredentialDetails.SAPSldServer,
                            SAPServer = Creds.CredentialDetails.SAPIPAddress,
                            Port = Creds.CredentialDetails.SAPLicensePort.ToString(),
                            SAPDatabase = Creds.CredentialDetails.SAPDBName,
                            SAPDBUserId = Creds.CredentialDetails.SAPDBuser,
                            SAPDBPassword = Creds.CredentialDetails.SAPDBPassword,
                            SAPUserID = Creds.CredentialDetails.SAPUser,
                            SAPPassword = Creds.CredentialDetails.SAPPassword,
                        };


                        var filemodel = new List<ExcelMapperViewModel.ExcelMapperModel>();//Try To Initiate
                        filemodel = _context.OPSFieldTable
                            .Where(tables => tables.MapId == Creds.CredentialDetails.MapId)
                            .Join(_context.OPSFieldMappings, tables => tables.MapId, mapping => mapping.MapId, (tables, mapping) => new { tables, mapping })
                            .Join(_context.ModuleSetup, tableMaps => tableMaps.mapping.ModuleName, module => module.ModuleCode, (tableMaps, module) => new ExcelMapperViewModel.ExcelMapperModel
                            {
                                MapId = tableMaps.mapping.MapId,
                                HeaderRow = tableMaps.tables.SourceColumnName,
                                DataRowStart = tableMaps.tables.SourceRowData,
                                Worksheet = tableMaps.tables.SourceTableName,
                                SAPTableId = tableMaps.tables.SAPTableId,
                                DestinationTable = tableMaps.tables.SAPTableNameModule,
                                EntityName = module.EntityName
                            }).ToList();

                        errorCode = 1004;

                        //ds = DataAccess.GetExcelData(Creds.CredentialDetails.FileName, filemodel);
                        string hcon = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : ""; //nilipat need ung connection string


                        string directoryPath = Path.GetDirectoryName(filename_list[0].FileName);

                        int file_count = 0;

                        bool FirstExecution = true;
                        string query_ = $"SELECT COUNT(DISTINCT FileName) FROM dbo.OPSFieldTables WHERE MapId = {Creds.CredentialDetails.MapId}";

                        using (SqlConnection connection = new SqlConnection(hcon))
                        {
                            SqlCommand command = new SqlCommand(query_, connection);
                            connection.Open();
                            file_count = (int)command.ExecuteScalar();
                            connection.Close();
                        }

                        ///////
                        var excelFiles = Directory.GetFiles(directoryPath, "*.xlsx")
                          .Where(file => Path.GetFileName(file).StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                                         Path.GetFileName(file).StartsWith("m", StringComparison.OrdinalIgnoreCase))
                          .ToList();


                        foreach (var excelFile in excelFiles)
                        {


                            string excel_fileName = Path.GetFileName(excelFile);

                            string charactersAfterUnderscore = excel_fileName.Substring(excel_fileName.IndexOf('_'));

                            int excelFileCount = Directory.GetFiles(directoryPath, "*.xlsx")
                                     .Count(file => Path.GetFileName(file).Contains(charactersAfterUnderscore));
                            ///////


                            //var excelFileCount = Directory.GetFiles(directoryPath, "*.xlsx")
                            //          .FirstOrDefault(file => Path.GetFileName(file).StartsWith("o", StringComparison.OrdinalIgnoreCase) ||
                            //                                   Path.GetFileName(file).StartsWith("m", StringComparison.OrdinalIgnoreCase));

                            //int excelFileCount = Directory.GetFiles(directoryPath, "*.xlsx").Count();

                            if (file_count == excelFileCount)
                            {
                                ///////
                                if (FirstExecution)
                                {
                                    //Deleting Data where UploadDate is null
                                    foreach (var stds in reversed_filename_list)
                                    {
                                        errorCode = 12005;

                                        foreach (var list_mode in filemodel)
                                        {

                                            errorCode = 12006;
                                            if (stds.SAPTableNameModule == list_mode.DestinationTable)
                                            {

                                                var opsft_id = _context.OPSFieldTable.Where(x => x.MapId == list_mode.MapId && x.SAPTableNameModule == list_mode.DestinationTable).FirstOrDefault();

                                                var isPrimakeys = _context.OPSFieldSets
                                                   .Where(sel => sel.MapId == Creds.CredentialDetails.MapId && sel.IsKeyValue && sel.SAPTableId == opsft_id.SAPTableId)//remove as Enum
                                                   .Select(sel => sel.SourceField).FirstOrDefault(); // Assuming PrimaryKey is the property representing the primary key

                                                string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                                                var builderr = new SqlConnectionStringBuilder(conz);

                                                var dbTableName = $"{list_mode.MapId.ToString()}_{list_mode.DestinationTable}";

                                                SqlConnection cnn;
                                                cnn = new SqlConnection(conz);
                                                string checktable = $"SELECT * FROM {builderr.InitialCatalog}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{dbTableName}'";

                                                using (SqlCommand cmd = new SqlCommand(checktable, cnn))
                                                {
                                                    cnn.Open();
                                                    using (SqlDataReader reader = cmd.ExecuteReader())
                                                    {
                                                        if (reader.HasRows)
                                                        {
                                                            reader.Close();
                                                            var clear_command = $"delete from dbo.[{dbTableName}] where {isPrimakeys} in (select {isPrimakeys} from dbo.[{list_mode.MapId.ToString()}_Header] where UploadDate is  null )";

                                                            using (SqlCommand clearCmd = new SqlCommand(clear_command, cnn))
                                                            {
                                                                clearCmd.CommandTimeout = 600;
                                                                int rowsAffected = clearCmd.ExecuteNonQuery();
                                                                // rowsAffected contains the number of rows deleted by the DELETE statement
                                                            }
                                                        }
                                                    }
                                                }


                                            }

                                        }

                                    }//end of First Execution

                                    FirstExecution = false;
                                    ///////
                                }

                                //Transfering Excel to DB
                                foreach (var stds in filename_list)
                                {
                                    errorCode = 1005;
                                    foreach (var list_mode in filemodel)
                                    {

                                        errorCode = 1006;
                                        if (stds.SAPTableNameModule == list_mode.DestinationTable)
                                        {
                                            errorCode = 1007;
                                            var opsft_id = _context.OPSFieldTable.Where(x => x.MapId == list_mode.MapId && x.SAPTableNameModule == list_mode.DestinationTable).FirstOrDefault();
                                            errorCode = 7001;
                                            var columnNames = _context.OPSFieldSets
                                               .Where(sel => sel.MapId == Creds.CredentialDetails.MapId && sel.SAPTableId == opsft_id.SAPTableId).OrderBy(sel => sel.VisOrder).ToList();//remove as Enum
                                            errorCode = 7002;
                                            var isPrimakeys = _context.OPSFieldSets
                                               .Where(sel => sel.MapId == Creds.CredentialDetails.MapId && sel.IsKeyValue && sel.SAPTableId == opsft_id.SAPTableId)//remove as Enum
                                               .Select(sel => sel.SourceField).FirstOrDefault(); // Assuming PrimaryKey is the property representing the primary key
                                            errorCode = 7003;
                                            string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";
                                            errorCode = 7004;
                                            // Parse the connection string to get the database name
                                            var builderr = new SqlConnectionStringBuilder(conz);
                                            errorCode = 7005;
                                            string databaseNamez = builderr.InitialCatalog;
                                            errorCode = 7006;
                                            //int MapId = Creds.CredentialDetails.MapId;

                                            var dbTableName = $"{list_mode.MapId.ToString()}_{list_mode.DestinationTable}";
                                            errorCode = 7007;

                                            ///////
                                            string _directory_ = System.IO.Path.GetDirectoryName(stds.FileName);
                                            string _fileName_ = System.IO.Path.GetFileNameWithoutExtension(stds.FileName);
                                            string stds_newFileName = $"{_directory_}\\{_fileName_}{charactersAfterUnderscore}";
                                            ///////
                                            ///
                                            int ExcelItemCount = 0;
                                            //bool opsexcelreturn = DataAccess.OPSGetExcelData(stds.FileName, list_mode, ExcelItemCount, isPrimakeys, dbTableName, conz, databaseNamez, columnNames, list_mode.DestinationTable, out errorMessage, list_mode.MapId, list_mode.EntityName, stds.ErrorPath);// w/o timestamp

                                            bool opsexcelreturn = DataAccess.OPSGetExcelData(stds_newFileName, list_mode, ExcelItemCount, isPrimakeys, dbTableName, conz, databaseNamez, columnNames, list_mode.DestinationTable, out errorMessage, list_mode.MapId, list_mode.EntityName, stds.ErrorPath);// w/ timestamp
                                            errorCode = 7008;
                                            GC.Collect();


                                        }

                                    }



                                }



                                foreach (var stds in filename_list)
                                {
                                    //bool exist = true;
                                    try
                                    {

                                        string[] filesInDirectory = Directory.GetFiles(stds.LocalPath);
                                        string FileNamExist = Path.Combine(stds.LocalPath, $"{Path.GetFileNameWithoutExtension(stds.FileName)}{charactersAfterUnderscore}");//w/Time Stamp
                                                                                                                                                                            //string FileNamExist = Path.Combine(stds.LocalPath, $"{Path.GetFileNameWithoutExtension(stds.FileName)}");

                                        if (filesInDirectory.Contains(FileNamExist, StringComparer.OrdinalIgnoreCase))
                                        {

                                            errorCode = 7010;
                                            string fileName = Path.GetFileNameWithoutExtension(stds.FileName);// w/timestamp

                                            //string fileName = Path.GetFileName(stds.FileName);// w/otimestamp


                                            string fileExtension = Path.GetExtension(stds.FileName);
                                            errorCode = 7011;


                                            //string targetFilePath = Path.Combine(stds.BackUpPath, fileName + "_backup_" + DateTime.Now.ToString("yyyyMMddHHmmss") + fileExtension); 

                                            string BackUpPath = $"{stds.BackUpPath}\\{DateTime.Now.ToString("MM")}{DateTime.Now.ToString("yyyy")}\\{DateTime.Now.ToString("yyyyMMdd")}";

                                            if (!Directory.Exists(BackUpPath))
                                            {
                                                // Create the directory if it does not exist
                                                Directory.CreateDirectory(BackUpPath);
                                            }

                                            string targetFilePath = Path.Combine(BackUpPath, $"{fileName}{Path.GetFileNameWithoutExtension(charactersAfterUnderscore)}" + "_backup" + fileExtension); // w/ TimeStap


                                            ////////
                                            string _directory_ = System.IO.Path.GetDirectoryName(stds.FileName);
                                            string _fileName_ = System.IO.Path.GetFileNameWithoutExtension(stds.FileName);
                                            string stds_newFileName = $"{_directory_}\\{_fileName_}{charactersAfterUnderscore}";
                                            ////

                                            errorCode = 7012;

                                            if (File.Exists(targetFilePath))
                                            {
                                                File.Delete(targetFilePath);
                                            }

                                            File.Move(stds_newFileName, targetFilePath);// w/ timestap

                                            errorCode = 7013;

                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        errorCode = 1005;


                                    }


                                }

                            }//end of  count check

                        }//end of ExcelFileCount


                        #region


                        errorCode = 1008;
                        var Primakey = "";
                        var primaryKeyValue = "";
                        var CardType = "";
                        var HouseBankCode = "";
                        var IPDocEntry = "";
                        var OPDocEntry = "";

                        bool IP_toBePosted = false;
                        var ARCMDocEntry = "";


                        //string hcon = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                        using (SqlConnection cnn = new SqlConnection(hcon))
                        {

                            cnn.Open();
                            //string FetchRowHeaderData = $"SELECT COUNT(*) FROM dbo.[{JsonBuilder.MapId}_{JsonMainTable}]";
                            //SqlCommand cmd = new SqlCommand(FetchRowHeaderData, cnn);
                            //HeaderRowCount = (int)cmd.ExecuteScalar();
                            try
                            {
                                errorCode = 8001;
                                string FetchHeader = $"SELECT * from dbo.[{Creds.CredentialDetails.MapId}_Header] WHERE UploadDate IS  NULL ORDER BY CreateDate";
                                foreach (DataRow itemData in DataAccess.Select(hcon, FetchHeader).Rows)
                                {
                                    try
                                    {
                                        Schedule.RunTime = DateTime.Now;
                                        _context.SaveChanges();

                                        StringBuilder json = new StringBuilder();
                                        errorCode = 8002;

                                        bool CanCelDn = false;
                                        bool CancelNoDST = false;
                                        bool NoInvoice = false;
                                        foreach (var JsonBuilder in filemodel)
                                        {
                                            errorCode = 1009;
                                            string JsonMainTable = JsonBuilder.DestinationTable;
                                            errorCode = 9001;
                                            var OPSSapTableId = _context.OPSFieldTable.Where(x => x.MapId == JsonBuilder.MapId && x.SAPTableNameModule == JsonBuilder.DestinationTable).FirstOrDefault();//remove as Enum
                                            errorCode = 9002;
                                            var OPSheaderfieldsz = _context.OPSFieldSets.Where(sel => sel.MapId == JsonBuilder.MapId && sel.SAPTableId == OPSSapTableId.SAPTableId).OrderBy(sel => sel.VisOrder).ToList();//remove as Enum
                                            errorCode = 9003;

                                            var OPSheaderfields = _context.OPSFieldSets
                                            .Where(sel => sel.MapId == JsonBuilder.MapId && sel.SAPTableId == OPSSapTableId.SAPTableId)//remove as Enum
                                            .OrderBy(sel => sel.VisOrder)
                                            .ToList();

                                            var OPSrowfields = _context.OPSFieldSets
                                                .Where(sel => sel.MapId == JsonBuilder.MapId && sel.SAPTableId != OPSSapTableId.SAPTableId)
                                                .Join(_context.OPSFieldTable, opsfs => opsfs.SAPTableId, opsft => opsft.SAPTableId, (opsfs, opsft) => new
                                                {
                                                    SapTableId = opsfs.SAPTableId,
                                                    DestinationTableName = opsft.SAPTableNameModule,
                                                    VisOrder = opsfs.VisOrder,
                                                    SourceField = opsfs.SourceField,
                                                    DestinationField = opsfs.DestinationField,
                                                    IsKeyValue = opsfs.IsKeyValue,
                                                }).OrderBy(opsfs => opsfs.SapTableId).ThenBy(opsfs => opsfs.VisOrder).ToList();

                                            errorCode = 9004;
                                            if (JsonMainTable == "Header")
                                            {
                                                Primakey = _context.OPSFieldSets
                                                       .Where(sel => sel.MapId == JsonBuilder.MapId && sel.IsKeyValue && sel.SAPTableId == OPSSapTableId.SAPTableId)
                                                       .Select(sel => sel.SourceField).FirstOrDefault(); // Change from Destinationfield?
                                                errorCode = 9005;
                                                primaryKeyValue = itemData[Primakey].ToString();//reader.GetString(reader.GetOrdinal($"{Primakey}"))

                                                if (JsonBuilder.EntityName == "BusinessPartners")
                                                {

                                                    switch (itemData["CARDTYPE"].ToString())
                                                    {
                                                        case "cSupplier":
                                                            CardType = "S";
                                                            break;

                                                        default:
                                                            CardType = "C";
                                                            break;
                                                    }


                                                }
                                                errorCode = 9006;

                                                Pkey = Primakey.ToString();
                                                PKValue = primaryKeyValue.ToString();//remove as Enum
                                            }


                                            string OPS_sqlSrcParamField, OPS_sqlSrcParamVal = "";
                                            OPS_sqlSrcParamVal = (string)$@"{Primakey}";

                                            string FetchDataHeader = $@"SELECT * from dbo.[{Creds.CredentialDetails.MapId}_{JsonMainTable}] WHERE {Primakey} = '{primaryKeyValue}'";
                                            errorCode = 9007;
                                            var dt = DataAccess.Select(hcon, FetchDataHeader);

                                            if (dt.Rows.Count == 0)
                                            {
                                                continue;
                                            }
                                            if (JsonMainTable != "Header")
                                            {
                                                json.AppendLine($@" ""{JsonMainTable}"": [");
                                            }
                                            errorCode = 9008;
                                            //loop here for lines
                                            foreach (DataRow item in dt.Rows)
                                            {
                                                if (JsonMainTable != "Header")
                                                {
                                                    if (JsonMainTable != "DocumentAdditionalExpenses" || Creds.CredentialDetails.Module.Equals("OINV") && JsonMainTable.Equals("DocumentAdditionalExpenses") || Creds.CredentialDetails.Module.Equals("ORIN") && JsonMainTable.Equals("DocumentAdditionalExpenses"))
                                                    {
                                                        json.AppendLine("   { ");

                                                    }
                                                }

                                                errorCode = 9009;


                                                errorCode = 3001;

                                                foreach (var Dest in OPSheaderfields)
                                                {

                                                    errorCode = 3002;
                                                    //if (Dest.SourceField == "CHECKSUM") 
                                                    //{ 

                                                    //}
                                                    var JSON_Sourcefield = Dest.SourceField;
                                                    var JSON_DestinationField = Dest.DestinationField;

                                                    var JSON_DataType = Dest.DataType;//I need this if Date To Standard (YYY-MMM_DDD)



                                                    if (JSON_DestinationField == "-")
                                                    {
                                                        errorCode = 3003;
                                                        continue;
                                                    }

                                                    var fieldValue = "";

                                                    switch (JSON_Sourcefield)
                                                    {
                                                        case "Query":

                                                            string input = Dest.ConditionalQuery;
                                                            var output = Regex.Matches(input, $"#(.+?)#")
                                                                                                .Cast<System.Text.RegularExpressions.Match>()
                                                                                                .Select(m => m.Groups[1].Value);

                                                            foreach (var columnName in output)
                                                            {
                                                                //eRROR IF THE U_HBSCode is Not Exist in the SAP.. There is no Row at position 0 
                                                                input = QueryAccess.ReplaceQueryParameter(input
                                                                    , $"#{columnName}#"
                                                                    , item[columnName].ToString(), HouseBankCode);
                                                            }
                                                            var dtResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                Creds.CredentialDetails.SAPDBName,
                                                                Creds.CredentialDetails.SAPDBuser,
                                                                Creds.CredentialDetails.SAPDBPassword), input);


                                                            errorCode = 45658;

                                                            //There is no Row at position 0 ; Occured When dtresult CardCode Does Not Exist in SAP
                                                            if (dtResult != null && dtResult.Rows.Count > 0)
                                                            {
                                                                fieldValue = dtResult.Rows[0][0].ToString() ?? "";
                                                                errorCode = 4002;
                                                            }


                                                            break;
                                                        case "Static Value":

                                                            if (JSON_DestinationField == "BaseEntry")
                                                            {
                                                                fieldValue = ARCMDocEntry;

                                                            }
                                                            else
                                                            {
                                                                fieldValue = Dest.ConditionalQuery;

                                                            }

                                                            break;

                                                        case "LICTRADNUM":

                                                            fieldValue = item[JSON_Sourcefield].ToString();

                                                            if (string.IsNullOrEmpty(fieldValue))
                                                            {
                                                                fieldValue = "000-000-000";
                                                            }

                                                            break;

                                                        case "U_OP_BPBANK":

                                                            fieldValue = item[JSON_Sourcefield].ToString() ?? "";
                                                            string sqlinput = "declare @CNT INT;\r\nset @CNT= (select DISTINCT count(U_BenBank) from \"@ODBS\" where U_BenBank ='#U_OP_BPBANK#')\r\nif (@CNT =0)\r\n\tBegin \r\n\t\tselect 'OTHERS' as U_BenBank\r\n\tEnd\r\nElse\r\n\tselect DISTINCT (U_BenBank) from \"@ODBS\" where U_BenBank ='#U_OP_BPBANK#'";

                                                            var sqloutput = Regex.Matches(sqlinput, $"#(.+?)#")
                                                                                                .Cast<System.Text.RegularExpressions.Match>()
                                                                                                .Select(m => m.Groups[1].Value);
                                                            foreach (var columnName in sqloutput)
                                                            {
                                                                //eRROR IF THE U_HBSCode is Not Exist in the SAP.. There is no Row at position 0 
                                                                sqlinput = QueryAccess.OriginalReplaceQueryParameter(sqlinput
                                                                    , $"#{columnName}#"
                                                                    , item[columnName].ToString(),null);
                                                            }


                                                            var sqldtResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                Creds.CredentialDetails.SAPDBName,
                                                                Creds.CredentialDetails.SAPDBuser,
                                                                Creds.CredentialDetails.SAPDBPassword), sqlinput);


                                                            if (sqldtResult != null || sqldtResult.Rows.Count > 0)
                                                            {
                                                                fieldValue = sqldtResult.Rows[0][0].ToString() ?? "";
                                                                HouseBankCode = fieldValue;
                                                            }

                                                            break;

                                                        case "NUMATCARD":
                                                            if (JsonBuilder.EntityName == "CreditNotes")
                                                            {
                                                                ARCMDocEntry = "";
                                                                var sqlinputcommand = $"select DocEntry from OINV where NumAtCard='{item[JSON_Sourcefield]}' and CANCELED='N' and DocStatus!='C'";
                                                                var DCResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                Creds.CredentialDetails.SAPDBName,
                                                                Creds.CredentialDetails.SAPDBuser,
                                                                Creds.CredentialDetails.SAPDBPassword), sqlinputcommand);


                                                                //There is no Row at position 0 ; Occured When dtresult CardCode Does Not Exist in SAP
                                                                if (DCResult != null && DCResult.Rows.Count > 0)
                                                                {
                                                                    ARCMDocEntry = DCResult.Rows[0][0].ToString() ?? "";

                                                                    errorCode = 4052;
                                                                }
                                                                fieldValue = item[JSON_Sourcefield].ToString() ?? "";
                                                            }
                                                            else
                                                            {   //For The BP
                                                                fieldValue = item[JSON_Sourcefield].ToString() ?? "";
                                                            }

                                                            break;

                                                        default:

                                                            fieldValue = item[JSON_Sourcefield].ToString() ?? "";


                                                            if (JSON_Sourcefield == "U_CANCELTYPE" || JSON_Sourcefield == "U_CancelType")
                                                            {
                                                                if (JsonBuilder.EntityName == "PurchaseInvoices")
                                                                {
                                                                    IPDocEntry = "";
                                                                    if (fieldValue == "REVERSAL" || fieldValue == "Reversal")
                                                                    {
                                                                        var _NumatCard = item["NUMATCARD"].ToString();

                                                                        var cmdIP = $"select DocEntry from OPCH where NumAtCard='{_NumatCard}' and Canceled='N' ";

                                                                        var DocdtResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                        Creds.CredentialDetails.SAPDBName,
                                                                        Creds.CredentialDetails.SAPDBuser,
                                                                        Creds.CredentialDetails.SAPDBPassword), cmdIP);

                                                                        if (DocdtResult != null && DocdtResult.Rows.Count > 0)
                                                                        {
                                                                            IPDocEntry = DocdtResult.Rows[0][0].ToString() ?? "";
                                                                            IP_toBePosted = false;
                                                                        }
                                                                        else
                                                                        {
                                                                            IP_toBePosted = true;
                                                                            IPDocEntry = "-1";
                                                                        }

                                                                    }
                                                                }//Purchase Invoice
                                                                else if (JsonBuilder.EntityName.Equals("IncomingPayments"))
                                                                {
                                                                    // string U_Series = item["Series"].ToString();
                                                                    IPDocEntry = "";
                                                                    OPDocEntry = "";
                                                                    if (!string.IsNullOrEmpty(fieldValue) && fieldValue.Equals("REVERSAL", StringComparison.OrdinalIgnoreCase))
                                                                    {
                                                                        string U_ExpenseType = item["U_EXPNSTYPE"].ToString();
                                                                        var CounterRef = item["COUNTERREF"].ToString();

                                                                        if (U_ExpenseType.ToLower() == "overpayment" || U_ExpenseType.ToLower() == "over payment")
                                                                        {
                                                                            CounterRef = CounterRef.Substring(0, CounterRef.Length - 3);
                                                                        }

                                                                        errorCode = 4028;

                                                                        var cmdIP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Canceled='N' and DOCTYPE = 'C' ";
                                                                        var cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Canceled='N' and DOCTYPE = 'A' ";
                                                                        //var pchc_cmdOP = "";

                                                                        //if (U_Series == "15")
                                                                        //{
                                                                        //    cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=74 and Canceled='N'";
                                                                        //}
                                                                        //else if (U_Series == "71")
                                                                        //{
                                                                        //    cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=75 and Canceled='N'";
                                                                        //}
                                                                        //else if (U_Series == "72")
                                                                        //{
                                                                        //    cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=76 and Canceled='N'";
                                                                        //}
                                                                        //else if (U_Series == "73")
                                                                        //{
                                                                        //    cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=77 and Canceled='N'";
                                                                        //}
                                                                        //else if (U_Series == "74")
                                                                        //{
                                                                        //    cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=78 and Canceled='N'";
                                                                        //}

                                                                        var DocdtResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                            Creds.CredentialDetails.SAPDBName,
                                                                            Creds.CredentialDetails.SAPDBuser,
                                                                            Creds.CredentialDetails.SAPDBPassword), cmdIP);

                                                                        var DocdtResult_OP = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                          Creds.CredentialDetails.SAPDBName,
                                                                          Creds.CredentialDetails.SAPDBuser,
                                                                          Creds.CredentialDetails.SAPDBPassword), cmdOP);

                                                                        if (DocdtResult != null && DocdtResult.Rows.Count > 0)
                                                                        {
                                                                            IPDocEntry = DocdtResult.Rows[0][0].ToString() ?? "";
                                                                            IP_toBePosted = false;

                                                                            if (DocdtResult_OP != null && DocdtResult_OP.Rows.Count > 0)
                                                                            {
                                                                                OPDocEntry = DocdtResult_OP.Rows[0][0].ToString() ?? "";
                                                                            }
                                                                            //else
                                                                            //{
                                                                            //    if (U_Series == "15")
                                                                            //    {
                                                                            //        pchc_cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=76 and Canceled='N'";


                                                                            //    }
                                                                            //    else if (U_Series == "73")
                                                                            //    {
                                                                            //        pchc_cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=77 and Canceled='N'";

                                                                            //    }

                                                                            //    var PCHC_DocdtResult_OP = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                            //        Creds.CredentialDetails.SAPDBName,
                                                                            //        Creds.CredentialDetails.SAPDBuser,
                                                                            //        Creds.CredentialDetails.SAPDBPassword), pchc_cmdOP);

                                                                            //    if (PCHC_DocdtResult_OP != null && PCHC_DocdtResult_OP.Rows.Count > 0)
                                                                            //    {
                                                                            //        OPDocEntry = PCHC_DocdtResult_OP.Rows[0][0].ToString() ?? "";

                                                                            //    }


                                                                            //}
                                                                        }
                                                                        else
                                                                        {
                                                                            if (DocdtResult_OP != null && DocdtResult_OP.Rows.Count > 0)
                                                                            {
                                                                                OPDocEntry = DocdtResult_OP.Rows[0][0].ToString() ?? "";
                                                                            }
                                                                            IP_toBePosted = true;
                                                                            IPDocEntry = "-1";
                                                                            //else
                                                                            //{
                                                                            //    //if (U_Series == "15")
                                                                            //    //{
                                                                            //    //    pchc_cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=76 and Canceled='N'";

                                                                            //    //}
                                                                            //    //else if (U_Series == "73")
                                                                            //    //{
                                                                            //    //    pchc_cmdOP = $"select DocEntry from ORCT where CounterRef='{CounterRef}' and Series=77 and Canceled='N'";

                                                                            //    //}

                                                                            //    var PCHC_DocdtResult_OP = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                            //        Creds.CredentialDetails.SAPDBName,
                                                                            //        Creds.CredentialDetails.SAPDBuser,
                                                                            //        Creds.CredentialDetails.SAPDBPassword), cmdOP);

                                                                            //    if (PCHC_DocdtResult_OP != null && PCHC_DocdtResult_OP.Rows.Count > 0)
                                                                            //    {
                                                                            //        OPDocEntry = PCHC_DocdtResult_OP.Rows[0][0].ToString() ?? "";

                                                                            //    }


                                                                            //}

                                                                        }

                                                                    }

                                                                }//end of else if (Incoming Payment)
                                                                else if (JsonBuilder.EntityName.Equals("CreditNotes"))
                                                                {
                                                                    errorCode = 5010;
                                                                    string DN_Date = item["DN_DATE"].ToString();

                                                                    if (fieldValue == "CancelDN" && string.IsNullOrEmpty(DN_Date.ToString()))
                                                                    {
                                                                        CanCelDn = true;
                                                                    }
                                                                    else if (fieldValue == "CancelNoDST")
                                                                    {
                                                                        CancelNoDST = true;
                                                                    }
                                                                }

                                                            }

                                                            if (JSON_Sourcefield == "BUSINESS_TAX")
                                                            {
                                                                if (JsonBuilder.EntityName == "Invoices" || JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    json.AppendLine(" \"DocumentLineAdditionalExpenses\": [ ");
                                                                }
                                                                json.AppendLine(" { ");

                                                                if (JsonBuilder.EntityName == "Invoices" || JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    json.AppendLine($"\"LineNumber\":\"{item["LINE_NUM"]}\",");

                                                                }
                                                                //if (JsonBuilder.EntityName == "CreditNotes")
                                                                //{
                                                                //   json.AppendLine($"\"LineNumber\":\"{item["LINE_NUM"]}\",");

                                                                //}
                                                                json.AppendLine("\"VatGroup\":\"OTNA\",");
                                                                json.AppendLine("\"ExpenseCode\":\"1\",");
                                                                json.AppendLine($@"""{JSON_DestinationField}"":""{fieldValue}""");
                                                                json.AppendLine("   }, ");
                                                                continue;
                                                            }
                                                            if (JSON_Sourcefield == "DST")
                                                            {
                                                                json.AppendLine("   { ");

                                                                if (JsonBuilder.EntityName == "Invoices" || JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    json.AppendLine($"\"LineNumber\":\"{item["LINE_NUM"]}\",");

                                                                }

                                                                json.AppendLine("\"VatGroup\":\"OTNA\",");
                                                                if (JsonBuilder.EntityName.Equals("CreditNotes") && CancelNoDST)
                                                                {
                                                                    json.AppendLine("\"ExpenseCode\":\"3\",");
                                                                    if (ARCMDocEntry.Equals(""))
                                                                    {
                                                                        CancelNoDST = false;

                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    json.AppendLine("\"ExpenseCode\":\"2\",");
                                                                }
                                                                json.AppendLine($@"""{JSON_DestinationField}"":""{fieldValue}""");
                                                                json.AppendLine("   }, ");
                                                                continue;
                                                            }

                                                            if (JSON_Sourcefield == "CLEARING" || JSON_Sourcefield == "OFFSET")
                                                            {
                                                                //if (JsonBuilder.EntityName == "CreditNotes")
                                                                //{
                                                                //    json.AppendLine(" \"DocumentLineAdditionalExpenses\": [ ");
                                                                //}

                                                                json.AppendLine("   { ");
                                                                if (JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    var offsetinput = $"SELECT CONVERT(DECIMAL(18, 2), '{fieldValue}') * -1";

                                                                    var OffsetResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                    Creds.CredentialDetails.SAPDBName,
                                                                    Creds.CredentialDetails.SAPDBuser,
                                                                    Creds.CredentialDetails.SAPDBPassword), offsetinput);

                                                                    if (OffsetResult != null || OffsetResult.Rows.Count > 0)
                                                                    {
                                                                        fieldValue = OffsetResult.Rows[0][0].ToString() ?? "";
                                                                        errorCode = 30088;
                                                                    }

                                                                    json.AppendLine($"\"LineNumber\":\"{item["LINE_NUM"]}\",");

                                                                }

                                                                if (JsonBuilder.EntityName == "PurchaseInvoices")
                                                                {
                                                                    json.AppendLine("\"VatGroup\":\"ITNA\",");
                                                                }

                                                                json.AppendLine("\"ExpenseCode\":\"4\",");
                                                                json.AppendLine($@"""{JSON_DestinationField}"":""{fieldValue}""");
                                                                json.AppendLine("   }, ");

                                                                if (JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    json.AppendLine("   ] ");

                                                                }
                                                                continue;

                                                            }

                                                            if (JSON_Sourcefield == "OTHER_INCOME")
                                                            {

                                                                json.AppendLine("   { ");

                                                                if (JsonBuilder.EntityName == "Invoices" || JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    json.AppendLine($"\"LineNumber\":\"{item["LINE_NUM"]}\",");

                                                                }

                                                                //var check_Freight = $"select RevAcct from OEXD where ExpnsCode=3";

                                                                //var RevAccount = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                //Creds.CredentialDetails.SAPDBName,
                                                                //Creds.CredentialDetails.SAPDBuser,
                                                                //Creds.CredentialDetails.SAPDBPassword), check_Freight);

                                                                //if (RevAccount != null && RevAccount.Rows.Count > 0)
                                                                //{
                                                                //    if (RevAccount.Rows[0][0].ToString().Equals("402005"))
                                                                //    {
                                                                json.AppendLine("\"VatGroup\":\"OTNA\",");

                                                                //    }
                                                                //    else
                                                                //    {
                                                                //        json.AppendLine("\"VatGroup\":\"OT2\",");

                                                                //    }
                                                                //}

                                                                if (JsonBuilder.EntityName == "Invoices" || JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    var AccountCode = item["ACCTCODE"].ToString();
                                                                    var checkExpenseCodeCommand = $"select ExpnsCode from OEXD where RevAcct={AccountCode}";

                                                                    var ExpenseCode = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                      Creds.CredentialDetails.SAPDBName,
                                                                      Creds.CredentialDetails.SAPDBuser,
                                                                      Creds.CredentialDetails.SAPDBPassword), checkExpenseCodeCommand);

                                                                    if (ExpenseCode != null && ExpenseCode.Rows.Count > 0)
                                                                    {
                                                                        json.AppendLine($"\"ExpenseCode\":\"{ExpenseCode.Rows[0][0].ToString()}\",");

                                                                    }
                                                                    else
                                                                    {
                                                                        json.AppendLine("\"ExpenseCode\":\"3\",");

                                                                    }


                                                                }
                                                                else
                                                                {

                                                                    json.AppendLine("\"ExpenseCode\":\"3\",");
                                                                }

                                                                json.AppendLine($@"""{JSON_DestinationField}"":""{fieldValue}""");

                                                                if (JsonBuilder.EntityName == "CreditNotes")
                                                                {
                                                                    json.AppendLine("   }, ");

                                                                }
                                                                else
                                                                {
                                                                    json.AppendLine("   } ");

                                                                }

                                                                if (JsonBuilder.EntityName == "Invoices")
                                                                {
                                                                    json.AppendLine("   ] ");

                                                                }
                                                                continue;
                                                            }//End of Other Income


                                                            if (fieldValue.Contains("\""))
                                                            {
                                                                fieldValue = fieldValue.Replace("\"", "\\\"");

                                                            }

                                                            if (fieldValue.Contains("\\"))
                                                            {
                                                                fieldValue = fieldValue.Replace("\\", "\\\\");

                                                            }

                                                            if (fieldValue.Contains("'") && !JSON_DestinationField.Contains("U_ExpnsType"))
                                                            {
                                                                fieldValue = fieldValue.Replace("'", "''");

                                                            }

                                                            break;

                                                    }

                                                    errorCode = 3004;
                                                    //if (JSON_Sourcefield != "BUSINESS_TAX" || JSON_Sourcefield != "DST" || JSON_Sourcefield != "OTHER_INCOME")
                                                    //{
                                                    json.AppendLine($@"""{JSON_DestinationField}"":""{fieldValue}"",");
                                                    //}

                                                }

                                                errorCode = 3005;


                                                if (JsonMainTable != "Header")
                                                {
                                                    if (JsonMainTable != "DocumentAdditionalExpenses" || Creds.CredentialDetails.Module.Equals("OINV") && JsonMainTable.Equals("DocumentAdditionalExpenses") || Creds.CredentialDetails.Module.Equals("ORIN") && JsonMainTable.Equals("DocumentAdditionalExpenses"))
                                                    {
                                                        json.AppendLine("    },");
                                                    }

                                                }


                                            }//end of  foreach (DataRow item in dt.Rows)

                                            json.Length--;
                                            if (JsonMainTable != "Header")
                                            {
                                                json.AppendLine($@"],");
                                            }
                                        }



                                        json.Length--;

                                        //POSTING!!!
                                        errorCode = 2001;
                                        var entityPost = filemodel.Where(sel => sel.DestinationTable.ToLower() == "header").Select(sel => sel.EntityName).FirstOrDefault();
                                        bool countExistz = false;
                                        string CardCode = "";
                                        errorCode = 2002;
                                        if (entityPost == "BusinessPartners")
                                        {
                                            errorCode = 2003;

                                            //string sapcon = ConfigurationManager.ConnectionStrings["SAPSAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAPSAOLinkBox"].ToString() : "";
                                            string sapcon = DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                    Creds.CredentialDetails.SAPDBName,
                                                                    Creds.CredentialDetails.SAPDBuser,
                                                                    Creds.CredentialDetails.SAPDBPassword);
                                            errorCode = 2004;
                                            using (SqlConnection connect = new SqlConnection(sapcon))
                                            {
                                                connect.Open();
                                                string fetchCardCode = $"SELECT CardCode FROM {Creds.CredentialDetails.Module} Where {Primakey} = '{primaryKeyValue}' and CardType = '{CardType}' ";
                                                SqlCommand cmd = new SqlCommand(fetchCardCode, connect);


                                                errorCode = 2005;
                                                using (SqlDataReader _reader = cmd.ExecuteReader())
                                                {
                                                    int columnCount = _reader.FieldCount;
                                                    if (_reader.Read())
                                                    {
                                                        for (int i = 0; i < _reader.FieldCount; i++)
                                                        {
                                                            errorCode = 2006;
                                                            CardCode = _reader[i].ToString();
                                                            countExistz = true;
                                                        }

                                                    }
                                                }

                                                connect.Close();
                                            }
                                        }
                                        errorCode = 2007;
                                        var result = "";
                                        errorCode = 2008;
                                        if (!countExistz)
                                        {

                                            JObject jObject = JObject.Parse($"{{{json.ToString()}}}");
                                            //Start of Post

                                            if (entityPost == "VendorPayments")
                                            {
                                                string transferSum = (string)jObject["TransferSum"];
                                                errorCode = 2009;
                                                if (transferSum != "" || transferSum != "0")
                                                {

                                                    jObject.Remove("PaymentChecks");
                                                }
                                            }


                                            // Check if the entity is "BusinessPartners"
                                            if (entityPost == "BusinessPartners")
                                            {
                                                JArray contactsArray = jObject["ContactEmployees"] as JArray;

                                                if (contactsArray != null)
                                                {
                                                    // Create a list to store unique names
                                                    List<string> uniqueNames = new List<string>();

                                                    // Create a new JArray to store unique contacts
                                                    JArray uniqueContacts = new JArray();

                                                    foreach (JObject contact in contactsArray)
                                                    {
                                                        string name = contact["Name"].ToString();

                                                        // Check if the name is unique
                                                        if (!uniqueNames.Contains(name))
                                                        {
                                                            uniqueNames.Add(name);
                                                            uniqueContacts.Add(contact);
                                                        }
                                                    }

                                                    // Replace the "ContactEmployees" array with the unique contacts
                                                    jObject["ContactEmployees"] = uniqueContacts;

                                                    // Convert the updated JObject back to a JSON string
                                                    //string updatedJsonString = jObject.ToString();

                                                    // Now, 'updatedJsonString' contains the updated JSON data
                                                }
                                            }


                                            if (entityPost == "CreditNotes" && CanCelDn || entityPost == "CreditNotes" && CancelNoDST && !ARCMDocEntry.Equals(""))
                                            {
                                                JArray jsonObject = jObject["DocumentLines"] as JArray;
                                                errorCode = 2024;
                                                if (jsonObject != null)
                                                {
                                                    foreach (var documentLine in jsonObject)
                                                    {
                                                        documentLine["BaseLine"] = 0;
                                                        documentLine["BaseType"] = 13;
                                                        documentLine["BaseEntry"] = ARCMDocEntry;
                                                    }
                                                }

                                                CanCelDn = false;
                                                CancelNoDST = false;

                                            }


                                            if (entityPost == "IncomingPayments")
                                            {
                                                errorCode = 2025;

                                                string DN_Date = (string)jObject["U_DN_Date"];

                                                JArray jsonObject = jObject["PaymentInvoices"] as JArray;

                                                errorCode = 5404;

                                                if (!string.IsNullOrEmpty(DN_Date))
                                                {

                                                    if (jObject["PaymentChecks"] != null)
                                                    {
                                                        jObject.Remove("PaymentChecks");
                                                    }

                                                    if (jObject["PaymentInvoices"] != null)
                                                    {
                                                        jObject.Remove("PaymentInvoices");
                                                    }

                                                }
                                                else
                                                {
                                                    string Expnstype = (string)jObject["U_ExpnsType"];
                                                    if (!Expnstype.ToLower().Contains("overpayment"))
                                                    {
                                                        if (jObject["PaymentInvoices"] != null)
                                                        {
                                                            errorCode = 5424;
                                                            foreach (var PaymentLine in jsonObject)
                                                            {
                                                                string DocEntryCred = PaymentLine["DocEntry"].ToString();
                                                                if (string.IsNullOrEmpty(DocEntryCred))
                                                                {
                                                                    List<string> DataValueList = new List<string>();
                                                                    DataValueList.Add(Pkey);
                                                                    PostingErrorMessage = "No Invoice Found";
                                                                    DataValueList.Add(PostingErrorMessage);
                                                                    AddData(ErrorListValue, DataValueList, PKValue);
                                                                    NoInvoice = true;
                                                                }

                                                            }

                                                        }
                                                        else
                                                        {

                                                            List<string> DataValueList = new List<string>();
                                                            DataValueList.Add(Pkey);
                                                            PostingErrorMessage = "No Payment Invoice Found";
                                                            DataValueList.Add(PostingErrorMessage);
                                                            AddData(ErrorListValue, DataValueList, PKValue);
                                                            NoInvoice = true;

                                                        }

                                                        //read rows if there is invoice
                                                        //if invoice null return error
                                                    }

                                                }

                                                if (NoInvoice)
                                                {
                                                    string checklogs = $"'{primaryKeyValue}'";

                                                    var existingLog = _context.SystemLogs.FirstOrDefault(log =>
                                                        log.Module == checklogs &&
                                                        log.ErrorMsg == PostingErrorMessage);

                                                    if (existingLog == null)
                                                    {
                                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                                        {
                                                            Task = $@"UPLOAD_ERROR",
                                                            CreateDate = DateTime.Now
                                                            ,
                                                            ApiUrl = $@"POST {Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/{entityPost}"
                                                            ,
                                                            Json = json.ToString()
                                                            ,
                                                            ErrorMsg = $"{PostingErrorMessage}",//result.ToString(),
                                                            Database = Creds.CredentialDetails.SAPDBName.ToString(),
                                                            Module = $@"{checklogs}"
                                                            ,
                                                            Table = $@"{Creds.CredentialDetails.Module}"
                                                        });
                                                        _context.SaveChanges();
                                                    }

                                                    continue;
                                                }

                                                string DocCurrency = (string)jObject["DocCurrency"];
                                                //string U_FXRate = (string)jObject["U_FX_Rate"];
                                                string _USDDocrate = (string)jObject["DocRate"];
                                                string ExDocDate = (string)jObject["DocDate"];
                                                string PaymentSequence = (string)jObject["U_Payment_Sequence"];


                                                errorCode = 5501;

                                                if (jsonObject != null && string.IsNullOrEmpty(DN_Date))
                                                {
                                                    foreach (var PaymentLine in jsonObject)
                                                    {
                                                        double SumApplied = (double)PaymentLine["SumApplied"];
                                                        string DocEntryCred = PaymentLine["DocEntry"].ToString();

                                                        authPost.Action = $@"Invoices({DocEntryCred})?$select=DocTotalSys,DocCurrency,DocTotalFc,PaidToDateFC,PaidToDateSys,PaidToDate,DocRate";
                                                        authPost.JsonString = "{}";
                                                        authPost.Method = "GET";
                                                        sapAces.SaveCredentials(authPost);
                                                        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                        string jsonReturn = sapAces.SendSLData(authPost);

                                                        errorCode = 6022;
                                                        if (PostingHelpers.IsValidJson(jsonReturn) && !string.IsNullOrEmpty(DocEntryCred))
                                                        {
                                                            errorCode = 5520;
                                                            JObject InvoiceObject = JObject.Parse($"{jsonReturn.ToString()}");
                                                            errorCode = 5522;
                                                            string InvoiceCurrency = (string)InvoiceObject["DocCurrency"].ToString();
                                                            errorCode = 5524;
                                                            double DocTotalSys = (double)InvoiceObject["DocTotalSys"];
                                                            errorCode = 5526;
                                                            double DocTotalFC = (double)InvoiceObject["DocTotalFc"];
                                                            errorCode = 5529;
                                                            double PaidToDateFC = (double)InvoiceObject["PaidToDateFC"];
                                                            errorCode = 5530;
                                                            double PaidToDateSys = (double)InvoiceObject["PaidToDateSys"];
                                                            errorCode = 5532;
                                                            double PaidToDate = (double)InvoiceObject["PaidToDate"];
                                                            errorCode = 5534;
                                                            double Invoice_Docrate = (double)InvoiceObject["DocRate"];

                                                            string DocRate = "";
                                                            if (InvoiceCurrency != DocCurrency)
                                                            {
                                                                errorCode = 5540;
                                                                if (!PaymentSequence.Equals("FULL"))
                                                                {
                                                                    errorCode = 5543;
                                                                    if (InvoiceCurrency.Equals("USD") && DocCurrency.Equals("PHP") || InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("PHP"))
                                                                    {
                                                                        authPost.Method = "POST";
                                                                        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                                        authPost.JsonString = $"{{\r\n    \"Currency\": \"{InvoiceCurrency}\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                                        sapAces.SaveCredentials(authPost);
                                                                        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                                        DocRate = sapAces.SendSLData(authPost);

                                                                        errorCode = 5553;
                                                                        PaymentLine["AppliedFC"] = Math.Round(SumApplied / double.Parse(DocRate), 2);
                                                                    }

                                                                    if (InvoiceCurrency.Equals("PHP") && DocCurrency.Equals("USD") || InvoiceCurrency.Equals("PHP") && DocCurrency.Equals("EUR"))
                                                                    {
                                                                        errorCode = 5559;
                                                                        PaymentLine["AppliedFC"] = SumApplied;

                                                                        errorCode = 5562;
                                                                        PaymentLine["SumApplied"] = SumApplied * double.Parse(_USDDocrate);

                                                                    }

                                                                    if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                                    {
                                                                        errorCode = 5569;
                                                                        var EuroPeso = SumApplied * double.Parse(_USDDocrate);
                                                                        var EuroDocRate = "";
                                                                        authPost.Method = "POST";
                                                                        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                                        authPost.JsonString = $"{{\r\n    \"Currency\": \"EUR\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                                        sapAces.SaveCredentials(authPost);
                                                                        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                                        errorCode = 5577;
                                                                        EuroDocRate = sapAces.SendSLData(authPost);

                                                                        PaymentLine["AppliedFC"] = EuroPeso / double.Parse(EuroDocRate);

                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    errorCode = 5587;
                                                                    if (InvoiceCurrency.Equals("USD") && DocCurrency.Equals("PHP") || InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("PHP"))
                                                                    {
                                                                        errorCode = 5585;
                                                                        authPost.Method = "POST";
                                                                        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                                        authPost.JsonString = $"{{\r\n    \"Currency\": \"{InvoiceCurrency}\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                                        sapAces.SaveCredentials(authPost);
                                                                        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                                        DocRate = sapAces.SendSLData(authPost);
                                                                        errorCode = 5596;
                                                                        PaymentLine["AppliedFC"] = DocTotalFC - PaidToDateFC;
                                                                    }


                                                                    if (InvoiceCurrency.Equals("PHP") && DocCurrency.Equals("USD") || InvoiceCurrency.Equals("PHP") && DocCurrency.Equals("EUR"))
                                                                    {
                                                                        errorCode = 5603;
                                                                        PaymentLine["AppliedFC"] = SumApplied;

                                                                        PaymentLine["SumApplied"] = DocTotalSys - PaidToDateSys;

                                                                    }

                                                                    if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                                    {
                                                                        errorCode = 5612;
                                                                        PaymentLine["AppliedFC"] = DocTotalFC - PaidToDateFC;
                                                                    }

                                                                }

                                                            }


                                                            if (InvoiceCurrency.Equals("USD") && DocCurrency.Equals("USD"))
                                                            {
                                                                errorCode = 5626;
                                                                if (!PaymentSequence.Equals("FULL"))
                                                                {
                                                                    PaymentLine["AppliedFC"] = SumApplied;
                                                                }
                                                                else
                                                                {
                                                                    PaymentLine["AppliedFC"] = DocTotalFC - PaidToDateFC;
                                                                }
                                                            }

                                                            if (InvoiceCurrency.Equals("PHP") && DocCurrency.Equals("PHP"))
                                                            {
                                                                errorCode = 5639;
                                                                if (PaymentSequence.Equals("FULL"))
                                                                {
                                                                    PaymentLine["SumApplied"] = DocTotalSys - PaidToDateSys;
                                                                }

                                                            }


                                                            #region
                                                            //if (PaymentSequence.Equals("FULL"))
                                                            //{
                                                            //    string DocRate = "";
                                                            //    string LocalRate = "";
                                                            //    if (InvoiceCurrency.Equals("PHP"))
                                                            //    {
                                                            //        authPost.Method = "POST";
                                                            //        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //        authPost.JsonString = $"{{\r\n    \"Currency\": \"USD\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //        sapAces.SaveCredentials(authPost);
                                                            //        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //        DocRate = sapAces.SendSLData(authPost);

                                                            //    }
                                                            //    else
                                                            //    {
                                                            //        authPost.Method = "POST";
                                                            //        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //        authPost.JsonString = $"{{\r\n    \"Currency\": \"{InvoiceCurrency}\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //        sapAces.SaveCredentials(authPost);
                                                            //        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //        DocRate = sapAces.SendSLData(authPost);

                                                            //        if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //        {
                                                            //            authPost.Method = "POST";
                                                            //            authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //            authPost.JsonString = $"{{\r\n    \"Currency\": \"USD\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //            sapAces.SaveCredentials(authPost);
                                                            //            SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //            LocalRate = sapAces.SendSLData(authPost);
                                                            //        }
                                                            //    }


                                                            //    if (InvoiceCurrency != DocCurrency)
                                                            //    {
                                                            //        double Amountapplied_CurrentRate = 0;
                                                            //        double AppliedForeignCurrency = 0;
                                                            //        double PaymentOnAccount = 0;
                                                            //        AppliedForeignCurrencyAmount = DocTotalFC;
                                                            //        double Trans_Amount = 0;
                                                            //        double Transaction_Balance = 0;

                                                            //        if (DocCurrency.Equals("PHP"))
                                                            //        {
                                                            //            Amountapplied_CurrentRate = SumApplied;

                                                            //            if (InvoiceCurrency.Equals("PHP"))
                                                            //            {
                                                            //                Transaction_Balance = DocTotalSys - PaidToDateSys;
                                                            //                AppliedForeignCurrency = SumApplied;
                                                            //                Trans_Amount = SumApplied;

                                                            //                PaymentOnAccount = (AppliedForeignCurrency - Trans_Amount) * -1; //* double.Parse(DocRate) * -1;

                                                            //            }
                                                            //            else
                                                            //            {
                                                            //                Transaction_Balance = DocTotalFC - PaidToDateFC;
                                                            //                AppliedForeignCurrency = Amountapplied_CurrentRate / double.Parse(DocRate);//change to U_FX instead of Docrate in sAP
                                                            //                //Trans_Amount = SumApplied / double.Parse(U_FXRate);
                                                            //                //Trans_Amount = (DocTotalSys - PaidToDateSys) / double.Parse(DocRate);

                                                            //                PaymentOnAccount = (AppliedForeignCurrency - Transaction_Balance) * double.Parse(DocRate) * -1;

                                                            //            }


                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //            {
                                                            //                Amountapplied_CurrentRate = SumApplied * double.Parse(LocalRate);

                                                            //            }
                                                            //            else
                                                            //            {

                                                            //                 Amountapplied_CurrentRate = SumApplied * double.Parse(DocRate);
                                                            //            }


                                                            //            if (InvoiceCurrency.Equals("PHP"))
                                                            //            {
                                                            //                Transaction_Balance = DocTotalSys - PaidToDateSys;

                                                            //                AppliedForeignCurrency = Transaction_Balance;

                                                            //                //double ForeignCurrency_local = SumApplied * double.Parse(_USDDocrate);
                                                            //                //Trans_Amount = SumApplied;

                                                            //                PaymentOnAccount = ((Amountapplied_CurrentRate - Transaction_Balance) / double.Parse(DocRate)) * -1; //* double.Parse(DocRate) * -1;

                                                            //            }
                                                            //            else
                                                            //            {

                                                            //                if(InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //                {
                                                            //                    Transaction_Balance = DocTotalFC - PaidToDateFC;

                                                            //                    //AppliedForeignCurrency = Amountapplied_CurrentRate / double.Parse(DocRate);

                                                            //                    AppliedForeignCurrency = Transaction_Balance * double.Parse(DocRate)/ double.Parse(_USDDocrate);
                                                            //                    PaymentOnAccount = (SumApplied - AppliedForeignCurrency) * -1;
                                                            //                    //PaymentOnAccount = ((AppliedForeignCurrency - Transaction_Balance) * double.Parse(DocRate) / double.Parse(DocRate)) * -1;

                                                            //                }
                                                            //                else
                                                            //                {

                                                            //                    Transaction_Balance = DocTotalFC - PaidToDateFC;
                                                            //                    AppliedForeignCurrency = Amountapplied_CurrentRate / double.Parse(DocRate);//change to U_FX instead of Docrate in sAP
                                                            //                                                                                               //Trans_Amount = SumApplied / double.Parse(U_FXRate);
                                                            //                                                                                               //Trans_Amount = (DocTotalSys - PaidToDateSys) / double.Parse(DocRate);

                                                            //                    PaymentOnAccount = (AppliedForeignCurrency - Transaction_Balance) * double.Parse(DocRate) * -1;
                                                            //                }


                                                            //            }

                                                            //            //PaymentOnAccount = (AppliedForeignCurrency - DocTotalFC) * double.Parse(DocRate)* -1;
                                                            //        }

                                                            //        if (InvoiceCurrency.Equals("PHP"))
                                                            //        {
                                                            //            PaymentLine["SumApplied"] = DocTotalSys - PaidToDateSys;
                                                            //            //PaymentLine["SumApplied"] = SumApplied * double.Parse(_USDDocrate); 

                                                            //        }
                                                            //        else if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //        {
                                                            //            PaymentLine["AppliedFC"] = DocTotalFC - PaidToDateFC;
                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            PaymentLine["AppliedFC"] = DocTotalFC - PaidToDateFC;
                                                            //            //PaymentLine["AppliedFC"] = SumApplied/double.Parse(U_FXRate);

                                                            //        }


                                                            //        //int currentYear = DateTime.Now.Year;
                                                            //        //JArray paymentCreditCardsArray = new JArray();
                                                            //        //if (PaymentOnAccount != 0)
                                                            //        //{
                                                            //        //    JObject CreditCard = new JObject();
                                                            //        //    CreditCard["LineNum"] = 0;
                                                            //        //    CreditCard["CreditAcct"] = "402008";
                                                            //        //    CreditCard["CreditCard"] = 3;
                                                            //        //    CreditCard["CreditCardNumber"] = "1";
                                                            //        //    CreditCard["CardValidUntil"] = $"{currentYear + 6}-12-31";
                                                            //        //    CreditCard["VoucherNum"] = "1";
                                                            //        //    CreditCard["PaymentMethodCode"] = 1;
                                                            //        //    CreditCard["CreditSum"] = PaymentOnAccount;

                                                            //        //    paymentCreditCardsArray.Add(CreditCard);
                                                            //        //    jObject["PaymentCreditCards"] = paymentCreditCardsArray;

                                                            //        //}

                                                            //    }
                                                            //}//end of FULL PAYMENT condition
                                                            //else
                                                            //{//Start of Partial
                                                            //    string DN_CurrencyRate = "";
                                                            //    string LocalRate = "";
                                                            //    if (InvoiceCurrency.Equals("PHP"))
                                                            //    {
                                                            //        authPost.Method = "POST";
                                                            //        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //        authPost.JsonString = $"{{\r\n    \"Currency\": \"USD\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //        sapAces.SaveCredentials(authPost);
                                                            //        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //        DN_CurrencyRate = sapAces.SendSLData(authPost);

                                                            //    }
                                                            //    else
                                                            //    {
                                                            //        authPost.Method = "POST";
                                                            //        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //        authPost.JsonString = $"{{\r\n    \"Currency\": \"{InvoiceCurrency}\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //        sapAces.SaveCredentials(authPost);
                                                            //        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //        DN_CurrencyRate = sapAces.SendSLData(authPost);

                                                            //        if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //        {
                                                            //            authPost.Method = "POST";
                                                            //            authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //            authPost.JsonString = $"{{\r\n    \"Currency\": \"USD\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //            sapAces.SaveCredentials(authPost);
                                                            //            SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //            LocalRate = sapAces.SendSLData(authPost);
                                                            //        }

                                                            //    }


                                                            //    double actualamount_received = 0;
                                                            //    double SummApplied_currentRate = 0;


                                                            //    double SummAplied_basedOnTemplate = 0;
                                                            //    double EURtoUSD_PArtial = 0;
                                                            //    double DN_OutstandingBalance = 0;

                                                            //    double AmountApplied_CurrentRate = 0;

                                                            //    double AR_AmountApplied = 0;
                                                            //    if (DocCurrency.Equals("PHP"))
                                                            //    {
                                                            //        actualamount_received = SumApplied;

                                                            //        if (InvoiceCurrency.Equals("PHP"))
                                                            //        {
                                                            //            SummApplied_currentRate = actualamount_received;
                                                            //            SummAplied_basedOnTemplate = SummApplied_currentRate;

                                                            //            DN_OutstandingBalance = SummAplied_basedOnTemplate;
                                                            //            AmountApplied_CurrentRate = SummAplied_basedOnTemplate;
                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            SummApplied_currentRate = actualamount_received / double.Parse(DN_CurrencyRate);
                                                            //            SummAplied_basedOnTemplate = SumApplied / double.Parse(U_FXRate);

                                                            //            DN_OutstandingBalance = SummAplied_basedOnTemplate * Invoice_Docrate;
                                                            //            if(InvoiceCurrency == DocCurrency)
                                                            //            {
                                                            //                AmountApplied_CurrentRate = SummAplied_basedOnTemplate * double.Parse(DN_CurrencyRate);
                                                            //            }
                                                            //            else
                                                            //            {
                                                            //                if(InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //                {
                                                            //                    authPost.Method = "POST";
                                                            //                    authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //                    authPost.JsonString = $"{{\r\n    \"Currency\": \"USD\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //                    sapAces.SaveCredentials(authPost);
                                                            //                    SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //                    string LocalCurrency = sapAces.SendSLData(authPost);

                                                            //                    AmountApplied_CurrentRate = SummAplied_basedOnTemplate * double.Parse(LocalCurrency);

                                                            //                }
                                                            //                else
                                                            //                {
                                                            //                    AmountApplied_CurrentRate = Math.Round(SummAplied_basedOnTemplate,2) * double.Parse(DN_CurrencyRate);

                                                            //                }

                                                            //            }



                                                            //        }
                                                            //    }
                                                            //    else
                                                            //    {

                                                            //        if(InvoiceCurrency!="PHP" && DocCurrency != "PHP")
                                                            //        {

                                                            //            if(InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //            {
                                                            //                actualamount_received = SumApplied * double.Parse(_USDDocrate);


                                                            //            }
                                                            //            else
                                                            //            {
                                                            //                actualamount_received = SumApplied * double.Parse(U_FXRate);

                                                            //            }

                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            actualamount_received = SumApplied * double.Parse(DN_CurrencyRate);
                                                            //        }


                                                            //        if (InvoiceCurrency.Equals("PHP"))
                                                            //        {
                                                            //            SummApplied_currentRate = actualamount_received;
                                                            //            SummAplied_basedOnTemplate = SummApplied_currentRate;

                                                            //        }

                                                            //        else
                                                            //        {
                                                            //            if(InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //            {
                                                            //                SummApplied_currentRate = actualamount_received / double.Parse(DN_CurrencyRate);

                                                            //                AR_AmountApplied = SummApplied_currentRate * Invoice_Docrate;


                                                            //                //double Trans_Balance = DocTotalFC - PaidToDateFC;
                                                            //                //EURtoUSD_PArtial = (SummApplied_currentRate - Trans_Balance) * -1;
                                                            //                //EURtoUSD_PArtial = USD_DN * double.Parse(LocalRate);
                                                            //            }
                                                            //            else
                                                            //            {

                                                            //                SummApplied_currentRate = actualamount_received / double.Parse(DN_CurrencyRate);

                                                            //                SummAplied_basedOnTemplate = SumApplied / double.Parse(U_FXRate);
                                                            //            }


                                                            //        }

                                                            //    }//end of else master

                                                            //    double ForeignFcPArtial = 0;
                                                            //    if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //    {
                                                            //        //ForeignFcPArtial = (actualamount_received - AR_AmountApplied) / double.Parse(_USDDocrate);
                                                            //    }
                                                            //    else
                                                            //    {
                                                            //         ForeignFcPArtial = AmountApplied_CurrentRate - DN_OutstandingBalance;

                                                            //    }


                                                            //    if (InvoiceCurrency.Equals("PHP"))
                                                            //    {
                                                            //        PaymentLine["SumApplied"] = SummAplied_basedOnTemplate;

                                                            //    }
                                                            //    else if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //    {
                                                            //        PaymentLine["AppliedFC"] = SummApplied_currentRate;

                                                            //    }
                                                            //    else
                                                            //    {
                                                            //        PaymentLine["AppliedFC"] = SummAplied_basedOnTemplate;

                                                            //    }


                                                            //    int currentYear = DateTime.Now.Year;
                                                            //    JArray paymentCreditCardsArray = new JArray();
                                                            //    if (ForeignFcPArtial != 0)
                                                            //    {
                                                            //        JObject CreditCard = new JObject();
                                                            //        CreditCard["LineNum"] = 0;
                                                            //        CreditCard["CreditAcct"] = "402008";
                                                            //        CreditCard["CreditCard"] = 3;
                                                            //        CreditCard["CreditCardNumber"] = "1";
                                                            //        CreditCard["CardValidUntil"] = $"{currentYear + 6}-12-31";
                                                            //        CreditCard["VoucherNum"] = "1";
                                                            //        CreditCard["PaymentMethodCode"] = 1;
                                                            //        CreditCard["CreditSum"] = ForeignFcPArtial;

                                                            //        //if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //        //{
                                                            //        //    CreditCard["CreditSum"] = ForeignFcPArtial;

                                                            //        //}
                                                            //        //else
                                                            //        //{
                                                            //        //    CreditCard["CreditSum"] = ForeignFcPArtial;

                                                            //        //}

                                                            //        paymentCreditCardsArray.Add(CreditCard);
                                                            //        jObject["PaymentCreditCards"] = paymentCreditCardsArray;

                                                            //    }
                                                            //}




                                                            //if (InvoiceCurrency.Equals("USD") && DocCurrency.Equals("PHP") || InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("PHP"))
                                                            //    {
                                                            //        authPost.Method = "POST";
                                                            //        authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //        authPost.JsonString = $"{{\r\n    \"Currency\": \"{InvoiceCurrency}\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //        sapAces.SaveCredentials(authPost);
                                                            //        SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //        string DocRate = sapAces.SendSLData(authPost);

                                                            //        double AppliedFc = SumApplied / double.Parse(U_FXRate);

                                                            //        double CreditCard_AmountDue = 0;

                                                            //        double MultiplyToDocRate = 0;
                                                            //        double DocTotalMinusPaidToDateDivideDocRate = 0;

                                                            //        double PaidToDateFCmultiplyDocRate = 0;

                                                            //        double TotalMinusPaid = 0;
                                                            //        if (PaymentSequence.Equals("FULL"))
                                                            //        {
                                                            //            //if(PaidToDate != PaidToDateSys)
                                                            //            //  {

                                                            //            //double DocTotalMinusPaidToDate = Math.Abs(DocTotalSys - PaidToDateSys);

                                                            //            // PaidToDateFCmultiplyDocRate = PaidToDateFC * Invoice_Docrate;

                                                            //            //DocTotalMinusPaidToDateDivideDocRate = Math.Round(DocTotalMinusPaidToDate / double.Parse(DocRate), 4);

                                                            //                TotalMinusPaid = DocTotalFC - PaidToDateFC; 
                                                            //                double AppliedFcMinusDPD = AppliedFc - TotalMinusPaid;

                                                            //                //double AppliedFcMinusDPD = AppliedFc - DocTotalMinusPaidToDateDivideDocRate;

                                                            //                MultiplyToDocRate = AppliedFcMinusDPD * double.Parse(DocRate);

                                                            //                if (MultiplyToDocRate < 0)
                                                            //                {
                                                            //                    MultiplyToDocRate = MultiplyToDocRate * -1;

                                                            //                }
                                                            //                else
                                                            //                {
                                                            //                    MultiplyToDocRate = MultiplyToDocRate * -1;
                                                            //                }
                                                            //           // }

                                                            //        }


                                                            //        CreditCard_AmountDue = (SumApplied - ((SumApplied / double.Parse(U_FXRate)) * double.Parse(DocRate))) * -1;


                                                            //        //if (PaidToDateFCmultiplyDocRate == PaidToDateSys && PaidToDateSys!=0)
                                                            //        //{
                                                            //        //    MultiplyToDocRate = MultiplyToDocRate + CreditCard_AmountDue;
                                                            //        //}

                                                            //        double Round4 = Math.Round(SumApplied / double.Parse(DocRate), 4);


                                                            //        if (AppliedFc < TotalMinusPaid && PaymentSequence.Equals("FULL"))
                                                            //        {
                                                            //            PaymentLine["AppliedFC"] = TotalMinusPaid;

                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            PaymentLine["AppliedFC"] = AppliedFc;
                                                            //        }

                                                            //        int currentYear = DateTime.Now.Year;
                                                            //         JArray paymentCreditCardsArray = new JArray();
                                                            //        if (CreditCard_AmountDue != 0)
                                                            //        {
                                                            //            JObject CreditCard = new JObject();
                                                            //            CreditCard["LineNum"] = 0;
                                                            //            CreditCard["CreditAcct"] = "402004";
                                                            //            CreditCard["CreditCard"] = 3;
                                                            //            CreditCard["CreditCardNumber"] = "1";
                                                            //            CreditCard["CardValidUntil"] = $"{currentYear + 6}-12-31";
                                                            //            CreditCard["VoucherNum"] = "1";
                                                            //            CreditCard["PaymentMethodCode"] = 1;
                                                            //            CreditCard["CreditSum"] = CreditCard_AmountDue + MultiplyToDocRate;

                                                            //            paymentCreditCardsArray.Add(CreditCard);

                                                            //        }



                                                            //        if (paymentCreditCardsArray != null)
                                                            //        {
                                                            //            jObject["PaymentCreditCards"] = paymentCreditCardsArray;

                                                            //        }

                                                            //        errorCode = 6023;
                                                            //        //double AppliedFc = SumApplied / double.Parse(U_FXRate);

                                                            //        //AppliedFc = Math.Round(AppliedFc, 2);


                                                            //    }

                                                            //    if (InvoiceCurrency.Equals("PHP") && DocCurrency.Equals("USD"))
                                                            //    {
                                                            //        if (PaymentSequence == "FULL")
                                                            //        {

                                                            //            PaymentLine["AppliedFC"] = PaymentLine["SumApplied"];

                                                            //            double res = double.Parse(PaymentLine["AppliedFC"].ToString()) * double.Parse(_USDDocrate);

                                                            //            authPost.Method = "POST";
                                                            //            authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //            authPost.JsonString = $"{{\r\n    \"Currency\": \"USD\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //            sapAces.SaveCredentials(authPost);
                                                            //            SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //            string DocRate = sapAces.SendSLData(authPost);

                                                            //            var RealizedGainedorLoss = (DocTotalSys - res)/double.Parse(DocRate);

                                                            //            PaymentLine["SumApplied"] = Math.Round(res,2);

                                                            //            int currentYear = DateTime.Now.Year;
                                                            //            JArray paymentCreditCardsArray = new JArray();
                                                            //            if (RealizedGainedorLoss != 0)
                                                            //            {
                                                            //                JObject CreditCard = new JObject();
                                                            //                CreditCard["LineNum"] = 0;
                                                            //                CreditCard["CreditAcct"] = "402004";
                                                            //                CreditCard["CreditCard"] = 3;
                                                            //                CreditCard["CreditCardNumber"] = "1";
                                                            //                CreditCard["CardValidUntil"] = $"{currentYear + 6}-12-31";
                                                            //                CreditCard["VoucherNum"] = "1";
                                                            //                CreditCard["PaymentMethodCode"] = 1;
                                                            //                CreditCard["CreditSum"] = RealizedGainedorLoss;

                                                            //                paymentCreditCardsArray.Add(CreditCard);
                                                            //                jObject["PaymentCreditCards"] = paymentCreditCardsArray;

                                                            //            }
                                                            //        }
                                                            //        else
                                                            //        {
                                                            //            PaymentLine["AppliedFC"] = PaymentLine["SumApplied"];
                                                            //            double res = double.Parse(PaymentLine["AppliedFC"].ToString()) * double.Parse(_USDDocrate);
                                                            //            res = Math.Round(res, 2);
                                                            //            PaymentLine["SumApplied"] = res.ToString();
                                                            //        }

                                                            //    }



                                                            //    if (InvoiceCurrency.Equals("USD") && DocCurrency.Equals("USD"))
                                                            //    {
                                                            //        if (DocTotalFC > SumApplied)
                                                            //        {
                                                            //            PaymentLine["AppliedFC"] = SumApplied;
                                                            //        }
                                                            //    }


                                                            //    if (InvoiceCurrency.Equals("EUR") && DocCurrency.Equals("USD"))
                                                            //    {
                                                            //        if (PaymentSequence.Equals("FULL"))
                                                            //        {
                                                            //            authPost.Method = "POST";
                                                            //            authPost.Action = $@"SBOBobService_GetCurrencyRate";
                                                            //            authPost.JsonString = $"{{\r\n    \"Currency\": \"USD\",\r\n    \"Date\": \"{ExDocDate}\"\r\n}}";
                                                            //            sapAces.SaveCredentials(authPost);
                                                            //            SAPAccess.LoginAction(); ////LOGIN TO DESTINATION TABLE
                                                            //            string DocRate = sapAces.SendSLData(authPost);


                                                            //            double USDtoPHP = SumApplied * double.Parse(_USDDocrate);

                                                            //            double RealizedeGainedOrLoss = (DocTotalSys - USDtoPHP) / double.Parse(DocRate);


                                                            //            int currentYear = DateTime.Now.Year;
                                                            //            JArray paymentCreditCardsArray = new JArray();
                                                            //            if (RealizedeGainedOrLoss != 0)
                                                            //            {
                                                            //                JObject CreditCard = new JObject();
                                                            //                CreditCard["LineNum"] = 0;
                                                            //                CreditCard["CreditAcct"] = "402004";
                                                            //                CreditCard["CreditCard"] = 3;
                                                            //                CreditCard["CreditCardNumber"] = "1";
                                                            //                CreditCard["CardValidUntil"] = $"{currentYear + 6}-12-31";
                                                            //                CreditCard["VoucherNum"] = "1";
                                                            //                CreditCard["PaymentMethodCode"] = 1;
                                                            //                CreditCard["CreditSum"] = RealizedeGainedOrLoss;

                                                            //                paymentCreditCardsArray.Add(CreditCard);
                                                            //                jObject["PaymentCreditCards"] = paymentCreditCardsArray;

                                                            //            }

                                                            //        }
                                                            //    }
                                                            #endregion



                                                        }//end of Posting Helpers.isValie Json


                                                    }//end of forEach Loop

                                                }//if jsonobject null


                                                string U_ExpenseType = (string)jObject["U_ExpnsType"];

                                                if (U_ExpenseType.ToLower() == "overpayment")
                                                {
                                                    string _CardCode = (string)jObject["CardCode"];
                                                    string _CardName = (string)jObject["CardName"];
                                                    double _TrfsSum = (double)jObject["TransferSum"];
                                                    string _CounterRef = (string)jObject["CounterReference"];
                                                    _CounterRef = _CounterRef.Substring(0, _CounterRef.Length - 3);


                                                    jObject["DocType"] = "rAccount";
                                                    jObject["U_bpcode"] = _CardName;
                                                    jObject["U_Code"] = _CardCode;
                                                    jObject["CounterReference"] = _CounterRef.ToString();

                                                    JArray paymentAccounts = new JArray();
                                                    JObject paymentAccount = new JObject();
                                                    paymentAccount["LineNum"] = 0;
                                                    paymentAccount["AccountCode"] = "200101";
                                                    paymentAccount["SumPaid"] = _TrfsSum;
                                                    paymentAccounts.Add(paymentAccount);

                                                    // Add PaymentAccounts subtable to the main JSON object
                                                    jObject["PaymentAccounts"] = paymentAccounts;

                                                    //sJson = jObject.ToString(Formatting.Indented);
                                                }//end of Overpayment logic

                                            }//end of Incoming Payments


                                            string newJSONReplacement = jObject.ToString(Formatting.Indented);
                                            StringBuilder stringBuilder = new StringBuilder(newJSONReplacement);

                                            json = stringBuilder;
                                            errorCode = 200911;


                                            result = phelper.Posting5Automation("POST", $@"{entityPost}", newJSONReplacement, "", Creds, CardCode, IPDocEntry, IP_toBePosted, OPDocEntry);

                                            errorCode = 20012;
                                        }
                                        else
                                        {
                                            JObject jObject = JObject.Parse($"{{{json.ToString()}}}");
                                            errorCode = 20010;


                                            if (jObject.ContainsKey("BPAddresses") && jObject["BPAddresses"] is JArray bpAddressesArray)
                                            {

                                                foreach (JObject bpAddress in bpAddressesArray)
                                                {
                                                    var name = bpAddress["AddressName"];
                                                    var BPAddressCmd = $"select LineNum from CRD1 where CardCode='{CardCode}' and Address='{name}'";

                                                    var dtResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                    Creds.CredentialDetails.SAPDBName,
                                                                    Creds.CredentialDetails.SAPDBuser,
                                                                    Creds.CredentialDetails.SAPDBPassword), BPAddressCmd);

                                                    if (dtResult != null && dtResult.Rows.Count > 0)
                                                    {
                                                        JProperty BPCode = new JProperty("BPCode", CardCode);
                                                        JProperty RowNum = new JProperty("RowNum", dtResult.Rows[0][0].ToString());

                                                        bpAddress.Add(BPCode);
                                                        bpAddress.Add(RowNum);
                                                    }



                                                }



                                            }


                                            JArray contactsArray = jObject["ContactEmployees"] as JArray;

                                            if (contactsArray != null)
                                            {
                                                // Create a list to store unique names
                                                List<string> uniqueNames = new List<string>();

                                                // Create a new JArray to store unique contacts
                                                JArray uniqueContacts = new JArray();

                                                foreach (JObject contact in contactsArray)
                                                {
                                                    string name = contact["Name"].ToString();

                                                    // Check if the name is unique
                                                    if (!uniqueNames.Contains(name))
                                                    {
                                                        uniqueNames.Add(name);
                                                        uniqueContacts.Add(contact);
                                                    }
                                                }

                                                // Replace the "ContactEmployees" array with the unique contacts
                                                jObject["ContactEmployees"] = uniqueContacts;
                                            }



                                            if (jObject.ContainsKey("ContactEmployees") && jObject["ContactEmployees"] is JArray contactEmployeesArray)
                                            {


                                                List<string> names = new List<string>();

                                                foreach (JObject contactEmployee in contactEmployeesArray)
                                                {
                                                    string name = (string)contactEmployee["Name"];
                                                    names.Add(name);
                                                }

                                                // Now 'names' contains the extracted "Name" values
                                                foreach (string name in names)
                                                {
                                                    var BPContactcommand = $"select CntctCode from OCPR where CardCode='{CardCode}' and Name = '{name}'";

                                                    var dtResult = DataAccess.Select(DataAccess.GetConnectionString(Creds.CredentialDetails.SAPIPAddress,
                                                                    Creds.CredentialDetails.SAPDBName,
                                                                    Creds.CredentialDetails.SAPDBuser,
                                                                    Creds.CredentialDetails.SAPDBPassword), BPContactcommand);

                                                    if (dtResult != null && dtResult.Rows.Count > 0)
                                                    {
                                                        string internalCodeValue = dtResult.Rows[0]["CntctCode"].ToString(); // Adjust the column name as needed
                                                        JProperty bpInternalCodeProperty = new JProperty("InternalCode", internalCodeValue);

                                                        foreach (JObject contactEmployee in contactEmployeesArray)
                                                        {
                                                            contactEmployee.Add(bpInternalCodeProperty);

                                                        }
                                                    }

                                                }
                                            }

                                            if (jObject.ContainsKey("BPWithholdingTaxCollection") && jObject["BPWithholdingTaxCollection"] is JArray bpWTArray)
                                            {
                                                foreach (JObject WTArray in bpWTArray)
                                                {
                                                    JProperty BPCode = new JProperty("BPCode", CardCode);
                                                    WTArray.Add(BPCode);

                                                }

                                            }


                                            string patchnewJSONReplacement = jObject.ToString(Formatting.Indented);


                                            StringBuilder stringBuilder = new StringBuilder(patchnewJSONReplacement);

                                            json = stringBuilder;
                                            //Start of Patch
                                            result = phelper.Posting5Automation("PATCH", $@"{entityPost}", patchnewJSONReplacement, "", Creds, CardCode, IPDocEntry, IP_toBePosted, OPDocEntry);

                                        }

                                        bool IsUploaded = false;
                                        errorCode = 200913;
                                        string con = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                                        // Parse the connection string to get the database name
                                        var builder = new SqlConnectionStringBuilder(con);
                                        string databaseName = builder.InitialCatalog;

                                        int MapId = Creds.CredentialDetails.MapId;

                                        if (result.ToLower().Contains($@"""error"" :") || result.ToLower().Contains($@"error :") || result.ToLower().Contains("error:"))
                                        {


                                            errorCode = 200914;
                                            JObject jObject = JObject.Parse(result);
                                            string value = jObject["error"]["message"]["value"].ToString();
                                            PostingErrorMessage = value;
                                            errorCode = 200915;

                                            var checkMod = $"'{primaryKeyValue}'";
                                            //Check if the SystemLog entry exists based on Module and ErrorMsg
                                            var existingLog = _context.SystemLogs.FirstOrDefault(log =>
                                                log.Module == checkMod &&
                                                log.ErrorMsg == value);

                                            if (existingLog == null)
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                                {
                                                    Task = $@"UPLOAD_ERROR",
                                                    CreateDate = DateTime.Now
                                                    ,
                                                    ApiUrl = $@"{(!countExistz ? "POST" : "PATCH")} {Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/{entityPost}"
                                                    ,
                                                    Json = json.ToString()
                                                    ,
                                                    ErrorMsg = value,//result.ToString(),
                                                    Database = Creds.CredentialDetails.SAPDBName.ToString(),
                                                    Module = $@"{checkMod}"
                                                    ,
                                                    Table = $@"{Creds.CredentialDetails.Module}"
                                                });
                                                _context.SaveChanges();
                                            }


                                            List<string> DataValueList = new List<string>();
                                            DataValueList.Add(Pkey);
                                            //DataValueList.Add(PKValue);
                                            DataValueList.Add(PostingErrorMessage);

                                            AddData(ErrorListValue, DataValueList, PKValue);


                                            //SET ISUPLOADED TO FALSE
                                            IsUploaded = false;
                                        }
                                        else
                                        {
                                            //SET ISUPLOADED TO TRUE
                                            IsUploaded = true;
                                            errorCode = 200916;
                                            using (SqlConnection connection = new SqlConnection(con))
                                            {
                                                connection.Open();
                                                string updateQuery = $"UPDATE dbo.[{MapId}_Header] SET UploadDate = @UploadDate WHERE {Primakey} = '{primaryKeyValue}' ";

                                                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                                                {

                                                    command.Parameters.AddWithValue("@UploadDate", DateTime.Now);
                                                    //command.Parameters.AddWithValue("@HbsCode",primaryKeyValue);
                                                    command.ExecuteNonQuery();

                                                }

                                                List<string> _DataValueList = new List<string>();

                                                //SuccessListValue.Add(MapId.ToString());
                                                _DataValueList.Add(Primakey);

                                                _AddData(SuccessListValue, _DataValueList, PKValue);


                                            }

                                        }


                                        #endregion

                                        //inside of the try
                                    }
                                    catch (Exception ex)
                                    {
                                        var PostingErrorMessages = $"{errorCode} {ex.Message}";

                                        if (ex.Message.ToLower().Contains("this method cannot be called until the send method has been called") || ex.Message.ToLower().Contains("object reference not set to an instance of an object"))
                                        {
                                            PostingErrorMessages = $"{errorCode} System Error,For Re Upload";
                                            List<string> DataValueList = new List<string>();

                                            DataValueList.Add(Pkey);
                                            //DataValueList.Add(PKValue);
                                            DataValueList.Add(PostingErrorMessages);
                                            AddData(ErrorListValue, DataValueList, PKValue);
                                        }
                                        else
                                        {
                                            List<string> DataValueList = new List<string>();

                                            DataValueList.Add(Pkey);
                                            //DataValueList.Add(PKValue);
                                            DataValueList.Add(PostingErrorMessages);
                                            AddData(ErrorListValue, DataValueList, PKValue);
                                        }

                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                        {
                                            Task = "UPLOAD_START",
                                            CreateDate = DateTime.Now,
                                            ApiUrl = $@"",
                                            Json = "",
                                            ErrorMsg = $"{PostingErrorMessages}",
                                            Module = $@"'{primaryKeyValue}'"
                                               ,
                                            Database = Creds.CredentialDetails.SAPDBName,
                                            Table = $@"{Creds.CredentialDetails.Module}"
                                        });
                                        _context.SaveChanges();
                                    }

                                    //inside of the loop

                                }
                                //Outside of the loop


                                if (SuccessListValue.Count > 0)
                                {
                                    string DBlocalcon = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                                    foreach (var stds in filename_list)
                                    {
                                        foreach (var list_mode in filemodel)
                                        {
                                            if (stds.SAPTableNameModule == list_mode.DestinationTable)
                                            {
                                                errorCode = 200116;
                                                DataAccess.OPSCreateSuccessExcel(SuccessListValue, DBlocalcon, list_mode, Creds.CredentialDetails.MapId, stds.LocalPath, out errorMessage);

                                                //if (!errorMessage.Equals(""))
                                                //{
                                                //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Create Success Template", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", Database = Creds.CredentialDetails.SAPDBName, ErrorMsg = $"{errorCode} - {errorMessage}", Table = $@"{Creds.CredentialDetails.Module}" });
                                                //    _context.SaveChanges();
                                                //}
                                                break;

                                            }
                                        }


                                    }
                                }

                                if (ErrorListValue.Count != 0)
                                {
                                    string DBlocalcon = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";

                                    foreach (var stds in filename_list)
                                    {
                                        foreach (var list_mode in filemodel)
                                        {
                                            if (stds.SAPTableNameModule == list_mode.DestinationTable)
                                            {
                                                errorCode = 200117;

                                                DataAccess.OPSCreateExcelData(stds.ErrorPath, ErrorListValue, DBlocalcon, list_mode, Creds.CredentialDetails.MapId, out errorMessage);
                                                //if (!errorMessage.Equals(""))
                                                //{
                                                //    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Create Error Template", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", Database = Creds.CredentialDetails.SAPDBName, ErrorMsg = $"{errorCode} - {errorMessage}", Table = $@"{Creds.CredentialDetails.Module}" });
                                                //    _context.SaveChanges();
                                                //}
                                                break;
                                            }
                                        }


                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_START_2", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", Database = Creds.CredentialDetails.SAPDBName, ErrorMsg = $"{errorCode} - {ex.Message} {errorMessage}" });
                                _context.SaveChanges();
                            }


                        }

                        //POSTING                         
                    }//End of While (End of 1 Row Data Post)


                    SuccessListValue.Clear();
                    ErrorListValue.Clear();

                    if (Creds.CredentialDetails.EntityName.Equals("BusinessPartners"))
                    {
                        var path = Creds.CredentialDetails.LocalPath;
                        if (Directory.Exists(path))
                        {
                            var xlsxFiles = Directory.GetFiles(path, "*.xlsx").Any();

                            int transCount = int.TryParse(Schedule.Api, out int result) ? result : 0;
                            if (!xlsxFiles || transCount > 4)
                            {
                                Schedule.Api = "0";
                                _context.SaveChanges();
                                continue;
                            }
                            else
                            {
                                transCount += 1;
                                Schedule.Api = transCount.ToString();
                                _context.SaveChanges();
                                break;
                            }
                        }
                    }

                }
                return "";


            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "UPLOAD_START_3", CreateDate = DateTime.Now, ApiUrl = $@"", Json = "", ErrorMsg = $"{errorCode} - {ex.Message} {errorMessage}" });
                _context.SaveChanges();
            }
            return str;
        }
        static Dictionary<string, List<string>> AddData(Dictionary<string, List<string>> ErrorListValue, List<string> DataValueList, string PKey)
        {
            if (!ErrorListValue.ContainsKey(PKey))
            {
                ErrorListValue.Add(PKey, DataValueList);
            }

            return ErrorListValue;
        }

        static Dictionary<string, List<string>> _AddData(Dictionary<string, List<string>> SuccessListValue, List<string> DataValueList, string PKey)
        {
            if (!SuccessListValue.ContainsKey(PKey))
            {
                SuccessListValue.Add(PKey, DataValueList);
            }

            return SuccessListValue;
        }



        public static string GetDates(string option)
        {
            DateTime currentDate = DateTime.Today;
            DateTime firstDateOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            DateTime lastDateOfMonth = firstDateOfMonth.AddMonths(1).AddDays(-1);

            string ret = "";
            switch (option)
            {
                case "current_Date":
                    Console.WriteLine("Current Date: {0}", currentDate.ToShortDateString());
                    ret = currentDate.ToShortDateString();
                    break;
                case "first_Date":
                    Console.WriteLine("First Date of Month: {0}", firstDateOfMonth.ToShortDateString());
                    //ret = "04/01/2023";
                    ret = firstDateOfMonth.ToShortDateString();
                    break;
                case "last_Date":
                    Console.WriteLine("Last Date of Month: {0}", lastDateOfMonth.ToShortDateString());
                    //ret = "04/30/2023";
                    ret = lastDateOfMonth.ToShortDateString();
                    break;
                default:
                    Console.WriteLine("Invalid option selected.");
                    ret = currentDate.ToShortDateString();
                    break;
            }

            return ret;
        }

        public string ExtractCrystalReport(string modules)
        {

            //    //foreach (string modulecreds in modules)
            //    //{


            //    var Schedule = _context.Schedules.Where(x => x.Process == modules).FirstOrDefault();

            //    DataSet ds = new DataSet();
            //    var Creds = GetQueryCredentialExport(modules);

            //    string currentMonth = DateTime.Now.ToString("MM");
            //    string currentYear = DateTime.Now.ToString("yyyy");

            //    if (Creds.CredentialDetails != null)
            //    {

            //        var authPost = new AuthenticationCredViewModel
            //        {
            //            URL = "https",
            //            Method = "PATCH",
            //            Action = Creds.CredentialDetails.EntityName, //For META_DATA  VERSION 2
            //            JsonString = "{}",
            //            SAPSldServer = Creds.CredentialDetails.SAPSldServer,
            //            SAPServer = Creds.CredentialDetails.SAPIPAddress,
            //            Port = Creds.CredentialDetails.SAPLicensePort.ToString(),
            //            SAPDatabase = Creds.CredentialDetails.SAPDBName,
            //            SAPDBUserId = Creds.CredentialDetails.SAPDBuser,
            //            SAPDBPassword = Creds.CredentialDetails.SAPDBPassword,
            //            SAPUserID = Creds.CredentialDetails.SAPUser,
            //            SAPPassword = Creds.CredentialDetails.SAPPassword,
            //        };



            //        List<string> DateList = new List<string>();


            //        var parameter = _context.CrystalExtractSetup
            //             .Where(x => x.Name == modules)
            //             .Join(_context.QueryManager, cs => cs.QueryId, qm => qm.Id, (cs, qm) => new { cs, qm })
            //             .Join(_context.QueryManagerMap, qmm => qmm.qm.Id, qmms => qmms.QueryId, (qmm, qmms) => new QueryManagerMapViewModel.QueryManagerMap
            //             {

            //                 Field = qmms.Field,
            //                 Condition = qmms.Condition,
            //                 Value = qmms.Value,

            //             })
            //             .ToList();



            //        string credQueryString = Creds.SyncCredsDetails.QueryString;
            //        string paramStr = "";

            //        foreach (var param in parameter)
            //        {

            //            var field = param.Field.ToString(); // characterToreplace
            //                                                //var Condition = param.Condition.ToString();
            //            var date_param = "'" + GetDates(param.Value.ToString()) + "'"; // replacement
            //                                                                           //var date_param = GetDates(param.Value);
            //            DateList.Add(date_param);


            //            credQueryString = credQueryString.Replace(field, date_param);

            //        }


            //        //paramStr = paramStr.EndsWith("and ") ? paramStr.Substring(0, paramStr.Length -2) : paramStr;
            //        //if (paramStr.EndsWith("and "))
            //        //{
            //        //    paramStr = paramStr.Substring(0, paramStr.Length - 5);
            //        //}

            //        //string TableName = "";
            //        //string ret = "";
            //        var localP = Creds.CredentialDetails.LocalPath;
            //        var localF = Creds.CredentialDetails.FileName;
            //        if (credQueryString != null)
            //        {

            //            var dataSAP = Creds.CredentialDetails.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
            //                                Creds.CredentialDetails.SAPServerName,
            //                                Creds.CredentialDetails.SAPDBuser,
            //                                Creds.CredentialDetails.SAPDBPassword,
            //                                //Creds.CredentialDetails.SAPDBName), $@"{Creds.SyncCredsDetails.QueryString} AND {paramStr} ORDER BY a.""CardCode"" ")
            //                                //Creds.CredentialDetails.SAPDBName), $@"{Creds.SyncCredsDetails.QueryString} AND {paramStr} ORDER BY a.""CardCode"" ")
            //                                Creds.CredentialDetails.SAPDBName), $@"{credQueryString}")


            //                            :
            //                            DataAccess.Select(QueryAccess.MSSQL_conString(

            //                                Creds.CredentialDetails.SAPServerName,
            //                                Creds.CredentialDetails.SAPDBuser,
            //                                Creds.CredentialDetails.SAPDBPassword,
            //                                Creds.CredentialDetails.SAPDBName),
            //                                //$@"{Creds.SyncCredsDetails.QueryString} {(!paramStr.IsEmpty() ? "AND" : "" )} {paramStr} ORDER BY a.""CardCode"" ");
            //                                $@"{credQueryString} {(!paramStr.IsEmpty() ? "AND" : "")}");




            //            foreach (DataRow drData in dataSAP.Rows)
            //            {

            //                Schedule.RunTime = DateTime.Now;
            //                _context.SaveChanges();

            //                var ReportType = drData["ReportType"].ToString();
            //                var DocDate = drData["DocDate"].ToString();
            //                var DocEntry = drData["DocEntry"].ToString();
            //                var Filename = drData["Filename"];

            //                var Type = "";
            //                var OcRCode5 = "";
            //                var OcrCode4 = "";
            //                var PrintStatus = "";
            //                if (!ReportType.ToString().Equals("BIR 2307"))
            //                {
            //                    Type = drData["Type"].ToString();
            //                    OcRCode5 = drData["OcrCode5"].ToString();//Travel - Medical
            //                    OcrCode4 = drData["OcrCode4"].ToString();//New Business - Renewal 
            //                    PrintStatus = drData["PrintStatus"].ToString();
            //                }


            //                ReportDocument repDoc = new ReportDocument();

            //                repDoc.Load(Creds.SyncCredsDetails.Path);
            //                CrystalDecisions.CrystalReports.Engine.ParameterFieldDefinitions crParameterdef;
            //                crParameterdef = repDoc.DataDefinition.ParameterFields;



            //                TableLogOnInfos crtableLogoninfos = new TableLogOnInfos();
            //                TableLogOnInfo crtableLogoninfo = new TableLogOnInfo();
            //                CrystalDecisions.Shared.ConnectionInfo crConnectionInfo = new CrystalDecisions.Shared.ConnectionInfo();



            //                crConnectionInfo.ServerName = Creds.CredentialDetails.SAPIPAddress;
            //                crConnectionInfo.DatabaseName = Creds.CredentialDetails.SAPDBName;
            //                crConnectionInfo.UserID = Creds.CredentialDetails.SAPDBuser;
            //                crConnectionInfo.Password = Creds.CredentialDetails.SAPDBPassword;


            //                crConnectionInfo.Type = ConnectionInfoType.CRQE;
            //                crConnectionInfo.IntegratedSecurity = false;

            //                foreach (CrystalDecisions.CrystalReports.Engine.Table CrTable in repDoc.Database.Tables)
            //                {
            //                    crtableLogoninfo = CrTable.LogOnInfo;
            //                    crtableLogoninfo.ConnectionInfo = crConnectionInfo;
            //                    CrTable.ApplyLogOnInfo(crtableLogoninfo);
            //                }


            //                foreach (ParameterFieldDefinition def in crParameterdef)
            //                {
            //                    var docMaps = _context.DocumentMaps.Where(x => x.DocId == Creds.SyncCredsDetails.DocumentId && x.CrystalParam == def.Name).FirstOrDefault();
            //                    if (string.IsNullOrEmpty(def.ReportName) && docMaps != null)
            //                    {

            //                        string myval = drData[docMaps.QueryField].ToString();
            //                        repDoc.SetParameterValue(def.Name, myval);
            //                    }
            //                }

            //                var ReportPath = "";
            //                var Duplicate_Path = "";
            //                string exportfile = $@"{Filename}.pdf";
            //                exportfile = SAPAccess.SanitizeFilename(exportfile);

            //                if (ReportType.Equals("Service Invoice") && !PrintStatus.Equals("BUP"))
            //                {
            //                    var CurrentDate = DateTime.Now.ToString("yyyyMMdd");
            //                    ReportPath = $"{Creds.CredentialDetails.LocalPath}\\{OcRCode5}\\{OcrCode4}\\{ReportType}\\{Type}\\{currentMonth}{currentYear}\\{CurrentDate}";
            //                    Duplicate_Path = $"{Creds.CredentialDetails.BackupPath}\\{OcRCode5}\\{OcrCode4}\\{ReportType}\\{Type}\\{currentMonth}{currentYear}\\{CurrentDate}";

            //                }
            //                else if (ReportType.Equals("Service Invoice") && Type.Equals("Client's Copy") && PrintStatus.Equals("BUP") ||
            //                         ReportType.Equals("Acknowledgement Receipt") && Type.Equals("Client's Copy") && PrintStatus.Equals("BUP"))
            //                {
            //                    ReportPath = $"{Creds.CredentialDetails.LocalPath}\\{OcRCode5}\\Back up\\{ReportType}\\{currentMonth}{currentYear}\\{DocDate}";
            //                    Duplicate_Path = $"{Creds.CredentialDetails.BackupPath}\\{OcRCode5}\\Back up\\{ReportType}\\{currentMonth}{currentYear}\\{DocDate}";


            //                }
            //                else if (ReportType.Equals("StandAlone AR"))
            //                {
            //                    ReportPath = $"{Creds.CredentialDetails.LocalPath}\\{ReportType}\\{Type}\\{currentMonth}{currentYear}\\{DocDate}";
            //                    Duplicate_Path = $"{Creds.CredentialDetails.BackupPath}\\{ReportType}\\{Type}\\{currentMonth}{currentYear}\\{DocDate}";

            //                }
            //                else if (ReportType.Equals("BIR 2307"))
            //                {
            //                    var CurrentDate = DateTime.Now.ToString("yyyyMMdd");
            //                    ReportPath = $"{Creds.CredentialDetails.LocalPath}\\{currentMonth}{currentYear}\\{CurrentDate}";
            //                    ExtractRemoteAccess(ReportPath, exportfile, Creds.CredentialDetails.FTPUser, Creds.CredentialDetails.FTPPass, repDoc);
            //                    continue;
            //                }
            //                else
            //                {
            //                    ReportPath = $"{Creds.CredentialDetails.LocalPath}\\{OcRCode5}\\{OcrCode4}\\{ReportType}\\{Type}\\{currentMonth}{currentYear}\\{DocDate}";
            //                    Duplicate_Path = $"{Creds.CredentialDetails.BackupPath}\\{OcRCode5}\\{OcrCode4}\\{ReportType}\\{Type}\\{currentMonth}{currentYear}\\{DocDate}";


            //                }

            //                if (!Directory.Exists(ReportPath))
            //                {
            //                    Directory.CreateDirectory(ReportPath);

            //                }


            //                string filePath = Path.Combine(ReportPath, exportfile.Replace(".rpt", ".pdf"));
            //                string jsonReturn = "";
            //                string errorCode = "";

            //                if (Directory.Exists(ReportPath))
            //                {
            //                    try
            //                    {
            //                        repDoc.ExportToDisk(ExportFormatType.PortableDocFormat, filePath);

            //                        if (File.Exists(filePath) && !ReportType.Equals("BIR 2307"))
            //                        {
            //                            //if (!Directory.Exists(Duplicate_Path))
            //                            //{
            //                            //    Directory.CreateDirectory(Duplicate_Path);

            //                            //}
            //                            //var backupFilePath = Path.Combine(Duplicate_Path, exportfile);

            //                            //File.Copy(filePath, backupFilePath, true); // true to overwrite if file already exists


            //                            string actionTemplate = string.Empty;
            //                            switch (ReportType)
            //                            {
            //                                case "Service Invoice":
            //                                    actionTemplate = "Invoices";
            //                                    errorCode = "6802";
            //                                    break;

            //                                case "AR Credit Memo":
            //                                    actionTemplate = "CreditNotes";
            //                                    errorCode = "6807";
            //                                    break;

            //                                case "Acknowledgement Receipt":
            //                                    actionTemplate = "IncomingPayments";
            //                                    errorCode = "6812";
            //                                    break;

            //                                case "StandAlone AR":
            //                                    actionTemplate = "IncomingPayments";
            //                                    errorCode = "6842";
            //                                    break;

            //                                default:
            //                                    throw new ArgumentException($"Invalid ReportType - {ReportType}");
            //                            }

            //                            if (!string.IsNullOrEmpty(actionTemplate))
            //                            {
            //                                authPost.Action = $@"{actionTemplate}({DocEntry})?$select=U_Print_status";
            //                                authPost.JsonString = $"{{\r\n    \"U_Print_status\":\"{PrintStatus}\"\r\n}}";
            //                                authPost.Method = "PATCH";
            //                                sapAces.SaveCredentials(authPost);
            //                                SAPAccess.LoginAction(); // LOGIN TO DESTINATION TABLE
            //                                jsonReturn = sapAces.SendSLData(authPost);

            //                                if (jsonReturn.Contains("error") && !jsonReturn.ToLower().Contains("entity with value"))
            //                                {
            //                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Print Status error", CreateDate = DateTime.Now, ApiUrl = $@"{authPost.Method} {authPost.Action}", Json = $"{authPost.JsonString}", ErrorMsg = $"{errorCode} - {jsonReturn}", Database = $"{Creds.CredentialDetails.SAPDBName.ToString()}", Module = $"", Table = $@"{actionTemplate}" });
            //                                    _context.SaveChanges();
            //                                }
            //                            }
            //                            errorCode = "6793";

            //                        }//if File Exist


            //                    }//try
            //                    catch (Exception ex)
            //                    {

            //                        var exceptionMessage = ex.Message.ToString();
            //                        var stackTrace = new StackTrace(ex, true);
            //                        // Get the top stack frame
            //                        var frame = stackTrace.GetFrame(0);
            //                        // Get the line number from the stack frame
            //                        var lineNumber = frame.GetFileLineNumber();

            //                        if (!ex.Message.ToLower().Contains("this method cannot be called"))
            //                        {
            //                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
            //                            {
            //                                Task = "Crystal E-Gen Report Error",
            //                                CreateDate = DateTime.Now,
            //                                ApiUrl = "",  // Consider setting an actual URL if available
            //                                Json = "",
            //                                ErrorMsg = $"{errorCode} - Line {lineNumber}: {exceptionMessage}"
            //                            });
            //                            _context.SaveChanges();
            //                        }

            //                        // Handle the exception, log the error message, or display it for debugging purposes.
            //                    }


            //                    //repDoc.ExportToDisk(ExportFormatType.PortableDocFormat, filePath);
            //                }

            //                repDoc.Refresh();
            //                repDoc.Close();
            //                repDoc.Dispose();
            //                //GC.Collect();
            //            } //foreach sa taas datarow


            //            //}
            //        }
            //    }//end of Creds is null
            //    //}


            return "success";
        }


        //public string ExtractRemoteAccess(string ReportPath, string filename, string ftp_username, string ftp_password, ReportDocument repDoc)
        //{
        //    try
        //    {
        //        using (new ImpersonateUser(ftp_username, ReportPath, ftp_password))
        //        {

        //            if (!Directory.Exists(ReportPath))
        //            {
        //                Directory.CreateDirectory(ReportPath);

        //            }

        //            string filePath = Path.Combine(ReportPath, filename);

        //            if (Directory.Exists(ReportPath))
        //            {
        //                //repDoc.ExportToDisk(ExportFormatType.PortableDocFormat, filePath);
        //            }


        //            return "";

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("An error occurred.-" + ftp_username + '_' + ftp_password + '-' + ex);

        //    }
        //    finally
        //    {
        //        // Ensure resources are properly disposed
        //        if (repDoc != null)
        //        {
        //            repDoc.Refresh();
        //            repDoc.Close();
        //            repDoc.Dispose();
        //        }
        //    }
        //}



        public async Task<string> PeopleNaveeReport(string ReportName, string payrollperiod,string to_date)
        {
            try
            {
                var Creds = GetSyncQueryCredential(ReportName);
                string filename = "";
                ////Get the Credential from SyncSetup
                int QueryID = Creds.ReportCredentialDetails.QueryId;
                var syncSet = _context.Syncs.Where(sel => sel.Code.Contains("PN Sync")).FirstOrDefault();
                var Query = _context.Queries.Where(sel => sel.Id == QueryID);
                var APISetup = _context.APISetups.Where(col => col.APICode == "PNReportAPI").FirstOrDefault();
                foreach (var fe in Query)
                {
                    if (fe.IsMultiQuery == true)
                    {
                        DataSet dtList = new DataSet();
                        var queryList = _context.QuerySubs.Where(sel => sel.QueryId == fe.Id).OrderBy(x => x.VisOrder).ToList();
                        foreach (var qry in queryList)
                        {
                            //// GET THE CONNECTION FROM SyncSetup
                            var constr = DataAccess.GetMySqlConn(syncSet.IpAddress, syncSet.ServerPort.ToString(), syncSet.DbName, syncSet.DbUser, syncSet.DbPass);
                            //// GET THE SQL SCRIPT FROM PN Database Table;  Note: Query from the Addon Record is just a backup only
                            var dtStrQry = DataAccess.MySqlSelect($@"SELECT `query` FROM tabReport Where `name` = '{qry.code}'; ", constr, out string _oMsgQry);
                            if (dtStrQry.Rows.Count > 0)
                            {
                                var querystring = dtStrQry.Rows[0]["query"].ToString();

                                if (!String.IsNullOrEmpty(payrollperiod) && dtStrQry.Rows[0]["query"].ToString().Contains("payperiod.name"))
                                {
                                    querystring = DataAccess.ChangepayrollPeriod(dtStrQry.Rows[0]["query"].ToString(), payrollperiod);
                                }

                                var dtResult = DataAccess.MySqlSelect(querystring, constr, out string _oMsgRes);
                                dtResult.TableName = qry.code;
                                dtList.Tables.Add(dtResult);
                            }
                            else
                            {

                            }
                        }

                        ////Create Excel Report FROM DataSet
                        filename = await DataAccess.CreateExcelReport(dtList, Creds.ReportCredentialDetails.SavePath,payrollperiod,to_date, queryList);
                        //filename = "Instruction_20240814_165937.xlsx";
                    }
                    else
                    {

                    }

                };

                if (!filename.Contains("error"))
                {
                    string _lock;
                    string _unlock;
                    string SavePath = Creds.ReportCredentialDetails.SavePath;
                    string FilePath = $"{SavePath}\\{filename}";
                    string password = Guid.NewGuid().ToString();


                    _lock = DataAccess.SetExcelPassword(FilePath, password);

                    if (_lock.Equals("lock"))
                    {
                        var check_email_status = _context.Schedules.Where(sel => sel.SchedCode.Contains("Email_Attachment")).FirstOrDefault();
                        var post_emailAttach = "";
                        var post_Sharepoint = "";

                        if (check_email_status.IsActive)
                        {
                            try
                            {
                                string zCode = "Email_Attachment";
                                var zEmailCreds = GetEmailCredentials(zCode);
                                if (zEmailCreds.EmailCreds != null)
                                {
                                    post_emailAttach = eml.Send(zEmailCreds, FilePath);
                                }

                            }
                            catch (Exception ex)
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Email_Error", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = ex.Message.ToString(), Json = "", Database = "", Table = "", Module = "" });
                                _context.SaveChanges();
                            }

                        }

                        bool Creds_api_sharepoint = Creds.ReportCredentialDetails.APIKey != null &&
                            Creds.ReportCredentialDetails.APILoginBody != null
                            && Creds.ReportCredentialDetails.APILoginURL != null &&
                            Creds.ReportCredentialDetails.APISecretKey != null &&
                            Creds.ReportCredentialDetails.APIToken != null &&
                            Creds.ReportCredentialDetails.APIURL != null;

                        if (Creds_api_sharepoint)
                        {
                            post_Sharepoint = await PostHelper.UploadToSharePoint(filename, SavePath, APISetup);
                            if (post_Sharepoint.Contains("error"))
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SHAREPOINT_UPLOAD_ERROR", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = post_Sharepoint, Json = "", Database = "", Table = "", Module = "" });
                                _context.SaveChanges();

                                var ecode = "ECode";
                                var eEmailCreds = GetEmailCredentials(ecode);
                                var eFilepath = "";
                                if (eEmailCreds.EmailCreds != null)
                                {
                                    var emailret = eml.Send(eEmailCreds, eFilepath);
                                    if (emailret.ToLower().Contains("error"))
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Email_Error", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = emailret, Json = "", Database = "", Table = "", Module = "" });
                                        _context.SaveChanges();
                                    }
                                }


                            }
                        }


                        if (post_Sharepoint.Contains("File uploaded successfully") && Creds_api_sharepoint || check_email_status.IsActive && post_emailAttach.Contains("Success") || check_email_status.IsActive == false && !Creds_api_sharepoint)
                        {
                            _unlock =  DataAccess.UnlockExcelFile(FilePath, password);

                            if (_unlock.Equals("unlock"))
                            {
                                if (check_email_status.IsActive || Creds_api_sharepoint)
                                {

                                    _context.EmailStorage.Add(new DomainLayer.Models.EmailStorage { SavePath = Creds.ReportCredentialDetails.SavePath, FileName = filename, Password = password });
                                    _context.SaveChanges();

                                    string Code = "EmailCode";
                                    var EmailCreds = GetEmailCredentials(Code);
                                    string nullFilePath = "";
                                    if (EmailCreds.EmailCreds != null)
                                    {
                                        return eml.Send(EmailCreds, nullFilePath);
                                    }
                                }
                                else
                                {
                                    return "Report Successfully Created Locally";
                                }
                            }
                            else
                            {
                                throw new Exception($"error UnlockExcelFile  - {_unlock}");
                            }
                        }
                        else
                        {
                            if (File.Exists(FilePath))
                            {
                                File.Delete(FilePath);
                            }

                            throw new Exception(post_Sharepoint);
                        }

                    }
                    else
                    {
                        throw new Exception($"error Set Excel Password  - {_lock}");
                    }
                }
                else
                {
                    throw new Exception(filename);
                }

            }
            catch (Exception ex)
            {
                return $"{ex.Message.ToString()}";
            }

            return "success";
        }

        public async Task<byte[]> PNdownloadFile(string ReportName, string payrollperiod)
        {
            try
            {
                var Creds = GetSyncQueryCredential(ReportName);
                string filename = "";
                // Get the Credential from SyncSetup
                int QueryID = Creds.ReportCredentialDetails.QueryId;
                var syncSet = _context.Syncs.Where(sel => sel.Code.Contains("PN Sync")).FirstOrDefault();
                var Query = _context.Queries.Where(sel => sel.Id == QueryID);
                var APISetup = _context.APISetups.Where(col => col.APICode == "PNReportAPI").FirstOrDefault();

                foreach (var fe in Query)
                {
                    if (fe.IsMultiQuery == true)
                    {
                        DataSet dtList = new DataSet();
                        var queryList = _context.QuerySubs.Where(sel => sel.QueryId == fe.Id).OrderBy(x=>x.VisOrder).ToList();
                        foreach (var qry in queryList)
                        {
                            // GET THE CONNECTION FROM SyncSetup
                            var constr = DataAccess.GetMySqlConn(syncSet.IpAddress, syncSet.ServerPort.ToString(), syncSet.DbName, syncSet.DbUser, syncSet.DbPass);
                            // GET THE SQL SCRIPT FROM PN Database Table
                            var dtStrQry = DataAccess.MySqlSelect($@"SELECT `query` FROM tabReport Where `name` = '{qry.code}'; ", constr, out string _oMsgQry);

                            if (dtStrQry.Rows.Count > 0)
                            {
                                var querystring = dtStrQry.Rows[0]["query"].ToString();

                                if (!String.IsNullOrEmpty(payrollperiod) && dtStrQry.Rows[0]["query"].ToString().Contains("payperiod.name"))
                                {
                                    querystring = DataAccess.ChangepayrollPeriod(dtStrQry.Rows[0]["query"].ToString(), payrollperiod);
                                }

                                var dtResult = DataAccess.MySqlSelect(querystring, constr, out string _oMsgRes);
                                dtResult.TableName = qry.code;
                                dtList.Tables.Add(dtResult);
                            }
                        }
                        
                        // Create Excel Report FROM DataSet
                        return await Task.Run(() => DataAccess.DownloadFileExcel(dtList,payrollperiod, queryList));
                    }
                }
                return Array.Empty<byte>();

               
            }
            catch (Exception ex)
            {
              
                return null;
            }
        }



    }
}