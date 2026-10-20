using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Bibliography;
using DomainLayer;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.Record.Chart;
using Remotion.FunctionalProgramming;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Xml;

namespace LinkBoxUI.Services
{
    public class MappingServices : IMappingRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        private SAPAccess sapAces = new SAPAccess();
        private GlobalServices globalServices = new GlobalServices();
        public NetsuiteHelper NShelper = new NetsuiteHelper();
        public  MapCreateViewModel View_Mapping()
        {
            MapCreateViewModel model = new MapCreateViewModel();

            model.FieldView = _context.FieldMappings.Select(x => new MapCreateViewModel.FieldMapping
            {
                MapId = x.MapId,
                MapCode = x.MapCode,
                MapName = x.MapName,
                AddonCode = x.AddonCode,
                SAPCode = x.SAPCode,
                PathCode = x.PathCode,
                HeaderWorksheet = x.HeaderWorksheet,
                RowWorksheet = x.RowWorksheet,
                FileName = x.FileName,
                FileType = x.FileType,
                ModuleName = x.ModuleName,
                DataType = x.DataType,
                APICode = x.APICode,
                DestModule = x.DestModule
            }).OrderBy(x=>x.ModuleName).ToList();

            model.PlatformFieldView = _context.FieldMappings
               .Join(_context.APISetups,
               fm => fm.APICode,
               api => api.APICode,
                (fm, api) => new MapCreateViewModel.FieldMapping
                {
                    MapId = fm.MapId,
                    MapCode = fm.MapCode,
                    MapName = fm.MapName,
                    AddonCode = fm.AddonCode,
                    SAPCode = fm.SAPCode,
                    PathCode = fm.PathCode,
                    HeaderWorksheet = fm.HeaderWorksheet,
                    RowWorksheet = fm.RowWorksheet,
                    FileName = fm.FileName,
                    FileType = fm.FileType,
                    ModuleName = fm.ModuleName,
                    DataType = fm.DataType,
                    APICode = fm.APICode,
                    DestModule = fm.DestModule,
                    Platform = api.Platform ?? ""
                }).OrderBy(x=>x.Platform).ToList();



            model.SAPCodeList = _context.SAPSetup.Where(x => x.IsActive == true).Select(x => new MapCreateViewModel.SAPCodes
            {
                SAPId = x.SAPId,
                SAPCode = x.SAPCode,
                SAPDBVersion = x.SAPDBVersion,
                SAPServerName = x.SAPServerName,
                SAPIPAddress = x.SAPIPAddress,
                SAPDBName = x.SAPDBName,
                SAPVersion = x.SAPVersion,
                SAPDBPort = x.SAPDBPort,
                SAPDBuser = x.SAPDBuser,
                SAPLicensePort = x.SAPLicensePort,
                SAPDBPassword = x.SAPDBPassword,
                SAPUser = x.SAPUser,
                SAPPassword = x.SAPPassword,
            }).ToList();

            model.ModuleSetups = _context.ModuleSetup.Where(x => x.IsActive == true).Select(x => new MapCreateViewModel.ModuleSetupViewModel
            {
                ModuleCode = x.ModuleCode,
                ModuleName = x.ModuleName

            }).ToList();

            model.AddonCodeList = _context.AddonSetup.Where(x => x.IsActive == true).Select(x => new MapCreateViewModel.AddonCodes
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

            }).ToList();

            model.PathCodeList = _context.PathSetup.Where(x => x.IsActive == true).Select(x => new MapCreateViewModel.PathCodes
            {
                PathId = x.PathId,
                PathCode = x.PathCode,
                LocalPath = x.LocalPath,
                BackupPath = x.BackupPath,
                ErrorPath = x.ErrorPath,
                RemotePath = x.RemotePath,
                RemoteIPAddress = x.RemoteIPAddress,
                RemotePort = x.RemotePort,
                RemoteServerName = x.RemoteServerName,
                RemoteUser = x.RemoteUserName,
                RemotePassword = x.RemotePassword

            }).ToList();

            var platforms = new[] { "Lazada", "Shopee", "TikTok" };

            model.APIView = _context.APISetups
                .Where(x => x.IsActive && platforms.Any(p => x.Platform.Contains(p)))
                .Select(x => new MapCreateViewModel.APIViewModel
                {
                    APIId = x.APIId,
                    APICode = x.APICode,
                    APIMethod = x.APIMethod,
                    APIURL = x.APIURL,
                    APIPlatform = x.Platform,
                    APIKey = x.APIKey,
                    APISecretKey = x.APISecretKey,
                    APIToken = x.APIToken,
                }).OrderBy(x=>x.APIPlatform)
                .ToList();

            model.OPSFieldView = _context.OPSFieldMappings
                .Join(_context.OPSFieldTable, opsfm => opsfm.MapId, opsft => opsft.MapId, (opsfm, opsft)
                => new MapCreateViewModel.OPSFieldMapping

                {
                    MapId = opsfm.MapId,
                    MapCode = opsfm.MapCode,
                    MapName = opsfm.MapName,
                    //SAPCode = opsfm.SAPCode,
                    //ModuleName = opsfm.ModuleName,
                    //PathCode = opsft.PathCode,
                }).GroupBy(x => x.MapId)
                .Select(group => group.FirstOrDefault())
                .ToList();


            model.QueryManagerView = _context.QueryManager.Select(x => new MapCreateViewModel.QueryManager
            {
                Id = x.Id,
                QueryCode = x.QueryCode,
                ConnectionType = x.ConnectionType,
                QueryName = x.QueryName,
                QueryString = x.QueryString,
                ConnectionString = x.ConnectionString,
                IsActive = x.IsActive,
                CreateDate = x.CreateDate,
                CreateUserId = x.CreateUserId
            }).ToList();


            var classFolder = HostingEnvironment.MapPath("~/App_Data/CAS BIR");

            model.ClassificationFiles = (classFolder != null && Directory.Exists(classFolder))
                ? Directory.EnumerateFiles(classFolder, "*.json", SearchOption.TopDirectoryOnly)
                    .Select(path => Path.GetFileName(path))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Select(name => new MapCreateViewModel.ClassificationFile
                    {
                        FileName = name,
                        DisplayName = Path.GetFileNameWithoutExtension(name)
                    })
                    .ToList()
                : new List<MapCreateViewModel.ClassificationFile>();


            return model;


        }

        public MapCreateViewModel View_Query()
        {
            MapCreateViewModel model = new MapCreateViewModel();
            model.QueryManagerView = _context.QueryManager.Select(x => new MapCreateViewModel.QueryManager
            {
                Id = x.Id,
                QueryCode = x.QueryCode,
                QueryName = x.QueryName,
                QueryString = x.QueryString,
                IsActive = x.IsActive,
                CreateDate = x.CreateDate,
                CreateUserId = x.CreateUserId
            }).ToList();

            return model;
        }
        public MapCreateViewModel View_API()
        {
            MapCreateViewModel model = new MapCreateViewModel();
            model.APIManagerView = _context.APIManager.Select(x => new MapCreateViewModel.APIManager
            {
                Id = x.Id,
                Title = x.Title,
                QueryCode = x.QueryCode,
                QueryName = x.QueryName,
                APICode = x.APICode,
                Method = x.Method,
                SAPCode = x.SAPCode,
                IsActive = x.IsActive,
                CreateDate = x.CreateDate,
                CreateUserId = x.CreateUserId
            }).ToList();

            model.APIView = _context.APISetups.Select(x => new MapCreateViewModel.APIViewModel
            {
                APIId = x.APIId,
                APICode = x.APICode,
                APIMethod = x.APIMethod
            }).ToList();

            //model.QueryManagerView = _context.QueryManager.Select(x => new MapCreateViewModel.QueryManager
            //{
            //    Id = x.Id,
            //    QueryCode = x.QueryCode,
            //    //QueryName = x.QueryName
            //}).ToList();

            return model;
        }
        public MapCreateViewModel OPSFind_File(int id)
        {
            var model = new MapCreateViewModel();

            model.OPSFileView = _context.OPSFieldTable.Where(x=>x.MapId == id).Select(x => new MapCreateViewModel.OPSFileMapping
            {
                FileName = x.FileName,
                FileType = x.FileType

            }).ToList();
            return model;
        }
        public bool AddTable_OpsMapId(int id, string TableMap)
        {
            bool isExist = _context.OPSFieldTable.Any(x => x.MapId == id && x.SAPTableNameModule == TableMap);
            return isExist;
        }



        public MapCreateViewModel Find_Map(int id)
        {
            var model = new MapCreateViewModel();
            //var result = developers.GroupJoin(skills, devloper => devloper.SkillID, skill => skill.Id, (devloper, skill) => new {
            //    Key = devloper,
            //    Skills = skill
            //});
            model.FieldView = _context.FieldMappings
                                .Join(_context.QueryManager, j0 => j0.SAPCode, j1 => j1.QueryCode, (j0, j1) => new { J0 = j0, J1 = j1 })
                                .GroupJoin(_context.PathSetup,
                                            fmap => fmap.J0.PathCode,
                                            ps => ps.PathCode,
                                        (fmap, ps) => ps.Select(x => new { fm = fmap, psa = x })
                                            .DefaultIfEmpty(new { fm = fmap, psa = (PathSetup)null }))
                                .SelectMany(g => g)
                                .Where(x => x.fm.J0.MapId == id)
                                .Select((x) => new MapCreateViewModel.FieldMapping
                                {
                                    MapId = x.fm.J0.MapId,
                                    MapCode = x.fm.J0.MapCode,
                                    MapName = x.fm.J0.MapName,
                                    AddonCode = x.fm.J0.AddonCode,
                                    QueryCode = x.fm.J0.SAPCode,
                                    ConnectionString = x.fm.J1.ConnectionString,
                                    QueryString = x.fm.J1.QueryString,
                                    ModuleName = x.fm.J0.ModuleName,
                                    DestModule = x.fm.J0.DestModule,
                                    PathId = x.psa.PathId,
                                    PathCode = x.fm.J0.PathCode,
                                    HeaderWorksheet = x.fm.J0.HeaderWorksheet,
                                    RowWorksheet = x.fm.J0.RowWorksheet,
                                    FileName = x.fm.J0.FileName,
                                    FileType = x.fm.J0.FileType,
                                    APICode = x.fm.J0.APICode,
                                }).ToList();

            //model.ApiParameters = _context.APIParameter.Where(x => x.MapId == id)
            //                    .Select(x => new MapCreateViewModel.APIParameter
            //                    {
            //                        APICode = x.APICode,
            //                        APIParameterKey = x.APIParameter,
            //                        APIParameterValue = x.APIParamValue,
            //                    }).ToList();

            model.Headers = _context.Headers
                              .Where(x => x.MapId == id)
                              .Select((x) => new MapCreateViewModel.Header
                              {


                                  SourceFieldId = x.SourceFieldId,
                                  TableName = x.SourceTableName,

                                  DestinationField = x.DestinationField,
                                  DestinationTableName = x.DestinationTableName,

                                  SourceHeaderStart = x.SourceHeaderStart,
                                  SourceRowStart = x.SourceRowStart,


                                  ConditionalQuery = x.ConditionalQuery,
                                  SourceType = x.SourceType,

                                  VisOrder = x.VisOrder,



                                  DataType = x.DataType,
                                  Length = x.Length,
                                  IsRequired = x.IsKeyValue,
                                  DefaultValue = x.DefaultValue,
                              }).OrderBy(x => x.VisOrder).ToList();

            //model.Rows = _context.Rows
            //               .Where(x => x.MapId == id)
            //               .Select((x) => new MapCreateViewModel.Row
            //               {
            //                   TableName = x.TableName,
            //                   SAPRowFieldId = x.SAPRowFieldId,
            //                   AddonRowField = x.AddonRowField,
            //                   DataType = x.DataType,
            //                   Length = x.Length,
            //                   IsRequired = x.IsRequired,
            //                   DefaultValue = x.DefaultValue,
            //               }).ToList();



            return model;
        }
        public MapCreateViewModel APItoAPIFind_Map(int id)
        {
            var model = new MapCreateViewModel();
          
            model.FieldView = _context.FieldMappings
                                
                                .Where(x => x.MapId == id)
                                .Select((x) => new MapCreateViewModel.FieldMapping
                                {
                                    MapId = x.MapId,
                                    MapCode = x.MapCode,
                                    MapName = x.MapName,
                                    SAPCode = x.SAPCode,
                                    FileName = x.FileName,
                                    FileType = x.FileType,
                                    ModuleName = x.ModuleName,
                                  
                                    APICode = x.APICode,
                                }).ToList();

            model.Headers = _context.Headers
                              .Where(x => x.MapId == id)
                              .Select((x) => new MapCreateViewModel.Header
                              {
                                  SourceFieldId = x.SourceFieldId,
                                  TableName = x.SourceTableName,
                                  DestinationField = x.DestinationField,
                                  DestinationTableName = x.DestinationTableName,
                                  SourceHeaderStart = x.SourceHeaderStart,
                                  SourceRowStart = x.SourceRowStart,
                                  ConditionalQuery = x.ConditionalQuery,
                                  SourceType = x.SourceType,
                                  VisOrder = x.VisOrder,
                                  DataType = x.DataType,
                                  Length = x.Length,
                                  IsRequired = x.IsKeyValue,
                                  DefaultValue = x.DefaultValue,
                              }).OrderBy(x => x.VisOrder).ToList();

            //model.DocumentMapViewList = _context.DocumentMaps
            //                            .Where(x => x.DocId == id)
            //                            .Select((x) => new MapCreateViewModel.DocumentMapView
            //                            {
            //                                CrystalParam = x.CrystalParam,
            //                                QueryField = x.QueryField,
            //                                DocId = x.DocId,
            //                                CreateUserId = x.CreateUserId,
            //                            }).OrderBy(x => x.CreateUserId).ToList();


            return model;
        }

        public MapCreateViewModel DataFileToSAP(int id)
        {
            var model = new MapCreateViewModel();

          
            var fieldMappings = _context.FieldMappings
            .Where(x => x.MapId == id)
            .ToList();

            model.FieldView = fieldMappings
            .Select(x =>
            {
                int.TryParse(x.PathCode, out int pathIdKey); // safe parse, no exception risk
                var matchedPath = _context.PathSetup.FirstOrDefault(p => p.PathId == pathIdKey);

                return new MapCreateViewModel.FieldMapping
                {
                    MapId = x.MapId,
                    MapCode = x.MapCode,
                    MapName = x.MapName,
                    SAPCode = x.SAPCode,
                    FileName = x.FileName,
                    FileType = x.FileType,
                    ModuleName = x.ModuleName,
                    RowWorksheet = x.RowWorksheet,
                    PathCode = matchedPath?.PathCode,
                    PathId = matchedPath?.PathId ?? 0,
                    Localpath = matchedPath?.LocalPath,
                };
            })
            .ToList();

            model.Headers = _context.Headers
                              .Where(x => x.MapId == id)
                              .Select((x) => new MapCreateViewModel.Header
                              {
                                  SourceFieldId = x.SourceFieldId,
                                  TableName = x.SourceTableName,
                                  DestinationField = x.DestinationField,
                                  DestinationTableName = x.DestinationTableName,
                                  SourceHeaderStart = x.SourceHeaderStart,
                                  SourceRowStart = x.SourceRowStart,
                                  ConditionalQuery = x.ConditionalQuery,
                                  SourceType = x.SourceType,
                                  VisOrder = x.VisOrder,
                                  DataType = x.DataType,
                                  Length = x.Length,
                                  IsRequired = x.IsKeyValue,
                                  DefaultValue = x.DefaultValue,
                              }).OrderBy(x => x.VisOrder).ToList();

            //model.DocumentMapViewList = _context.DocumentMaps
            //                            .Where(x => x.DocId == id)
            //                            .Select((x) => new MapCreateViewModel.DocumentMapView
            //                            {
            //                                CrystalParam = x.CrystalParam,
            //                                QueryField = x.QueryField,
            //                                DocId = x.DocId,
            //                                CreateUserId = x.CreateUserId,
            //                            }).OrderBy(x => x.CreateUserId).ToList();


            return model;
        }
        public MapCreateViewModel opsFind_Map(int id)
        {
            var model = new MapCreateViewModel();

            model.OPSFieldMapDetails = _context.OPSFieldMappings.Where(x => x.MapId == id)
                .Select( x => new MapCreateViewModel.OPSFieldMapping
                {
                     MapId= x.MapId,
                     MapCode= x.MapCode,
                     MapName= x.MapName,
                    
                })
                .FirstOrDefault();

            model.OPSTableView = _context.OPSFieldTable.Where(x => x.MapId == id)
                .Join(_context.PathSetup, opsft => opsft.PathCode, ps => ps.PathCode, (opsft, ps) => new MapCreateViewModel.OPSFieldTable
                {
                    SAPTableId = opsft.SAPTableId,
                    MapId = opsft.MapId,
                    SourceTableName = opsft.SourceTableName,
                    SourceColumnName = opsft.SourceColumnName,
                    SourceRowData = opsft.SourceRowData,
                    Pathcode = opsft.PathCode,
                    FileName = opsft.FileName,
                    FileType = opsft.FileType,
                    SAPTableNameModule = opsft.SAPTableNameModule,
                    PathId = ps.PathId

                }).ToList();

            model.FieldSetView = _context.OPSFieldSets
                            .Where(x => x.MapId == id)
                            .Join(_context.OPSFieldTable,opsfs=>opsfs.SAPTableId,opsft=>opsft.SAPTableId,(opsfs,opsft) => new MapCreateViewModel.OPSFieldSet
                            {
                                MapId=opsfs.MapId,
                                SAPTableId= opsfs.SAPTableId,
                                SourceField= opsfs.SourceField,
                                DestinationField= opsfs.DestinationField,
                                DataType = opsfs.DataType,
                                ConditionalQuery = opsfs.ConditionalQuery,
                                SAPTableNameModule = opsft.SAPTableNameModule,
                                Length = opsfs.Length,
                                VisOrder = opsfs.VisOrder,
                                IsRequired = opsfs.IsKeyValue,

                                
                            }).OrderBy(x => x.VisOrder).ToList();


            //model.FieldView = _context.FieldMappings
            //                    .Join(_context.ModuleSetup, j0 => j0.ModuleName, j1 => j1.ModuleCode, (j0, j1) => new { J0 = j0, J1 = j1 })
            //                    .GroupJoin(_context.PathSetup,
            //                                fmap => fmap.J0.PathCode,
            //                                ps => ps.PathCode,
            //                            (fmap, ps) => ps.Select(x => new { fm = fmap, psa = x })
            //                                .DefaultIfEmpty(new { fm = fmap, psa = (PathSetup)null }))
            //                    .SelectMany(g => g)
            //                    .Where(x => x.fm.J0.MapId == id)
            //                    .Select((x) => new MapCreateViewModel.FieldMapping
            //                    {
            //                        MapId = x.fm.J0.MapId,
            //                        MapCode = x.fm.J0.MapCode,
            //                        MapName = x.fm.J0.MapName,
            //                        AddonCode = x.fm.J0.AddonCode,
            //                        SAPCode = x.fm.J0.SAPCode,
            //                        ModuleName = x.fm.J0.ModuleName,
            //                        DestModule = x.fm.J0.DestModule,
            //                        PathId = x.psa.PathId,
            //                        PathCode = x.fm.J0.PathCode,
            //                        HeaderWorksheet = x.fm.J0.HeaderWorksheet,
            //                        RowWorksheet = x.fm.J0.RowWorksheet,
            //                        FileName = x.fm.J0.FileName,
            //                        FileType = x.fm.J0.FileType,
            //                        APICode = x.fm.J0.APICode,
            //                    }).ToList();



            //model.Headers = _context.Headers
            //                  .Where(x => x.MapId == id)
            //                  .Select((x) => new MapCreateViewModel.Header
            //                  {


            //                      SourceFieldId = x.SourceFieldId,
            //                      TableName = x.SourceTableName,

            //                      DestinationField = x.DestinationField,
            //                      DestinationTableName = x.DestinationTableName,

            //                      SourceHeaderStart = x.SourceHeaderStart,
            //                      SourceRowStart = x.SourceRowStart,


            //                      ConditionalQuery = x.ConditionalQuery,
            //                      SourceType = x.SourceType,

            //                      VisOrder = x.VisOrder,



            //                      DataType = x.DataType,
            //                      Length = x.Length,
            //                      IsRequired = x.IsKeyValue,
            //                      DefaultValue = x.DefaultValue,
            //                  }).OrderBy(x => x.VisOrder).ToList();

            return model;
        }

        public MapCreateViewModel Validate_MapCode(string code)
        {
            var model = new MapCreateViewModel();

            model.FieldView = _context.OPSFieldMappings.Where(x => x.MapCode == code).Select((x) => new MapCreateViewModel.FieldMapping
            {
                MapCode = x.MapCode,
            }).ToList();
            return model;
        }
        public MapCreateViewModel GetFieldMap(int id, string tablename)
        {
            var model = new MapCreateViewModel();
            model.Headers = _context.Headers
                   .Where(x => x.MapId == id && x.SourceTableName == tablename)
                   .Select((x) => new MapCreateViewModel.Header
                   {
                       TableName = x.SourceTableName,
                       SourceFieldId = x.SourceFieldId,
                       DestinationField = x.DestinationField,
                       DestinationTableName = x.DestinationTableName,
                       DataType = x.DataType,
                       Length = x.Length,
                       IsRequired = x.IsKeyValue,
                       DefaultValue = x.DefaultValue,
                       VisOrder = x.VisOrder,
                   }).OrderBy(s => s.VisOrder).ToList();
            return model;
        }

        public MapCreateViewModel ValidateMap(string code)
        {

            var model = new MapCreateViewModel();

            model.FieldView = _context.FieldMappings
                                .Where(x => x.MapCode == code)
                                .Select((x) => new MapCreateViewModel.FieldMapping
                                {
                                    MapCode = x.MapCode,
                                }).ToList();
            return model;
        }

        public MapCreateViewModel ValidateMapModule(string SAPCode, string Module)
        {
            var model = new MapCreateViewModel();

            model.FieldView = _context.FieldMappings
                                .Where(x => x.ModuleName == Module && x.SAPCode == SAPCode)
                                .Select((x) => new MapCreateViewModel.FieldMapping
                                {
                                    MapCode = x.MapCode,
                                }).ToList();
            return model;
        }

        public SetupCreateViewModel.AddonViewModel Select_AddonSetup(string code)
        {
            return _context.AddonSetup.Where(x => x.AddonCode == code).Select((x) => new SetupCreateViewModel.AddonViewModel
            {
                AddonDBName = x.AddonDBName,
                AddonIPAddress = x.AddonIPAddress,
                AddonPort = x.AddonPort,
                AddonServerName = x.AddonServerName,
                AddonDBuser = x.AddonDBuser,
                AddonDBPassword = x.AddonDBPassword
            }).FirstOrDefault();
        }

        public SetupCreateViewModel.SAPViewModel Select_SAPSetup(string code)
        {
            return _context.SAPSetup.Where(x => x.SAPCode == code).Select((x) => new SetupCreateViewModel.SAPViewModel
            {
                SAPCode = x.SAPCode,
                SAPServerName = x.SAPServerName,
                SAPSldServer = x.SAPSldServer,
                SAPIPAddress = x.SAPIPAddress,
                SAPDBPort = x.SAPDBPort,
                SAPDBuser = x.SAPDBuser,
                SAPDBPassword = x.SAPDBPassword,
                SAPDBName = x.SAPDBName,
                SAPDBVersion = x.SAPDBVersion,
                SAPUser = x.SAPUser,
                SAPPassword = x.SAPPassword,
                SAPLicensePort = x.SAPLicensePort,
            }).FirstOrDefault();
        }

        public void Create_FieldMapping(FieldMapping field, int id)
        {
            try
            {

                field.MapId = _context.FieldMappings.Select(x => x.MapId).AsEnumerable().LastOrDefault() + 1;
                field.CreateDate = DateTime.Now;
                field.CreateUserID = id;
                _context.FieldMappings.Add(field);
                _context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var eve in ex.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
        }

        public void Update_FieldMapping(FieldMapping field, int id)
        {
            var map = field;
            map.UpdateUserID = id;
            map.UpdateDate = DateTime.Now;
            _context.Entry(map).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
        }

        public void Create_Headers(List<string[]> headers, int id, string table, int check, int newid, int mapid)
        {
            using ( var db = new LinkboxDb())
            {
                using (DbContextTransaction dbtransaction = db.Database.BeginTransaction())
                {

                    try
                    {

                        int index = 1;
                        string groupheader = "";
                        if (headers == null) headers = new List<string[]>();
                        foreach (var item in headers)
                        {
                            var sourcefieldTable = item[8].ToString();
                            if (groupheader == "")
                                groupheader = sourcefieldTable;
                            else if (groupheader != sourcefieldTable)
                            {
                                groupheader = sourcefieldTable;
                                index = 1; //reset to zero for other table header
                            }

                            var _id = (check == 1 ? mapid : newid);


                            var sourcefieldId = item[0].ToString(); // 0->1
                            var modulecode = item[5].ToString();
                           
                            var destfieldId = item[1].ToString();
                            var destfieldTable = item[4].ToString();
                            int.TryParse(item[9].ToString(), out int visorder);
                            var IsHeader = _context.ModuleSetup.Where(sel => sel.ModuleCode == modulecode).Any();
                            var dtexist = _context.Headers.Where(x => x.MapId == newid && x.SourceFieldId == sourcefieldId && x.SourceTableName == sourcefieldTable && x.DestinationField == destfieldId && x.DestinationTableName == destfieldTable).Any(); // SourceField->Destination Field
                            if (dtexist)
                            {
                                var headerdata = _context.Headers.Where(x => x.MapId == newid && x.SourceFieldId == sourcefieldId && x.SourceTableName == sourcefieldTable && x.DestinationField == destfieldId && x.DestinationTableName == destfieldTable).FirstOrDefault();// SourceField->Destination Field
                                headerdata.MapId = newid;
                                headerdata.SourceFieldId = item[0].ToString();
                                headerdata.SourceTableName = item[8].ToString();
                                headerdata.SourceHeaderStart = item[5].ToString();
                                headerdata.SourceRowStart = item[6].ToString();
                                headerdata.DestinationField = destfieldId;
                                headerdata.DestinationTableName = destfieldTable;
                                headerdata.DataType = item[2].ToString();
                                headerdata.ConditionalQuery = item[3].ToString();
                                //header.Length = item[3].ToString();
                                headerdata.IsKeyValue = item[7].ToString() == "NULL" ? false : true;
                                headerdata.CreateDate = DateTime.Now;
                                headerdata.CreateUserID = id;
                                headerdata.SourceType = (IsHeader == true ? "Header" : "Row");
                                //headerdata.VisOrder = index;
                                var arcnt = item.Count() - 1;
                                //if (arcnt == 5)
                                //{
                                //    headerdata.DefaultValue = item[arcnt].ToString() == "NULL" ? "" : item[arcnt].ToString();
                                //}
                                _context.SaveChanges();
                                SaveChanges();
                            }
                            else //if (!string.IsNullOrEmpty(sourcefieldId))
                            {                                         

                                Header header = new Header();
                                header.MapId = newid;
                                header.SourceFieldId = item[0].ToString();
                                header.SourceTableName = item[8].ToString();
                                header.SourceHeaderStart = item[5].ToString();
                                header.SourceRowStart = item[6].ToString();
                                header.DestinationField = item[1].ToString();
                                header.DestinationTableName = item[4].ToString();
                                header.DataType = item[2].ToString();
                                header.ConditionalQuery = item[3].ToString();
                                //header.Length = item[3].ToString();
                                header.IsKeyValue = item[7].ToString() == "NULL" ? false : true;
                                header.CreateDate = DateTime.Now;
                                header.CreateUserID = id;
                                header.SourceType = (IsHeader == true ? "Header" : "Row");
                                header.VisOrder = (visorder == 0 ? index : visorder);
                                var arcnt = item.Count() - 1;
                                //if (arcnt == 5)
                                //{
                                //    header.DefaultValue = item[arcnt].ToString() == "NULL" ? "" : item[arcnt].ToString();
                                //}

                                _context.Headers.Add(header);
                                _context.SaveChanges();
                                SaveChanges();
                            }
                            index++;
                        }

                        ////REMOVE FROM THE LIST IF NOT EXIST                      
                        var datamap = _context.Headers.Where(sel => sel.MapId == newid).ToList();
                        datamap.ForEach(fe =>
                        {
                            if (!headers.AsEnumerable().Where(sel=> sel[0].ToString() == fe.SourceFieldId 
                                                                && sel[8].ToString() == fe.SourceTableName 
                                                                && sel[1].ToString() == fe.DestinationField
                                                                && sel[4].ToString() == fe.DestinationTableName).Any())
                            {
                                var dele = _context.Headers.Where(x => x.MapId == newid && x.VisOrder == fe.VisOrder && x.SourceFieldId == fe.SourceFieldId && x.DestinationField == fe.DestinationField && x.SourceTableName == fe.SourceTableName && x.DestinationTableName == fe.DestinationTableName).FirstOrDefault();
                                if (dele != null)
                                {
                                    _context.Headers.Remove(dele);
                                    _context.SaveChanges();
                                }
                            }
                        });                       

                        dbtransaction.Commit(); //final process
                    }
                    catch (DbEntityValidationException ex)
                    {
                        dbtransaction.Rollback();
                    }
                }
            }
        }

        public void Create_SourceDestinationTable(List<string[]> sourcefieldtb, List<string[]> headerval, int id, string table, int check, int newid, int mapid)
        {


            using (var db = new LinkboxDb())
            {
                using (DbContextTransaction dbtransaction = db.Database.BeginTransaction())
                {

                    try
                    {
                        var _id = (check == 1 ? mapid : newid);
                        foreach (var item in sourcefieldtb)
                        {
                         
                            
                            var fieldtb = _context.OPSFieldTable.AsEnumerable().Where(x=>x.MapId == _id && x.SAPTableNameModule == item[0].ToString()).Any();

                            if (fieldtb)
                            {
                                var tabledata = _context.OPSFieldTable.AsEnumerable().Where(x => x.MapId == _id && x.SAPTableNameModule == item[0].ToString()).FirstOrDefault();

                                tabledata.SourceColumnName = item[1].ToString();
                                tabledata.SourceRowData = item[2].ToString();
                                //tabledata.FileName= item[4].ToString(); comment because sometimes it duplicates? so it causes
                                tabledata.UpdateDate=DateTime.Now;
                                tabledata.UpdateUserID = id;
                                tabledata.SourceTableName = item[6].ToString();
                                
                                _context.Entry(tabledata).State= EntityState.Modified;
                                _context.SaveChanges();
                                //SaveChanges();


                               
                                var dele = _context.OPSFieldSets.AsEnumerable().Where(x => x.MapId == _id && x.SAPTableId == tabledata.SAPTableId).ToList();
                                 foreach(var delete in dele)
                                {
                                    _context.OPSFieldSets.Remove(delete);
                                    _context.SaveChanges();
                                }

                                int index = 1;
                                string groupheader = "";

                                var sourcefieldTable = item[0].ToString();
                                if (groupheader == "")
                                    groupheader = sourcefieldTable;
                                else if (groupheader != sourcefieldTable)
                                {
                                    groupheader = sourcefieldTable;
                                    index = 1; //reset to zero for other table header
                                }


                                foreach (var fieldsetItem in headerval)
                                {
                                    OPSFieldSets opsfieldset = new OPSFieldSets();

                                    int.TryParse(fieldsetItem[5].ToString(), out int visorder);
                                    if (tabledata.SAPTableNameModule == fieldsetItem[6].ToString())
                                    {
                                        if (fieldsetItem[0]!="" && fieldsetItem[1] != "")
                                        {
                                            opsfieldset.MapId = newid;
                                            opsfieldset.SAPTableId = tabledata.SAPTableId;
                                            opsfieldset.SourceField = fieldsetItem[0].ToString();
                                            opsfieldset.DestinationField = fieldsetItem[1].ToString();
                                            opsfieldset.DataType = fieldsetItem[2].ToString();
                                            opsfieldset.ConditionalQuery = fieldsetItem[3].ToString();
                                            opsfieldset.IsKeyValue = fieldsetItem[4].ToString() == "NULL" ? false : true;
                                            opsfieldset.VisOrder = (visorder == 0 ? index : visorder);
                                            opsfieldset.CreateDate = DateTime.Now;
                                            opsfieldset.CreateUserID = id;
                                            _context.OPSFieldSets.Add(opsfieldset);
                                            _context.SaveChanges();

                                            SaveChanges();
                                            index++;
                                        }
                                      
                                    }



                                }

                            }
                            else
                            {
                                //creation part sa else to

                                OPSFieldTable opstable = new OPSFieldTable();

                                opstable.MapId = newid;
                                opstable.SAPTableNameModule = item[0].ToString();
                                opstable.SourceTableName = item[6].ToString();
                                opstable.SourceColumnName = item[1].ToString();
                                opstable.SourceRowData = item[2].ToString();
                                opstable.PathCode = item[3].ToString();
                                opstable.FileName = item[4].ToString();
                                opstable.FileType = item[5].ToString();
                                opstable.CreateDate = DateTime.Now;
                                opstable.CreateUserID = id;
                                _context.OPSFieldTable.Add(opstable);
                                _context.SaveChanges();
                                //SaveChanges();

                                int index = 1;
                                string groupheader = "";

                                var sourcefieldTable = item[0].ToString();
                                if (groupheader == "")
                                    groupheader = sourcefieldTable;
                                else if (groupheader != sourcefieldTable)
                                {
                                    groupheader = sourcefieldTable;
                                    index = 1; //reset to zero for other table header
                                }

                                foreach (var fieldsetItem in headerval)
                                {
                                    OPSFieldSets opsfieldset = new OPSFieldSets();

                                    int.TryParse(fieldsetItem[5].ToString(), out int visorder);
                                    if (opstable.SAPTableNameModule == fieldsetItem[6].ToString())
                                    {
                                        opsfieldset.MapId = newid;
                                        opsfieldset.SAPTableId = opstable.SAPTableId;
                                        opsfieldset.SourceField = fieldsetItem[0].ToString();
                                        opsfieldset.DestinationField = fieldsetItem[1].ToString();
                                        opsfieldset.DataType = fieldsetItem[2].ToString();
                                        opsfieldset.ConditionalQuery = fieldsetItem[3].ToString();
                                        opsfieldset.IsKeyValue = fieldsetItem[4].ToString() == "NULL" ? false : true;
                                        opsfieldset.VisOrder = (visorder == 0 ? index : visorder);
                                        opsfieldset.CreateDate = DateTime.Now;
                                        opsfieldset.CreateUserID = id;
                                        _context.OPSFieldSets.Add(opsfieldset);
                                        _context.SaveChanges();

                                        SaveChanges();
                                        index++;
                                    }
                                    


                                }


                            }

                       
                        }


                        //delete the table and Fieldset
                      

                        var dbtablelist = _context.OPSFieldTable.Where(x => x.MapId == _id).ToList();
                        var dbfieldlist = _context.OPSFieldSets.Where(x => x.MapId == _id).ToList();

                        var itemsToRemove = new List<OPSFieldTable>();

                        foreach (var dbTableItem in dbtablelist)
                        {
                            if (!sourcefieldtb.Any(sourcefield => sourcefield.Contains(dbTableItem.SAPTableNameModule)))
                            {
                                itemsToRemove.Add(dbTableItem);
                            }
                        }

                        var fieldsToRemove = dbfieldlist
                            .Where(x => itemsToRemove.Any(item => item.SAPTableId == x.SAPTableId))
                            .ToList();

                        foreach (var itemToRemove in itemsToRemove)
                        {
                            _context.OPSFieldTable.Remove(itemToRemove);
                            _context.SaveChanges();
                        }

                        foreach (var fieldToRemove in fieldsToRemove)
                        {
                            _context.OPSFieldSets.Remove(fieldToRemove);
                             _context.SaveChanges();
                        }




                        // dbtransaction.Rollback();
                        dbtransaction.Commit(); //final process
                    }
                    catch (DbEntityValidationException ex)
                    {
                        dbtransaction.Rollback();
                    }
                }
            }
        }
        public void Create_Rows(List<string[]> rows, int id, string table, int check, int newid, int mapid)
        {
            if (rows == null) rows = new List<string[]>();
            foreach (var item in rows)
            {
                var _id = (check == 1 ? mapid : newid);
                var fieldid = item[0].ToString();
                var dtexist = _context.Rows.Where(x => x.MapId == newid && x.SAPRowFieldId == fieldid).Any();
                if (dtexist)
                {
                    var rowdata = _context.Rows.Where(x => x.MapId == newid && x.SAPRowFieldId == fieldid).FirstOrDefault();
                    rowdata.MapId = newid;
                    rowdata.SAPRowFieldId = item[0].ToString();
                    rowdata.TableName = table;
                    rowdata.AddonRowField = item[1].ToString();
                    rowdata.DataType = item[2].ToString();
                    rowdata.Length = item[3].ToString();
                    rowdata.IsRequired = item[4].ToString() == "NULL" ? false : true;
                    rowdata.CreateDate = DateTime.Now;
                    rowdata.CreateUserID = id;
                    rowdata.DefaultValue = item[5].ToString() == "NULL" ? "" : item[5].ToString();
                    _context.SaveChanges();
                    SaveChanges();
                }
                else
                {
                    Row row = new Row();
                    row.MapId = newid;
                    row.SAPRowFieldId = item[0].ToString();
                    row.TableName = table;
                    row.AddonRowField = item[1].ToString();
                    row.DataType = item[2].ToString();
                    row.Length = item[3].ToString();
                    row.IsRequired = item[4].ToString() == "NULL" ? false : true;
                    row.CreateDate = DateTime.Now;
                    row.CreateUserID = id;
                    row.DefaultValue = item[5].ToString() == "NULL" ? "" : item[5].ToString();
                    _context.Rows.Add(row);
                    _context.SaveChanges();
                    SaveChanges();
                }
            }
            ////REMOVE FROM THE LIST IF NOT EXIST
            DataTable dt = new DataTable("Rows");
            dt.Columns.Add("SAPFieldId");
            foreach (string[] value in rows)
            {
                dt.Rows.Add(value[0].ToString());
            }
            var datatable = dt.AsEnumerable().Select(sel => sel[0].ToString()).ToArray();
            var dataparam = _context.Rows.Where(x => x.MapId == newid && !datatable.Contains(x.SAPRowFieldId));
            if (dataparam != null)
            {
                foreach (var del in dataparam)
                {
                    _context.Rows.Remove(del);
                }
                _context.SaveChanges();
                SaveChanges();
            }
        }

        public void CreateParameters(List<string[]> parameters, int mapid, string apicode)
        {
            if (parameters == null) parameters = new List<string[]>();
            foreach (var item in parameters)
            {
                var _apiparam = item[2].ToString();
                var dtexist = _context.APIParameter.Where(x => x.MapId == mapid && x.APICode == apicode && x.APIParameter == _apiparam).Any();
                if (dtexist)
                {
                    var parameterdata = _context.APIParameter.Where(x => x.MapId == mapid && x.APICode == apicode && x.APIParameter == _apiparam).FirstOrDefault();
                    parameterdata.APIParamValue = item[3].ToString();
                    _context.SaveChanges();
                    SaveChanges();
                }
                else
                {
                    ApiParameter parameterdata = new ApiParameter();
                    parameterdata.MapId = Convert.ToInt32(item[0]);
                    parameterdata.APICode = item[1].ToString();
                    parameterdata.APIParameter = item[2].ToString();
                    parameterdata.APIParamValue = item[3].ToString();
                    _context.APIParameter.Add(parameterdata);
                    _context.SaveChanges();
                    SaveChanges();
                }
            }
            ////REMOVE FROM THE LIST IF NOT EXIST
            DataTable dt = new DataTable("APIParameter");
            dt.Columns.Add("ParameterKey");
            foreach (string[] value in parameters)
            {
                dt.Rows.Add(value[2].ToString());
            }
            var datatable = dt.AsEnumerable().Select(sel => sel[0].ToString()).ToArray();
            var dataparam = _context.APIParameter.Where(x => x.MapId == mapid && x.APICode == apicode && !datatable.Contains(x.APIParameter));
            if (dataparam != null)
            {
                foreach (var del in dataparam)
                {
                    _context.APIParameter.Remove(del);
                }
                _context.SaveChanges();
                SaveChanges();
            }

        }
        public List<string[]> NewFieldValue(List<string[]> list, int count)
        {
            List<string[]> _list = new List<string[]>();
            _list = list;
            for (int i = 0; i < count; i++)
            {
                if (_list.Count > 0) _list.RemoveAt(0);
            }
            return _list;
        }
        public string Get_Constring(SetupCreateViewModel.AddonViewModel AddonPath)
        {
            return QueryAccess.con(AddonPath.AddonDBName,
                                            AddonPath.AddonServerName,
                                            AddonPath.AddonPort,
                                            AddonPath.AddonServerName,
                                            AddonPath.AddonDBuser,
                                            AddonPath.AddonDBPassword);
        }

        public void GenerateTable(string database, string constring, List<string[]> headerfield, List<string[]> rowfield,
                                  string rowname, string headername, int headercount, int rowcount)
        {
            QueryAccess.GenerateTable(database, constring, headerfield, rowfield,
                                      rowname, headername, headercount, rowcount);
        }
        public void GenerateRowTable(string database, string constring, List<string[]> rowfield, List<DataRow> TableList,
                          string rowname, int rowcount, List<DataRow> RowsPrimaryKey)
        {

            QueryAccess.GenerateRowTable(database, constring, rowfield, TableList,
                                      rowname, rowcount, RowsPrimaryKey);

        }

        public void GenerateHeaderTable(string database, string constring, List<string[]> headerfield, string headername, int headercount, List<DataRow> HeaderPrimaryKey)
        {

            QueryAccess.GenerateHeaderTable(database, constring, headerfield,
                                       headername, headercount, HeaderPrimaryKey);

        }
        public MapCreateViewModel Populate(string table, string code, string header, string row)
        {
            string[] tables = new string[2];
            var model = new MapCreateViewModel();

            tables[0] = header;
            tables[1] = row;
            var SAPSetup = Select_SAPSetup(code);
            string Hanaconn = SAPSetup.SAPDBVersion.Contains("HANA") ? QueryAccess.HANA_conString(SAPSetup.SAPSldServer,
                                                                                               SAPSetup.SAPDBuser,
                                                                                               SAPSetup.SAPDBPassword,
                                                                                               SAPSetup.SAPDBName)
                                                                                               :
                                                                    QueryAccess.MSSQL_conString(SAPSetup.SAPSldServer,
                                                                                               SAPSetup.SAPDBuser,
                                                                                               SAPSetup.SAPDBPassword,
                                                                                               SAPSetup.SAPDBName);
            if (QueryAccess.CheckCon(Hanaconn, SAPSetup.SAPDBVersion) is true)
            {
                //model.HeaderHanaFields = QueryAccess.connHana(Hanaconn, tables.ToList(), SAPSetup.SAPDBName, SAPSetup.SAPDBVersion)
                //                                    .Item1.Select(x => new MapCreateViewModel.HeaderHanaField
                //                                    {
                //                                        ColumnName = x.ToString(),
                //                                    }).ToList();

                //model.HeaderHanaFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = "" });
                //model.RowHanaFields = QueryAccess.connHana(Hanaconn, tables.ToList(), SAPSetup.SAPDBName, SAPSetup.SAPDBVersion)
                //                                 .Item2.Select(x => new MapCreateViewModel.RowHanaField
                //                                 {
                //                                     ColumnName = x.ToString(),
                //                                 }).ToList();
                model.RowHanaFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = "" });
                model.DataTypes = Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().Select(x => new MapCreateViewModel.Data { DataType = x.ToString().ToUpper() }).ToList();

            }
            return model;
        }
        public MapCreateViewModel PopulateAPI(string table, string code, string header, string row, string APICode, int MapId)
        {
            long errcode = 0;
            string[] tables = new string[2];
            var model = new MapCreateViewModel();
            model.HeaderHanaFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowHanaFields = new List<MapCreateViewModel.RowHanaField>();
            model.HeaderAPIFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowAPIFields = new List<MapCreateViewModel.RowHanaField>();

            #region DefaultValue
            model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = "" }); ////ADD BLANK AS OPTION            
            model.RowAPIFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = "" }); ////ADD BLANK AS OPTION
            #endregion

            tables[0] = header; //unused
            tables[1] = row; //unused
            errcode = 1100;
            var SAPSetup = Select_SAPSetup(code);

            string module = "";
            if (table.ToLower().Contains("item"))
            { module = "Item"; }
            else if (table.ToLower().Contains("incoming payments"))
            { module = "IncomingPayments"; }
            else if (table.ToLower().Contains("bp master"))
            { module = "BusinessPartner"; }
            else if (table.ToLower().Contains("bill of materials"))
            { module = "ProductTree"; }
            else if (table.ToLower().Contains("unit of measurement"))
            { module = "UnitOfMeasurement"; }
            else if (table.ToLower().Contains("price list"))
            { module = "PriceList"; }
            else
            { module = "Document"; }

            errcode = 1200;
            string Hanaconn = SAPSetup.SAPDBVersion.Contains("HANA") ? QueryAccess.HANA_conString(SAPSetup.SAPServerName,
                                                                                               SAPSetup.SAPDBuser,
                                                                                               SAPSetup.SAPDBPassword,
                                                                                               SAPSetup.SAPDBName)
                                                                                               :
                                                                    QueryAccess.MSSQL_conString(SAPSetup.SAPServerName,
                                                                                                SAPSetup.SAPDBuser,
                                                                                                SAPSetup.SAPDBPassword,
                                                                                                SAPSetup.SAPDBName);
            List<ApiParameterViewModel.ApiParameter> paramlist = _context.APIParameter.Where(sel => sel.MapId == MapId && sel.APICode == APICode)
                                                        .Select(sel => new ApiParameterViewModel.ApiParameter
                                                        {
                                                            APICode = sel.APICode,
                                                            APIParameter = sel.APIParameter,
                                                            APIParamValue = sel.APIParamValue,
                                                        }).ToList();

            #region GETSAP_Fields_Types 
            errcode = 1300;
            if (QueryAccess.CheckCon(Hanaconn, SAPSetup.SAPDBVersion) is true)
            {
                errcode = 1400;
                //string docid = SAPSetup.SAPDBVersion.Contains("HANA") ? DataAccess.SelectHana(QueryAccess.HANA_conString(
                //                                                                                    SAPSetup.SAPServerName,
                //                                                                                    SAPSetup.SAPDBuser,
                //                                                                                    SAPSetup.SAPDBPassword,
                //                                                                                    SAPSetup.SAPDBName), QueryAccess.GetTopOrder()).AsEnumerable().Select(x => x["DocEntry"].ToString()).FirstOrDefault()
                //                                                                                    :
                //                                                                                    DataAccess.Select(QueryAccess.MSSQL_conString(
                //                                                                                    SAPSetup.SAPServerName,
                //                                                                                    SAPSetup.SAPDBuser,
                //                                                                                    SAPSetup.SAPDBPassword,
                //                                                                                    SAPSetup.SAPDBName), QueryAccess.GetTopOrder()).AsEnumerable().Select(x => x["DocEntry"].ToString()).FirstOrDefault();

                //Get the fields from JSON Service Layer
                var auth = new AuthenticationCredViewModel
                {
                    Method = "GET",
                    //Action = //$@"Orders({docid})", //For JSON_DATA VERSION 1
                    Action = "$metadata?retrieveFields=1", //For META_DATA  VERSION 2
                    JsonString = "{}",
                    SAPSldServer = SAPSetup.SAPSldServer,
                    SAPServer = SAPSetup.SAPServerName,
                    Port = SAPSetup.SAPLicensePort.ToString(),
                    SAPDatabase = SAPSetup.SAPDBName,
                    SAPDBUserId = SAPSetup.SAPDBuser,
                    SAPDBPassword = SAPSetup.SAPDBPassword,
                    SAPUserID = SAPSetup.SAPUser,
                    SAPPassword = SAPSetup.SAPPassword
                };
                errcode = 1500;
                sapAces.SaveCredentials(auth);
                errcode = 1600;
                bool blnLogin = SAPAccess.LoginAction();

                #region SAP_METADATA_XML
                if (blnLogin == true)
                {
                    errcode = 1700;
                    string sxml = sapAces.SendSLData(auth);
                    XmlDocument xmlDoc = new XmlDocument();
                    if (!string.IsNullOrEmpty(sxml))
                    {
                        xmlDoc.LoadXml(sxml);
                        foreach (XmlElement element in xmlDoc.DocumentElement)
                        {
                            foreach (XmlNode nodeSchem in element.ChildNodes)
                            {
                                foreach (XmlNode nodeEntity in nodeSchem.ChildNodes)
                                {
                                    if (nodeEntity.Name.Equals("EntityType") && nodeEntity.Attributes[0].Value.Equals(module))
                                    {
                                        foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                        {
                                            if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                model.HeaderHanaFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = nodeProp.Attributes[0].Value });
                                                if (module.ToLower() == "item")
                                                {
                                                    model.RowHanaFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = nodeProp.Attributes[0].Value });
                                                }
                                            }
                                        }
                                    }
                                    else if (nodeEntity.Name.Equals("ComplexType") && nodeEntity.Attributes[0].Value.Equals($"{module}Line"))
                                    {
                                        foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                        {
                                            if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                model.RowHanaFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = nodeProp.Attributes[0].Value });
                                            }
                                        }
                                    }

                                    ////FOR EPI BOY
                                    //if (model.RowHanaFields.Count == 0)
                                    //{
                                    //    foreach (var col in model.HeaderHanaFields)
                                    //    {
                                    //        model.RowHanaFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = col.ColumnName });
                                    //    }
                                    //}

                                    if (model.HeaderHanaFields.Count > 0 && model.RowHanaFields.Count > 0)
                                        break;
                                }

                            }
                        }
                    }
                    //model.HeaderHanaFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = "" }); ////ADD BLANK AS OPTION
                    //model.RowHanaFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = "" }); ////ADD BLANK AS OPTION
                }
                else
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "FIELD_MAPPING", CreateDate = DateTime.Now, ApiUrl = $@"POST {SAPSetup.SAPSldServer}:{SAPSetup.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = "", ErrorMsg = $"{errcode} SLD Login Failed" });
                    _context.SaveChanges();
                }
                #endregion

            }
            #endregion

            #region GETAPI_Field
            //string ret = JsonSboSample();
            var apimethod = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APIMethod).FirstOrDefault(); apimethod = (apimethod == null ? "" : apimethod);
            var apiurl = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APIURL).FirstOrDefault(); apiurl = (apiurl == null ? "" : apiurl);
            var apiuser = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APIKey).FirstOrDefault(); apiuser = (apiuser == null ? "" : apiuser);
            var apipwd = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APISecretKey).FirstOrDefault(); apipwd = (apipwd == null ? "" : apipwd);
            var apiloginurl = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APILoginUrl).FirstOrDefault(); apiloginurl = (apiloginurl == null ? "" : apiloginurl);
            var apiloginbody = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APILoginBody).FirstOrDefault(); apiloginbody = (apiloginbody == null ? "" : apiloginbody);

            var authToken = _context.APISetups.Where(x => x.APIURL.ToLower().EndsWith("/token")).FirstOrDefault();
            if (authToken != null)
            {
                string token = APITokenAuthorizer.GetToken(authToken.APIMethod, authToken.APIURL, authToken.APIKey, authToken.APISecretKey);
                if (token != "")
                {
                    string response = APITokenAuthorizer.GetAPIFields(apimethod, apiurl, apiuser, apipwd, token, 3388, "D365CRM");
                    if (!response.Contains("error") && response != "")
                    {
                        JObject jObj = JObject.Parse(response);
                        var keys = jObj.Properties().Select(p => p.Name).ToList();
                        keys.ForEach(x =>
                        {
                            model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = x });
                        });
                    }
                }

            }
            else
            {
                apiloginbody = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APILoginBody).FirstOrDefault(); apiloginbody = (apiloginbody == null ? "" : apiloginbody);
                string ret = (!apiloginurl.ToLower().Contains("b1s/v1")) ? sapAces.APIResponse(apimethod, apiurl, "", "", "", apiuser, apipwd, 1, paramlist)
                                        : (sapAces.XmlPostJson("POST", apiloginurl, $"{apiloginbody}")).ToLower().Contains("error") ? "" : sapAces.XmlPostJson(apimethod, apiurl, "");
                if (!string.IsNullOrEmpty(ret))
                {

                    JObject json = JObject.Parse(ret);
                    if (module.ToLower().Contains("document"))
                    {
                        //ForShopifyApi
                        if (json["orders"].ToString().Contains("gid://shopify/Order"))
                        {
                            var jheader = json["orders"];
                            var JChild = jheader.Children<JObject>();
                            var hlist = JChild.Properties().GroupBy(x => x.Name).Select(x => x.First()).ToList();
                            hlist.ForEach(x =>
                            {
                                JToken propertyValue = x.Value;
                                if (!(propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object))
                                {
                                    model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = x.Name });
                                }
                            });
                            if (hlist.Where(x => x.Name.Contains("line_items")).Any())
                            {
                                var jlines = json["orders"][0]["line_items"];
                                if (jlines != null)
                                {
                                    var jlinechild = jlines.Children<JObject>();
                                    var lis = jlinechild.Properties().GroupBy(x => x.Name).Select(x => x.First()).ToList();
                                    lis.ForEach(x =>
                                    {
                                        JToken propertyValue = x.Value;
                                        if (!(propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object))
                                        {
                                            model.RowAPIFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = x.Name });
                                        }
                                    });
                                }
                            }

                        }
                        //Default
                        else
                        {

                        }

                    }
                    else if (module.ToLower().Contains("item"))
                    {
                        //ForShopifyApi
                        if (json["products"].ToString().Contains("gid://shopify/Product"))
                        {
                            var jheader = json["products"];
                            var JChild = jheader.Children<JObject>();
                            var hlist = JChild.Properties().GroupBy(x => x.Name).Select(x => x.First()).ToList();
                            hlist.ForEach(x =>
                            {
                                JToken propertyValue = x.Value;
                                if (!(propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object))
                                {
                                    model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = x.Name });
                                }
                            });

                            var varchild = json["products"][0]["variants"];
                            var lchild = varchild.Children<JObject>();
                            var lis = lchild.Properties().GroupBy(x => x.Name).Select(x => x.First()).ToList();
                            lis.ForEach(x =>
                            {
                                JToken propertyValue = x.Value;
                                if (!(propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object))
                                {
                                    //model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = x.Name });
                                    model.RowAPIFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = x.Name });
                                }
                            });
                        }
                        else
                        {

                        }
                    }
                }
            }
            #endregion

            #region SAP_DATATYPE
            model.DataTypes = Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().Select(x => new MapCreateViewModel.Data { DataType = x.ToString().ToUpper() }).ToList();
            #endregion

            return model;
        }

        public MapCreateViewModel PopulateFileAPI(string FileName, string code, string header, string row, string APICode)
        {
            string[] tables = new string[2];
            var model = new MapCreateViewModel();
            model.HeaderHanaFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowHanaFields = new List<MapCreateViewModel.RowHanaField>();
            model.HeaderAPIFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowAPIFields = new List<MapCreateViewModel.RowHanaField>();

            #region DefaultValue
            model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = "--Default Value--" }); ////ADD BLANK AS OPTION            
            model.RowAPIFields.Add(new MapCreateViewModel.RowHanaField { ColumnName = "--Default Value--" }); ////ADD BLANK AS OPTION
            #endregion

            tables[0] = header; //unused
            tables[1] = row; //unused

            #region FILE_COLUMN_DATA
            var InputFileName = Path.GetFileName(FileName);
            string FilePath = AppDomain.CurrentDomain.BaseDirectory + $"FileUpload\\{InputFileName}";
            if (File.Exists(FilePath))
            {
                var xcelcolumn = ExcelAccessV2.GetFileHeader(FilePath);
                xcelcolumn.ForEach(x =>
                {
                    model.HeaderHanaFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = x.ToString() });
                });

            }
            #endregion

            #region GETAPI_Field
            //string ret = JsonSboSample();
            var apimethod = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APIMethod).FirstOrDefault(); apimethod = (apimethod == null ? "" : apimethod);
            var apiurl = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APIURL).FirstOrDefault(); apiurl = (apiurl == null ? "" : apiurl);
            var apiuser = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APIKey).FirstOrDefault(); apiuser = (apiuser == null ? "" : apiuser);
            var apipwd = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APISecretKey).FirstOrDefault(); apipwd = (apipwd == null ? "" : apipwd);
            var apiloginurl = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APILoginUrl).FirstOrDefault(); apiloginurl = (apiloginurl == null ? "" : apiloginurl);
            var apiloginbody = _context.APISetups.Where(x => x.APICode == APICode).Select(x => x.APILoginBody).FirstOrDefault(); apiloginbody = (apiloginbody == null ? "" : apiloginbody);
            string ret = (!apiloginurl.ToLower().Contains("b1s/v1")) ? sapAces.APIResponse(apimethod, apiurl, "", "", "", apiuser, apipwd, 1, null)
                                    : (sapAces.XmlPostJson("POST", apiloginurl, $"{apiloginbody}")).ToLower().Contains("error") ? "" : sapAces.XmlPostJson(apimethod, apiurl, "");
            if (!string.IsNullOrEmpty(ret))
            {

                JObject json = JObject.Parse(ret);

                /////For ZENDESK Api
                var jheader = json["tickets"];
                var JChild = jheader.Children<JObject>();
                var hlist = JChild.Properties().GroupBy(x => x.Name).Select(x => x.First()).ToList();
                hlist.ForEach(x =>
                {
                    JToken propertyValue = x.Value;
                    if (!(propertyValue.Type == JTokenType.Array || propertyValue.Type == JTokenType.Object))
                    {
                        model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = x.Name });
                    }
                    if ((x.Name == "custom_fields")) //THIS IS FOR CUSTOMIZED FIELDS
                    {
                        var jcustomhead = json["tickets"][0]["custom_fields"];
                        var JcustomChild = jcustomhead.Children<JObject>();
                        var hcustomlist = JcustomChild.Properties().GroupBy(xh => xh.Value).Select(xh => xh.First()).ToList();
                        hcustomlist.ForEach(xh =>
                        {
                            if (xh.Name == "id")
                            {
                                model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = "custom_field_" + xh.Value.ToString().Trim() });
                            }
                        });
                    }
                });
            }
            #endregion

            #region SAP_DATATYPE
            model.DataTypes = Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().Select(x => new MapCreateViewModel.Data { DataType = x.ToString().ToUpper() }).ToList();
            #endregion

            return model;
        }

        public MapCreateViewModel PopulateSAPAPI(string SourceAPICode, string HeaderTable, string RowTable, string DestinationAPICode, string Module)
        {
            long errcode = 0;
            var model = new MapCreateViewModel();
            //SourceAPICode
            model.HeaderHanaFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowHanaFields = new List<MapCreateViewModel.RowHanaField>();
            //DestinationAPICode
            model.HeaderAPIFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowAPIFields = new List<MapCreateViewModel.RowHanaField>();

            string module = "";
            if (Module.ToLower().Contains("item"))
            { module = "Item"; }
            else if (Module.ToLower().Contains("incoming payments"))
            { module = "IncomingPayments"; }
            else if (Module.ToLower().Contains("bp master"))
            { module = "BusinessPartner"; }
            else if (Module.ToLower().Contains("bill of materials"))
            { module = "ProductTree"; }
            else if (Module.ToLower().Contains("unit of measurement"))
            { module = "UnitOfMeasurement"; }
            else if (Module.ToLower().Contains("price list"))
            { module = "PriceList"; }
            else
            { module = "Document"; }

            #region Source SAP_METADATA_XML
            errcode = 1100;
            var SAPSetup = Select_SAPSetup(SourceAPICode);
            errcode = 1400;

            //Get the fields from JSON Service Layer            
            if (SAPSetup != null)    
            {
                var auth = new AuthenticationCredViewModel
                {
                    Method = "GET",
                    Action = "$metadata?retrieveFields=1", //For META_DATA  VERSION 2
                    JsonString = "{}",
                    SAPSldServer = SAPSetup.SAPSldServer,
                    SAPServer = SAPSetup.SAPServerName,
                    Port = SAPSetup.SAPLicensePort.ToString(),
                    SAPDatabase = SAPSetup.SAPDBName,
                    SAPDBUserId = SAPSetup.SAPDBuser,
                    SAPDBPassword = SAPSetup.SAPDBPassword,
                    SAPUserID = SAPSetup.SAPUser,
                    SAPPassword = SAPSetup.SAPPassword
                };
         
                errcode = 1500;
                sapAces.SaveCredentials(auth);
                errcode = 1600;
                bool blnLogin = SAPAccess.LoginAction();

                List<string> rowfieldnames = new List<string>();

                if (blnLogin == true)
                {
                    errcode = 1700;
                    string sxml = sapAces.SendSLData(auth);
                    XmlDocument xmlDoc = new XmlDocument();
                    if (!string.IsNullOrEmpty(sxml))
                    {
                        xmlDoc.LoadXml(sxml);
                        foreach (XmlElement element in xmlDoc.DocumentElement)
                        {
                            foreach (XmlNode nodeSchem in element.ChildNodes)
                            {
                                //FOR HEADER AND FETCHING OF ROW NAMES
                                foreach (XmlNode nodeEntity in nodeSchem.ChildNodes)
                                {
                                    if (nodeEntity.Name.Equals("EntityType") && nodeEntity.Attributes[0].Value.Equals(module))
                                    {
                                        foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                        {
                                            if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                model.HeaderHanaFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = nodeProp.Attributes[0].Value });
                                            }
                                            else if (nodeProp.Name.Equals("Property") && nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                //FOR ROWS
                                                rowfieldnames.Add(nodeProp.Attributes["Type"].Value.Replace("Collection(SAPB1.", "").Replace(")", ""));
                                            }
                                        }
                                    }
                                    if (model.HeaderHanaFields.Count > 0 && model.RowHanaFields.Count > 0)
                                        break;
                                }

                                //FOR ROW
                                foreach (XmlNode nodeEntity in nodeSchem.ChildNodes)
                                {
                                    foreach (string rowmodule in rowfieldnames)
                                    {
                                        if (nodeEntity.Name.Equals("ComplexType") && (nodeEntity.Attributes[0].Value.Equals(rowmodule)))
                                        {
                                            foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                            {
                                                if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                                {
                                                    model.RowHanaFields.Add(new MapCreateViewModel.RowHanaField { TableName = rowmodule, ColumnName = nodeProp.Attributes[0].Value });
                                                }
                                            }
                                        }
                                        //if (model.HeaderHanaFields.Count > 0 && model.RowHanaFields.Count > 0)
                                        //    break;
                                    }
                                }

                            }
                        }
                    }
                }
                else
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "FIELD_MAPPING", CreateDate = DateTime.Now, ApiUrl = $@"POST {SAPSetup.SAPSldServer}:{SAPSetup.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = "", ErrorMsg = $"{errcode} SLD Login Failed" });
                    _context.SaveChanges();
                }
                #endregion

                #region destination SAP_METADATA_XML
                rowfieldnames = new List<string>();
                errcode = 1100;
                SAPSetup = Select_SAPSetup(DestinationAPICode);
                errcode = 1400;

                //Get the fields from JSON Service Layer
                auth = new AuthenticationCredViewModel
                {
                    Method = "GET",
                    Action = "$metadata?retrieveFields=1", //For META_DATA  VERSION 2
                    JsonString = "{}",
                    SAPSldServer = SAPSetup.SAPSldServer,
                    SAPServer = SAPSetup.SAPServerName,
                    Port = SAPSetup.SAPLicensePort.ToString(),
                    SAPDatabase = SAPSetup.SAPDBName,
                    SAPDBUserId = SAPSetup.SAPDBuser,
                    SAPDBPassword = SAPSetup.SAPDBPassword,
                    SAPUserID = SAPSetup.SAPUser,
                    SAPPassword = SAPSetup.SAPPassword
                };
                errcode = 1500;
                sapAces.SaveCredentials(auth);
                errcode = 1600;
                blnLogin = SAPAccess.LoginAction();


                if (blnLogin == true)
                {
                    errcode = 1700;
                    string sxml = sapAces.SendSLData(auth);
                    XmlDocument xmlDoc = new XmlDocument();
                    if (!string.IsNullOrEmpty(sxml))
                    {
                        xmlDoc.LoadXml(sxml);
                        foreach (XmlElement element in xmlDoc.DocumentElement)
                        {
                            foreach (XmlNode nodeSchem in element.ChildNodes)
                            {
                                foreach (XmlNode nodeEntity in nodeSchem.ChildNodes)
                                {
                                    if (nodeEntity.Name.Equals("EntityType") && nodeEntity.Attributes[0].Value.Equals(module))
                                    {
                                        foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                        {
                                            if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                model.HeaderAPIFields.Add(new MapCreateViewModel.HeaderHanaField { ColumnName = nodeProp.Attributes[0].Value });
                                            }
                                            else if (nodeProp.Name.Equals("Property") && nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                //FOR ROWS
                                                rowfieldnames.Add(nodeProp.Attributes["Type"].Value.Replace("Collection(SAPB1.", "").Replace(")", ""));
                                            }
                                        }
                                    }

                                    if (model.HeaderAPIFields.Count > 0 && model.RowAPIFields.Count > 0)
                                        break;
                                }

                                //FOR ROW
                                foreach (XmlNode nodeEntity in nodeSchem.ChildNodes)
                                {
                                    foreach (string rowmodule in rowfieldnames)
                                    {
                                        if (nodeEntity.Name.Equals("ComplexType") && (nodeEntity.Attributes[0].Value.Equals(rowmodule)))
                                        {
                                            foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                            {
                                                if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                                {
                                                    model.RowAPIFields.Add(new MapCreateViewModel.RowHanaField { TableName = rowmodule, ColumnName = nodeProp.Attributes[0].Value });
                                                }
                                            }
                                        }
                                        //if (model.HeaderAPIFields.Count > 0 && model.RowAPIFields.Count > 0)
                                        //    break;
                                    }
                                }

                            }
                        }
                    }
                }
                else
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "FIELD_MAPPING", CreateDate = DateTime.Now, ApiUrl = $@"POST {SAPSetup.SAPSldServer}:{SAPSetup.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = "", ErrorMsg = $"{errcode} SLD Login Failed" });
                    _context.SaveChanges();
                }
                #endregion

                #region SAP_DATATYPE
                model.DataTypes = Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().Select(x => new MapCreateViewModel.Data { DataType = x.ToString().ToUpper() }).ToList();
                #endregion            
            }

            return model;
        }

        public JObject FetchSAPAPIData(string Code, string Module)
        {
            long errcode = 0;

            JObject dataobject = new JObject();
            JObject Jheader = new JObject();
            JArray Jheaderfields = new JArray();
            JObject Jrow = new JObject();
            JObject JFields = new JObject();
            JArray Jtypefields = new JArray();
            JObject Jtypes = new JObject();

            var model = new MapCreateViewModel();
            //SourceAPICode
            model.HeaderHanaFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowHanaFields = new List<MapCreateViewModel.RowHanaField>();
            //DestinationAPICode
            model.HeaderAPIFields = new List<MapCreateViewModel.HeaderHanaField>();
            model.RowAPIFields = new List<MapCreateViewModel.RowHanaField>();
            string entityname = _context.ModuleSetup.Where(sel => sel.ModuleCode == Module).Select(sel=> sel.EntityType).FirstOrDefault();
            string module = entityname;
            //if (Module.ToLower().Contains("item"))
            //{ module = "Item"; }
            //else if (Module.ToLower().Contains("incoming payments"))
            //{ module = "IncomingPayments"; }
            //else if (Module.ToLower().Contains("businesspartner") || Module.ToLower().Contains("ocrd"))
            //{ module = "BusinessPartner"; }
            //else if (Module.ToLower().Contains("bill of materials"))
            //{ module = "ProductTree"; }
            //else if (Module.ToLower().Contains("unit of measurement"))
            //{ module = "UnitOfMeasurement"; }
            //else if (Module.ToLower().Contains("price list"))
            //{ module = "PriceList"; }
            //else
            //{ module = "Document"; }

            #region Source SAP_METADATA_XML
            errcode = 1100;
            var SAPSetup = Select_SAPSetup(Code);
            errcode = 1400;

            //Get the fields from JSON Service Layer
            var auth = new AuthenticationCredViewModel
            {
                Method = "GET",
                Action = "$metadata?retrieveFields=1", //For META_DATA  VERSION 2
                JsonString = "{}",
                SAPSldServer = SAPSetup.SAPSldServer,
                SAPServer = SAPSetup.SAPServerName,
                Port = SAPSetup.SAPLicensePort.ToString(),
                SAPDatabase = SAPSetup.SAPDBName,
                SAPDBUserId = SAPSetup.SAPDBuser,
                SAPDBPassword = SAPSetup.SAPDBPassword,
                SAPUserID = SAPSetup.SAPUser,
                SAPPassword = SAPSetup.SAPPassword
            };
            errcode = 1500;
            sapAces.SaveCredentials(auth);
            errcode = 1600;
            bool blnLogin = SAPAccess.LoginAction();

            List<string[]> rowfieldnames = new List<string[]>();
            //ADD EMPTY-OPTION
            JFields.Add(new JProperty("-", "-"));

            if (blnLogin == true)
            {
                errcode = 1700;
                string sxml = sapAces.SendSLData(auth);
                XmlDocument xmlDoc = new XmlDocument();
                if (!string.IsNullOrEmpty(sxml))
                {
                    try
                    {
                        xmlDoc.LoadXml(sxml);
                        foreach (XmlElement element in xmlDoc.DocumentElement)
                        {
                            foreach (XmlNode nodeSchem in element.ChildNodes)
                            {
                                //FOR HEADER AND FETCHING OF ROW NAMES
                                foreach (XmlNode nodeEntity in nodeSchem.ChildNodes)
                                {
                                    if (nodeEntity.Name.Equals("EntityType") && nodeEntity.Attributes[0].Value.Equals(module))
                                    {
                                        foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                        {
                                            if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                JFields.Add(new JProperty(nodeProp.Attributes["Name"].Value, nodeProp.Attributes["Type"].Value.Replace("Edm.", "").Replace("SAPB1.", "")));
                                            }
                                            else if (nodeProp.Name.Equals("Property") && nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                            {
                                                //FOR ROWS
                                                rowfieldnames.Add(new string[] { nodeProp.Attributes["Name"].Value, nodeProp.Attributes["Type"].Value.Replace("Collection(SAPB1.", "").Replace(")", "") });
                                            }
                                        }
                                        Jrow["Header"] = JFields;
                                    }
                                    if (model.HeaderHanaFields.Count > 0 && model.RowHanaFields.Count > 0)
                                        break;
                                }

                                //FOR ROW
                                foreach (XmlNode nodeEntity in nodeSchem.ChildNodes)
                                {
                                    foreach (string[] rowmodule in rowfieldnames)
                                    {
                                        if (nodeEntity.Name.Equals("ComplexType") && (nodeEntity.Attributes[0].Value.Equals(rowmodule[1])))
                                        {
                                            List<string[]> Subrowfields = new List<string[]>();

                                            JFields = new JObject();                                            
                                            JFields.Add(new JProperty("-", "-")); //** ADD OPTION AS NOTHING '-' **//

                                            //ADD THE LIST OF FIELDS
                                            foreach (XmlNode nodeProp in nodeEntity.ChildNodes)
                                            {
                                                if (nodeProp.Name.Equals("Property") && !nodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                                {                                                   
                                                    JFields.Add(new JProperty(nodeProp.Attributes["Name"].Value, nodeProp.Attributes["Type"].Value.Replace("Edm.", "").Replace("SAPB1.", "")));
                                                }
                                                else
                                                {
                                                    //FOR SUB FIELDS
                                                    Subrowfields.Add(new string[] { nodeProp.Attributes["Name"].Value, nodeProp.Attributes["Type"].Value.Replace("Collection(SAPB1.", "").Replace(")", "") });
                                                }
                                            }

                                            //ADD THE LIST OF SUBFIELDS
                                            foreach (XmlNode SubnodeEntity in nodeSchem.ChildNodes)
                                            {
                                                foreach (string[] Subrowmodule in Subrowfields)
                                                {
                                                    if (SubnodeEntity.Name.Equals("ComplexType") && (SubnodeEntity.Attributes[0].Value.Equals(Subrowmodule[1])))
                                                    {
                                                        foreach (XmlNode SubnodeProp in SubnodeEntity.ChildNodes)
                                                        {
                                                            if (SubnodeProp.Name.Equals("Property") && !SubnodeProp.Attributes[1].Value.ToLower().Contains("collection"))
                                                            {
                                                                JFields.Add(new JProperty($@"[{Subrowmodule[0]}]{SubnodeProp.Attributes["Name"].Value}", SubnodeProp.Attributes["Type"].Value.Replace("Edm.", "").Replace("SAPB1.", "")));
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                           Jrow[rowmodule[0]] = JFields;
                                        }
                                    }
                                }

                            }
                        }
                    }
                    catch (System.Xml.XmlException)
                    {
                        Jheaderfields.Add("None");
                        Jheader["Header"] = Jheaderfields;
                        dataobject = Jheader;
                        dataobject["Rows"] = new JObject();
                        dataobject["DataType"] = new JObject();
                    }
                }
            }
            else
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "FIELD_MAPPING", CreateDate = DateTime.Now, ApiUrl = $@"POST {SAPSetup.SAPSldServer}:{SAPSetup.SAPLicensePort}/b1s/v1/AlternateCatNum", Json = "", ErrorMsg = $"{errcode} SLD Login Failed" });
                _context.SaveChanges();
            }
            #endregion
            //Jheader["Header"] = Jheaderfields;
            //dataobject = Jheader;
            //Jrow["Header"] = Jheaderfields;
            dataobject["Rows"] = Jrow;

            #region SAP_DATATYPE
            var arry  = Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().Select(x => x.ToString().ToUpper()).ToList();
            arry.ForEach(fe =>
            {
                Jtypefields.Add(fe.ToString());
            });                    
            dataobject["DataType"] = Jtypefields;
            #endregion

            return dataobject;
        }

        public List<MapCreateViewModel.Data> FetchSAPDataTypes()
        {

            var model = new MapCreateViewModel();

            #region SAP_DATATYPE
            model.DataTypes = Enum.GetValues(typeof(SqlDbType)).Cast<SqlDbType>().Select(x => new MapCreateViewModel.Data { DataType = x.ToString().ToUpper() }).ToList();
            #endregion

            return model.DataTypes;
        }

        public MapCreateViewModel Get_DataType(string table, string code, string field)
        {

            string[] tables = new string[2];
            var model = new MapCreateViewModel();
            if (table == "Sales Order") { tables[0] = "ORDR"; tables[1] = "RDR1"; }
            var SAPSetup = Select_SAPSetup(code);

            string Hanaconn = SAPSetup.SAPDBVersion.Contains("HANA") ? QueryAccess.HANA_conString(SAPSetup.SAPServerName,
                                                                                               SAPSetup.SAPDBuser,
                                                                                               SAPSetup.SAPDBPassword,
                                                                                               SAPSetup.SAPDBName)
                                                                                               :
                                                                    QueryAccess.MSSQL_conString(SAPSetup.SAPServerName,
                                                                                                SAPSetup.SAPDBuser,
                                                                                                SAPSetup.SAPDBPassword,
                                                                                                SAPSetup.SAPDBName);

            string Db = SAPSetup.SAPDBVersion.Contains("HANA") ? "HANA" : "MSSQL";

            //model.HeaderDataTypes = QueryAccess.getDataType(Hanaconn, tables.ToList(), SAPSetup.SAPDBName, SAPSetup.SAPDBVersion, field)
            //                                   .Item1.AsEnumerable().Select(x => new MapCreateViewModel.HeadData
            //                                   {
            //                                       DataType = x[0].ToString(),
            //                                       Length = x[1].ToString(),
            //                                       Scale = x[2].ToString(),
            //                                       DBType = Db,
            //                                   }).ToList();

            //model.RowDataTypes = QueryAccess.getDataType(Hanaconn, tables.ToList(), SAPSetup.SAPDBName, SAPSetup.SAPDBVersion, field)
            //                                .Item2.AsEnumerable().Select(x => new MapCreateViewModel.RowData
            //                                {
            //                                    DataType = x[0].ToString(),
            //                                    Length = x[1].ToString(),
            //                                    Scale = x[2].ToString(),
            //                                    DBType = Db,
            //                                }).ToList();

            return model;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public string JsonSboSample()
        {
            string ret = "";
            AuthenticationCredViewModel auth = new AuthenticationCredViewModel();
            auth.URL = "http";
            auth.Port = "50001";
            auth.Action = "Orders(631)";
            auth.Method = "GET";
            auth.SAPServer = "192.168.2.15";
            auth.SAPDatabase = "DIRECDEALS_TEST";
            auth.SAPUserID = "Direc2";
            auth.SAPPassword = "1234";
            auth.SAPDBUserId = "SYSTEM";
            auth.SAPDBPassword = "Sb1@dbti";
            auth.JsonData = JObject.Parse(@"{}");
            auth.JsonString = "{}";
            auth.Id = 0;
            sapAces.SaveCredentials(auth);
            if (SAPAccess.LoginAction() == true)
            {
                ret = sapAces.SendSLData(auth);
            }
            return ret;
        }
    }
}