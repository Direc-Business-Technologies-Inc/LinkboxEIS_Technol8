using InfrastructureLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer.ViewModels;
//using SAPbobsCOM;
using LinkBoxUI.Context;
using DomainLayer;
using DataCipher;
using System.Data;
using DataAccessLayer.Class;
using DomainLayer.Models;
using System.Web.Http;
using System.IO;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Management;
//using CrystalDecisions.CrystalReports.Engine;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace LinkBoxUI.Services
{
    public class ConfigurationServices : IConfigurationRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        public SetupCreateViewModel View_Setup(HttpConfiguration config)
        {
            SetupCreateViewModel model = new SetupCreateViewModel();



            List<string> SAPVersionList = new List<string>();

            model.AddonView = _context.AddonSetup.Select(x => new SetupCreateViewModel.AddonViewModel
            {
                AddonId = x.AddonId,
                AddonCode = x.AddonCode,
                AddonDBVersion = x.AddonDBVersion,
                AddonServerName = x.AddonServerName,
                AddonIPAddress = x.AddonIPAddress,
                AddonDBName = x.AddonDBName,
                AddonPort = x.AddonPort,
                AddonDBuser = x.AddonDBuser,
                AddonDBPassword = x.AddonDBPassword,
                IsActive = x.IsActive
            }).ToList();

            model.SAPView = _context.SAPSetup.Select(x => new SetupCreateViewModel.SAPViewModel
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

            }).ToList();

            model.PathView = _context.PathSetup.Select(x => new SetupCreateViewModel.PathViewModel
            {
                PathId = x.PathId,
                PathCode = x.PathCode,
                LocalPath = x.LocalPath,
                BackupPath = x.BackupPath,
                ErrorPath = x.ErrorPath,
                RemotePath = x.RemotePath,
                RemoteServerName = x.RemoteServerName,
                RemoteIPAddress = x.RemoteIPAddress,
                RemotePort = x.RemotePort,
                RemoteUserName = x.RemoteUserName,
                RemotePassword = x.RemotePassword,
                //FileType = x.FileType,
                IsActive = x.IsActive
            }).ToList();


            model.QueryManagerDocViewList = _context.QueryManager.Select(x => new SetupCreateViewModel.QueryManagerDocView
            {
                QueryId = x.Id,
                QueryCode = x.QueryCode,
                QueryString = x.QueryString,
                IsActive = x.IsActive,

            }).ToList();


            model.APIView = _context.APISetups.Select(x => new SetupCreateViewModel.APIViewModel
            {
                APIId = x.APIId,
                APICode = x.APICode,
                APIURL = x.APIURL,
                APIMethod = x.APIMethod,
                IsActive = x.IsActive,
                APIKey = x.APIKey,
                APISecretKey = x.APISecretKey,
                APIToken = x.APIToken,
                Platform = x.Platform
            }).ToList();

            model.ParameterList = _context.Paramenters.Select(x => new SetupCreateViewModel.Parameter
            {
                Code = x.Code,
                Id = x.Id,
                ParameterType = x.ParameterType,
                Value = x.Value,
                IsActive = x.IsActive
            }).ToList();

            model.DocumentList = _context.Documents.Select(x => new SetupCreateViewModel.Document
            {
                Code = x.Code,
                Id = x.Id,
                FileName = x.FileName,
                IsActive = x.IsActive,
                QueryManagerID = x.QueryManagerId
            }).ToList();


            model.CompanyList = _context.CompanyDetails.Select(x => new SetupCreateViewModel.Company
            {

                Id = x.Id,
                FileName = x.FileName,
                IsActive = x.IsActive,
                Address = x.Address,
                MobileNo = x.MobileNo,
                TelNo = x.TelNo,
                CompanyName = x.CompanyName,
                FilePath = x.FilePath

            }).ToList();

            model.EmailView = _context.EmailSetup.Select(x => new SetupCreateViewModel.EmailViewModel
            {
                EmailId = x.EmailId,
                EmailCode = x.EmailCode,
                EmailDesc = x.EmailDesc,
                Email = x.Email,
                DisplayName = x.DisplayName,
                Password = x.Password,
                Port = x.Port,
                SMTPClient = x.SMTPClient,
                IsActive = x.IsActive
            }).ToList();

            model.ModuleSetups = _context.ModuleSetup.Select(x => new SetupCreateViewModel.ModuleSetupViewModel
            {
                Id = x.Id,
                ModuleCode = x.ModuleCode,
                ModuleName = x.ModuleName,
                PrimaryKey = x.PrimaryKey,
                EntityType = x.EntityType,
                EntityName = x.EntityName,
                IsActive = x.IsActive
            }).ToList();

            //foreach (var item in Enum.GetValues(typeof(BoDataServerTypes)))
            //{
            //    SAPVersionList.Add(item.ToString());
            //}

            SAPVersionList.AddRange(DataAccess.ConnectionTypes());                      

            model.SAPList = SAPVersionList.Select(x => new SetupCreateViewModel.SAPVersion
            {
                SAPDBVersion = x.ToString(),

            }).ToList();

            //var local = config.Properties;
            var apiDescription = new List<SetupCreateViewModel.ApiDescription>();
            var apiDescs = config.Services.GetApiExplorer().ApiDescriptions;
            foreach (var x in apiDescs)
            {
                string method = x.HttpMethod.Method.ToString();
                string path = x.Route.RouteTemplate;
                if (!_context.APISetups.Where(z => z.APIURL.ToLower().Contains(path.ToLower())).Any())
                {
                    apiDescription.Add(new SetupCreateViewModel.ApiDescription
                    {
                        APIUrl = "http://localhost:40710/" + path,
                        Method = method
                    });
                }

            }

            model.ApiDescriptions = apiDescription.Select(x => new SetupCreateViewModel.ApiDescription
            {
                APIUrl = x.APIUrl,
                Method = x.Method
            }).ToList();

            return model;
        }
        public void Create_AddonSetup(AddonSetup addon, int id)
        {
            addon.CreateDate = DateTime.Now;
            addon.IsActive = true;
            addon.CreateUserID = id;
            _context.AddonSetup.Add(addon);
            _context.SaveChanges();
        }
        public bool Update_AddonSetup(AddonSetup addon, int id)
        {
            var addonsetup = addon;
            addonsetup.UpdateUserID = id;
            addonsetup.UpdateDate = DateTime.Now;
            _context.Entry(addonsetup).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }
        public void Create_SapSetup(SAPSetup sap, int id)
        {
                sap.CreateDate = DateTime.Now;
                sap.IsActive = true;
                sap.CreateUserID = id;
                _context.SAPSetup.Add(sap);
                _context.SaveChanges();
            
        }
        public bool Update_SapSetup(SAPSetup sap, int id)
        {
            var sapsetup = sap;
            sapsetup.UpdateUserID = id;
            sapsetup.UpdateDate = DateTime.Now;
            _context.Entry(sapsetup).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public void Create_APISetup(APISetup api, int id)
        {
            api.CreateDate = DateTime.Now;
            api.IsActive = true;
            api.CreateUserID = id;
            _context.APISetups.Add(api);
            _context.SaveChanges();
        }

        public void Create_EmailSetup(EmailSetup email, int id)
        {
            email.CreateDate = DateTime.Now;
            email.IsActive = true;
            email.CreateUserID = id;
            email.Password = Cryption.Encrypt($"{email.Password}");
            _context.EmailSetup.Add(email);
            _context.SaveChanges();
        }
        public bool Update_APISetup(APISetup api, int id)
        {
            var apisetup = api;
            apisetup.UpdateUserID = id;
            apisetup.CreateUserID = id;
            apisetup.UpdateDate = DateTime.Now;
            apisetup.CreateDate = DateTime.Now;
            _context.Entry(apisetup).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            //SaveChanges();
            return true;
        }

        public bool Update_Query(QueryManager api, int id)
        {
            var queryManager = api;
            queryManager.UpdateUserId = id;
            queryManager.CreateUserId = id;
            queryManager.CreateDate = DateTime.Now;
            queryManager.UpdateDate = DateTime.Now;
            _context.Entry(queryManager).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            //SaveChanges();
            return true;
        }
        public bool Update_QueryMap(QueryManagerMap map, int id, List<string[]> StoreHeaderVal)
        {
            var valueID = 0;
            var queryId = 0;

            using (var db = new LinkboxDb())
            {
                using (DbContextTransaction dbtransaction = db.Database.BeginTransaction())
                {

                    try
                    {
                        if (StoreHeaderVal == null) StoreHeaderVal = new List<string[]>();
                        foreach (var item in StoreHeaderVal)
                        {
                            if (item[5].ToString() != "0")
                            {
                                valueID = int.Parse(item[5].ToString());
                            }
                            else
                            {
                                valueID = 0;
                            }

                            queryId = int.Parse(item[4].ToString());
                            var dtexist = _context.QueryManagerMap.Where(x => x.Id == valueID && x.QueryId == queryId).Any(); // SourceField->Destination Field

                            if (dtexist)
                            {
                                var querymapManager = _context.QueryManagerMap.Where(x => x.Id == valueID && x.QueryId == queryId).FirstOrDefault(); // SourceField->Destination Field


                                //var querymapManager = map;

                                querymapManager.Field = item[0].ToString();
                                querymapManager.Condition = item[1].ToString();
                                querymapManager.Value = item[2].ToString();
                                querymapManager.QueryId = int.Parse(item[4].ToString());
                                querymapManager.UpdateUserId = id;
                                querymapManager.CreateUserId = id;
                                querymapManager.DataType = item[6].ToString();
                                querymapManager.CreateDate = DateTime.Now;
                                querymapManager.UpdateDate = DateTime.Now;
                                //_context.Entry(querymapManager).State = System.Data.Entity.EntityState.Modified;
                                _context.SaveChanges();
                                //SaveChanges();


                            }
                            if (valueID == 0 || valueID == null)
                            {
                                var newMap = new QueryManagerMap();

                                newMap.Field = item[0].ToString();
                                newMap.Condition = item[1].ToString();
                                newMap.Value = item[2].ToString();
                                newMap.QueryId = int.Parse(item[4].ToString());
                                newMap.CreateDate = DateTime.Now;
                                newMap.UpdateDate = DateTime.Now;
                                newMap.CreateUserId = id;
                                newMap.IsActive = true;




                                _context.QueryManagerMap.Add(newMap);
                                _context.SaveChanges();

                                SaveChanges();

                                item[5] = newMap.Id.ToString();


                            }




                        }

                        var datamap = _context.QueryManagerMap.Where(sel => sel.QueryId == queryId).ToList();
                        var itemsToDelete = new List<QueryManagerMap>();

                        datamap.ForEach(fe =>
                        {
                            if (!StoreHeaderVal.Any(sel => sel[5].ToString() == fe.Id.ToString() && sel[4].ToString() == fe.QueryId.ToString()))
                            {
                                itemsToDelete.Add(fe);
                            }
                        });

                        if (itemsToDelete.Count > 0)
                        {
                            _context.QueryManagerMap.RemoveRange(itemsToDelete);
                            _context.SaveChanges();
                        }

                        dbtransaction.Commit();

                        return true;
                    }
                    catch (DbEntityValidationException ex)
                    {
                        dbtransaction.Rollback();
                        return false;
                    }
                }
            }



        }


        public bool Update_EmailSetup(EmailSetup emailSetup, int id)
        {
            var email = emailSetup;
            email.UpdateUserID = id;
            email.IsActive = emailSetup.IsActive;
            email.CreateDate = DateTime.Now;
            email.UpdateDate = DateTime.Now;
            email.Password = Cryption.Encrypt($"{email.Password}");
            _context.Entry(email).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }
        public void Create_PathSetup(PathSetup path, int id)
        {
            path.CreateDate = DateTime.Now;
            path.IsActive = true;
            path.CreateUserID = id;
            _context.PathSetup.Add(path);
            _context.SaveChanges();
        }
        public SetupCreateViewModel Find_Addon(int id)
        {
            var model = new SetupCreateViewModel();
            model.AddonView = _context.AddonSetup
                                .Where(x => x.AddonId == id)
                                .Select((x) => new SetupCreateViewModel.AddonViewModel
                                {
                                    AddonCode = x.AddonCode,
                                    AddonDBVersion = x.AddonDBVersion,
                                    AddonServerName = x.AddonServerName,
                                    AddonIPAddress = x.AddonIPAddress,
                                    AddonDBName = x.AddonDBName,
                                    AddonPort = x.AddonPort,
                                    AddonDBuser = x.AddonDBuser,
                                    AddonDBPassword = x.AddonDBPassword,
                                    IsActive = x.IsActive,
                                }).ToList();
            return model;
        }

        public SetupCreateViewModel Find_ConnectionString(string Type)
        {
            var model = new SetupCreateViewModel();

            switch (Type)
            {
                case "SAP":
                    model.DatabaseConnectionView = _context.SAPSetup
                                .Where(x => x.IsActive == true)
                                .Select((x) => new SetupCreateViewModel.DbConnection
                                {
                                    Id = x.SAPId,
                                    QueryName = x.SAPCode
                                }).ToList();
                    break;
                default:
                    model.DatabaseConnectionView = _context.AddonSetup
                                .Where(x => x.IsActive == true)
                                .Select((x) => new SetupCreateViewModel.DbConnection
                                {
                                    Id = x.AddonId,
                                    QueryName = x.AddonCode
                                }).ToList();
                    break;
            }

            return model;
        }

        public SetupCreateViewModel GetConnection(string Type, string ConString)
        {
            var model = new SetupCreateViewModel();

            switch (Type)
            {
                case "SAP":
                    model.DatabaseConnectionView = _context.SAPSetup
                                .AsEnumerable()
                                .Where(x => x.IsActive == true && x.SAPId == Convert.ToInt32(ConString))
                                .Select((x) => new SetupCreateViewModel.DbConnection
                                {
                                    Id = x.SAPId,
                                    QueryName = x.SAPCode,
                                    ConnectionType = x.SAPDBVersion,
                                    ConnectionString = (x.SAPDBVersion.Contains("HANA") ? "DRIVER={HDBODBC32};" + $"SERVERNODE={x.SAPServerName}{(x.SAPDBPort > 0 ? $":{x.SAPDBPort}" : "")};UID={x.SAPDBuser};PWD={x.SAPDBPassword};CS={x.SAPDBName}" :
                                                                                           $"Data Source={x.SAPServerName}{(x.SAPDBPort > 0 ? $":{x.SAPDBPort}" : "")};Initial Catalog={x.SAPDBName};Persist Security Info=True;User ID={x.SAPDBuser};Password={x.SAPDBPassword}")
                                }).ToList();
                    break;
                default:
                    model.DatabaseConnectionView = _context.AddonSetup
                                .AsEnumerable()
                                .Where(x => x.IsActive == true && x.AddonId == Convert.ToInt32(ConString))
                                .Select((x) => new SetupCreateViewModel.DbConnection
                                {
                                    Id = x.AddonId,
                                    QueryName = x.AddonCode,
                                    ConnectionType = x.AddonDBVersion,
                                    ConnectionString = (x.AddonDBVersion.Contains("HANA") ? "DRIVER={HDBODBC32};" + $"SERVERNODE={x.AddonServerName}{(x.AddonPort > 0 ? $":{x.AddonPort}" : "")};UID={x.AddonDBuser};PWD={x.AddonDBPassword};CS={x.AddonDBName}" :
                                                                                           $"Data Source={x.AddonServerName}{(x.AddonPort > 0 ? $":{x.AddonPort}" : "")};Initial Catalog={x.AddonDBName};Persist Security Info=True;User ID={x.AddonDBuser};Password={x.AddonDBPassword}")
                                }).ToList();
                    break;
            }

            return model;
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
                    ret = "04/01/2023";
                    //ret = firstDateOfMonth.ToShortDateString();
                    break;
                case "last_Date":
                    Console.WriteLine("Last Date of Month: {0}", lastDateOfMonth.ToShortDateString());
                    ret = "04/30/2023";
                    //ret = lastDateOfMonth.ToShortDateString();
                    break;
                default:
                    Console.WriteLine("Invalid option selected.");
                    ret = currentDate.ToShortDateString();
                    break;
            }

            return ret;
        }


        public DataTable Fill_DataTable(SetupCreateViewModel model, string Query,int Id)
        {
            string QueryString = Query;
            var parameter = _context.QueryManagerMap.Where(x => x.QueryId == Id).ToList();

            foreach (var param in parameter)
            {

                var field = param.Field.ToString(); // characterToreplace
                                                    //var Condition = param.Condition.ToString();
                var date_param = "'" + GetDates(param.Value.ToString()) + "'"; // replacement
                                                                               //var date_param = GetDates(param.Value);
                


                QueryString =QueryString.Replace(field, date_param);



            }
            DataTable dt = new DataTable();
            foreach (var item in model.DatabaseConnectionView)
            {
                if (item.ConnectionType.Contains("HANA"))
                {
                    dt = DataAccess.SelectHana(item.ConnectionString, QueryString);
                }
                else
                {
                    dt = DataAccess.Select(item.ConnectionString, QueryString);
                }
            }
            return dt;
        }

        public void Create_Query(QueryManager query, int id)
        {
            query.CreateDate = DateTime.Now;
            query.UpdateDate = DateTime.Now;
            query.IsActive = true;
            query.CreateUserId = id;
            _context.QueryManager.Add(query);
            _context.SaveChanges();
        }

        public void Create_DocMap(DocumentMap map, List<string[]> storeval, int id, int doc_id)
        {
            foreach (var item in storeval)
            {
                var CrystalParameter = item[0].ToString();
                var QueryField = item[1].ToString();
                var model = _context.DocumentMaps.Where(x => x.DocId == doc_id && x.CrystalParam == CrystalParameter).FirstOrDefault();


                if (model != null)
                {
                    
                        model.QueryField = item[1].ToString();
                        model.UpdateDate = DateTime.Now;

                        _context.Entry(model).State = EntityState.Modified;
                        _context.SaveChanges();


                }
                else
                {
                    map.CrystalParam = item[0].ToString();
                    map.QueryField = item[1].ToString();
                    map.DocId = doc_id;
                    map.CreateDate = DateTime.Now;
                    map.CreateUserId = id;
                    _context.DocumentMaps.Add(map);
                    _context.SaveChanges();

                }

            }
          

        }
        public void Create_QueryMap(QueryManagerMap map, List<string[]> headerval, int id, int newid)
        {
            if (headerval != null)
            {
                foreach (var item in headerval)
                {

                    map.Field = item[1].ToString();
                    map.Condition = item[3].ToString();
                    map.Value = item[4].ToString();
                    map.DataType = item[2].ToString();
                    map.QueryId = int.Parse(item[0].ToString());
                    map.CreateDate = DateTime.Now;
                    map.UpdateDate = DateTime.Now;
                    map.CreateUserId = id;
                    map.IsActive = true;





                    _context.QueryManagerMap.Add(map);
                    _context.SaveChanges();
                }

            }

        }

        public SetupCreateViewModel Find_SAP(int id)
        {
            var model = new SetupCreateViewModel();
            model.SAPView = _context.SAPSetup.Where(x => x.SAPId == id).Select((x) => new SetupCreateViewModel.SAPViewModel
            {
                SAPCode = x.SAPCode,
                SAPDBVersion = x.SAPDBVersion,
                SAPLicensePort = x.SAPLicensePort,
                SAPSldServer = x.SAPSldServer,
                SAPServerName = x.SAPServerName,
                SAPIPAddress = x.SAPIPAddress,
                SAPDBPort = x.SAPDBPort,
                SAPDBName = x.SAPDBName,
                SAPVersion = x.SAPVersion,
                SAPDBuser = x.SAPDBuser,
                SAPDBPassword = x.SAPDBPassword,
                SAPUser = x.SAPUser,
                SAPPassword = x.SAPPassword,
                IsActive = x.IsActive,

            }).ToList();

            return model;
        }

        public SetupCreateViewModel Find_SAPString(string id)
        {
            var model = new SetupCreateViewModel();
            model.SAPView = _context.SAPSetup.Where(x => x.SAPCode == id).Select((x) => new SetupCreateViewModel.SAPViewModel
            {
                SAPCode = x.SAPCode,
                SAPDBVersion = x.SAPDBVersion,
                SAPLicensePort = x.SAPLicensePort,
                SAPSldServer = x.SAPSldServer,
                SAPServerName = x.SAPServerName,
                SAPIPAddress = x.SAPIPAddress,
                SAPDBPort = x.SAPDBPort,
                SAPDBName = x.SAPDBName,
                SAPVersion = x.SAPVersion,
                SAPDBuser = x.SAPDBuser,
                SAPDBPassword = x.SAPDBPassword,
                SAPUser = x.SAPUser,
                SAPPassword = x.SAPPassword,
                IsActive = x.IsActive,

            }).ToList();

            return model;
        }

      

        public SetupCreateViewModel Find_Query(int id)
        {
            var model = new SetupCreateViewModel();


            model.DatabaseConnectionView = _context.QueryManager.Where(x => x.Id == id).Select((x) => new SetupCreateViewModel.DbConnection
            {
                QueryCode = x.QueryCode,
                QueryName = x.QueryName,
                ConnectionString = x.ConnectionString,
                ConnectionType = x.ConnectionType,
                QueryString = x.QueryString,
                IsActive = x.IsActive
            }).ToList();


            return model;
        }

        public QueryManagerViewModel ValidateQueryManager(string code)
        {
            var model = new QueryManagerViewModel();

            model.QueryManagerView = _context.QueryManager.Where(x => x.QueryCode == code).Select((x) => new QueryManagerViewModel.QueryManager
            {
                QueryCode = x.QueryCode,
            }).ToList();
            return model;
        }
        public QueryManagerMapViewModel Find_QueryMap(int id)
        {
            var model = new QueryManagerMapViewModel();


            model.QueryManagerMapView = _context.QueryManagerMap.Where(x => x.QueryId == id).Select((x) => new QueryManagerMapViewModel.QueryManagerMap
            {
                Id = x.Id,
                Field = x.Field,
                Value = x.Value,
                Condition = x.Condition,
                QueryId = x.QueryId,
                DataType = x.DataType,
                IsActive = x.IsActive
            }).ToList();

            return model;
        }

        public SetupCreateViewModel Find_API(int id)
        {
            var model = new SetupCreateViewModel();
            model.APIView = _context.APISetups.Where(x => x.APIId == id).Select((x) => new SetupCreateViewModel.APIViewModel
            {
                APIId = x.APIId,
                APICode = x.APICode,
                APIMethod = x.APIMethod,
                APIModule = x.APIModule,
                APIURL = x.APIURL,
                IsActive = x.IsActive,
                APIKey = x.APIKey,
                APISecretKey = x.APISecretKey,
                Platform =x.Platform,
                AuthCode = x.AuthCode,
                AccessToken = x.AccessToken,
                RefreshToken=x.RefreshToken,
                ShopCode = x.ShopCode,
                APIToken = x.APIToken,
                APILoginUrl = x.APILoginUrl,
                APILoginBody = x.APILoginBody,
                AccessTokenExpiry = x.AccessTokenExpiry.ToString(),
                RefreshTokenExpiry = x.RefreshTokenExpiry.ToString(),
            }).ToList();

            return model;
        }

        public SetupCreateViewModel Find_Email(int id)
        {
            var model = new SetupCreateViewModel();
            model.EmailView = _context.EmailSetup.AsEnumerable().Where(x => x.EmailId == id).Select((x) => new SetupCreateViewModel.EmailViewModel
            {
                EmailId = x.EmailId,
                EmailCode = x.EmailCode,
                CreateDate = x.CreateDate,
                CreateUserID = x.CreateUserID,
                EmailDesc = x.EmailDesc,
                Email = x.Email,
                DisplayName = x.DisplayName,
                Password = Cryption.Decrypt(x.Password).Replace(x.Email, ""),
                SMTPClient = x.SMTPClient,
                Port = x.Port,
                IsActive = x.IsActive,
            }).ToList();

            return model;
        }

        public SetupCreateViewModel Find_Path(int id)
        {
            var model = new SetupCreateViewModel();

            model.PathView = _context.PathSetup
                               .Where(x => x.PathId == id)
                               .Select((x) => new SetupCreateViewModel.PathViewModel
                               {
                                   PathCode = x.PathCode,
                                   LocalPath = x.LocalPath,
                                   BackupPath = x.BackupPath,
                                   ErrorPath = x.ErrorPath,
                                   RemotePath = x.RemotePath,
                                   RemoteServerName = x.RemoteServerName,
                                   RemoteIPAddress = x.RemoteIPAddress,
                                   RemotePort = x.RemotePort,
                                   RemoteUserName = x.RemoteUserName,
                                   RemotePassword = x.RemotePassword,
                                   //FileType = x.FileType,
                                   IsActive = x.IsActive,

                               }).ToList();
            var localpath = model.PathView.Select(x => x.LocalPath).FirstOrDefault();
            //var files = Directory.GetFiles($@"{localpath}", "*.*", SearchOption.TopDirectoryOnly).Where(s => s.ToLower().EndsWith(".csv") || s.ToLower().EndsWith(".xls") || s.ToLower().EndsWith(".xlsx"));
            //model.FileList = files.Select(sel => new SetupCreateViewModel.Files
            //{
            //    FileName = Path.GetFileName(sel).ToLower(),
            //    FilePath = sel,
            //}).OrderByDescending(x => x.FileName.IndexOf("master", StringComparison.OrdinalIgnoreCase) >= 0)
            //    .ThenByDescending(x => x.FileName.StartsWith("O", StringComparison.OrdinalIgnoreCase))
            //    .ThenBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
            //    .ToList();

            return model;
        }

        public MapCreateViewModel PopulateSAPDBTable(string Schema, string Module, SetupCreateViewModel.SAPViewModel SAPDetails)
        {
            List<DataRow> itemExtDet = DataAccess.Select(SAPDetails.SAPDBVersion,
                                                              SAPDetails.SAPServerName,
                                                              SAPDetails.SAPDBuser,
                                                              SAPDetails.SAPDBPassword,
                                                              SAPDetails.SAPDBName,
                                                                                    SAPDetails.SAPDBVersion.Contains("HANA") ?
                                                                                    $@"SELECT COLUMN_NAME, DATA_TYPE_NAME
                                                                                        ,CASE WHEN DATA_TYPE_NAME = 'DECIMAL'
			                                                                                        THEN CAST(""LENGTH"" || ',' || ""SCALE"" as nvarchar)
                                                                                                 ELSE
                                                                                                    cast(""LENGTH"" as nvarchar)
                                                                                                END ""LENGTH""
                                                                                        , POSITION
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{Schema}' AND TABLE_NAME = '{Module}'
                                                                                    ORDER BY POSITION"
                                                                                    :
                                                                                    $@"SELECT COLUMN_NAME, DATA_TYPE as ""DATA_TYPE_NAME"", CHARACTER_MAXIMUM_LENGTH as ""LENGTH""
                                                                                    FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME = '{Module}' ")
                                                              .AsEnumerable().Select(x => x).ToList();

            var model = new MapCreateViewModel();

            model.Headers = itemExtDet
                               .Select((x) => new MapCreateViewModel.Header
                               {
                                   TableName = x["COLUMN_NAME"].ToString(),
                                   DataType = x["DATA_TYPE_NAME"].ToString(),
                                   Length = x["LENGTH"].ToString(),
                                   //RemotePath = x.RemotePath,
                                   //RemoteServerName = x.RemoteServerName,
                                   //RemoteIPAddress = x.RemoteIPAddress,
                                   //RemotePort = x.RemotePort,
                                   //RemoteUserName = x.RemoteUserName,
                                   //RemotePassword = x.RemotePassword,
                                   ////FileType = x.FileType,
                                   //IsActive = x.IsActive,

                               }).ToList();

            return model;
        }

        public MapCreateViewModel PopulateSAPDBTableRow(string Schema, string Module, SetupCreateViewModel.SAPViewModel SAPDetails)
        {

            List<DataRow> itemExtDet = DataAccess.Select(SAPDetails.SAPDBVersion,
                                                              SAPDetails.SAPServerName,
                                                              SAPDetails.SAPDBuser,
                                                              SAPDetails.SAPDBPassword,
                                                              SAPDetails.SAPDBName,
                                                                                    SAPDetails.SAPDBVersion.Contains("HANA") ?
                                                                                    $@"SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE_NAME
                                                                                        ,CASE WHEN DATA_TYPE_NAME = 'DECIMAL'
			                                                                                        THEN CAST(""LENGTH"" || ',' || ""SCALE"" as nvarchar)

                                                                                                 ELSE

                                                                                                    cast(""LENGTH"" as nvarchar)

                                                                                                END ""LENGTH""
                                                                                        , POSITION
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{Schema}' AND TABLE_NAME LIKE '{Module.Substring(1, Module.Length - 1)}%' AND TABLE_NAME <> '{Module}'
                                                                                    ORDER BY TABLE_NAME, POSITION"
                                                                                    :
                                                                                    $@"SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE as ""DATA_TYPE_NAME"", CHARACTER_MAXIMUM_LENGTH as ""LENGTH""
                                                                                    FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME LIKE '{Module.Substring(1, Module.Length - 1)}%' AND TABLE_NAME <> '{Module}'
                                                                                    ORDER BY TABLE_NAME")
                                                              .AsEnumerable().Select(x => x).ToList();

            var model = new MapCreateViewModel();

            model.Headers = itemExtDet
                               .Select((x) => new MapCreateViewModel.Header
                               {
                                   SourceFieldId = x["COLUMN_NAME"].ToString(),
                                   TableName = x["TABLE_NAME"].ToString(),
                                   DataType = x["DATA_TYPE_NAME"].ToString(),
                                   Length = x["LENGTH"].ToString(),
                                   //RemotePath = x.RemotePath,
                                   //RemoteServerName = x.RemoteServerName,
                                   //RemoteIPAddress = x.RemoteIPAddress,
                                   //RemotePort = x.RemotePort,
                                   //RemoteUserName = x.RemoteUserName,
                                   //RemotePassword = x.RemotePassword,
                                   ////FileType = x.FileType,
                                   //IsActive = x.IsActive,

                               }).ToList();

            return model;
        }

        public MapCreateViewModel PopulateSAPDBTableRequired(string Schema, string Module, SetupCreateViewModel.SAPViewModel SAPDetails)
        {

            List<DataRow> itemExtDet = DataAccess.SelectHana(QueryAccess.HANA_conString(
                                                              SAPDetails.SAPServerName,
                                                              SAPDetails.SAPDBuser,
                                                              SAPDetails.SAPDBPassword,
                                                              SAPDetails.SAPDBName),
                                                              SAPDetails.SAPDBVersion.Contains("HANA") ?
                                                                                    $@"SELECT COLUMN_NAME, DATA_TYPE_NAME
                                                                                        ,CASE WHEN DATA_TYPE_NAME = 'DECIMAL'
			                                                                                        THEN CAST(""LENGTH"" || ',' || ""SCALE"" as nvarchar)

                                                                                                 ELSE

                                                                                                    cast(""LENGTH"" as nvarchar)

                                                                                                END ""LENGTH""
                                                                                        , POSITION
                                                                                        ,CASE when IS_NULLABLE = 'TRUE' THEN 'NULL' ELSE 'NOT NULL' END ""IS_NULLABLE""
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{Schema}' AND TABLE_NAME = '{Module}'
                                                                                    ORDER BY POSITION"
                                                                                    :
                                                                                    $@"SELECT COLUMN_NAME, DATA_TYPE as ""DATA_TYPE_NAME"", CHARACTER_MAXIMUM_LENGTH as ""LENGTH""
                                                                                        ,CASE when IS_NULLABLE = 'YES' THEN 'NULL' ELSE 'NOT NULL' END ""IS_NULLABLE""
                                                                                    FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME = '{Module}' ")
                                                              .AsEnumerable().Select(x => x).ToList();

            var model = new MapCreateViewModel();

            model.Headers = itemExtDet
                               .Select((x) => new MapCreateViewModel.Header
                               {
                                   TableName = x["COLUMN_NAME"].ToString(),
                                   DataType = x["DATA_TYPE_NAME"].ToString(),
                                   Length = x["LENGTH"].ToString(),
                                   IsNullable = x["IS_NULLABLE"].ToString(),
                                   //RemotePath = x.RemotePath,
                                   //RemoteServerName = x.RemoteServerName,
                                   //RemoteIPAddress = x.RemoteIPAddress,
                                   //RemotePort = x.RemotePort,
                                   //RemoteUserName = x.RemoteUserName,
                                   //RemotePassword = x.RemotePassword,
                                   ////FileType = x.FileType,
                                   //IsActive = x.IsActive,

                               }).ToList();

            return model;
        }

        public MapCreateViewModel PopulateSAPDBTableRowRequired(string Schema, string Module, SetupCreateViewModel.SAPViewModel SAPDetails)
        {

            List<DataRow> itemExtDet = DataAccess.Select(SAPDetails.SAPDBVersion,
                                                              SAPDetails.SAPServerName,
                                                              SAPDetails.SAPDBuser,
                                                              SAPDetails.SAPDBPassword,
                                                              SAPDetails.SAPDBName,
                                                              SAPDetails.SAPDBVersion.Contains("HANA") ?
                                                                                    $@"SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE_NAME
                                                                                        ,CASE WHEN DATA_TYPE_NAME = 'DECIMAL'
			                                                                                        THEN CAST(""LENGTH"" || ',' || ""SCALE"" as nvarchar)

                                                                                                 ELSE

                                                                                                    cast(""LENGTH"" as nvarchar)

                                                                                                END ""LENGTH""
                                                                                        , POSITION
                                                                                        ,CASE when IS_NULLABLE = 'TRUE' THEN 'NULL' ELSE 'NOT NULL' END ""IS_NULLABLE""
                                                                                    FROM SYS.COLUMNS
                                                                                    WHERE SCHEMA_NAME = '{Schema}' AND TABLE_NAME LIKE '{Module.Substring(1, Module.Length - 1)}%' AND TABLE_NAME <> '{Module}'
                                                                                    ORDER BY TABLE_NAME, POSITION"
                                                                                    :
                                                                                    $@"SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE as ""DATA_TYPE_NAME"", CHARACTER_MAXIMUM_LENGTH as ""LENGTH"" 
                                                                                        ,CASE when IS_NULLABLE = 'YES' THEN 'NULL' ELSE 'NOT NULL' END ""IS_NULLABLE""
                                                                                    FROM INFORMATION_SCHEMA.COLUMNS  
                                                                                    WHERE TABLE_NAME LIKE '{Module.Substring(1, Module.Length - 1)}%' AND TABLE_NAME <> '{Module}'
                                                                                    ORDER BY TABLE_NAME")
                                                              .AsEnumerable().Select(x => x).ToList();
            var model = new MapCreateViewModel();

            model.Headers = itemExtDet
                               .Select((x) => new MapCreateViewModel.Header
                               {
                                   SourceFieldId = x["COLUMN_NAME"].ToString(),
                                   TableName = x["TABLE_NAME"].ToString(),
                                   DataType = x["DATA_TYPE_NAME"].ToString(),
                                   Length = x["LENGTH"].ToString(),
                                   IsNullable = x["IS_NULLABLE"].ToString(),
                                   //RemotePath = x.RemotePath,
                                   //RemoteServerName = x.RemoteServerName,
                                   //RemoteIPAddress = x.RemoteIPAddress,
                                   //RemotePort = x.RemotePort,
                                   //RemoteUserName = x.RemoteUserName,
                                   //RemotePassword = x.RemotePassword,
                                   ////FileType = x.FileType,
                                   //IsActive = x.IsActive,

                               }).ToList();

            return model;
        }

        public SetupCreateViewModel Find_File(int id)
        {
            var model = new SetupCreateViewModel();

            model.DocumentList = _context.Documents
                               .Where(x => x.Id == id)
                               .Select((x) => new SetupCreateViewModel.Document
                               {
                                   Code = x.Code,
                                   Id = x.Id,
                                   FileName = x.FileName,
                                   SavePath = x.SavePath,
                                   IsActive = x.IsActive,
                                   Credential = x.Credential,
                                   QueryManagerID = x.QueryManagerId
                               }).ToList();

            return model;
        }
        //public static string GetDates(string option)
        //{
        //    DateTime currentDate = DateTime.Today;
        //    DateTime firstDateOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        //    DateTime lastDateOfMonth = firstDateOfMonth.AddMonths(1).AddDays(-1);

        //    string ret = "";
        //    switch (option)
        //    {
        //        case "current_Date":
        //            Console.WriteLine("Current Date: {0}", currentDate.ToShortDateString());
        //            ret = currentDate.ToShortDateString();
        //            break;
        //        case "first_Date":
        //            Console.WriteLine("First Date of Month: {0}", firstDateOfMonth.ToShortDateString());
        //            ret = "04/01/2023";
        //            ret = firstDateOfMonth.ToShortDateString();
        //            break;
        //        case "last_Date":
        //            Console.WriteLine("Last Date of Month: {0}", lastDateOfMonth.ToShortDateString());
        //            ret = "04/30/2023";
        //            ret = lastDateOfMonth.ToShortDateString();
        //            break;
        //        default:
        //            Console.WriteLine("Invalid option selected.");
        //            ret = currentDate.ToShortDateString();
        //            break;
        //    }

        //    return ret;
        //}

        public SetupCreateViewModel GetCrystalParam(int id)
        {

            var viewmodel = new SetupCreateViewModel();
            var document = _context.Documents
                               .Where(x => x.Id == id)
                               .Select(x => new SetupCreateViewModel.Document
                               {
                                   Id = x.Id,
                                   SavePath = x.FilePath,
                                   FileName = x.FileName,
                                   QueryManagerID = x.QueryManagerId
                               }).FirstOrDefault();

            var qmModel = _context.QueryManager
                               .Where(x => x.Id == document.QueryManagerID)
                               .Select(x => new MapCreateViewModel.QueryManager  //.QueryManagerDocView
                               {
                                   Id = x.Id,
                                   QueryString = x.QueryString,
                                   QueryCode = x.QueryCode,
                                   ConnectionString = x.ConnectionString,
                                   ConnectionType = x.ConnectionType,
                               }).FirstOrDefault();

            var qmMapsModel = _context.QueryManagerMap
                              .Where(x => x.QueryId == document.QueryManagerID)
                              .Select(x => new MapCreateViewModel.QueryManagerMap  //.QueryManagerDocView
                              {
                                  Id = x.Id,
                                  Field = x.Field,
                                  Value=x.Value,
                                  DataType= x.DataType,
                              }).ToList();

                var QueryString = qmModel.QueryString;

          



            foreach (var param in qmMapsModel)
            {

                var field = param.Field.ToString(); // characterToreplace
                                                    //var Condition = param.Condition.ToString();
                var date_param = "'" + GetDates(param.Value.ToString()) + "'"; // replacement
                                                                               //var date_param = GetDates(param.Value);



                QueryString = QueryString.Replace(field, date_param);



            }
            var selectTop1String = QueryString;//.Replace("SELECT", "SELECT TOP 1 ");

            var dataTable = Fill_DataTable(GetConnection(qmModel.ConnectionType, qmModel.ConnectionString), selectTop1String,qmModel.Id);

            List<string> ParamList1 = new List<string>();
            List<string> Param = new List<string>();
            viewmodel.Columns = dataTable.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToList();




            //ReportDocument repDoc = new ReportDocument();
            //repDoc.Load(document.SavePath);
            //ParameterFieldDefinitions crParameterdef;
            //crParameterdef = repDoc.DataDefinition.ParameterFields;
            //List<Dictionary<string, object>> ParamList = new List<Dictionary<string, object>>();

            //foreach (ParameterFieldDefinition def in crParameterdef)
            //{
            //    var docM = new Dictionary<string, object>();
            //    var docMapModel = _context.DocumentMaps.Where(x => x.DocId == id && x.CrystalParam == def.Name).FirstOrDefault();
            //    if (string.IsNullOrEmpty(def.ReportName))
            //    {
            //        docM.Add("cr_param", def.Name);
            //        docM.Add("id", docMapModel != null ? docMapModel.Id : 0);
            //        docM.Add("val", docMapModel != null ? docMapModel.QueryField : null);
            //        ParamList.Add(docM);
            //    }
            //}
            //viewmodel.DocumentMapDict = ParamList;

            return viewmodel;
        }

        public List<string> FindQueryString(int id)
        {
            var model = _context.QueryManager
                               .Where(x => x.Id == id)
                               .Select(x => new MapCreateViewModel.QueryManager  //.QueryManagerDocView
                               {
                                   Id = x.Id,
                                   QueryString = x.QueryString,
                                   QueryCode = x.QueryCode,
                                   ConnectionString = x.ConnectionString,
                                   ConnectionType = x.ConnectionType,
                               }).FirstOrDefault();

            var QueryString = model.QueryString;
            var selectTop1String = QueryString.Replace("SELECT", "SELECT TOP 1 ");
            var dataTable = Fill_DataTable(GetConnection(model.ConnectionType, model.ConnectionString), selectTop1String,model.Id);
            List<string> ParamList = new List<string>();
            List<string> Param = new List<string>();
            var column = dataTable.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToList();
            
     
           
            column.ForEach(x =>
            {
                ParamList.Add(x);
                //var model2 = _context.DocumentMaps.Where(model2 => model2.QueryField == ParamList.ToString()).FirstOrDefault();
                //if (model2.ToString() == x)
                //{
                //    Param.Add(x);
                //}
            });
            Param = ParamList.Where(x => _context.DocumentMaps.Any(m => m.QueryField == x))
                 .ToList();

            return ParamList;
        }



        public SetupCreateViewModel FindModule(int id)
        {
            var model = new SetupCreateViewModel();

            model.ModuleSetup = _context.ModuleSetup
                               .Where(x => x.Id == id)
                               .Select((x) => new SetupCreateViewModel.ModuleSetupViewModel
                               {
                                   Id = x.Id,
                                   ModuleCode = x.ModuleCode,
                                   ModuleName = x.ModuleName,
                                   PrimaryKey = x.PrimaryKey,
                                   EntityType = x.EntityType,
                                   EntityName = x.EntityName,
                                   IsActive = x.IsActive
                               }).FirstOrDefault();

            return model;
        }

        public SetupCreateViewModel FindCompany(int id)
        {
            var model = new SetupCreateViewModel();

            model.CompanyDetails = _context.CompanyDetails
                               .Where(x => x.Id == id)
                               .Select((x) => new SetupCreateViewModel.Company
                               {
                                   Id = x.Id,
                                   FileName = x.FileName,
                                   IsActive = x.IsActive,
                                   Address = x.Address,
                                   MobileNo = x.MobileNo,
                                   TelNo = x.TelNo,
                                   CompanyName = x.CompanyName,
                                   FilePath = x.FilePath
                               }).FirstOrDefault();

            return model;
        }
        public SetupCreateViewModel Find_Addon(string code)
        {
            var model = new SetupCreateViewModel();
            model.AddonView = _context.AddonSetup
                                .Where(x => x.AddonCode == code)
                                .Select((x) => new SetupCreateViewModel.AddonViewModel
                                {
                                    AddonCode = x.AddonCode,
                                    AddonDBVersion = x.AddonDBVersion,
                                    AddonServerName = x.AddonServerName,
                                    AddonIPAddress = x.AddonIPAddress,
                                    AddonDBName = x.AddonDBName,
                                    AddonPort = x.AddonPort,
                                    AddonDBuser = x.AddonDBuser,
                                    AddonDBPassword = x.AddonDBPassword,
                                    IsActive = x.IsActive,
                                }).ToList();
            return model;
        }
        public SetupCreateViewModel Find_SAP(string code)
        {
            var model = new SetupCreateViewModel();
            model.SAPView = _context.SAPSetup.Where(x => x.SAPCode == code).Select((x) => new SetupCreateViewModel.SAPViewModel
            {
                SAPCode = x.SAPCode,
                SAPDBVersion = x.SAPDBVersion,
                SAPLicensePort = x.SAPLicensePort,
                SAPSldServer = x.SAPSldServer,
                SAPServerName = x.SAPServerName,
                SAPIPAddress = x.SAPIPAddress,
                SAPDBPort = x.SAPDBPort,
                SAPDBName = x.SAPDBName,
                SAPVersion = x.SAPVersion,
                SAPDBuser = x.SAPDBuser,
                SAPDBPassword = x.SAPDBPassword,
                SAPUser = x.SAPUser,
                SAPPassword = x.SAPPassword,
                IsActive = x.IsActive,

            }).ToList();

            return model;
        }
        public SetupCreateViewModel Find_Path(string code)
        {
            var model = new SetupCreateViewModel();

            model.PathView = _context.PathSetup
                               .Where(x => x.PathCode == code)
                               .Select((x) => new SetupCreateViewModel.PathViewModel
                               {
                                   PathCode = x.PathCode,
                                   LocalPath = x.LocalPath,
                                   BackupPath = x.BackupPath,
                                   ErrorPath = x.ErrorPath,
                                   RemotePath = x.RemotePath,
                                   RemoteServerName = x.RemoteServerName,
                                   RemoteIPAddress = x.RemoteIPAddress,
                                   RemotePort = x.RemotePort,
                                   RemoteUserName = x.RemoteUserName,
                                   RemotePassword = x.RemotePassword,
                                   //FileType = x.FileType,
                                   IsActive = x.IsActive,

                               }).ToList();

            return model;
        }
        public bool Update_PathSetup(PathSetup path, int id)
        {
            var pathsetup = path;
            pathsetup.UpdateUserID = id;
            pathsetup.UpdateDate = DateTime.Now;
            _context.Entry(pathsetup).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }
        public SetupCreateViewModel ValidateCode(string code)
        {
            var model = new SetupCreateViewModel();

            model.AddonView = _context.AddonSetup
                                .Where(x => x.AddonCode == code)
                                .Select((x) => new SetupCreateViewModel.AddonViewModel
                                {
                                    AddonCode = x.AddonCode,

                                }).ToList();
            model.PathView = _context.PathSetup
                               .Where(x => x.PathCode == code)
                               .Select((x) => new SetupCreateViewModel.PathViewModel
                               {
                                   PathCode = x.PathCode,

                               }).ToList();
            model.SAPView = _context.SAPSetup
                              .Where(x => x.SAPCode == code)
                              .Select((x) => new SetupCreateViewModel.SAPViewModel
                              {
                                  SAPCode = x.SAPCode,

                              }).ToList();
            model.APIView = _context.APISetups
                           .Where(x => x.APICode == code)
                           .Select((x) => new SetupCreateViewModel.APIViewModel
                           {
                               APICode = x.APICode,

                           }).ToList();
            model.EmailView = _context.EmailSetup
                          .Where(x => x.EmailCode == code)
                          .Select((x) => new SetupCreateViewModel.EmailViewModel
                          {
                              EmailCode = x.EmailCode,

                          }).ToList();

            return model;
        }
       
        public SetupCreateViewModel GetMethod(HttpConfiguration config, string APIUrl)
        {
            var model = new SetupCreateViewModel();
            var apiDescription = new List<SetupCreateViewModel.ApiDescription>();
            var apiDescs = config.Services.GetApiExplorer().ApiDescriptions;
            foreach (var x in apiDescs)
            {
                string method = x.HttpMethod.Method.ToString();
                string path = x.Route.RouteTemplate;
                apiDescription.Add(new SetupCreateViewModel.ApiDescription
                {
                    APIUrl = "http://localhost:40710/" + path,
                    Method = method
                });
            }

            model.APIDesc = apiDescription.Where(x => x.APIUrl == APIUrl).Select(x => new SetupCreateViewModel.ApiDescription
            {
                Method = x.Method
            }).FirstOrDefault();
            return model;
        }
        public void Create_API(APIManager query, int id)
        {
            query.CreateDate = DateTime.Now;
            query.UpdateDate = DateTime.Now;
            query.IsActive = true;
            query.CreateUserId = id;
            query.Method = query.Method;
            query.QueryCode = query.Method.ToLower().Contains("get") ? query.QueryCode : "-";
            query.SAPCode = query.Method.ToLower().Contains("post") ? query.SAPCode : "-";
            query.QueryName = _context.QueryManager.Where(x => x.QueryCode == query.QueryCode).Select(x => x.QueryName).FirstOrDefault();
            _context.APIManager.Add(query);
            _context.SaveChanges();
        }
        public bool Update_APIManager(APIManager api, int id)
        {
            var queryManager = api;
            queryManager.UpdateUserId = id;
            queryManager.CreateUserId = id;
            queryManager.CreateDate = DateTime.Now;
            queryManager.UpdateDate = DateTime.Now;
            queryManager.Method = api.Method;
            queryManager.QueryCode = api.Method.ToLower().Contains("get") ? api.QueryCode : "-";
            queryManager.SAPCode = api.Method.ToLower().Contains("post") ? api.SAPCode : "-";
            queryManager.QueryName = _context.QueryManager.Where(x => x.QueryCode == queryManager.QueryCode).Select(x => x.QueryName).FirstOrDefault();
            _context.Entry(queryManager).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }
        public SetupCreateViewModel Find_APIManager(int id)
        {
            var model = new SetupCreateViewModel();
            model.APIManagerList = _context.APIManager.Where(x => x.Id == id).Select((x) => new SetupCreateViewModel.APIManager
            {
                QueryCode = x.QueryCode,
                QueryName = x.QueryName,
                APICode = x.APICode,
                Method = x.Method,
                Title = x.Title,
                IsActive = x.IsActive
            }).ToList();

            return model;
        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public SetupCreateViewModel GetAPIMethod(string code)
        {
            var model = new SetupCreateViewModel();
            model.APIDetails = _context.APISetups.Where(x => x.APICode == code).Select(x => new SetupCreateViewModel.APIViewModel
            {
                APIMethod = x.APIMethod
            }).FirstOrDefault();

            return model;
        }

        public MapCreateViewModel GetQueries()
        {
            MapCreateViewModel model = new MapCreateViewModel();
            model.QueryManagerView = _context.QueryManager.Select(x => new MapCreateViewModel.QueryManager
            {
                Id = x.Id,
                QueryCode = x.QueryCode,
                //QueryName = x.QueryName
            }).ToList();

            return model;
        }

        public List<SetupCreateViewModel.SAPViewModel> GetSAPSetups()
        {
            SetupCreateViewModel model = new SetupCreateViewModel();
            model.SAPView = _context.SAPSetup.Select(x => new SetupCreateViewModel.SAPViewModel
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

            }).ToList();

            return model.SAPView;
        }

    }
}