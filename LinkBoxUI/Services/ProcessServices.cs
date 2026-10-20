using DomainLayer;
using LinkBoxUI.Context;
using InfrastructureLayer.Repositories;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer.Models;

namespace LinkBoxUI.Services
{
    public class ProcessServices : IProcessRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        public ProcessCreateViewModel View_Process()
        {
            ProcessCreateViewModel model = new ProcessCreateViewModel();

            model.ProcessView = _context.Process.GroupJoin(_context.FieldMappings, a => a.MapId, b => b.MapId, (a, b) => new ProcessCreateViewModel.process
            {
                ProcessId = a.ProcessId,
                ProcessCode = a.ProcessCode,
                FieldMappingCode = b.Select(sel => sel.MapCode).FirstOrDefault(),
                FieldMappingName = b.Select(sel => sel.MapName).FirstOrDefault(),
                ModName = b.Select(sel => sel.ModuleName).FirstOrDefault(),
                ProcessName = a.ProcessName,
                PostSAP = a.PostSAP,
                ProcessType= a.ProcessType,
                IsActive = a.IsActive

            }).OrderBy(x=>x.ProcessType).ToList();

           
        

            model.MapList = _context.FieldMappings.Select(x => new ProcessCreateViewModel.mapping
            {
                MapCode = x.MapCode,
                MapName = x.MapName,
                AddonCode = x.AddonCode
            }).OrderBy(x => x.AddonCode).ToList();

            model.opsMapList = _context.OPSFieldMappings.Select(x => new ProcessCreateViewModel.opsmapping
            {
                MapCode = x.MapCode,
                MapName = x.MapName,
                
            }).OrderBy(x=> x.MapCode).ToList();
            //model.MapList.Add(new ProcessCreateViewModel.mapping { MapCode = "-N/A-", MapName = "-N/A-" });

            model.EmailList = _context.Emails.Select(x => new ProcessCreateViewModel.Email
            {
                EmailId = x.EmailId,
                EmailCode = x.EmailCode

            }).ToList();

            model.APIList = _context.APISetups.Where(x => x.IsActive == true).Select(x => new ProcessCreateViewModel.API
            {
                APIId = x.APIId,
                APICode = x.APICode

            }).ToList();

            model.SapList = _context.SAPSetup.Select(x => new ProcessCreateViewModel.SapSetup
            {
                SAPId = x.SAPId,
                SAPCode = x.SAPCode,
            }).ToList();

            return model;
        }

        public bool Create_Process(ProcessSetup process, string[] map, int id)
        {
            try
            {


                //Saving on Process table
                int mapid = 0;
                process.MapId = mapid;
                process.IsActive = true;
                process.CreateDate = DateTime.Now;
                process.CreateUserID = id;
                _context.Process.Add(process);
                _context.SaveChanges();

                //Saving on ProcessMaps table

                foreach (string code in map)
                {
                    ProcessMap processMaps = new ProcessMap();
                    processMaps.ProcessId = process.ProcessId;
                    //var mapid = (map == "-N/A-") ? 0 : _context.FieldMappings.Where(x => x.MapCode == map).FirstOrDefault().MapId;
                    mapid = (code == "-N/A-") ? 0 : _context.FieldMappings.Where(x => x.MapCode == code).FirstOrDefault().MapId;
                    processMaps.MapId = mapid;
                    _context.ProcessMap.Add(processMaps);
                    _context.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }
        public bool OPSCreate_Process(ProcessSetup process, string[] map, int id)
        {
            try
            {

                //Saving on Process table
                int mapid = 0;
                process.MapId = mapid;
                process.IsActive = true;
                process.CreateDate = DateTime.Now;
                process.CreateUserID = id;
                process.ProcessType = "OPS";
                _context.Process.Add(process);
                _context.SaveChanges();

                //Saving on ProcessMaps table

                foreach (string code in map)
                {
                    ProcessMap processMaps = new ProcessMap();
                    processMaps.ProcessId = process.ProcessId;
                    //var mapid = (map == "-N/A-") ? 0 : _context.FieldMappings.Where(x => x.MapCode == map).FirstOrDefault().MapId;
                    mapid = (code == "-N/A-") ? 0 : _context.OPSFieldMappings.Where(x => x.MapCode == code).FirstOrDefault().MapId;
                    processMaps.MapId = mapid;
                    //processMaps.ProcessType = "OPS";
                    _context.ProcessMap.Add(processMaps);
                    _context.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        public ProcessCreateViewModel Find_Process(int id)
        {
            var model = new ProcessCreateViewModel();
            //model.ProcessView = _context.Process.Where(x => x.ProcessId == id)
            //                            .Join(_context.FieldMappings, a => a.MapId, b => b.MapId, (a, b) =>
            //                            new ProcessCreateViewModel.process
            //                            {
            //                                ProcessId = a.ProcessId,
            //                                ProcessCode = a.ProcessCode,
            //                                FieldMappingCode = b.MapCode,
            //                                FieldMappingName = b.MapName,
            //                                ModName = b.ModuleName,
            //                                ProcessName = a.ProcessName,
            //                                APICode = a.APICode,
            //                                IsActive = a.IsActive,
            //                                PostSAP = a.PostSAP,
            //                            }).ToList();

            model.ProcessView = _context.FieldMappings.Join(_context.ProcessMap, a => a.MapId, b => b.MapId, (a, b) => new { a, b }).
                                      Join(_context.Process.Where(x => x.ProcessId == id), c => c.b.ProcessId, d => d.ProcessId, (d, c) => new { d, c }).
                                      Select(x => new ProcessCreateViewModel.process
                                      {
                                          ProcessId = x.c.ProcessId,
                                          ProcessCode = x.c.ProcessCode,
                                          ProcessType= x.c.ProcessType,
                                          FieldMappingCode = x.d.a.MapCode,
                                          FieldMappingName = x.d.a.MapName,
                                          ModName = x.d.a.ModuleName,
                                          APICode = x.c.APICode,
                                          ProcessName = x.c.ProcessName,
                                          PostSAP = x.c.PostSAP,
                                          IsActive = x.c.IsActive

                                      }).ToList();

            return model;
        }

        public ProcessCreateViewModel OPSFind_Process(int id)
        {
            var model = new ProcessCreateViewModel();
            //model.ProcessView = _context.Process.Where(x => x.ProcessId == id)
            //                            .Join(_context.FieldMappings, a => a.MapId, b => b.MapId, (a, b) =>
            //                            new ProcessCreateViewModel.process
            //                            {
            //                                ProcessId = a.ProcessId,
            //                                ProcessCode = a.ProcessCode,
            //                                FieldMappingCode = b.MapCode,
            //                                FieldMappingName = b.MapName,
            //                                ModName = b.ModuleName,
            //                                ProcessName = a.ProcessName,
            //                                APICode = a.APICode,
            //                                IsActive = a.IsActive,
            //                                PostSAP = a.PostSAP,
            //                            }).ToList();

            model.ProcessView = _context.FieldMappings.Join(_context.ProcessMap, a => a.MapId, b => b.MapId, (a, b) => new { a, b }).
                                      Join(_context.Process.Where(x => x.ProcessId == id), c => c.b.ProcessId, d => d.ProcessId, (d, c) => new { d, c }).
                                      Select(x => new ProcessCreateViewModel.process
                                      {
                                          ProcessId = x.c.ProcessId,
                                          ProcessType= x.c.ProcessType,
                                          ProcessCode = x.c.ProcessCode,
                                          FieldMappingCode = x.d.a.MapCode,
                                          FieldMappingName = x.d.a.MapName,
                                          ModName = x.d.a.ModuleName,
                                          APICode = x.c.APICode,
                                          ProcessName = x.c.ProcessName,
                                          PostSAP = x.c.PostSAP,
                                          IsActive = x.c.IsActive

                                      }).ToList();

            return model;
        }

        public bool Update_Process(ProcessSetup process, string[] map, int id)
        {

            //mapid = _context.FieldMappings.Where(x => x.MapCode == code).FirstOrDefault().MapId;
            //process.MapId = mapid;
            process.UpdateDate = DateTime.Now;
            process.UpdateUserID = id;
            
            _context.Entry(process).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();

            //Delete on ProcessMaps table
            ProcessMap processMaps = new ProcessMap();
            var maplist = _context.ProcessMap.Where(x => x.ProcessId == process.ProcessId)
                      .Select(x => new
                      {
                          ProcessId = x.ProcessId,
                          MapId = x.MapId,

                      }).ToList();

            foreach (var item in maplist)
            {
                processMaps = new ProcessMap { ProcessId = item.ProcessId, MapId = item.MapId };
                _context.Entry(processMaps).State = System.Data.Entity.EntityState.Deleted;
                _context.SaveChanges();
            }

            //Saving on ProcessMaps table
            foreach (string code in map)
            {
                int mapid = 0;
                processMaps = new ProcessMap();
                processMaps.ProcessId = process.ProcessId;
                //var mapid = (map == "-N/A-") ? 0 : _context.FieldMappings.Where(x => x.MapCode == map).FirstOrDefault().MapId;
                mapid = (code == "-N/A-") ? 0 : _context.FieldMappings.Where(x => x.MapCode == code).FirstOrDefault().MapId;
                processMaps.MapId = mapid;
                _context.ProcessMap.Add(processMaps);
                _context.SaveChanges();
            }

            return true;
        }

        public bool OPSUpdate_Process(ProcessSetup process, string[] map, int id)
        {

            //mapid = _context.FieldMappings.Where(x => x.MapCode == code).FirstOrDefault().MapId;
            //process.MapId = mapid;
            process.UpdateDate = DateTime.Now;
            process.UpdateUserID = id;
            process.ProcessType = "OPS";
            _context.Entry(process).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();

            //Delete on ProcessMaps table
            ProcessMap processMaps = new ProcessMap();
            var maplist = _context.ProcessMap.Where(x => x.ProcessId == process.ProcessId)
                      .Select(x => new
                      {
                          ProcessId = x.ProcessId,
                          MapId = x.MapId,
                          

                      }).ToList();

            foreach (var item in maplist)
            {
                processMaps = new ProcessMap { ProcessId = item.ProcessId, MapId = item.MapId };
                _context.Entry(processMaps).State = System.Data.Entity.EntityState.Deleted;
                _context.SaveChanges();
            }

            //Saving on ProcessMaps table
            foreach (string code in map)
            {
                int mapid = 0;
                processMaps = new ProcessMap();
                processMaps.ProcessId = process.ProcessId;
                //var mapid = (map == "-N/A-") ? 0 : _context.FieldMappings.Where(x => x.MapCode == map).FirstOrDefault().MapId;
                mapid = (code == "-N/A-") ? 0 : _context.OPSFieldMappings.Where(x => x.MapCode == code).FirstOrDefault().MapId;
                processMaps.MapId = mapid;
                _context.ProcessMap.Add(processMaps);
                _context.SaveChanges();
            }

            return true;
        }

        public ProcessCreateViewModel Validate_Process(string code)
        {
            var model = new ProcessCreateViewModel();

            model.ProcessView = _context.Process.Where(x => x.ProcessCode == code).Select((x) => new ProcessCreateViewModel.process
            {
                ProcessCode = x.ProcessCode,

                ProcessType= x.ProcessType,
            }).ToList();
            return model;
        }

        public ProcessCreateViewModel OPSValidate_Process(string code)
        {
            var model = new ProcessCreateViewModel();

            model.ProcessView = _context.Process.Where(x => x.ProcessCode == code).Select((x) => new ProcessCreateViewModel.process
            {
                ProcessCode = x.ProcessCode,
            }).ToList();
            return model;
        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }
    }
}