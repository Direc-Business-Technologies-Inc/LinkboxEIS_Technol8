using DataAccessLayer.Class;
using DomainLayer;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Properties;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Web;
using static DomainLayer.ViewModels.SchedCreateViewMdel;

namespace LinkBoxUI.Services
{
    public class ScheduleServices : IScheduleRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        public SchedCreateViewMdel View_Schedule()
        {
            SchedCreateViewMdel model = new SchedCreateViewMdel();
            model.SchedView = _context.Schedules.Select(x =>
                    new SchedCreateViewMdel.Schedule
                    {
                        SchedId = x.SchedId,
                        SchedCode = x.SchedCode,
                        Frequency = x.Frequency,
                        StartDate = x.StartDate.Value,
                        StartTime = x.StartTime.Value,
                        Process = x.Process,
                        ScheduleType = x.ScheduleType,
                        IsActive = x.IsActive

                    }).ToList();

            model.ProcessList = _context.Process.Select(x =>
                    new SchedCreateViewMdel.process
                    {
                        ProcessCode = x.ProcessCode,
                        ProcessType= x.ProcessType,
                    }).ToList();

            model.CrystalList = _context.CrystalExtractSetup.Where(x => x.IsActive).Select(x =>

            new SchedCreateViewMdel.crystal
            {
                CrystalCode = x.Name
            }).ToList();



            model.EmailList = _context.EmailTemplate.Select(x =>
                    new SchedCreateViewMdel.Email
                    {
                        EmailCode = x.Code
                    }).ToList();

            model.EmailCred = _context.EmailSetup.Select(x =>
                new SchedCreateViewMdel.Email
                {
                    EmailCode = x.EmailCode
                }).ToList();
            model.SyncList = _context.SyncQueries.Select(x =>
                    new SchedCreateViewMdel.Sync
                    {
                        SyncQueryCode = x.SyncQueryCode
                    }).ToList();

            model.APIList = _context.APISetups.Select(x => new SchedCreateViewMdel.API
            {
                APIId = x.APIId,
                APICode = x.APICode

            }).ToList();

            return model;
        }

        public void Create_Schedule(UploadSchedule schedule, int id)
        {
            schedule.CreateDate = DateTime.Now;
            schedule.IsActive = true;
            schedule.CreateUserID = id;


            var crystalId = _context.CrystalExtractSetup.Where(x => x.Name == schedule.Process).Select(x => x.Id).FirstOrDefault();
            //_context.Schedules.Add(schedule);
            //_context.SaveChanges();

            _context.Schedules.Add(schedule);
            _context.SaveChanges();
            string url = "";
            string method = "";
            if (schedule.ScheduleType == "Crystal")
            {
                
                var api = _context.CrystalExtractSetup.Where(x => x.Name == schedule.Process).Join(_context.APISetups, p => p.APICode, a => a.APICode, (p, a) =>
                                                 new {
                                                     API = a.APIURL,
                                                     method = a.APIMethod
                                                 }).FirstOrDefault();
                url = api == null ? _context.APISetups.Where(x => x.APICode == schedule.Api).Select(x => x.APIURL).FirstOrDefault() : api.API;
                method = api.method;
                url = $@"{url}?id={crystalId}";
            }
            else
            {


                var api = _context.Process.Where(x => x.ProcessCode == schedule.Process).Join(_context.APISetups, p => p.APICode, a => a.APICode, (p, a) =>
                                                  new {
                                                      API = a.APIURL,
                                                      method = a.APIMethod
                                                  }).FirstOrDefault();
                url = api == null ? _context.APISetups.Where(x => x.APICode == schedule.Api).Select(x => x.APIURL).FirstOrDefault() : api.API;
                method = api.method;
                var apimapcode = _context.Process.Where(x => x.ProcessCode == schedule.Process).Join(_context.FieldMappings, p => p.MapId, f => f.MapId, (p, f) =>
                                            new { APICode = f.APICode }).FirstOrDefault();
                var IsProcOnly = _context.Process.Where(x => x.ProcessCode == schedule.Process && x.MapId == 0).Any();
                var procestosap = _context.Process.Where(x => x.ProcessCode == schedule.Process).Select(x => x.PostSAP).FirstOrDefault();
                ////IF FIELD MAPPING IS API THEN SET THE MAPID            
                if (apimapcode != null)
                {
                    var mapid = _context.Process.Where(x => x.ProcessCode == schedule.Process).Select(x => x.MapId).FirstOrDefault();
                    ////VERSION_1
                    //url = $@"{url}?mapid={mapid}";

                    ////VERSION_2
                    url = $@"{url}?Code={schedule.SchedCode}";
                }
                else if (IsProcOnly == true)
                {
                    url = $@"{url}?Code={schedule.SchedCode}";
                }
            }
            TaskSchedulerHelpers.AddSchedule(schedule.SchedCode, schedule.Frequency, schedule.StartDate.ToString(),
                                      schedule.StartTime.ToString(), schedule.ScheduleType, url,method);
        }

        //public void OPSCreate_Schedule(UploadSchedule schedule, int id)
        //{
        //    schedule.CreateDate = DateTime.Now;
        //    schedule.IsActive = true;
        //    schedule.CreateUserID = id;


        //    string url = "";




        //    var api = _context.Process.Where(x => x.ProcessCode == schedule.Process).Join(_context.APISetups, p => p.APICode, a => a.APICode, (p, a) =>
        //                                     new { API = a.APIURL }).FirstOrDefault();
        //        url = api == null ? _context.APISetups.Where(x => x.APICode == schedule.Api).Select(x => x.APIURL).FirstOrDefault() : api.API;
        //        var apimapcode = _context.Process.Where(x => x.ProcessCode == schedule.Process).Join(_context.FieldMappings, p => p.MapId, f => f.MapId, (p, f) =>
        //                                    new { APICode = f.APICode }).FirstOrDefault();
        //        var IsProcOnly = _context.Process.Where(x => x.ProcessCode == schedule.Process && x.MapId == 0).Any();
        //        var procestosap = _context.Process.Where(x => x.ProcessCode == schedule.Process).Select(x => x.PostSAP).FirstOrDefault();
        //        ////IF FIELD MAPPING IS API THEN SET THE MAPID            
        //        if (apimapcode != null)
        //        {
        //            var mapid = _context.Process.Where(x => x.ProcessCode == schedule.Process).Select(x => x.MapId).FirstOrDefault();
        //            ////VERSION_1
        //            //url = $@"{url}?mapid={mapid}";

        //            ////VERSION_2
        //            url = $@"{url}?Code={schedule.SchedCode}";
        //        }
        //        else if (IsProcOnly == true)
        //        {
        //            url = $@"{url}?Code={schedule.SchedCode}";
        //        }
           
        //    TaskSchedulerHelpers.AddSchedule(schedule.SchedCode, schedule.Frequency, schedule.StartDate.ToString(),
        //                              schedule.StartTime.ToString(), schedule.ScheduleType, url);
        //}
        public SchedCreateViewMdel Find_Schedule(int id, string type)
        {
            var model = new SchedCreateViewMdel();

            model.SchedView = _context.Schedules.Where(x => x.SchedId == id && x.ScheduleType == type).Select(a =>
                        new SchedCreateViewMdel.Schedule
                        {
                            SchedId = a.SchedId,
                            SchedCode = a.SchedCode,
                            Frequency = a.Frequency,
                            ScheduleType = a.ScheduleType,
                            StartDate = a.StartDate.Value,
                            StartTime = a.StartTime.Value,
                            Process = a.Process,
                            IsActive = a.IsActive,
                            Api = a.Api,
                            Credential = a.Credential
                        }).ToList();
            return model;
        }

        public bool Update_Schedule(UploadSchedule schedule, int id)
        {
            var sched = schedule;
            sched.UpdateDate = DateTime.Now;
            sched.CreateDate = DateTime.Now;
            sched.UpdateUserID = id;
            sched.CreateUserID = id;
            var crystalId = _context.CrystalExtractSetup.Where(x => x.Name == schedule.Process).Select(x => x.Id).FirstOrDefault();

            _context.Entry(sched).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();


            string url = "";
            string method = "";
            if (schedule.ScheduleType == "Crystal")
            {
                var api = _context.CrystalExtractSetup.Where(x => x.Name == schedule.Process).Join(_context.APISetups, p => p.APICode, a => a.APICode, (p, a) =>
                                             new { API = a.APIURL,
                                                    method = a.APIMethod}).FirstOrDefault();
                url = api == null ? _context.APISetups.Where(x => x.APICode == schedule.Api).Select(x => x.APIURL).FirstOrDefault() : api.API;

                method = api.method;
                url = $@"{url}?id={crystalId}";
            }
            else
            {
                var api = _context.Process.Where(x => x.ProcessCode == schedule.Process).Join(_context.APISetups, p => p.APICode, a => a.APICode, (p, a) =>
                                        new {
                                            API = a.APIURL,
                                            method = a.APIMethod
                                        }).FirstOrDefault();
                url = api == null ? _context.APISetups.Where(x => x.APICode == sched.Api).Select(x => x.APIURL).FirstOrDefault() : api.API;
                //var met = _context.APISetups.Where(x => x.APICode == sched.Api).Select(x => x.APIMethod).FirstOrDefault();
                method = api == null ? _context.APISetups.Where(x => x.APICode == sched.Api).Select(x => x.APIMethod).FirstOrDefault() : api.method;
                var apimapcode = _context.Process.Where(x => x.ProcessCode == schedule.Process).Join(_context.FieldMappings, p => p.MapId, f => f.MapId, (p, f) =>
                                            new { APICode = f.APICode }).FirstOrDefault();
                var IsProcOnly = _context.Process.Where(x => x.ProcessCode == schedule.Process && x.MapId == 0).Any();
                var procestosap = _context.Process.Where(x => x.ProcessCode == schedule.Process).Select(x => x.PostSAP).FirstOrDefault();
                ////IF FIELD MAPPING IS API THEN SET THE MAPID            
                if (apimapcode != null)
                {
                    ////VERSION_1 : MapId
                    //var mapid = _context.Process.Where(x => x.ProcessCode == schedule.Process).Select(x => x.MapId).FirstOrDefault();
                    //url = $@"{url}?mapid={mapid}";

                    ////VERSION_2 : Taskname
                    url = $@"{url}?Code={schedule.SchedCode}";
                }
                else if (IsProcOnly == true)
                {
                    url = $@"{url}?Code={schedule.SchedCode}";
                }
            }
            if (schedule.IsActive)
            {
                TaskSchedulerHelpers.AddSchedule(schedule.SchedCode, schedule.Frequency,
                                          schedule.StartDate.ToString(), schedule.StartTime.ToString(), schedule.ScheduleType, url,method);
            }
            else
            {
                TaskSchedulerHelpers.UpdateSchedule(schedule.SchedCode);
            }
            return true;
        }

        public bool Run_Schedule(string Code)
        {
            try
            {

                var serverName = ConfigurationManager.AppSettings["ServerName"].ToString();
                var user = ConfigurationManager.AppSettings["User"].ToString();
                var domain = ConfigurationManager.AppSettings["Domain"].ToString();
                var password = ConfigurationManager.AppSettings["Password"].ToString();

                using (TaskService ts = new TaskService(serverName,
                                                        user,
                                                        domain,
                                                        password))
                {
                    Settings setting = new Settings();
                    TaskSchedulerHelpers.RunTask(Code);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool Enable_Schedule(string Code)
        {
            try
            {
                Settings setting = new Settings();
                TaskSchedulerHelpers.EnableTask(Code);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool Disable_Schedule(string Code)
        {
            try
            {
                Settings setting = new Settings();
                TaskSchedulerHelpers.DisableTask(Code);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public SchedCreateViewMdel Validate_Schedule(string code)
        {
            var model = new SchedCreateViewMdel();

            model.SchedView = _context.Schedules.Where(x => x.SchedCode == code).Select((x) => new SchedCreateViewMdel.Schedule
            {
                SchedCode = x.SchedCode,
            }).ToList();
            return model;
        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }
    }
}