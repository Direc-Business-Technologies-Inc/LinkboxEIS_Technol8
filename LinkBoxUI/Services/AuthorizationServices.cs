using LinkBoxUI.Context;
using InfrastructureLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer.ViewModels;
using DomainLayer;

namespace LinkBoxUI.Services
{
    public class AuthorizationServices : IAuthorizationRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        public bool Create_Authorization(Authorization authorization, int id)
        {
            if (_context.Authorizations.Where(e => e.Description == authorization.Description).Any())
            {
                return false;
            }
            else
            {
                authorization.CreateUserID = id;
                authorization.IsActive = true;
                authorization.CreateDate = DateTime.Now;
                _context.Authorizations.Add(authorization);
                _context.SaveChanges();
                SaveChanges();
                return true;
            }
        }
        public Authorizations View_Authorization()
        {
            var auth = new Authorizations();
            auth.AuthorizationsList = _context.Authorizations.Select(sel =>
                    new Authorizations.AuthorizationViewModel
                    {
                        AuthId = sel.AuthId,
                        Description = sel.Description,
                        IsActive = sel.IsActive
                    }).ToList();
            auth.ModulesList = _context.Modules.Where(x => x.IsActive == true).Select(sel =>
                    new Authorizations.Modules
                    {
                        ModName = sel.ModName,
                        ModId = sel.ModId,
                        ModCode = sel.ModuleCode
                    }).ToList();
            return auth;
        }

        public Authorizations Find_Authorization(int id)
        {
            var model = new Authorizations();
            model.AuthorizationsList = _context.Authorizations.Where(x => x.AuthId == id).Select(x => new Authorizations.AuthorizationViewModel
            {
                Description = x.Description,
                IsActive = x.IsActive,
            }).ToList();
            return model;
        }
        public bool Update_Authorization(Authorization authorization, int id)
        {
            var oaut = authorization;

            if (_context.Authorizations.Where(e => e.Description == authorization.Description && e.AuthId != authorization.AuthId).Any())
            {
                return false;
            }
            else
            {
                oaut.UpdateDate = DateTime.Now;
                oaut.UpdateUserID = id;
                _context.Entry(oaut).State = System.Data.Entity.EntityState.Modified;
                _context.SaveChanges();
                SaveChanges();
                return true;
            }

        }

        public Authorizations Get_AuthorizationModules(int id)
        {
            var model = new Authorizations();

            model.AuthorizationModulesList = _context.AuthorizationModules.Where(a => a.AuthId == id)
                                                     .Join(_context.Authorizations, a => a.AuthId, b => b.AuthId, (a, b) => new { a, b })
                                                     .Join(_context.Modules.Where(c => c.IsActive == true), ab => ab.a.ModId,
                                                      c => c.ModId, (ab, c) => new Authorizations.AuthorizationModules
                                                      {
                                                          AMId = ab.a.Id,
                                                          AuthName = ab.b.Description,
                                                          AuthId = ab.b.AuthId,
                                                          ModId = c.ModId,
                                                          ModuleName = c.ModName,
                                                          ModuleCode = c.ModuleCode,
                                                          IsActive = ab.a.IsActive
                                                      }).ToList();
            model.ModulesList = _context.Modules.Where(x => x.IsActive == true).Select(sel => new Authorizations.Modules
            {
                ModName = sel.ModName,
                ModId = sel.ModId,
                ModCode = sel.ModuleCode
            }).ToList();
            return model;
        }

        public void Update_AuthorizationModules(int userid, int moduleid, bool isactive)
        {
            var modules = _context.AuthorizationModules.Find(moduleid);
            modules.IsActive = isactive;
            modules.UpdateDate = DateTime.Now;
            modules.UpdateUserID = userid;
            _context.Entry(modules).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
        }
        public bool isActiveModule(int id)
        {
            var modules = _context.AuthorizationModules.Find(id);
            return modules.IsActive;
        }
        public int GetModuleId(int id)
        {
            var modules = _context.AuthorizationModules.Find(id);
            return modules.ModId;
        }
        public List<int> NewModuleList(List<int> olddata, List<int> newdata)
        {
            foreach (var x in olddata)
            {
                newdata.Remove(x);
            }
            return newdata;
        }
        public void Add_AuthorizationModules(int createid, int authid, int moduleid)
        {
            var module = new AuthorizationModule();
            module.AuthId = authid;
            module.ModId = moduleid;
            module.IsActive = true;
            module.CreateDate = DateTime.Now;
            module.CreateUserID = createid;
            _context.AuthorizationModules.Add(module);
            _context.SaveChanges();
        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }
    }
}