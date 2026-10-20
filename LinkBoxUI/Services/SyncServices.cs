using InfrastructureLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer;
using LinkBoxUI.Context;
using DomainLayer.ViewModels;
//using SAPbobsCOM;
using System.Data;
using DataAccessLayer.Class;
using DomainLayer.Models;
using StructureMap.Diagnostics;
using System.Data.Entity.Validation;
using System.Data.Entity;
using df = DocumentFormat.OpenXml.Wordprocessing;
using static System.Net.WebRequestMethods;
using System.Windows.Media;

namespace LinkBoxUI.Services
{
    public class SyncServices : ISyncRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        public SyncViewModel View_Sync()
        {
            SyncViewModel sync = new SyncViewModel();
            List<string> SAPVersionList = new List<string>();
            sync.SyncView = _context.Syncs.Select(x => new SyncViewModel.Sync
            {
                Code = x.Code,
                Id = x.Id,
                DbName = x.DbName,
                DbUser = x.DbUser,
                Path = x.Path,
                FtpPath = x.FtpPath,
                ServerPort = x.ServerPort,
                FileType = x.FileType,
                IsActive = x.IsActive,
                DbVersion = x.DbVersion,
                IpAddress = x.IpAddress,
            }).ToList();


            sync.CrystalExtractView = _context.CrystalExtractSetup.Select(x => new SyncViewModel.CrystalExtract
            {
                Id = x.Id,
                Name = x.Name,
                QueryCode= x.QueryCode,
                SyncId = x.DocumentId,
                DocumentCode= x.DocumentCode,
                APICode=x.APICode,
                IsActive = x.IsActive
           }).ToList();

            sync.ReportExtractView = _context.CrystalExtractSetup.Select(x => new SyncViewModel.ReportExtract
            {
                Id = x.Id,
                Name = x.Name,
                QueryCode = x.QueryCode,
                DocumentCode = x.DocumentCode,
                APICode = x.APICode,
                IsActive = x.IsActive

            }).ToList();


            sync.SyncQueryMapsView = _context.SyncQueryMap.Select(x => new SyncViewModel.SyncQueryMap
            {
               ReportID = x.ReportID,
               QueryID = x.QueryID,
               VisOrder=x.VisOrder,
            }).ToList();

            sync.QueryView = _context.QueryManager.Select(x => new SyncViewModel.Query
            {
                Code = x.QueryCode,
                QueryString = x.QueryString,
                Id = x.Id,
                IsActive = x.IsActive

            }).ToList();

       

            sync.ReportQueryView = _context.Queries.Select(x => new SyncViewModel.Query
            {
                Code = x.Code,
                QueryString = x.QueryString,
                Id = x.Id,
                IsActive = x.IsActive

            }).ToList();

            sync.DocumentView = _context.Documents.Where(x=>x.IsActive).Select(x => new SyncViewModel.DocQuery
            {
                Code = x.Code,
                FileName = x.FileName,
                Id = x.Id,
                IsActive = x.IsActive

            }).ToList();

            sync.ReportSyncView = _context.Syncs.Where(x =>x.IsActive).Select(x => new SyncViewModel.PathQuery
            {
                Id = x.Id,
                Code = x.Code,
            }).ToList();

            sync.APIView = _context.APISetups.Where(x => x.IsActive).Select(x => new SyncViewModel.APISetup
            {
                APICode = x.APICode,
                Id = x.APIId,
                IsActive = x.IsActive

            }).ToList();


            sync.QueryView = _context.Queries.Where(sel=> sel.IsActive == true).Select(sel=> new SyncViewModel.Query { Code = sel.Code, Id = sel.Id }).ToList();


            sync.SyncQueryView = _context.SyncQueries.Select(x => new SyncViewModel.SyncQuery
            {
                Id = x.Id,
                SyncQueryCode = x.SyncQueryCode,
                SyncCode = x.SyncCode,
                QueryCode = x.QueryCode,
                IsActive = x.IsActive,

            }).ToList();            

            //foreach (var item in Enum.GetValues(typeof(BoDataServerTypes)))
            //{
            //    SAPVersionList.Add(item.ToString());
            //}

            SAPVersionList.AddRange(DataAccess.ConnectionTypes());

            sync.SAPList = SAPVersionList.Select(x => new SyncViewModel.SAPVersion
            {
                SAPDBVersion = x.ToString(),

            }).ToList();

            return sync;
        }

        public bool Create_SyncSetup(Sync sync, string check, int id)
        {
            sync.Path = check == "FTP" ? "" : sync.Path;
            sync.FtpPath = check == "FTP" ? check : "";
            sync.FtpUser = check == "FTP" ? check : "";
            sync.FtpPass = check == "FTP" ? check : "";
            sync.IsActive = true;
            sync.CreateDate = DateTime.Now;
            sync.CreateUserID = id;
            _context.Syncs.Add(sync);
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public SyncViewModel Find_SyncSteup(int id)
        {

            var model = new SyncViewModel();

            model.SyncView = _context.Syncs.Where(x => x.Id == id).Select(x => new SyncViewModel.Sync

            {
                Code = x.Code,
                DbName = x.DbName,
                DbUser = x.DbUser,
                DbPass = x.DbPass,
                ServerPort = x.ServerPort,
                Path = x.Path,
                FtpPath = x.FtpPath,
                FtpUser = x.FtpUser,
                FtpPass = x.FtpPass,
                IsActive = x.IsActive,
                FileType = x.FileType,
                DbVersion = x.DbVersion,
                IpAddress = x.IpAddress,

            }).ToList();

            return model;
        }
        public bool Update_SyncSetup(Sync sync, string check, int id)
        {
            var Sync = sync;
            Sync.Path = check == "FTP" ? "" : sync.Path;
            Sync.FtpPath = check == "FTP" ? sync.FtpPath : "";
            Sync.FtpUser = check == "FTP" ? sync.FtpUser : "";
            Sync.FtpPass = check == "FTP" ? sync.FtpPass : "";
            Sync.UpdateDate = DateTime.Now;
            Sync.CreateDate = DateTime.Now;
            Sync.UpdateUserID = id;
            _context.Entry(Sync).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public bool Create_Query(DomainLayer.Query query, int id)
        {
            query.IsActive = true;
            query.CreateDate = DateTime.Now;
            query.CreateUserID = id;
            _context.Queries.Add(query);
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public SyncViewModel Find_Query(int id)
        {
            var model = new SyncViewModel();

            model.QueryView = _context.Queries.Where(x => x.Id == id).Select(x => new SyncViewModel.Query
            {
                Code = x.Code,
                QueryString = x.QueryString,
                IsActive = x.IsActive,

            }).ToList();
            return model;
        }

        public bool Update_Query(QueryManager query, int id)
        {

            var Query = query;
            Query.QueryCode = query.QueryCode;
            Query.QueryString = query.QueryString;
            Query.QueryName = query.QueryName;
            Query.ConnectionString = query.ConnectionString;
            Query.UpdateDate = DateTime.Now;
            Query.CreateDate = DateTime.Now;
            Query.UpdateUserId = id;
            _context.Entry(Query).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }


        public bool Create_SyncQuery(SyncQuery syncquery, int syncid, int queryid, int id)
        {
            string selectSyncCode = _context.SyncQueries.Where(x => x.Id == syncid).Select(x => x.SyncQueryCode).FirstOrDefault();
            string selectQueryCode = _context.QueryManager.Where(x => x.Id == queryid).Select(x => x.QueryCode).FirstOrDefault();
            syncquery.SyncId = syncid;
            syncquery.SyncCode = selectSyncCode;
            syncquery.QueryId = queryid;
            syncquery.QueryCode = selectQueryCode;
            syncquery.IsActive = true;
            syncquery.CreateDate = DateTime.Now;
            syncquery.CreateUserID = id;
            _context.SyncQueries.Add(syncquery);
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public bool Create_CrystalExtract(string CrystalExtractSetup, string selectQueryCode, int apiid)
        {
            var CrystalCode = _context.CrystalExtractSetup.Where(x=>x.Name== CrystalExtractSetup).Select(x=>x.Name).FirstOrDefault();
            if(CrystalCode == CrystalExtractSetup)
            {
                return false;
            }
            else
            {
                CrystalExtractSetup apisyncsetup = new CrystalExtractSetup();
               // string selectDocumentCode = _context.Documents.Where(x => x.Id == documentid).Select(x => x.Code).FirstOrDefault();
                //string selectQueryCode = _context.QueryManager.Where(x => x.Id == queryid).Select(x => x.).FirstOrDefault();
                string selectAPIcode = _context.APISetups.Where(x => x.APIId == apiid).Select(x => x.APICode).FirstOrDefault();

                //CrystalExtractSetup.DocumentId = documentid;
                //CrystalExtractSetup.DocumentCode = selectDocumentCode;
                apisyncsetup.Name = CrystalExtractSetup;
                //apisyncsetup.QueryId = queryid;
                apisyncsetup.QueryCode = selectQueryCode;
                apisyncsetup.IsActive = true;
                apisyncsetup.CreateDate = DateTime.Now;
                apisyncsetup.CreateUserID = 1;
                apisyncsetup.APIId = apiid;
                apisyncsetup.APICode = selectAPIcode;

                _context.CrystalExtractSetup.Add(apisyncsetup);
                _context.SaveChanges();
                return true;

            }
           
        }

        public bool Create_ReportExtract(CrystalExtractSetup ReportExTractSetup, int id)
        {
            using (var db = new LinkboxDb())
            {
                using (DbContextTransaction dbtransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var ReportCode = _context.CrystalExtractSetup.Where(x => x.Name == ReportExTractSetup.Name).Select(x => x.Name).FirstOrDefault();
                        if (!String.IsNullOrEmpty(ReportCode))
                        {
                            return false;
                        }
                        else
                        {
                            string syncCode = _context.Syncs.Where(x => x.Id == ReportExTractSetup.DocumentId).Select(x => x.Code).FirstOrDefault();
                            string selectQueryCode = _context.Queries.Where(x => x.Id == ReportExTractSetup.QueryId).Select(x => x.Code).FirstOrDefault();
                            string selectAPIcode = _context.APISetups.Where(x => x.APIId == ReportExTractSetup.APIId).Select(x => x.APICode).FirstOrDefault();

                            ReportExTractSetup.DocumentCode = syncCode;
                            ReportExTractSetup.APICode = selectAPIcode;
                            ReportExTractSetup.QueryCode = selectQueryCode;
                            ReportExTractSetup.IsActive = true;
                            ReportExTractSetup.CreateDate = DateTime.Now;
                            ReportExTractSetup.CreateUserID = id;
                            _context.CrystalExtractSetup.Add(ReportExTractSetup);
                            _context.SaveChanges();

                            //if (querymap != null)
                            //{
                            //    var ReportID = _context.CrystalExtractSetup.Where(x => x.Name == ReportExTractSetup.Name).Select(x =>x.Id).FirstOrDefault();
                            //    int incremental = 1;
                            //    foreach (int map in querymap)
                            //    {
                            //        SyncQueryMap QueryMaps = new SyncQueryMap();
                            //        QueryMaps.ReportID = ReportID;
                            //        QueryMaps.QueryID = map;
                            //        QueryMaps.VisOrder = incremental++;
                            //        _context.SyncQueryMap.Add(QueryMaps);
                            //        _context.SaveChanges();
                            //    }

                            //}

                            dbtransaction.Commit(); //final process
                            return true;
                        }
                  
                    }
                    catch (DbEntityValidationException ex)
                    {
                        dbtransaction.Rollback();
                        return false;
                    }
                   
                }

            }


        }

        public SyncViewModel Find_SyncQuery(int id)
        {
            var model = new SyncViewModel();
            model.SyncQueryView = _context.SyncQueries.Where(x => x.Id == id).Select(x => new SyncViewModel.SyncQuery
            {
                SyncQueryCode = x.SyncQueryCode,
                QueryCode = x.QueryCode,
                SyncCode = x.SyncCode,
                QueryId = x.QueryId,
                SyncId = x.SyncId,
                IsActive = x.IsActive,
            }).ToList();

            //model.SyncQueryMapsView = _context.SyncQueries
            //    .Join( _context.SyncQueryMap, sq => sq.Id, qm => qm.ReportID , (sq, qm) => new { sq = sq, qm = qm} )
            //    .GroupJoin( _context.Queries,
            //                qm => qm.qm.QueryID,
            //                qry => qry.Id,
            //                (qm, qry) => qry.Select(x=> new { sqm = qm, qrm = x })
            //                    .DefaultIfEmpty(new { qm = qry, qry = (Query)null }))
            //    .SelectMany(f => f)
            //    .Where(x => x.Id == id)
            //    .Select(sel => new SyncViewModel.SyncQueryMap
            //    {
            //        QueryCode = sel.Query
            //    }).ToList();
            var qrylist = from sq in _context.SyncQueries
                                      join qm in _context.SyncQueryMap on sq.Id equals qm.ReportID into joinQM
                                      from j in joinQM.DefaultIfEmpty()
                                      join qr in _context.Queries on j.QueryID equals qr.Id into joinQR
                                      from qy in joinQR.DefaultIfEmpty()
                                      select new SyncViewModel.SyncQueryMap
                                      {
                                          QueryCode = qy.Code
                                      };
            model.SyncQueryMapsView = qrylist.ToList();
            return model;
        }

        public SyncViewModel Find_CrystalExtract(int id)
        {
            var model = new SyncViewModel();
            model.CrystalExtractView = _context.CrystalExtractSetup.Where(x => x.Id == id).Select(x => new SyncViewModel.CrystalExtract
            {
                Name = x.Name,
                QueryCode = x.QueryCode,
                DocumentCode = x.DocumentCode,
                QueryId = x.QueryId,
                DocumentId = x.DocumentId,
                APICode = x.APICode,
                IsActive = x.IsActive,
            }).ToList();

         

            return model;
        }

        public SyncViewModel Find_QueryManager(int id)
        {
            var model = new SyncViewModel();
            model.CrystalExtractView = _context.QueryManager.Select(x => new SyncViewModel.CrystalExtract
            {
                QueryCode = x.QueryCode,
                QueryString = x.QueryString,
                QueryId = x.Id,
                IsActive = x.IsActive

            }).ToList();


            return model;
        }

        public SyncViewModel Find_Document(int id)
        {
            var model = new SyncViewModel();
            model.CrystalExtractView = _context.Documents.Select(x => new SyncViewModel.CrystalExtract
            {
                DocumentCode = x.Code,
                DocumentName = x.FileName,
                DocumentId = x.Id,
                IsActive = x.IsActive

            }).ToList();


            return model;
        }



        public bool Update_SyncQuery(SyncQuery syncquery, int id)
        {
            var syncQuery = _context.SyncQueries.Find(syncquery.Id);
            syncQuery.UpdateDate = DateTime.Now;
            syncQuery.UpdateUserID = id;
            syncQuery.IsActive = syncquery.IsActive;
            _context.Entry(syncQuery).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public bool Update_CrystalExtract(CrystalExtractSetup crystalextract, int id)
        {
            var crystal = _context.CrystalExtractSetup.Find(crystalextract.Id);
            crystal.UpdateDate = DateTime.Now;
            crystal.UpdateUserID = id;
            crystal.IsActive = crystalextract.IsActive;
            _context.Entry(crystal).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public SyncViewModel Validate_Sync(string code)
        {
            var model = new SyncViewModel();

            model.SyncView = _context.Syncs.Where(x => x.Code == code).Select((x) => new SyncViewModel.Sync
            {
                Code = x.Code,
            }).ToList();

            model.QueryView = _context.Queries.Where(x => x.Code == code).Select((x) => new SyncViewModel.Query
            {
                Code = x.Code,
            }).ToList();

            model.SyncQueryView = _context.SyncQueries.Where(x => x.SyncQueryCode == code).Select((x) => new SyncViewModel.SyncQuery
            {
                SyncQueryCode = x.SyncQueryCode,
            }).ToList();

            return model;

        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public SyncViewModel Get_SyncQuery(int id)
        {
            var model = new SyncViewModel();
            model.SyncQueryView = _context.SyncQueries.Where(a => a.Id == id).Join(_context.Syncs, a => a.SyncId, b => b.Id, (a, b) =>
           new
           {
               a,
               b
           }).Join(_context.Queries, ab => ab.a.QueryId, c => c.Id, (ab, c) => new SyncViewModel.SyncQuery
           {
               Id = ab.a.Id,
               SyncQueryCode = ab.a.SyncQueryCode,
               IsActive = ab.a.IsActive,
               QueryId = c.Id,
               QueryCode = c.Code,
               QueryString = c.QueryString,
               SyncId = ab.b.Id,
               SyncCode = ab.b.Code,
               Path = ab.b.Path,
               RemotePath = ab.b.FtpPath,
               RemoteUser = ab.b.FtpUser,
               RemotePassword = ab.b.FtpPass,
               DbName = ab.b.DbName,
               DbUser = ab.b.DbUser,
               DbPass = ab.b.DbPass,
               FileType = ab.b.FileType,
               IpAddress = ab.b.IpAddress,
               DbVersion = ab.b.DbVersion,

           }).ToList();
            return model;
        }

        public DataTable Fill_DataTable(SyncViewModel syncquery)
        {
            string message = "";
            DataTable dt = new DataTable();
            foreach (var item in syncquery.SyncQueryView)
            {
                if (item.DbVersion == "dst_HANADB")
                {
                    dt = SyncAccess.Execute(syncquery, (m) => message = m);
                }
                else if (item.DbVersion.Contains("MSSQL"))
                {
                    dt = SyncAccess.Execute2(syncquery, (m) => message = m);
                }
            }
            return dt;
        }

        public void Export(DataTable dt, SyncViewModel syncquery)
        {
            SyncAccess.ExportData(dt, syncquery);
        }
    }
}